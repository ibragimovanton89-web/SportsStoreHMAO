using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Import;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Серверное управление собственным каталогом: ограниченные проекции, проверка прав и версий, атомарный аудит.</summary>
/// <param name="factory">Отдельный DbContext на операцию.</param><param name="access">Проверка актуальной личности.</param>
public sealed partial class AdminCatalog(IDbContextFactory<ApplicationDbContext> factory, AdminAccess access) : IAdminCatalog
{
    /// <summary>Создаёт случайный свободный код без изменения карточки, остатков или истории; не резервирует код до сохранения.</summary>
    /// <param name="productId">Существующая карточка с проверкой прав на её редактирование.</param>
    /// <param name="ct">Отмена запросов к PostgreSQL.</param>
    /// <returns>Свободный внутренний SKU вида SS-0123456789ABCDEF.</returns>
    /// <exception cref="InvalidOperationException">После ограниченного числа попыток свободный код не найден.</exception>
    public async Task<string> GenerateSkuAsync(Guid productId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await access.RequireAsync(db, false, ct);
        await EditableAsync(db, productId, ct);
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var sku = "SS-" + Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8));
            if (!await db.ProductVariants.AsNoTracking().AnyAsync(x => x.Sku == sku, ct)) return sku;
        }
        throw new InvalidOperationException("Не удалось подобрать свободный SKU. Повторите генерацию.");
    }

    /// <summary>Читает реальные счётчики без загрузки сущностей.</summary>
    public async Task<AdminOverview> OverviewAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return new(await db.Products.CountAsync(x => x.Status == ProductStatus.Draft, ct), await db.Products.CountAsync(x => x.Status == ProductStatus.Published, ct),
            await db.SupplierOffers.CountAsync(x => x.ProductVariantId == null, ct), await db.ImportBatches.CountAsync(x => x.Status == ImportStatus.Invalid, ct),
            await db.PriceProposals.CountAsync(x => x.Status == ProposalStatus.Pending, ct));
    }

    /// <summary>Находит товары по названию, SKU и коду связанного предложения с серверной пагинацией.</summary>
    public async Task<AdminPage<ProductData>> ProductsAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var search = (query.Search ?? "").Trim(); var q = db.Products.AsNoTracking().AsQueryable();
        if (search.Length > 0) q = q.Where(p => p.Name.Contains(search) || db.ProductVariants.Any(v => v.ProductId == p.Id && (v.Sku.Contains(search)
            || db.SupplierOffers.Any(o => o.ProductVariantId == v.Id && o.ExternalCode.Contains(search)))));
        if (Enum.TryParse<ProductStatus>(query.Filter, out var status)) q = q.Where(x => x.Status == status);
        var total = await q.CountAsync(ct); var page = Page(query.Page);
        q = query.Sort == "new" ? q.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id) : q.OrderBy(x => x.Name).ThenBy(x => x.Id);
        return new(await q.Skip((page - 1) * 25).Take(25).Select(x => new ProductData(x.Id, x.Version, x.Name, x.Description, x.BrandId, x.CategoryId, x.Status)).ToListAsync(ct), total, page);
    }

    /// <summary>Возвращает карточку без EF-отслеживания.</summary>
    public async Task<ProductData> ProductAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await db.Products.AsNoTracking().Where(x => x.Id == id).Select(x => new ProductData(x.Id, x.Version, x.Name, x.Description, x.BrandId, x.CategoryId, x.Status)).SingleAsync(ct);
    }

    /// <summary>Возвращает варианты выбранной карточки; создание ограничено сотней вариантов на карточку.</summary>
    public async Task<IReadOnlyList<VariantData>> VariantsAsync(Guid productId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await db.ProductVariants.AsNoTracking().Where(x => x.ProductId == productId).OrderBy(x => x.Sku).Take(100)
            .Select(x => new VariantData(x.Id, x.Version, x.ProductId, x.Sku, x.SaleUnit, x.Size, x.Color, x.ManufacturerCode, x.Barcode)).ToListAsync(ct);
    }

    /// <summary>Читает выбранный вариант напрямую без поиска по похожим кодам.</summary>
    public async Task<VariantData> VariantAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await db.ProductVariants.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new VariantData(x.Id, x.Version, x.ProductId, x.Sku, x.SaleUnit, x.Size, x.Color, x.ManufacturerCode, x.Barcode)).SingleAsync(ct);
    }

    /// <summary>Проецирует предложения и три тарифа в один ограниченный SQL-запрос без отправки staging в браузер.</summary>
    private static IQueryable<OfferData> OfferProjection(ApplicationDbContext db, IQueryable<SupplierOffer> q) => q.Select(o => new OfferData(o.Id, o.Version,
        o.ExternalCode, o.SourceName, o.SourceSection, o.SupplierUnit, o.UnitsPerBox, o.Stock, o.SourceDate,
        db.SupplierOfferPrices.Where(p => p.SupplierOfferId == o.Id && db.SupplierPriceTiers.Any(t => t.Id == p.SupplierPriceTierId && t.Code == "small")).Select(p => (decimal?)p.Amount).FirstOrDefault(),
        db.SupplierOfferPrices.Where(p => p.SupplierOfferId == o.Id && db.SupplierPriceTiers.Any(t => t.Id == p.SupplierPriceTierId && t.Code == "wholesale")).Select(p => (decimal?)p.Amount).FirstOrDefault(),
        db.SupplierOfferPrices.Where(p => p.SupplierOfferId == o.Id && db.SupplierPriceTiers.Any(t => t.Id == p.SupplierPriceTierId && t.Code == "large")).Select(p => (decimal?)p.Amount).FirstOrDefault(),
        o.ProductVariantId, o.SaleUnitsPerSupplierUnit, o.ConversionConfirmed, db.ProductVariants.Any(v => v.PricingSupplierOfferId == o.Id)));

    /// <summary>Фильтрует и сортирует предложения на сервере; размер страницы всегда 25.</summary>
    public async Task<AdminPage<OfferData>> OffersAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var search = (query.Search ?? "").Trim(); var q = db.SupplierOffers.AsNoTracking().AsQueryable();
        if (search.Length > 0) q = q.Where(x => x.SourceName.Contains(search) || x.ExternalCode.Contains(search)
            || db.ProductVariants.Any(v => v.Id == x.ProductVariantId && v.Sku.Contains(search)));
        if (query.Filter == "unmapped") q = q.Where(x => x.ProductVariantId == null);
        if (query.Filter == "mapped") q = q.Where(x => x.ProductVariantId != null);
        var count = await q.CountAsync(ct); var page = Page(query.Page);
        q = query.Sort == "code" ? q.OrderBy(x => x.ExternalCode).ThenBy(x => x.Id) : q.OrderBy(x => x.SourceName).ThenBy(x => x.Id);
        return new(await OfferProjection(db, q.Skip((page - 1) * 25).Take(25)).ToListAsync(ct), count, page);
    }

    /// <summary>Читает выбранное предложение для формы сопоставления.</summary>
    public async Task<OfferData> OfferAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await OfferProjection(db, db.SupplierOffers.AsNoTracking().Where(x => x.Id == id)).SingleAsync(ct);
    }

    /// <summary>Ищет страницу справочника вместо загрузки всего набора вариантов выбора.</summary>
    public async Task<AdminPage<LookupItem>> LookupsAsync(bool categories, AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var q = categories ? db.Categories.AsNoTracking().Select(x => new { x.Id, x.Name, x.Version, x.ParentId })
            : db.Brands.AsNoTracking().Select(x => new { x.Id, x.Name, x.Version, ParentId = (Guid?)null });
        if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(x => x.Name.ToLower().StartsWith(query.Search.Trim().ToLower()));
        var total = await q.CountAsync(ct); var page = Page(query.Page);
        return new(await q.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25)
            .Select(x => new LookupItem(x.Id, x.Name, x.Version, x.ParentId)).ToListAsync(ct), total, page);
    }

    /// <summary>Возвращает сохранённый выбор по идентификатору после проверки прав сотрудника.</summary>
    /// <param name="categories">Определяет таблицу категорий или брендов.</param>
    /// <param name="id">Идентификатор записи; отсутствующая запись возвращает null.</param>
    /// <param name="ct">Отмена запроса.</param>
    public async Task<LookupItem?> LookupAsync(bool categories, Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await access.RequireAsync(db, false, ct);
        return categories
            ? await db.Categories.AsNoTracking().Where(x => x.Id == id).Select(x => new LookupItem(x.Id, x.Name, x.Version, x.ParentId)).SingleOrDefaultAsync(ct)
            : await db.Brands.AsNoTracking().Where(x => x.Id == id).Select(x => new LookupItem(x.Id, x.Name, x.Version, null)).SingleOrDefaultAsync(ct);
    }

    /// <summary>Сохраняет справочник; изменение существующего имени разрешено только Admin, поскольку влияет на опубликованные карточки.</summary>
    public async Task<Guid> SaveLookupAsync(bool categories, LookupItem data, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, data.Id != Guid.Empty, ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861001)", ct);
        var prior = await PriorAsync(db, actor, operationId, ct); if (prior is not null) return Guid.Parse(prior);
        var name = AdminAccess.Text(data.Name, "Название"); Entity entity;
        if (categories)
        {
            var item = data.Id == Guid.Empty ? new Category() : await db.Categories.SingleAsync(x => x.Id == data.Id, ct);
            if (data.Id != Guid.Empty) AdminAccess.Version(item, data.Version); else db.Categories.Add(item);
            var parent = data.ParentId; var seen = new HashSet<Guid> { item.Id };
            while (parent is not null)
            {
                if (!seen.Add(parent.Value)) throw new ArgumentException("Категория не может быть своим потомком.");
                parent = (await db.Categories.SingleOrDefaultAsync(x => x.Id == parent, ct) ?? throw new ArgumentException("Родительская категория не найдена.")).ParentId;
            }
            item.Name = name; item.ParentId = data.ParentId; entity = item;
        }
        else
        {
            var item = data.Id == Guid.Empty ? new Brand() : await db.Brands.SingleAsync(x => x.Id == data.Id, ct);
            if (data.Id != Guid.Empty) AdminAccess.Version(item, data.Version); else db.Brands.Add(item);
            item.Name = name; entity = item;
        }
        AdminAccess.Audit(db, actor, operationId, "lookup.save", categories ? "Category" : "Brand", entity.Id.ToString(), "Сохранена запись справочника.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return entity.Id;
    }

    /// <summary>Удаляет только свободную запись справочника; связанные карточки и история не затрагиваются.</summary>
    public async Task DeleteLookupAsync(bool categories, Guid id, uint version, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, true, ct); if (await PriorAsync(db, actor, operationId, ct) is not null) return;
        if (await db.Products.AnyAsync(x => categories ? x.CategoryId == id : x.BrandId == id, ct)
            || categories && (await db.Categories.AnyAsync(x => x.ParentId == id, ct) || await db.MarkupRules.AnyAsync(x => x.CategoryId == id, ct)))
            throw new InvalidOperationException("Запись используется. Сначала переназначьте зависимости.");
        Entity entity = categories ? await db.Categories.SingleAsync(x => x.Id == id, ct) : await db.Brands.SingleAsync(x => x.Id == id, ct);
        AdminAccess.Version(entity, version); db.Remove(entity);
        AdminAccess.Audit(db, actor, operationId, "lookup.delete", categories ? "Category" : "Brand", id.ToString(), "Удалена неиспользуемая запись справочника.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    /// <summary>Проверяет доступ к существующей карточке; Manager ограничен черновиками.</summary>
    private async Task<Product> EditableAsync(ApplicationDbContext db, Guid id, CancellationToken ct)
    {
        // Блокировка связывает проверку Draft с записью: параллельная публикация не открывает Manager обход.
        var p = await db.Products.FromSqlInterpolated($"SELECT *, xmin FROM \"Product\" WHERE \"Id\" = {id} FOR UPDATE").SingleAsync(ct);
        await access.RequireAsync(db, p.Status != ProductStatus.Draft, ct); return p;
    }

    /// <summary>Проверяет обязательное название и существование справочников до сохранения.</summary>
    internal static async Task FillProductAsync(ApplicationDbContext db, Product p, string name, string? description, Guid? brand, Guid? category, CancellationToken ct)
    {
        p.Name = AdminAccess.Text(name, "Название"); p.Description = AdminAccess.Optional(description, 8000);
        if (brand is not null && !db.Brands.Local.Any(x => x.Id == brand) && !await db.Brands.AnyAsync(x => x.Id == brand, ct)) throw new ArgumentException("Бренд не найден.");
        if (category is not null && !db.Categories.Local.Any(x => x.Id == category) && !await db.Categories.AnyAsync(x => x.Id == category, ct)) throw new ArgumentException("Категория не найдена.");
        p.BrandId = brand; p.CategoryId = category; p.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Создаёт только Draft; изменение статуса отделено от редактирования для защиты от дополнительных полей формы.</summary>
    public async Task<Guid> SaveProductAsync(ProductData data, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, false, ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861001)", ct);
        var prior = await PriorAsync(db, actor, operationId, ct); if (prior is not null) return Guid.Parse(prior);
        var p = data.Id == Guid.Empty ? new Product() : await EditableAsync(db, data.Id, ct);
        if (data.Id != Guid.Empty) AdminAccess.Version(p, data.Version); else db.Products.Add(p);
        var lookups = await ResolveLookupsAsync(db, data.BrandId, data.CategoryId, data.BrandName, data.CategoryName, ct);
        await FillProductAsync(db, p, data.Name, data.Description, lookups.Brand, lookups.Category, ct);
        if (p.Status == ProductStatus.Published && (p.BrandId is null || p.CategoryId is null)) throw new ArgumentException("Опубликованная карточка должна иметь бренд и категорию.");
        AdminAccess.Audit(db, actor, operationId, "product.save", "Product", p.Id.ToString(), "Сохранены данные собственной карточки.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return p.Id;
    }

    /// <summary>Проверяет данные варианта; уникальность дополнительно защищена индексом PostgreSQL.</summary>
    internal static async Task FillVariantAsync(ApplicationDbContext db, ProductVariant v, VariantData data, CancellationToken ct)
    {
        v.Sku = AdminAccess.Text(data.Sku, "SKU");
        if (await db.ProductVariants.AnyAsync(x => x.Id != v.Id && x.Sku == v.Sku, ct)) throw new ArgumentException("Такой SKU уже используется.");
        if (data.Unit is not ("шт" or "пар" or "компл" or "упак")) throw new ArgumentException("Выберите допустимую единицу продажи.");
        if (v.SaleUnit != data.Unit && (await db.InventoryBalances.AnyAsync(x => x.ProductVariantId == v.Id, ct) || await db.SupplierOffers.AnyAsync(x => x.ProductVariantId == v.Id, ct)))
            throw new InvalidOperationException("Нельзя менять единицу варианта со складом или связью поставщика: требуется отдельная операция перевода остатков.");
        v.SaleUnit = data.Unit; v.Size = AdminAccess.Optional(data.Size); v.Color = AdminAccess.Optional(data.Color);
        v.ManufacturerCode = AdminAccess.Optional(data.ManufacturerCode); v.Barcode = AdminAccess.Optional(data.Barcode);
    }

    /// <summary>Сохраняет вариант; добавление варианта к опубликованной карточке требует сначала вернуть карточку в черновик.</summary>
    public async Task<Guid> SaveVariantAsync(VariantData data, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, false, ct); var prior = await PriorAsync(db, actor, operationId, ct); if (prior is not null) return Guid.Parse(prior);
        var product = await EditableAsync(db, data.ProductId, ct);
        var v = data.Id == Guid.Empty ? new ProductVariant { ProductId = product.Id } : await db.ProductVariants.SingleAsync(x => x.Id == data.Id, ct);
        if (v.ProductId != product.Id) throw new ArgumentException("Вариант принадлежит другой карточке.");
        if (data.Id != Guid.Empty) AdminAccess.Version(v, data.Version);
        else
        {
            if (product.Status != ProductStatus.Draft) throw new InvalidOperationException("Для добавления варианта верните карточку в черновик.");
            if (await db.ProductVariants.CountAsync(x => x.ProductId == product.Id, ct) >= 100) throw new ArgumentException("Допускается до 100 вариантов в карточке.");
            db.ProductVariants.Add(v);
        }
        await FillVariantAsync(db, v, data, ct);
        product.UpdatedAt = DateTime.UtcNow;
        AdminAccess.Audit(db, actor, operationId, "variant.save", "ProductVariant", v.Id.ToString(), "Сохранены данные варианта.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return v.Id;
    }

    /// <summary>Вычисляет препятствия публикации; отсутствие собственного остатка не является ошибкой.</summary>
    private static async Task<List<string>> ProblemsAsync(ApplicationDbContext db, Product p, CancellationToken ct)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(p.Name)) errors.Add("Укажите название.");
        if (p.BrandId is null) errors.Add("Выберите бренд."); if (p.CategoryId is null) errors.Add("Выберите категорию.");
        if (!await db.ProductImages.AnyAsync(x => x.ProductId == p.Id, ct)) errors.Add("Загрузите хотя бы одно изображение.");
        var variants = await db.ProductVariants.Where(x => x.ProductId == p.Id).ToListAsync(ct);
        if (variants.Count == 0) errors.Add("Создайте хотя бы один вариант.");
        foreach (var variant in variants)
        {
            if (string.IsNullOrWhiteSpace(variant.Sku) || string.IsNullOrWhiteSpace(variant.SaleUnit)) errors.Add("Заполните SKU и единицу варианта.");
            if (!await db.SalePrices.AnyAsync(x => x.ProductVariantId == variant.Id && x.Segment == CustomerSegment.Retail && x.MinimumQuantity == 1 && x.Amount > 0, ct)) errors.Add($"Для {variant.Sku} нет действующей розничной цены.");
        }
        return errors;
    }

    /// <summary>Возвращает причины, которые сотрудник должен устранить до публикации.</summary>
    public async Task<IReadOnlyList<string>> PublicationProblemsAsync(Guid productId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await ProblemsAsync(db, await db.Products.AsNoTracking().SingleAsync(x => x.Id == productId, ct), ct);
    }

    /// <summary>Меняет статус только по отдельной команде Admin с проверкой версии и готовности карточки.</summary>
    public async Task SetStatusAsync(Guid id, uint version, ProductStatus status, Guid operationId, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentException("Неизвестный статус.");
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var actor = await access.RequireAsync(db, true, ct); if (await PriorAsync(db, actor, operationId, ct) is not null) return;
        var p = await db.Products.SingleAsync(x => x.Id == id, ct); AdminAccess.Version(p, version);
        if (status == ProductStatus.Published)
        {
            var problems = await ProblemsAsync(db, p, ct); if (problems.Count > 0) throw new InvalidOperationException(string.Join(" ", problems));
        }
        p.Status = status; p.UpdatedAt = DateTime.UtcNow;
        AdminAccess.Audit(db, actor, operationId, "product.status", "Product", id.ToString(), $"Статус карточки: {status}.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    /// <summary>Ищет результат ранее завершённой команды в атомарном журнале.</summary>
    internal static Task<string?> PriorAsync(ApplicationDbContext db, AdminSession actor, Guid operationId, CancellationToken ct) =>
        db.AdminAudits.Where(x => x.ActorId == actor.UserId && x.OperationId == operationId).Select(x => x.ObjectId).SingleOrDefaultAsync(ct);

    /// <summary>Ограничивает смещение запроса, не позволяя переполнение или неограниченный обход.</summary>
    private static int Page(int page) => Math.Clamp(page, 1, 100000);
}

