using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Storefront;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
namespace SportsStore.Infrastructure.Services;

/// <summary>Публичное чтение каталога коротким контекстом; закупочные суммы никогда не попадают в результат.</summary>
/// <param name="factory">Фабрика контекста на отдельную операцию.</param>
public sealed class StorefrontService(IDbContextFactory<ApplicationDbContext> factory) : IStorefront
{
    /// <summary>Ограничивает выборку опубликованными товарами с базовой розничной ценой RUB.</summary>
    private static IQueryable<Product> Published(ApplicationDbContext db) => db.Products.AsNoTracking().Where(p => p.Status == ProductStatus.Published && db.ProductVariants.Any(v => v.ProductId == p.Id && db.SalePrices.Any(s => s.ProductVariantId == v.Id && s.Segment == CustomerSegment.Retail && s.MinimumQuantity == 1 && s.Currency == "RUB" && s.Amount > 0)));
    /// <inheritdoc />
    public async Task<StorePage> ListAsync(string? search, int page, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var query = Published(db); page = Math.Clamp(page, 1, 100000);
        var term = (search ?? "").Trim(); if (term.Length > 200) term = term[..200]; if (term.Length > 0) query = query.Where(p => p.Name.ToLower().Contains(term.ToLower()));
        var total = await query.CountAsync(ct); var products = await query.OrderBy(p => p.Name).ThenBy(p => p.Id).Skip((page - 1) * 24).Take(24).ToListAsync(ct);
        return new(await ProjectAsync(db, products, ct), total, page);
    }
    /// <inheritdoc />
    public async Task<StoreProduct?> ProductAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var products = await Published(db).Where(p => p.Id == id).ToListAsync(ct);
        return (await ProjectAsync(db, products, ct)).SingleOrDefault();
    }
    /// <summary>Пакетно загружает варианты, картинки и количества только выбранных карточек.</summary>
    private static async Task<IReadOnlyList<StoreProduct>> ProjectAsync(ApplicationDbContext db, List<Product> products, CancellationToken ct)
    {
        var ids = products.Select(p => p.Id).ToArray();
        var variants = await (from v in db.ProductVariants.AsNoTracking() join s in db.SalePrices on v.Id equals s.ProductVariantId
            where ids.Contains(v.ProductId) && s.Segment == CustomerSegment.Retail && s.MinimumQuantity == 1 && s.Currency == "RUB" && s.Amount > 0
            select new { v.ProductId, v.Id, v.Sku, v.SaleUnit, v.Size, v.Color, s.Amount,
                Available = db.InventoryBalances.Where(b => b.ProductVariantId == v.Id).Sum(b => (decimal?)(b.OnHand - b.Reserved)) ?? 0,
                Incoming = (from l in db.PurchaseOrderLines join o in db.PurchaseOrders on l.PurchaseOrderId equals o.Id join offer in db.SupplierOffers on l.SupplierOfferId equals offer.Id
                    where offer.ProductVariantId == v.Id && offer.ConversionConfirmed && offer.SaleUnitsPerSupplierUnit > 0 && l.SupplierUnit == offer.SupplierUnit && (o.Status == PurchaseStatus.Submitted || o.Status == PurchaseStatus.InTransit || o.Status == PurchaseStatus.PartiallyReceived)
                    select (decimal?)((l.Quantity - l.ReceivedQuantity) * offer.SaleUnitsPerSupplierUnit)).Sum() ?? 0 }).ToListAsync(ct);
        var images = await db.ProductImages.AsNoTracking().Where(i => ids.Contains(i.ProductId)).OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToListAsync(ct);
        var brandIds = products.Select(p => p.BrandId).ToArray(); var categoryIds = products.Select(p => p.CategoryId).ToArray();
        var brands = await db.Brands.Where(b => brandIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Name, ct);
        var categories = await db.Categories.Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        return products.Select(p => new StoreProduct(p.Id, p.Name, p.Description, p.BrandId is Guid b ? brands.GetValueOrDefault(b) : null, p.CategoryId is Guid c ? categories.GetValueOrDefault(c) : null,
            images.Where(i => i.ProductId == p.Id).Select(i => i.Url).ToArray(), variants.Where(v => v.ProductId == p.Id).Select(v => new StoreVariant(v.Sku, v.SaleUnit, v.Size, v.Color, v.Amount, v.Available, v.Incoming)).ToArray())).ToArray();
    }
}
