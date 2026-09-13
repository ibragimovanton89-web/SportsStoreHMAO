using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Application.Pricing;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Pricing;
using SportsStore.Infrastructure.Import;
namespace SportsStore.Infrastructure.Admin;

/// <summary>Приёмка закупки, создание карточки из выбранного товара и цена от фактического закупочного уровня.</summary>
public sealed partial class Purchasing
{
    /// <summary>Увеличивает доступный физический остаток варианта, сохраняя существующий резерв.</summary>
    private static async Task AddInventoryAsync(ApplicationDbContext db, Guid variantId, Guid warehouseId, decimal quantity, CancellationToken ct)
    {
        var balance = db.InventoryBalances.Local.SingleOrDefault(x => x.ProductVariantId == variantId && x.WarehouseId == warehouseId)
            ?? await db.InventoryBalances.SingleOrDefaultAsync(x => x.ProductVariantId == variantId && x.WarehouseId == warehouseId, ct);
        if (balance is null) { balance = new() { ProductVariantId = variantId, WarehouseId = warehouseId }; db.InventoryBalances.Add(balance); }
        balance.OnHand += quantity;
    }
    /// <summary>Принимает только дополнительное количество; аудит и склад фиксируются в одной транзакции.</summary>
    public Task ReceiveAsync(Guid id, uint version, IReadOnlyList<ReceivePosition>? positions, Guid operationId, CancellationToken ct = default) =>
        ChangeAsync(id, version, operationId, true, "purchase.receive", async (db, order, _) =>
        {
            if (order.Status is not (PurchaseStatus.Submitted or PurchaseStatus.InTransit or PurchaseStatus.PartiallyReceived))
                throw new InvalidOperationException("Принимать можно переданную поставщику, находящуюся в пути или частично принятую закупку.");
            var lines = await db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == id).OrderBy(x => x.Id).ToListAsync(ct);
            if (positions is not null && (positions.Count is < 1 or > 1000 || positions.Select(x => x.LineId).Distinct().Count() != positions.Count || positions.Any(x => x.Quantity <= 0 || !lines.Any(l => l.Id == x.LineId))))
                throw new ArgumentException("Приёмка должна содержать разные строки этого заказа и положительное количество.");
            var receivedAny = false;
            foreach (var line in lines)
            {
                var quantity = positions is null ? line.Quantity - line.ReceivedQuantity : positions.FirstOrDefault(x => x.LineId == line.Id)?.Quantity ?? 0;
                if (quantity == 0) continue;
                if (quantity > line.Quantity - line.ReceivedQuantity) throw new ArgumentException("Нельзя принять больше заказанного остатка.");
                var offer = await db.SupplierOffers.SingleAsync(x => x.Id == line.SupplierOfferId, ct);
                if (line.SupplierUnit != offer.SupplierUnit) throw new InvalidOperationException("Единица поставщика изменилась после создания заказа. Приёмка заблокирована до сверки данных.");
                var receipt = new PurchaseReceiptLine { PurchaseOrderLineId = line.Id, WarehouseId = order.WarehouseId, OperationId = operationId, Quantity = quantity };
                if (offer.ProductVariantId is Guid variant && offer.ConversionConfirmed && offer.SaleUnitsPerSupplierUnit is > 0)
                {
                    receipt.ProductVariantId = variant; receipt.Conversion = offer.SaleUnitsPerSupplierUnit;
                    await AddInventoryAsync(db, variant, order.WarehouseId, quantity * receipt.Conversion.Value, ct);
                }
                else
                {
                    var stock = await db.UnallocatedStocks.SingleOrDefaultAsync(x => x.SupplierOfferId == offer.Id && x.WarehouseId == order.WarehouseId, ct);
                    if (stock is null) { stock = new() { SupplierOfferId = offer.Id, WarehouseId = order.WarehouseId, SupplierUnit = line.SupplierUnit }; db.UnallocatedStocks.Add(stock); }
                    if (stock.SupplierUnit != line.SupplierUnit) throw new InvalidOperationException("Единица прежнего нераспределённого остатка отличается. Сначала нужна сверка склада.");
                    stock.Quantity += quantity;
                }
                db.PurchaseReceiptLines.Add(receipt); line.ReceivedQuantity += quantity; receivedAny = true;
            }
            if (!receivedAny) throw new InvalidOperationException("Нет количества для приёмки.");
            order.Status = lines.All(x => x.ReceivedQuantity == x.Quantity) ? PurchaseStatus.Received : PurchaseStatus.PartiallyReceived;
        }, ct);
    /// <summary>Создаёт карточку в единице поставщика только из созданной закупки или фактически принятого остатка.</summary>
    /// <param name="offerId">Выбранная позиция поставщика.</param>
    /// <param name="name">Собственное название новой карточки.</param>
    /// <param name="brandId">Существующий бренд; имеет приоритет над введённым названием.</param>
    /// <param name="categoryId">Существующая категория; имеет приоритет над введённым названием.</param>
    /// <param name="operationId">Идентификатор идемпотентной команды.</param>
    /// <param name="ct">Отмена транзакции.</param>
    /// <param name="brandName">Название нового или существующего бренда; сохраняется только вместе с новой карточкой.</param>
    /// <param name="categoryName">Название категории; новая категория создаётся без родителя.</param>
    /// <returns>Идентификатор созданной либо ранее связанной карточки.</returns>
    public async Task<Guid> CreateCardAsync(Guid offerId, string name, Guid? brandId, Guid? categoryId, Guid operationId, CancellationToken ct = default, string? brandName = null, string? categoryName = null)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, false, ct); await LockAsync(db, ct);
        var prior = await AdminCatalog.PriorAsync(db, actor, operationId, ct); if (prior is not null) return Guid.Parse(prior);
        var offer = await db.SupplierOffers.SingleAsync(x => x.Id == offerId, ct);
        var supplier = await db.Suppliers.SingleAsync(x => x.Id == offer.SupplierId, ct); await PriceImportService.LockAsync(db, supplier.Code, ct);
        var selected = await (from l in db.PurchaseOrderLines join o in db.PurchaseOrders on l.PurchaseOrderId equals o.Id
            where l.SupplierOfferId == offerId && o.Status != PurchaseStatus.Draft && o.Status != PurchaseStatus.Cancelled select l).AnyAsync(ct);
        if (!selected && !await db.UnallocatedStocks.AnyAsync(x => x.SupplierOfferId == offerId && x.Quantity > 0, ct))
            throw new InvalidOperationException("Сначала выберите позицию и создайте закупочный заказ.");
        ProductVariant variant; Product product;
        if (offer.ProductVariantId is Guid linked)
        {
            variant = await db.ProductVariants.SingleAsync(x => x.Id == linked, ct); product = await db.Products.SingleAsync(x => x.Id == variant.ProductId, ct);
            await access.RequireAsync(db, product.Status != ProductStatus.Draft, ct);
            if (!offer.ConversionConfirmed || offer.SaleUnitsPerSupplierUnit is null or <= 0) throw new InvalidOperationException("В исторических данных варианта не подтверждены единицы. Создание карточки заблокировано до технической проверки данных.");
        }
        else
        {
            var lookups = await AdminCatalog.ResolveLookupsAsync(db, brandId, categoryId, brandName, categoryName, ct);
            product = new(); await AdminCatalog.FillProductAsync(db, product, name, null, lookups.Brand, lookups.Category, ct); db.Products.Add(product);
            variant = new() { ProductId = product.Id, PricingSupplierOfferId = offer.Id };
            var sku = "SS-" + offer.ExternalCode;
            if (await db.ProductVariants.AnyAsync(x => x.Sku == sku, ct)) sku += "-" + variant.Id.ToString("N")[..6];
            await AdminCatalog.FillVariantAsync(db, variant, new(variant.Id, 0, product.Id, sku, offer.SupplierUnit, null, null, null, null), ct); db.ProductVariants.Add(variant);
            // Продаём ту же целую единицу: одна упаковка остаётся упаковкой; содержимое не делим на штуки.
            offer.ProductVariantId = variant.Id; offer.SaleUnitsPerSupplierUnit = 1; offer.ConversionConfirmed = true; supplier.Revision++;
        }
        foreach (var stock in await db.UnallocatedStocks.Where(x => x.SupplierOfferId == offerId && x.Quantity > 0).ToListAsync(ct))
        {
            if (stock.SupplierUnit != offer.SupplierUnit) throw new InvalidOperationException("Единица принятого товара отличается от текущего прайса. Автоматический перенос заблокирован до сверки склада.");
            await AddInventoryAsync(db, variant.Id, stock.WarehouseId, stock.Quantity * offer.SaleUnitsPerSupplierUnit!.Value, ct); stock.Quantity = 0;
        }
        // Запоминаем назначение ранее принятого товара: последующая смена связи не должна менять смысл склада.
        var pendingReceipts = await (from r in db.PurchaseReceiptLines join l in db.PurchaseOrderLines on r.PurchaseOrderLineId equals l.Id
            where l.SupplierOfferId == offerId && r.ProductVariantId == null select r).ToListAsync(ct);
        foreach (var receipt in pendingReceipts) { receipt.ProductVariantId = variant.Id; receipt.Conversion = offer.SaleUnitsPerSupplierUnit; }
        AdminAccess.Audit(db, actor, operationId, "purchase.card", "Product", product.Id.ToString(), "Для выбранной закупки создана или открыта карточка; принятый нераспределённый остаток зачислен варианту один раз.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return product.Id;
    }
    /// <summary>Возвращает базовый оптовый порог и существующие ступени, чтобы все действующие цены были доступны в карточке.</summary>
    /// <param name="variantId">Вариант собственного каталога.</param>
    /// <param name="ct">Отмена чтения.</param>
    public async Task<IReadOnlyList<decimal>> WholesaleQuantitiesAsync(Guid variantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await access.RequireAsync(db, false, ct);
        var quantities = await db.SalePrices.AsNoTracking().Where(x => x.ProductVariantId == variantId && x.Segment == CustomerSegment.Wholesale).Select(x => x.MinimumQuantity).ToListAsync(ct);
        return quantities.Append(1m).Distinct().Order().ToArray();
    }
    /// <summary>Читает актуальную розничную цену и до 50 исторических закупочных цен, переведённых в единицу продажи.</summary>
    public Task<PurchaseRetail> RetailAsync(Guid variantId, CancellationToken ct = default) => SalePriceAsync(variantId, CustomerSegment.Retail, ct);

    /// <summary>Читает базовую цену выбранного формата продажи и подтверждённые закупочные снимки.</summary>
    /// <param name="variantId">Вариант собственного каталога.</param>
    /// <param name="segment">Розница или опт; проверяется сервером.</param>
    /// <param name="ct">Отмена чтения.</param>
    /// <param name="minimumQuantity">Количество единиц SKU, от которого действует эта цена.</param>
    public async Task<PurchaseRetail> SalePriceAsync(Guid variantId, CustomerSegment segment, CancellationToken ct = default, decimal minimumQuantity = 1)
    {
        if (minimumQuantity < 1 || minimumQuantity > 1000000 || decimal.Round(minimumQuantity, 4) != minimumQuantity) throw new ArgumentException("Неверный порог количества.");
        if (!Enum.IsDefined(segment)) throw new ArgumentException("Неизвестный формат продажи.");
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var sale = await db.SalePrices.AsNoTracking().SingleOrDefaultAsync(x => x.ProductVariantId == variantId && x.Segment == segment && x.MinimumQuantity == minimumQuantity, ct);
        var costs = await (from l in db.PurchaseOrderLines.AsNoTracking() join o in db.PurchaseOrders on l.PurchaseOrderId equals o.Id join offer in db.SupplierOffers on l.SupplierOfferId equals offer.Id
            where offer.ProductVariantId == variantId && offer.ConversionConfirmed && offer.SaleUnitsPerSupplierUnit > 0 && l.UnitPrice > 0
                && o.Status != PurchaseStatus.Draft && o.Status != PurchaseStatus.Cancelled && l.SupplierUnit == offer.SupplierUnit
            orderby o.CreatedAt descending, l.Id
            select new PurchaseCost(l.Id, o.Number, o.TierName, l.UnitPrice!.Value / offer.SaleUnitsPerSupplierUnit!.Value)).Take(50).ToListAsync(ct);
        return new(variantId, sale?.Version ?? 0, sale?.Amount, costs);
    }
    /// <summary>Явно применяет ручную цену либо процент от выбранного снимка закупки; импорт не меняет эту цену.</summary>
    public Task SetRetailAsync(Guid variantId, uint priceVersion, Guid? costLineId, decimal? fixedPrice, decimal? markupPercent, Guid operationId, CancellationToken ct = default) =>
        SetSalePriceAsync(variantId, CustomerSegment.Retail, priceVersion, costLineId, fixedPrice, markupPercent, operationId, ct);

    /// <summary>Сохраняет базовую розничную либо оптовую цену независимо от другой цены; сохраняет историю и защиту от импорта.</summary>
    /// <param name="variantId">Вариант собственного каталога.</param>
    /// <param name="segment">Формат продажи, не закупочный тариф поставщика.</param>
    /// <param name="priceVersion">Ожидаемая версия цены выбранного формата.</param>
    /// <param name="costLineId">Подтверждённая закупочная строка для расчёта наценки.</param>
    /// <param name="fixedPrice">Фиксированная цена в рублях либо null для расчёта.</param>
    /// <param name="markupPercent">Наценка к закупке либо null для ручной цены.</param>
    /// <param name="operationId">Идентификатор идемпотентной операции.</param>
    /// <param name="ct">Отмена транзакции.</param>
    /// <param name="minimumQuantity">Количество единиц SKU для этой ценовой ступени.</param>
    public async Task SetSalePriceAsync(Guid variantId, CustomerSegment segment, uint priceVersion, Guid? costLineId, decimal? fixedPrice, decimal? markupPercent, Guid operationId, CancellationToken ct = default, decimal minimumQuantity = 1)
    {
        if (minimumQuantity < 1 || minimumQuantity > 1000000 || decimal.Round(minimumQuantity, 4) != minimumQuantity) throw new ArgumentException("Неверный порог количества.");
        if (!Enum.IsDefined(segment)) throw new ArgumentException("Неизвестный формат продажи.");
        if ((fixedPrice.HasValue == markupPercent.HasValue) || fixedPrice is <= 0 or > 99999999999999m || markupPercent is < 0 or > 1000000m
            || fixedPrice.HasValue && decimal.Round(fixedPrice.Value, 2) != fixedPrice || markupPercent.HasValue && decimal.Round(markupPercent.Value, 4) != markupPercent)
            throw new ArgumentException("Выберите один способ: положительная фиксированная цена либо неотрицательный процент наценки.");
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, true, ct); await LockAsync(db, ct); if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) return;
        if (!await db.ProductVariants.AnyAsync(x => x.Id == variantId, ct)) throw new ArgumentException("Вариант не найден.");
        var sale = await db.SalePrices.SingleOrDefaultAsync(x => x.ProductVariantId == variantId && x.Segment == segment && x.MinimumQuantity == minimumQuantity, ct);
        if ((sale?.Version ?? 0) != priceVersion) throw new DbUpdateConcurrencyException("Цена уже изменена. Обновите сведения.");
        var amount = fixedPrice ?? 0; var source = "Ручная цена владельца";
        if (markupPercent is decimal markup)
        {
            var data = await (from l in db.PurchaseOrderLines join o in db.PurchaseOrders on l.PurchaseOrderId equals o.Id join offer in db.SupplierOffers on l.SupplierOfferId equals offer.Id
                where l.Id == costLineId && offer.ProductVariantId == variantId && o.Status != PurchaseStatus.Draft && o.Status != PurchaseStatus.Cancelled
                select new { l.UnitPrice, l.SupplierUnit, CurrentUnit = offer.SupplierUnit, offer.ConversionConfirmed, offer.SaleUnitsPerSupplierUnit, o.Number, o.TierName }).SingleOrDefaultAsync(ct)
                ?? throw new ArgumentException("Выберите закупочную строку этого варианта.");
            if (data.UnitPrice is null or <= 0 || data.SupplierUnit != data.CurrentUnit || !data.ConversionConfirmed || data.SaleUnitsPerSupplierUnit is null or <= 0)
                throw new InvalidOperationException("Закупочная цена или перевод единиц не подтверждены.");
            amount = PriceCalculator.Calculate(data.UnitPrice.Value, data.SaleUnitsPerSupplierUnit, true, markup);
            source = JsonSerializer.Serialize(new { Purchase = data.UnitPrice.Value, Conversion = data.SaleUnitsPerSupplierUnit.Value, Markup = markup, Order = data.Number, Tier = data.TierName, PurchaseLineId = costLineId });
        }
        // Оба режима — явное решение владельца; новый прайс не должен переоценивать остаток от другой закупки.
        await PricingService.PublishAsync(db, sale, variantId, segment, minimumQuantity, amount, true, source, ct);
        AdminAccess.Audit(db, actor, operationId, segment == CustomerSegment.Retail ? "purchase.retail" : "purchase.wholesale", "ProductVariant", variantId.ToString(),  $"Формат: {segment}. " + (markupPercent.HasValue ? "Применена цена от выбранной закупки и процента владельца." : "Применена фиксированная цена владельца."));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
