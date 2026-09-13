using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Application.Pricing;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Pricing;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Служебные команды цен и чтения поставщиков для Web и CLI; права, версии и аудит проверяются сервером.</summary>
/// <param name="factory">Фабрика отдельных контекстов.</param><param name="access">Проверка сессии.</param>
public sealed class AdminCommerce(IDbContextFactory<ApplicationDbContext> factory, AdminAccess access) : IAdminCommerce
{
    /// <summary>Возвращает поставщиков для выбора тарифа.</summary>
    public async Task<IReadOnlyList<SupplierData>> SuppliersAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).Take(100).Select(x => new SupplierData(x.Id, x.Version, x.Name, x.SelectedPriceTierId, x.PriceTierConfirmed)).ToListAsync(ct);
    }
    /// <summary>Выбирает уже сопоставленное предложение; не разрешает Manager менять опубликованный товар.</summary>
    public async Task SetSourceAsync(Guid variantId, uint version, Guid offerId, Guid operationId, CancellationToken ct = default)
    {
        await ChangeAsync(false, operationId, "variant.source", "ProductVariant", variantId, async db =>
        {
            var variant = await db.ProductVariants.SingleAsync(x => x.Id == variantId, ct); AdminAccess.Version(variant, version);
            var status = await db.Products.Where(x => x.Id == variant.ProductId).Select(x => x.Status).SingleAsync(ct);
            await access.RequireAsync(db, status != ProductStatus.Draft, ct);
            if (!await db.SupplierOffers.AnyAsync(x => x.Id == offerId && x.ProductVariantId == variantId, ct)) throw new ArgumentException("Сначала сопоставьте предложение с этим вариантом.");
            variant.PricingSupplierOfferId = offerId;
        }, ct);
    }
    /// <summary>Возвращает ограниченную страницу правил.</summary>
    public async Task<AdminPage<MarkupData>> RulesAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var q = db.MarkupRules.AsNoTracking().AsQueryable();
        if (Enum.TryParse<CustomerSegment>(query.Filter, out var segment)) q = q.Where(x => x.Segment == segment);
        var count = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        return new(await q.OrderBy(x => x.Segment).ThenBy(x => x.MinimumQuantity).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25)
            .Select(x => new MarkupData(x.Id, x.Version, x.Segment, x.CategoryId, x.ProductVariantId, x.MinimumQuantity, x.MarkupPercent)).ToListAsync(ct), count, page);
    }
    /// <summary>Сохраняет правило по умолчанию, категории или варианту, исключая неоднозначную область.</summary>
    public async Task<Guid> SaveRuleAsync(MarkupData rule, Guid operationId, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(rule.Segment) || rule.Percent < 0 || rule.Percent > 99999999999999m || decimal.Round(rule.Percent, 4) != rule.Percent
            || rule.Minimum < 1 || decimal.Round(rule.Minimum, 4) != rule.Minimum || rule.Segment == CustomerSegment.Retail && rule.Minimum != 1
            || rule.CategoryId is not null && rule.VariantId is not null) throw new ArgumentException("Проверьте сегмент, наценку, ступень и единственную область правила.");
        var id = rule.Id == Guid.Empty ? Guid.NewGuid() : rule.Id;
        return await ChangeAsync(true, operationId, "markup.save", "MarkupRule", id, async db =>
        {
            if (rule.CategoryId is not null && !await db.Categories.AnyAsync(x => x.Id == rule.CategoryId, ct)) throw new ArgumentException("Категория не найдена.");
            if (rule.VariantId is not null && !await db.ProductVariants.AnyAsync(x => x.Id == rule.VariantId, ct)) throw new ArgumentException("Вариант не найден.");
            var r = rule.Id == Guid.Empty ? new MarkupRule { Id = id } : await db.MarkupRules.SingleAsync(x => x.Id == id, ct);
            if (rule.Id != Guid.Empty) AdminAccess.Version(r, rule.Version); else db.MarkupRules.Add(r);
            r.Segment = rule.Segment; r.CategoryId = rule.CategoryId; r.ProductVariantId = rule.VariantId; r.MinimumQuantity = rule.Minimum; r.MarkupPercent = rule.Percent;
        }, ct);
    }
    /// <summary>Проверяет причины отсутствия расчёта и вызывает существующую реализацию в общей транзакции.</summary>
    public async Task<ProposalResult> ProposeAsync(Guid offerId, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var actor = await access.RequireAsync(db, false, ct); var admin = await access.IsAdministratorAsync(db, ct);
        if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null)
            return new(await db.PriceProposals.Where(x => x.SupplierOfferId == offerId && x.Status == ProposalStatus.Pending).Select(x => x.Id).ToListAsync(ct), ["Эта операция уже выполнена; показаны сохранённые предложения."]);
        var offer = await db.SupplierOffers.SingleAsync(x => x.Id == offerId, ct); var problems = new List<string>();
        if (offer.ProductVariantId is null) problems.Add("Предложение не сопоставлено с вариантом.");
        if (!offer.ConversionConfirmed || offer.SaleUnitsPerSupplierUnit is null or <= 0) problems.Add("Не подтверждён перевод единиц.");
        var supplier = await db.Suppliers.SingleAsync(x => x.Id == offer.SupplierId, ct);
        if (!supplier.PriceTierConfirmed || supplier.SelectedPriceTierId is null) problems.Add("Не выбран или не подтверждён закупочный тариф.");
        else if (!await db.SupplierOfferPrices.AnyAsync(x => x.SupplierOfferId == offerId && x.SupplierPriceTierId == supplier.SelectedPriceTierId, ct)) problems.Add("Нет закупочной цены выбранного тарифа.");
        if (offer.ProductVariantId is Guid variantId)
        {
            var variant = await db.ProductVariants.SingleAsync(x => x.Id == variantId, ct);
            if (variant.PricingSupplierOfferId != offerId) problems.Add("Это предложение не выбрано источником расчёта.");
            if (await db.SalePrices.AnyAsync(x => x.ProductVariantId == variantId && x.IsManual, ct)) problems.Add("Ручные цены защищены и не пересчитываются.");
            var product = await db.Products.SingleAsync(x => x.Id == variant.ProductId, ct); var path = new List<Guid>(); var cursor = product.CategoryId;
            while (cursor is Guid category)
            {
                if (path.Contains(category)) throw new InvalidOperationException("Цикл категорий.");
                path.Add(category); cursor = await db.Categories.Where(x => x.Id == category).Select(x => x.ParentId).SingleAsync(ct);
            }
            var rules = await db.MarkupRules.AsNoTracking().ToListAsync(ct);
            foreach (var segment in Enum.GetValues<CustomerSegment>())
                if (!rules.Any(r => r.Segment == segment && PriceCalculator.SelectRule(rules, variantId, path, segment, r.MinimumQuantity) is not null))
                    problems.Add(segment == CustomerSegment.Retail ? "Нет подходящего розничного правила наценки." : "Нет подходящего оптового правила наценки.");
        }
        // Расчёт никогда не публикуется автоматически: общий флаг автоприменения удалён.
        var ids = await PricingService.ProposeInContextAsync(db, offerId, null, ct);
        if (ids.Count == 0 && problems.Count == 0) problems.Add("Нет новых допустимых расчётов. Проверьте тариф, область наценки и ручные цены.");
        AdminAccess.Audit(db, actor, operationId, "price.propose", "SupplierOffer", offerId.ToString(), "Подготовлены допустимые предложения пересчёта; автопубликация исключена.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(ids, problems);
    }
    /// <summary>Преобразует служебный источник расчёта в понятную цепочку без вывода JSON.</summary>
    private static string Explain(string source)
    {
        try
        {
            using var json = JsonDocument.Parse(source); var r = json.RootElement;
            return $"Закупка {r.GetProperty("Purchase").GetDecimal():N2} ₽ / {r.GetProperty("Conversion").GetDecimal():N4} ед. × (1 + {r.GetProperty("Markup").GetDecimal():N2}%) → округление до копеек";
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException) { return "Ручная установка цены или прежний источник расчёта."; }
    }
    /// <summary>Возвращает страницу пересчётов с текущей ценой для сравнения.</summary>
    public async Task<AdminPage<ProposalData>> ProposalsAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var q = from p in db.PriceProposals.AsNoTracking() join v in db.ProductVariants on p.ProductVariantId equals v.Id join product in db.Products on v.ProductId equals product.Id
                where query.Search == "" || v.Sku.Contains(query.Search) || product.Name.Contains(query.Search)
                select new { p, v.Sku, product.Name };
        if (query.Filter != "all") q = q.Where(x => x.p.Status == ProposalStatus.Pending);
        var count = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        var rows = await q.OrderByDescending(x => x.p.CreatedAt).ThenBy(x => x.p.Id).Skip((page - 1) * 25).Take(25)
            .Select(x => new { x.p.Id, x.Sku, x.Name, x.p.Segment, x.p.MinimumQuantity, x.p.Amount, x.p.Status, x.p.Source,
                Current = db.SalePrices.Where(s => s.ProductVariantId == x.p.ProductVariantId && s.Segment == x.p.Segment && s.MinimumQuantity == x.p.MinimumQuantity).Select(s => (decimal?)s.Amount).FirstOrDefault() }).ToListAsync(ct);
        return new(rows.Select(x => new ProposalData(x.Id, x.Sku, x.Name, x.Segment, x.MinimumQuantity, x.Amount, x.Current, x.Status, Explain(x.Source))).ToArray(), count, page);
    }
    /// <summary>Применяет проверку Fingerprint и защиту ручной цены существующего сервиса.</summary>
    public async Task ApplyPriceAsync(Guid proposalId, Guid operationId, CancellationToken ct = default) =>
        await ChangeAsync(true, operationId, "price.apply", "PriceProposal", proposalId, async db =>
            await PricingService.ApplyInContextAsync(db, await db.PriceProposals.SingleAsync(x => x.Id == proposalId, ct), ct), ct);

    /// <summary>Устанавливает защищённую ручную цену с существующей историей публикации.</summary>
    public async Task ManualAsync(Guid variantId, CustomerSegment segment, decimal minimum, decimal amount, Guid operationId, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(segment) || amount <= 0 || decimal.Round(amount, 2) != amount || minimum < 1 || segment == CustomerSegment.Retail && minimum != 1)
            throw new ArgumentException("Проверьте цену и количественную ступень.");
        await ChangeAsync(true, operationId, "price.manual", "ProductVariant", variantId, async db =>
        {
            if (!await db.ProductVariants.AnyAsync(x => x.Id == variantId, ct)) throw new ArgumentException("Вариант не найден.");
            var sale = await db.SalePrices.SingleOrDefaultAsync(x => x.ProductVariantId == variantId && x.Segment == segment && x.MinimumQuantity == minimum, ct);
            await PricingService.PublishAsync(db, sale, variantId, segment, minimum, amount, true, "Ручная административная установка", ct);
        }, ct);
    }
    /// <summary>Возвращает страницу истории цен с читаемым источником.</summary>
    public async Task<AdminPage<PriceHistoryData>> HistoryAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var q = from h in db.SalePriceHistories.AsNoTracking() join v in db.ProductVariants on h.ProductVariantId equals v.Id
                where query.Search == "" || v.Sku.Contains(query.Search) select new { h, v.Sku };
        var count = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        var rows = await q.OrderByDescending(x => x.h.ChangedAt).ThenBy(x => x.h.Id).Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        return new(rows.Select(x => new PriceHistoryData(x.h.ChangedAt, x.Sku, x.h.Segment, x.h.MinimumQuantity, x.h.OldAmount, x.h.NewAmount, Explain(x.h.Source))).ToArray(), count, page);
    }
    /// <summary>Выполняет типизированную коммерческую команду с проверкой прав, повторов и аудитом в одной сериализуемой транзакции.</summary>
    private async Task<Guid> ChangeAsync(bool admin, Guid operationId, string action, string type, Guid id, Func<ApplicationDbContext, Task> change, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var actor = await access.RequireAsync(db, admin, ct); var prior = await AdminCatalog.PriorAsync(db, actor, operationId, ct); if (prior is not null) return Guid.Parse(prior);
        await change(db);
        AdminAccess.Audit(db, actor, operationId, action, type, id.ToString(), "Выполнено изменение коммерческих настроек или цены.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return id;
    }
}
