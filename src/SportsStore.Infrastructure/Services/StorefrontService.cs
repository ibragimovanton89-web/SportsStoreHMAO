using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Storefront;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
namespace SportsStore.Infrastructure.Services;

/// <summary>Розничная витрина: фильтрует и разбивает данные в PostgreSQL, не изменяет цены и склад.</summary>
/// <param name="factory">Короткий контекст на операцию.</param>
public sealed class StorefrontService(IDbContextFactory<ApplicationDbContext> factory) : IStorefront
{
    /// <summary>Замыкание собственной иерархии категорий; путь предотвращает рекурсию при повреждённых данных.</summary>
    private static IQueryable<CategoryLink> Tree(ApplicationDbContext db) => db.Database.SqlQueryRaw<CategoryLink>("""
        WITH RECURSIVE tree AS (
          SELECT "Id" AS "Ancestor", "Id" AS "Descendant", 0 AS "Depth", ARRAY["Id"] AS path FROM "Category"
          UNION ALL
          SELECT t."Ancestor", c."Id", t."Depth" + 1, t.path || c."Id"
          FROM tree t JOIN "Category" c ON c."ParentId" = t."Descendant"
          WHERE NOT c."Id" = ANY(t.path)
        ) SELECT "Ancestor", "Descendant", "Depth" FROM tree
        """);
    /// <summary>Проекция связи предка с потомком, включая саму категорию.</summary>
    private sealed class CategoryLink
    {
        /// <summary>Корень выбранной ветви.</summary>
        public Guid Ancestor { get; set; }
        /// <summary>Доступный потомок.</summary>
        public Guid Descendant { get; set; }
        /// <summary>Расстояние в дереве.</summary>
        public int Depth { get; set; }
    }
    /// <summary>Внутренняя SQL-проекция варианта; наружу передаются только публичные поля.</summary>
    private sealed class VariantRow
    {
        /// <summary>Карточка варианта.</summary>
        public Guid ProductId { get; set; }
        /// <summary>Идентификатор варианта.</summary>
        public Guid Id { get; set; }
        /// <summary>Название собственной карточки для поиска.</summary>
        public string Name { get; set; } = "";
        /// <summary>Собственный бренд.</summary>
        public Guid? BrandId { get; set; }
        /// <summary>Собственная категория.</summary>
        public Guid? CategoryId { get; set; }
        /// <summary>Код магазина.</summary>
        public string Sku { get; set; } = "";
        /// <summary>Подтверждённый артикул.</summary>
        public string? ManufacturerCode { get; set; }
        /// <summary>Единица продажи.</summary>
        public string Unit { get; set; } = "";
        /// <summary>Размер или null.</summary>
        public string? Size { get; set; }
        /// <summary>Цвет или null.</summary>
        public string? Color { get; set; }
        /// <summary>Сохранённая розничная цена RUB за одну единицу.</summary>
        public decimal Price { get; set; }
        /// <summary>Собственный свободный остаток.</summary>
        public decimal Available { get; set; }
        /// <summary>Подтверждённое неполученное количество.</summary>
        public decimal Incoming { get; set; }
    }
    /// <summary>Единый запрос допустимых публичных вариантов и их количеств, без закупочных сумм.</summary>
    private static IQueryable<VariantRow> Variants(ApplicationDbContext db) =>
        from v in db.ProductVariants.AsNoTracking()
        join p in db.Products.AsNoTracking() on v.ProductId equals p.Id
        join s in db.SalePrices.AsNoTracking() on v.Id equals s.ProductVariantId
        where p.Status == ProductStatus.Published && s.Segment == CustomerSegment.Retail && s.MinimumQuantity == 1 && s.Currency == "RUB" && s.Amount > 0
        select new VariantRow { ProductId = p.Id, Name = p.Name, BrandId = p.BrandId, CategoryId = p.CategoryId,
            Id = v.Id, Sku = v.Sku, ManufacturerCode = v.ManufacturerCode, Unit = v.SaleUnit, Size = v.Size, Color = v.Color, Price = s.Amount,
            Available = db.InventoryBalances.Where(b => b.ProductVariantId == v.Id).Sum(b => (decimal?)(b.OnHand - b.Reserved)) ?? 0,
            Incoming = (from l in db.PurchaseOrderLines join o in db.PurchaseOrders on l.PurchaseOrderId equals o.Id join offer in db.SupplierOffers on l.SupplierOfferId equals offer.Id
                where offer.ProductVariantId == v.Id && offer.ConversionConfirmed && offer.SaleUnitsPerSupplierUnit > 0 && l.SupplierUnit == offer.SupplierUnit && (o.Status == PurchaseStatus.Submitted || o.Status == PurchaseStatus.InTransit || o.Status == PurchaseStatus.PartiallyReceived)
                select (decimal?)((l.Quantity - l.ReceivedQuantity) * offer.SaleUnitsPerSupplierUnit)).Sum() ?? 0 };
    /// <inheritdoc />
    public Task<StorePage> ListAsync(string? search, int page, CancellationToken ct = default) => ListAsync(new CatalogQuery { Search = search ?? "", Page = page }, ct);
    /// <inheritdoc />
    public async Task<StorePage> ListAsync(CatalogQuery query, CancellationToken ct = default)
    {
        query = query.Normalize();
        await using var db = await factory.CreateDbContextAsync(ct);
        // Один снимок обеспечивает согласованные счётчики и страницу при одновременном снятии публикации.
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var baseline = Variants(db); var hasProducts = await baseline.AnyAsync(ct);
        if (query.Search.Length > 0)
        {
            // ICU обеспечивает русский регистр даже при LC_CTYPE=C у базы; Contains параметризует буквальный шаблон.
            var term = query.Search.ToLowerInvariant();
            baseline = baseline.Where(v => EF.Functions.Collate(v.Name, "und-x-icu").ToLower().Contains(term) || EF.Functions.Collate(v.Sku, "und-x-icu").ToLower().Contains(term) || (v.ManufacturerCode != null && EF.Functions.Collate(v.ManufacturerCode, "und-x-icu").ToLower().Contains(term)));
        }
        if (query.Category is Guid category) baseline = baseline.Where(v => Tree(db).Any(t => t.Ancestor == category && t.Descendant == v.CategoryId));
        var baselineTotal = await baseline.Select(v => v.ProductId).Distinct().CountAsync(ct);
        var brands = await db.Brands.AsNoTracking().Where(b => baseline.Any(v => v.BrandId == b.Id)).OrderBy(b => b.Name).ThenBy(b => b.Id).Select(b => new StoreLookup(b.Id, b.Name, null)).ToArrayAsync(ct);
        var categories = await db.Categories.AsNoTracking().Where(c => Tree(db).Any(t => t.Ancestor == c.Id && baseline.Any(v => v.CategoryId == t.Descendant))).OrderBy(c => c.Name).ThenBy(c => c.Id).Select(c => new StoreLookup(c.Id, c.Name, c.ParentId)).ToArrayAsync(ct);
        var sizes = await baseline.Where(v => v.Size != null && v.Size != "").Select(v => v.Size!).Distinct().OrderBy(x => x).ToArrayAsync(ct);
        var colors = await baseline.Where(v => v.Color != null && v.Color != "").Select(v => v.Color!).Distinct().OrderBy(x => x).ToArrayAsync(ct);
        var matches = baseline;
        var brandIds = query.Brands.ToArray(); var sizeValues = query.Sizes.ToArray(); var colorValues = query.Colors.ToArray();
        if (brandIds.Length > 0) matches = matches.Where(v => v.BrandId != null && brandIds.Contains(v.BrandId.Value));
        if (sizeValues.Length > 0) matches = matches.Where(v => v.Size != null && sizeValues.Contains(v.Size));
        if (colorValues.Length > 0) matches = matches.Where(v => v.Color != null && colorValues.Contains(v.Color));
        if (query.MinPrice is decimal min) matches = matches.Where(v => v.Price >= min);
        if (query.MaxPrice is decimal max) matches = matches.Where(v => v.Price <= max);
        matches = query.Availability switch { "stock" => matches.Where(v => v.Available > 0), "incoming" => matches.Where(v => v.Available == 0 && v.Incoming > 0), "absent" => matches.Where(v => v.Available == 0 && v.Incoming == 0), _ => matches };
        var products = db.Products.AsNoTracking().Where(p => matches.Any(v => v.ProductId == p.Id));
        var total = await products.CountAsync(ct); var page = Math.Min(query.Page, Math.Max(1, (total + 23) / 24)); query = query with { Page = page };
        var ordered = query.Sort switch {
            "price-asc" => products.OrderBy(p => matches.Where(v => v.ProductId == p.Id).Min(v => v.Price)),
            "price-desc" => products.OrderByDescending(p => matches.Where(v => v.ProductId == p.Id).Min(v => v.Price)),
            "newest" => products.OrderByDescending(p => p.CreatedAt), _ => products.OrderBy(p => EF.Functions.Collate(p.Name, "und-x-icu")) };
        var ids = await ordered.ThenBy(p => p.Id).Skip((page - 1) * 24).Take(24).Select(p => p.Id).ToArrayAsync(ct);
        return new(await ProjectAsync(db, ids, matches, ct), total, page) { Query = query, CatalogHasProducts = hasProducts, BaselineTotal = baselineTotal, Facets = new(brands, categories, sizes, colors) };
    }
    /// <inheritdoc />
    public async Task<StoreProduct?> ProductAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var variants = Variants(db).Where(v => v.ProductId == id);
        if (!await variants.AnyAsync(ct)) return null;
        var product = (await ProjectAsync(db, [id], variants, ct)).Single();
        var category = await db.Products.Where(p => p.Id == id).Select(p => p.CategoryId).SingleAsync(ct);
        var path = await (from t in Tree(db) join c in db.Categories on t.Ancestor equals c.Id where t.Descendant == category orderby t.Depth descending select new StoreLookup(c.Id, c.Name, c.ParentId)).ToArrayAsync(ct);
        return product with { CategoryPath = path };
    }
    /// <summary>Три пакетных запроса для выбранной страницы; число запросов не зависит от количества карточек.</summary>
    private static async Task<IReadOnlyList<StoreProduct>> ProjectAsync(ApplicationDbContext db, Guid[] ids, IQueryable<VariantRow> query, CancellationToken ct)
    {
        if (ids.Length == 0) return [];
        var products = await (from p in db.Products.AsNoTracking() where ids.Contains(p.Id)
            join b in db.Brands on p.BrandId equals b.Id into brands from b in brands.DefaultIfEmpty()
            join c in db.Categories on p.CategoryId equals c.Id into categories from c in categories.DefaultIfEmpty()
            select new { p.Id, p.Name, p.Description, Brand = b == null ? null : b.Name, Category = c == null ? null : c.Name }).ToArrayAsync(ct);
        var variants = await query.Where(v => ids.Contains(v.ProductId)).OrderBy(v => v.Price).ThenBy(v => v.Id).ToArrayAsync(ct);
        var images = await db.ProductImages.AsNoTracking().Where(i => ids.Contains(i.ProductId)).OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => new { i.ProductId, i.Url }).ToArrayAsync(ct);
        return ids.Select(id => { var p = products.Single(p => p.Id == id); return new StoreProduct(p.Id, p.Name, p.Description, p.Brand, p.Category,
            images.Where(i => i.ProductId == id).Select(i => i.Url).ToArray(), variants.Where(v => v.ProductId == id).Select(v => new StoreVariant(v.Sku, v.Unit, v.Size, v.Color, v.Price, v.Available, v.Incoming) { Id = v.Id, ManufacturerCode = v.ManufacturerCode }).ToArray()); }).ToArray();
    }
}
