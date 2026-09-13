using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Import;
using SportsStore.Infrastructure.Persistence;
namespace SportsStore.Infrastructure.Admin;

/// <summary>Ограниченные серверные запросы закупочного кабинета.</summary>
public sealed partial class Purchasing
{
    /// <summary>Проецирует сводку и агрегаты SQL без передачи всех строк заказа браузеру.</summary>
    private static IQueryable<PurchaseSummary> Summary(ApplicationDbContext db, IQueryable<PurchaseOrder> orders) => orders.Select(o => new PurchaseSummary(
        o.Id, o.Version, o.Number, o.SupplierId, db.Suppliers.Where(s => s.Id == o.SupplierId).Select(s => s.Name).First(), o.WarehouseId,
        o.SupplierPriceTierId, o.TierName, o.MinimumAmount, o.Status,
        db.PurchaseOrderLines.Where(l => l.PurchaseOrderId == o.Id).Sum(l => (decimal?)(l.Quantity * (l.UnitPrice ?? 0))) ?? 0,
        db.PurchaseOrderLines.Where(l => l.PurchaseOrderId == o.Id).Sum(l => (int?)l.Quantity) ?? 0,
        db.PurchaseOrderLines.Count(l => l.PurchaseOrderId == o.Id && l.UnitPrice == null), o.CreatedAt, o.Note));
    /// <summary>Возвращает одну страницу закупок, включая сохранённую сборку текущего сотрудника.</summary>
    public async Task<AdminPage<PurchaseSummary>> OrdersAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var actor = await access.RequireAsync(db, false, ct); var admin = await access.IsAdministratorAsync(db, ct);
        var q = db.PurchaseOrders.AsNoTracking().Where(x => x.Status != PurchaseStatus.Draft || x.CreatedBy == actor.UserId || admin);
        if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(x => x.Number.Contains(query.Search) || db.PurchaseOrderLines.Any(l => l.PurchaseOrderId == x.Id && (l.Name.Contains(query.Search) || l.ExternalCode.Contains(query.Search))));
        if (Enum.TryParse<PurchaseStatus>(query.Filter, out var status)) q = q.Where(x => x.Status == status);
        var count = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        return new(await Summary(db, q.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25)).ToListAsync(ct), count, page);
    }
    /// <summary>Читает актуальную сумму заказа и ожидаемую версию формы.</summary>
    public async Task<PurchaseSummary> OrderAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await Summary(db, db.PurchaseOrders.AsNoTracking().Where(x => x.Id == id)).SingleAsync(ct);
    }
    /// <summary>Возвращает максимум 25 позиций; для выбранных строк показывает снимки, для остальных — текущий прайс.</summary>
    public async Task<AdminPage<PurchasePosition>> PositionsAsync(Guid id, AdminQuery query, bool selectedOnly, CancellationToken ct = default, string? brand = null, string? category = null)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var order = await db.PurchaseOrders.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        var q = db.SupplierOffers.AsNoTracking().Where(x => x.SupplierId == order.SupplierId);
        if (!string.IsNullOrEmpty(brand) || !string.IsNullOrEmpty(category))
        {
            var facets = await ReadFacetsAsync(db, order.SupplierId, ct);
            var matching = facets.Where(x => (string.IsNullOrEmpty(brand) || x.Value.Brand == brand) && (string.IsNullOrEmpty(category) || x.Value.Category == category)).Select(x => x.Key).ToArray();
            q = q.Where(x => matching.Contains(x.Id));
        }
        if (selectedOnly) q = q.Where(x => db.PurchaseOrderLines.Any(l => l.PurchaseOrderId == id && l.SupplierOfferId == x.Id));
        if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(x => x.SourceName.Contains(query.Search) || x.ExternalCode.Contains(query.Search)
            || db.PurchaseOrderLines.Any(l => l.PurchaseOrderId == id && l.SupplierOfferId == x.Id && l.Name.Contains(query.Search)));
        var count = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        q = query.Sort == "code" ? q.OrderBy(x => x.ExternalCode).ThenBy(x => x.Id) : q.OrderBy(x => x.SourceName).ThenBy(x => x.Id);
        var offers = await q.Skip((page - 1) * 25).Take(25).ToListAsync(ct); var ids = offers.Select(x => x.Id).ToArray();
        var lines = await db.PurchaseOrderLines.AsNoTracking().Where(x => x.PurchaseOrderId == id && ids.Contains(x.SupplierOfferId)).ToDictionaryAsync(x => x.SupplierOfferId, ct);
        var prices = await (from p in db.SupplierOfferPrices.AsNoTracking() join t in db.SupplierPriceTiers on p.SupplierPriceTierId equals t.Id where ids.Contains(p.SupplierOfferId) select new { p.SupplierOfferId, t.Code, p.Amount }).ToListAsync(ct);
        var variantIds = offers.Where(x => x.ProductVariantId != null).Select(x => x.ProductVariantId!.Value).ToArray();
        var products = await db.ProductVariants.Where(x => variantIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.ProductId, ct);
        var rows = offers.Select(o =>
        {
            var facet = BallMarketFacets.Classify(o.SourceName, o.SourceSection);
            var line = lines.GetValueOrDefault(o.Id); decimal? Price(string code) => prices.Where(p => p.SupplierOfferId == o.Id && p.Code == code).Select(p => (decimal?)p.Amount).FirstOrDefault();
            return new PurchasePosition(o.Id, line?.Id, line?.ExternalCode ?? o.ExternalCode, line?.Name ?? o.SourceName, line?.SupplierUnit ?? o.SupplierUnit,
                line is null ? o.UnitsPerBox : line.UnitsPerBox, line is null ? Price("small") : line.SmallPrice, line is null ? Price("wholesale") : line.WholesalePrice,
                line is null ? Price("large") : line.LargePrice, line is null ? Price(order.TierCode) : line.UnitPrice, line?.Quantity ?? 0, line?.ReceivedQuantity ?? 0,
                o.ProductVariantId is Guid v ? products.GetValueOrDefault(v) : null, o.Stock, facet.Brand, facet.Category);
        }).ToArray();
        return new(rows, count, page);
    }
    /// <summary>Читает ограниченный набор текстовых признаков без загрузки каталога и цен; действует и для ранее импортированного прайса.</summary>
    private static async Task<Dictionary<Guid, (string Brand, string Category)>> ReadFacetsAsync(ApplicationDbContext db, Guid supplierId, CancellationToken ct)
    {
        var rows = await db.SupplierOffers.AsNoTracking().Where(x => x.SupplierId == supplierId).OrderBy(x => x.Id).Select(x => new { x.Id, x.SourceName, x.SourceSection }).Take(20001).ToListAsync(ct);
        if (rows.Count > 20000) throw new InvalidOperationException("Список поставщика превышает лимит фильтрации 20 000 позиций.");
        return rows.ToDictionary(x => x.Id, x => BallMarketFacets.Classify(x.SourceName, x.SourceSection));
    }
    /// <summary>Возвращает полные списки признаков поставщика, независимо от выбранной страницы и фильтра «Только выбранное».</summary>
    public async Task<PurchaseFacets> FacetsAsync(Guid orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await access.RequireAsync(db, false, ct);
        var supplierId = await db.PurchaseOrders.Where(x => x.Id == orderId).Select(x => x.SupplierId).SingleAsync(ct);
        var rows = await ReadFacetsAsync(db, supplierId, ct);
        return new(rows.Values.Select(x => x.Brand).Distinct().Order().ToArray(), rows.Values.Select(x => x.Category).Distinct().Order().ToArray());
    }
    /// <summary>Читает три тарифа с отдельными ручными и исходными порогами.</summary>
    public async Task<IReadOnlyList<PurchaseTier>> TiersAsync(Guid supplierId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await db.SupplierPriceTiers.AsNoTracking().Where(x => x.SupplierId == supplierId && (x.Code == "small" || x.Code == "wholesale" || x.Code == "large"))
            .OrderBy(x => x.Code == "small" ? 0 : x.Code == "wholesale" ? 1 : 2).Select(x => new PurchaseTier(x.Id, x.Version, x.Code, x.Name, x.MinimumAmount, x.ManualMinimumAmount)).ToListAsync(ct);
    }
    /// <summary>Возвращает до 100 собственных складов для выбора.</summary>
    public async Task<IReadOnlyList<LookupItem>> WarehousesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await db.Warehouses.AsNoTracking().OrderBy(x => x.Name).Take(100).Select(x => new LookupItem(x.Id, x.Name, x.Version, null)).ToListAsync(ct);
    }
    /// <summary>Читает фактический непривязанный остаток по складам; один и тот же товар не начисляется варианту дважды.</summary>
    public async Task<AdminPage<UnallocatedPosition>> UnallocatedAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var q = from s in db.UnallocatedStocks.AsNoTracking() join o in db.SupplierOffers on s.SupplierOfferId equals o.Id join w in db.Warehouses on s.WarehouseId equals w.Id
                where s.Quantity > 0 && (query.Search == "" || o.SourceName.Contains(query.Search) || o.ExternalCode.Contains(query.Search)) select new { s, o, w.Name };
        var count = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        return new(await q.OrderBy(x => x.o.SourceName).ThenBy(x => x.s.Id).Skip((page - 1) * 25).Take(25)
            .Select(x => new UnallocatedPosition(x.o.Id, x.o.ExternalCode, x.o.SourceName, x.s.SupplierUnit, x.s.Quantity, x.Name)).ToListAsync(ct), count, page);
    }
    /// <summary>Читает собственные варианты с физическим остатком и ожидаемыми поставками в единицах продажи.</summary>
    public async Task<AdminPage<OwnedPosition>> OwnedAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        // LEFT JOIN сохраняет видимость новых карточек, которым ещё не добавили вариант.
        var q = from p in db.Products.AsNoTracking() join variant in db.ProductVariants on p.Id equals variant.ProductId into variants
                from v in variants.DefaultIfEmpty()
                where query.Search == "" || p.Name.Contains(query.Search) || (v != null && (v.Sku.Contains(query.Search)
                    || db.SupplierOffers.Any(o => o.ProductVariantId == v.Id && o.ExternalCode.Contains(query.Search))))
                select new { v, p.Name, p.Id, p.Status, p.CreatedAt, p.Version };
        if (Enum.TryParse<ProductStatus>(query.Filter, out var status)) q = q.Where(x => x.Status == status);
        var count = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        var sorted = query.Sort == "new" ? q.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ThenBy(x => x.v.Id) : q.OrderBy(x => x.Name).ThenBy(x => x.Id).ThenBy(x => x.v.Id);
        return new(await sorted.Skip((page - 1) * 25).Take(25).Select(x => new OwnedPosition(x.Id, x.v == null ? Guid.Empty : x.v.Id, x.Name, x.v == null ? "Без варианта" : x.v.Sku, x.v == null ? "—" : x.v.SaleUnit,
            db.InventoryBalances.Where(b => x.v != null && b.ProductVariantId == x.v.Id).Sum(b => (decimal?)(b.OnHand - b.Reserved)) ?? 0,
            (from l in db.PurchaseOrderLines join o in db.PurchaseOrders on l.PurchaseOrderId equals o.Id join offer in db.SupplierOffers on l.SupplierOfferId equals offer.Id
             where x.v != null && offer.ProductVariantId == x.v.Id && offer.ConversionConfirmed && offer.SaleUnitsPerSupplierUnit > 0 && l.SupplierUnit == offer.SupplierUnit
                && (o.Status == PurchaseStatus.Submitted || o.Status == PurchaseStatus.InTransit || o.Status == PurchaseStatus.PartiallyReceived)
             select (decimal?)((l.Quantity - l.ReceivedQuantity) * offer.SaleUnitsPerSupplierUnit!.Value)).Sum() ?? 0, x.Status, x.Version)).ToListAsync(ct), count, page);
    }
}
