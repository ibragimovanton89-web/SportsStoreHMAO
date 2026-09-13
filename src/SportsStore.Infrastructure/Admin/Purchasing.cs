using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Import;
namespace SportsStore.Infrastructure.Admin;

/// <summary>Закупки поставщику с историческими ценами, независимым складом и атомарным аудитом.</summary>
/// <param name="factory">Фабрика коротких контекстов.</param><param name="access">Текущие права сотрудника.</param>
public sealed partial class Purchasing(IDbContextFactory<ApplicationDbContext> factory, AdminAccess access) : IPurchasing
{
    /// <summary>Сериализует закупочные изменения и приёмку до чтения изменяемого состояния.</summary>
    private static Task LockAsync(ApplicationDbContext db, CancellationToken ct) => db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861100)", ct);
    /// <summary>Выбирает снимок цены строго по выбранному коду; неизвестное значение не превращается в ноль.</summary>
    private static decimal? SelectedPrice(PurchaseOrderLine line, string code) => code switch { "small" => line.SmallPrice, "wholesale" => line.WholesalePrice, "large" => line.LargePrice, _ => null };
    /// <summary>Допускает только поддержанные три рублёвых тарифа.</summary>
    private static void ValidateTier(SupplierPriceTier tier) { if (tier.Currency != "RUB" || tier.Code is not ("small" or "wholesale" or "large")) throw new ArgumentException("Нужен один из трёх закупочных тарифов в рублях."); }
    /// <summary>Переносит условия тарифа в исторический снимок закупки.</summary>
    private static void SetTier(PurchaseOrder order, SupplierPriceTier tier) { ValidateTier(tier); order.SupplierPriceTierId = tier.Id; order.TierCode = tier.Code; order.TierName = tier.Name; order.MinimumAmount = tier.ManualMinimumAmount ?? tier.MinimumAmount; }
    /// <summary>Проверяет право редактировать состав до передачи; Manager редактирует только свои подборки.</summary>
    private async Task EditableAsync(ApplicationDbContext db, PurchaseOrder order, AdminSession actor, CancellationToken ct)
    {
        if (order.Status is not (PurchaseStatus.Draft or PurchaseStatus.Created)) throw new InvalidOperationException("Состав переданного заказа закрыт для редактирования. До приёмки Admin может вернуть его в статус «Создан».");
        if (order.CreatedBy != actor.UserId) await access.RequireAsync(db, true, ct);
    }
    /// <summary>Начинает одну сохраняемую в БД подборку сотрудника на поставщика; повтор возвращает её.</summary>
    public async Task<Guid> StartAsync(Guid supplierId, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, false, ct); await LockAsync(db, ct);
        var prior = await AdminCatalog.PriorAsync(db, actor, operationId, ct); if (prior is not null) return Guid.Parse(prior);
        var existing = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.SupplierId == supplierId && x.CreatedBy == actor.UserId && x.Status == PurchaseStatus.Draft, ct);
        if (existing is not null)
        {
            AdminAccess.Audit(db, actor, operationId, "purchase.start", "PurchaseOrder", existing.Id.ToString(), "Открыта сохранённая подборка позиций.");
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return existing.Id;
        }
        var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == supplierId, ct) ?? throw new ArgumentException("Поставщик не найден. Сначала загрузите прайс.");
        await PriceImportService.LockAsync(db, supplier.Code, ct);
        var tier = await db.SupplierPriceTiers.SingleOrDefaultAsync(x => x.SupplierId == supplierId && x.Code == "small", ct) ?? throw new ArgumentException("В прайсе нет тарифа мелкого опта.");
        var warehouse = await db.Warehouses.OrderBy(x => x.Name).FirstOrDefaultAsync(ct);
        if (warehouse is null) { warehouse = new() { Name = "Основной склад" }; db.Warehouses.Add(warehouse); }
        var order = new PurchaseOrder { SupplierId = supplierId, WarehouseId = warehouse.Id, CreatedBy = actor.UserId };
        order.Number = $"ЗК-{DateTime.UtcNow:yyyyMMdd}-{order.Id.ToString("N")[..8].ToUpperInvariant()}"; SetTier(order, tier); db.PurchaseOrders.Add(order);
        AdminAccess.Audit(db, actor, operationId, "purchase.start", "PurchaseOrder", order.Id.ToString(), "Начат подбор позиций закупки.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return order.Id;
    }
    /// <summary>Выполняет изменение заказа под блокировками, с ожидаемой версией и атомарным аудитом.</summary>
    private async Task ChangeAsync(Guid id, uint version, Guid operationId, bool admin, string action, Func<ApplicationDbContext, PurchaseOrder, AdminSession, Task> change, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, admin, ct); await LockAsync(db, ct);
        if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) return;
        var order = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ArgumentException("Заказ не найден.");
        var code = await db.Suppliers.Where(x => x.Id == order.SupplierId).Select(x => x.Code).SingleAsync(ct); await PriceImportService.LockAsync(db, code, ct);
        AdminAccess.Version(order, version); await change(db, order, actor); order.UpdatedAt = DateTime.UtcNow;
        AdminAccess.Audit(db, actor, operationId, action, "PurchaseOrder", id.ToString(), $"Закупка {order.Number}: {ActionLabel(action)}.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    /// <summary>Безопасное описание закупочного действия для журнала.</summary>
    private static string ActionLabel(string action) => action switch { "purchase.quantity" => "изменён состав", "purchase.configure" => "выбраны тариф и склад", "purchase.receive" => "принято количество на склад", _ => "изменён этап закупки" };
    /// <summary>Меняет количество единиц поставщика; ноль удаляет ещё не переданную строку.</summary>
    public Task SetQuantityAsync(Guid id, uint version, Guid offerId, int quantity, Guid operationId, CancellationToken ct = default) =>
        ChangeAsync(id, version, operationId, false, "purchase.quantity", async (db, order, actor) =>
        {
            await EditableAsync(db, order, actor, ct); if (quantity is < 0 or > 1000000) throw new ArgumentException("Количество должно быть от 0 до 1 000 000.");
            var line = await db.PurchaseOrderLines.SingleOrDefaultAsync(x => x.PurchaseOrderId == id && x.SupplierOfferId == offerId, ct);
            if (quantity == 0) { if (line is not null) db.PurchaseOrderLines.Remove(line); return; }
            var offer = await db.SupplierOffers.SingleOrDefaultAsync(x => x.Id == offerId && x.SupplierId == order.SupplierId, ct) ?? throw new ArgumentException("Позиция принадлежит другому поставщику.");
            // Уменьшение разрешено даже после снижения остатка в новом прайсе; увеличение сверх известного остатка запрещено.
            if (quantity > (line?.Quantity ?? 0) && offer.Stock is decimal stock && quantity > stock)
                throw new InvalidOperationException($"У поставщика доступно {stock:0.####} {offer.SupplierUnit}. Увеличить количество сверх остатка нельзя.");
            if (line is null)
            {
                if (await db.PurchaseOrderLines.CountAsync(x => x.PurchaseOrderId == id, ct) >= 1000) throw new ArgumentException("Один заказ ограничен 1000 выбранными позициями.");
                var prices = await (from p in db.SupplierOfferPrices join t in db.SupplierPriceTiers on p.SupplierPriceTierId equals t.Id where p.SupplierOfferId == offerId select new { t.Code, p.Amount }).ToDictionaryAsync(x => x.Code, x => (decimal?)x.Amount, ct);
                line = new() { PurchaseOrderId = id, SupplierOfferId = offerId, ExternalCode = offer.ExternalCode, Name = offer.SourceName, SupplierUnit = offer.SupplierUnit, UnitsPerBox = offer.UnitsPerBox,
                    SmallPrice = prices.GetValueOrDefault("small"), WholesalePrice = prices.GetValueOrDefault("wholesale"), LargePrice = prices.GetValueOrDefault("large") };
                db.PurchaseOrderLines.Add(line);
            }
            line.Quantity = quantity; line.UnitPrice = SelectedPrice(line, order.TierCode);
        }, ct);
    /// <summary>Меняет тариф по сохранённым ценовым снимкам строк; новый прайс их не переписывает.</summary>
    public Task ConfigureAsync(Guid id, uint version, Guid tierId, Guid warehouseId, string? note, Guid operationId, CancellationToken ct = default) =>
        ChangeAsync(id, version, operationId, false, "purchase.configure", async (db, order, actor) =>
        {
            await EditableAsync(db, order, actor, ct);
            var tier = await db.SupplierPriceTiers.SingleOrDefaultAsync(x => x.Id == tierId && x.SupplierId == order.SupplierId, ct) ?? throw new ArgumentException("Тариф другого поставщика недопустим.");
            if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId, ct)) throw new ArgumentException("Склад не найден.");
            SetTier(order, tier); order.WarehouseId = warehouseId; order.Note = AdminAccess.Optional(note, 1000);
            foreach (var line in await db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == id).ToListAsync(ct)) line.UnitPrice = SelectedPrice(line, order.TierCode);
        }, ct);
    /// <summary>Фиксирует допустимый переход; отправка внешнему поставщику здесь не выполняется.</summary>
    public Task ChangeStatusAsync(Guid id, uint version, PurchaseStatus status, Guid operationId, CancellationToken ct = default) =>
        ChangeAsync(id, version, operationId, status != PurchaseStatus.Created, "purchase.status", async (db, order, actor) =>
        {
            var received = await db.PurchaseOrderLines.AnyAsync(x => x.PurchaseOrderId == id && x.ReceivedQuantity > 0, ct);
            var allowed = (order.Status, status) switch
            {
                (PurchaseStatus.Draft, PurchaseStatus.Created) => true,
                (PurchaseStatus.Created, PurchaseStatus.Submitted) => true,
                (PurchaseStatus.Submitted, PurchaseStatus.InTransit) => true,
                (PurchaseStatus.Submitted or PurchaseStatus.InTransit, PurchaseStatus.Created) => !received,
                (PurchaseStatus.Draft or PurchaseStatus.Created or PurchaseStatus.Submitted or PurchaseStatus.InTransit, PurchaseStatus.Cancelled) => !received,
                _ => false
            };
            if (!allowed) throw new InvalidOperationException("Этот переход статуса недопустим. Принятый товар нельзя отменить повторной сменой статуса.");
            if (order.Status == PurchaseStatus.Draft) await EditableAsync(db, order, actor, ct); else await access.RequireAsync(db, true, ct);
            var lines = await db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == id).ToListAsync(ct);
            if (status is PurchaseStatus.Created or PurchaseStatus.Submitted)
            {
                if (lines.Count == 0 || lines.Any(x => x.UnitPrice is null or <= 0)) throw new InvalidOperationException("Выберите позиции с известной положительной ценой текущего тарифа.");
                if ((status == PurchaseStatus.Submitted || order.Status == PurchaseStatus.Draft) && await (from line in db.PurchaseOrderLines join offer in db.SupplierOffers on line.SupplierOfferId equals offer.Id
                    where line.PurchaseOrderId == id && offer.Stock != null && line.Quantity > offer.Stock select line.Id).AnyAsync(ct))
                    throw new InvalidOperationException("Остаток поставщика изменился: в заказе есть превышение. Обновите сведения и уменьшите количество.");
                if (status == PurchaseStatus.Submitted && (order.MinimumAmount is null || lines.Sum(x => x.Quantity * x.UnitPrice!.Value) < order.MinimumAmount))
                    throw new InvalidOperationException("До передачи поставщику укажите порог тарифа и доберите необходимую сумму либо явно исправьте условия тарифа и сохраните выбор заказа.");
            }
            order.Status = status;
        }, ct);
    /// <summary>Сохраняет ручные пороги отдельно от исходного прайса; действующие заказы сохраняют свои снимки.</summary>
    public async Task SaveTiersAsync(Guid supplierId, IReadOnlyList<PurchaseTier> tiers, Guid operationId, CancellationToken ct = default)
    {
        if (tiers.Count != 3 || tiers.Select(x => x.Id).Distinct().Count() != 3) throw new ArgumentException("Передайте три разных тарифа.");
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, true, ct); await LockAsync(db, ct); if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) return;
        var code = await db.Suppliers.Where(x => x.Id == supplierId).Select(x => x.Code).SingleAsync(ct); await PriceImportService.LockAsync(db, code, ct);
        foreach (var input in tiers)
        {
            if (input.ManualMinimum is < 0 or > 99999999999999m || input.ManualMinimum.HasValue && decimal.Round(input.ManualMinimum.Value, 2) != input.ManualMinimum) throw new ArgumentException("Порог должен быть неотрицательной суммой с точностью до копеек.");
            var tier = await db.SupplierPriceTiers.SingleOrDefaultAsync(x => x.Id == input.Id && x.SupplierId == supplierId, ct) ?? throw new ArgumentException("Тариф не принадлежит поставщику.");
            ValidateTier(tier); AdminAccess.Version(tier, input.Version); tier.ManualMinimumAmount = input.ManualMinimum;
        }
        AdminAccess.Audit(db, actor, operationId, "purchase.thresholds", "Supplier", supplierId.ToString(), "Сохранены ручные пороги трёх закупочных тарифов; снимки заказов не изменены.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
