using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using SportsStore.Application.Storefront;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Services;
using Xunit;
namespace SportsStore.Tests;

public sealed partial class AdminTests
{
    /// <summary>Проверяет публичность вариантов, отсутствие утечки скрытых фасетов и поиск по собственным данным.</summary>
    [Fact]
    public async Task StorefrontVisibilitySearchAndLiteralSymbols()
    {
        await using var db = await factory.CreateDbContextAsync(); var ids = await StorefrontFixture.SeedAsync(db); var service = new StorefrontService(factory);
        foreach (var term in new[] { "футболка", "ФУТБОЛКА", "adidas", "ADIDAS", "00001234", "jd8036", "JD8036", "  тренировочная   футболка " }) Assert.Contains((await service.ListAsync(new CatalogQuery { Search = term, Sort = "price-asc" })).Items, p => p.Id == ids.Product);
        Assert.Equal(27, (await service.ListAsync("100%_Sport", 1)).Total); Assert.Equal(0, (await service.ListAsync("100%XSport", 1)).Total);
        var all = await service.ListAsync(new CatalogQuery()); Assert.Equal(28, all.Total);
        Assert.DoesNotContain(all.Facets.Brands, b => b.Name.StartsWith("Скрытый")); Assert.DoesNotContain(all.Facets.Categories, c => c.Name.StartsWith("Скрытая")); Assert.DoesNotContain("Тайный размер", all.Facets.Sizes);
        Assert.Null(await service.ProductAsync(ids.Hidden));
        var card = (await service.ProductAsync(ids.Product))!; Assert.Equal(3, card.Variants.Count); Assert.Equal(7, card.Variants.Single(v => v.Id == ids.RedS).Available); Assert.DoesNotContain(card.Variants, v => v.Price == 777);
        Assert.Equal(new[] { "Спорт", "Футболки" }, card.CategoryPath.Select(c => c.Name));
        (await db.Products.SingleAsync(p => p.Id == ids.Product)).Status = ProductStatus.Archived; await db.SaveChangesAsync();
        Assert.Null(await service.ProductAsync(ids.Product)); Assert.Equal(0, (await service.ListAsync("JD8036", 1)).Total); Assert.DoesNotContain("M", (await service.ListAsync(new CatalogQuery())).Facets.Sizes);
    }
    /// <summary>Все характеристики и цена должны принадлежать одному варианту; минимальная цена учитывает ограничения.</summary>
    [Fact]
    public async Task StorefrontFiltersMatchOneVariantAndCategoryDescendants()
    {
        await using var db = await factory.CreateDbContextAsync(); var ids = await StorefrontFixture.SeedAsync(db); var service = new StorefrontService(factory);
        Assert.Equal(28, (await service.ListAsync(new CatalogQuery { Category = ids.Root })).Total);
        Assert.Empty((await service.ListAsync(new CatalogQuery { Sizes = ["S"], Colors = ["Синий"] })).Items);
        var redM = await service.ListAsync(new CatalogQuery { Sizes = ["M"], Colors = ["Красный"] }); Assert.Equal(ids.RedM, Assert.Single(Assert.Single(redM.Items).Variants).Id); Assert.Equal(1500, redM.Items[0].Variants.Min(v => v.Price));
        Assert.Empty((await service.ListAsync(new CatalogQuery { Sizes = ["M"], Availability = "stock" })).Items);
        Assert.Empty((await service.ListAsync(new CatalogQuery { Sizes = ["M"], MaxPrice = 1200 })).Items);
        Assert.Equal(2, Assert.Single((await service.ListAsync(new CatalogQuery { Colors = ["Красный", "Синий"], Sizes = ["M"] })).Items).Variants.Count);
        Assert.Empty((await service.ListAsync(new CatalogQuery { Brands = [Guid.NewGuid()] })).Items);
        Assert.Equal(3, Assert.Single((await service.ListAsync(new CatalogQuery { Brands = [Guid.NewGuid(), ids.Brand], Search = "футболка" })).Items).Variants.Count);
    }
    /// <summary>Сортировки стабильны, страницы не пересекаются, границы не создают пустые несуществующие страницы.</summary>
    [Fact]
    public async Task StorefrontPagingStableOrderingAndInvalidInput()
    {
        await using var db = await factory.CreateDbContextAsync(); var ids = await StorefrontFixture.SeedAsync(db); var service = new StorefrontService(factory);
        foreach (var sort in new[] { "name", "price-asc", "price-desc", "newest" })
        {
            var first = await service.ListAsync(new CatalogQuery { Sort = sort }); var second = await service.ListAsync(new CatalogQuery { Sort = sort, Page = 2 });
            Assert.Equal(24, first.Items.Count); Assert.Equal(4, second.Items.Count); Assert.Empty(first.Items.Select(p => p.Id).Intersect(second.Items.Select(p => p.Id)));
            Assert.Equal(first.Items.Select(p => p.Id), (await service.ListAsync(new CatalogQuery { Sort = sort })).Items.Select(p => p.Id));
        }
        Assert.Equal(ids.Product, (await service.ListAsync(new CatalogQuery { Sort = "price-asc" })).Items[0].Id);
        Assert.Equal(ids.Product, (await service.ListAsync(new CatalogQuery { Sort = "newest" })).Items[0].Id);
        Assert.Equal(2, (await service.ListAsync(new CatalogQuery { Page = int.MaxValue })).Page);
        var normalized = new CatalogQuery { MinPrice = 2000, MaxPrice = 1000, Sort = "wholesale", Availability = "supplier", Page = -1, Search = new string('a', 300) }.Normalize();
        Assert.Equal(1000, normalized.MinPrice); Assert.Equal(2000, normalized.MaxPrice); Assert.Equal("name", normalized.Sort); Assert.Equal("all", normalized.Availability); Assert.Equal(200, normalized.Search.Length);
    }
    /// <summary>Вариант без розничной базовой цены скрывается; wholesale и розничная ступень от двух единиц не заменяют базовую.</summary>
    [Fact]
    public async Task StorefrontNeverUsesWholesaleOrQuantityTierAsBasePrice()
    {
        await using var db = await factory.CreateDbContextAsync(); var ids = await StorefrontFixture.SeedAsync(db); var service = new StorefrontService(factory);
        db.SalePrices.RemoveRange(await db.SalePrices.Where(s => s.ProductVariantId == ids.RedS && s.Segment == CustomerSegment.Retail).ToListAsync());
        await db.SaveChangesAsync();
        var invalid = new SalePrice { ProductVariantId = ids.RedS, MinimumQuantity = 2, Amount = 500 }; db.SalePrices.Add(invalid); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.Entry(invalid).State = EntityState.Detached;
        Assert.DoesNotContain((await service.ProductAsync(ids.Product))!.Variants, v => v.Id == ids.RedS);
        db.SalePrices.RemoveRange(await db.SalePrices.Where(s => s.ProductVariantId == ids.RedM || s.ProductVariantId == ids.BlueM).ToListAsync()); await db.SaveChangesAsync();
        Assert.Null(await service.ProductAsync(ids.Product)); Assert.Equal(0, (await service.ListAsync("футболка", 1)).Total);
    }
    /// <summary>Поступление учитывает только неполученные подтверждённые единицы; склад поставщика и нераспределённый товар исключены.</summary>
    [Fact]
    public async Task StorefrontIncomingUsesConfirmedPurchasesAndStatusPriority()
    {
        await using var db = await factory.CreateDbContextAsync(); var ids = await StorefrontFixture.SeedAsync(db); var service = new StorefrontService(factory);
        var supplier = new Supplier { Code = "store-test", Name = "Тест" }; var tier = new SupplierPriceTier { SupplierId = supplier.Id, Code = "small", Name = "Демо" };
        var warehouse = await db.Warehouses.SingleAsync(); var offer = new SupplierOffer { SupplierId = supplier.Id, ExternalCode = "000001", SourceName = "Демо", SupplierUnit = "шт", ProductVariantId = ids.RedM, Stock = 999, SaleUnitsPerSupplierUnit = 2, ConversionConfirmed = true };
        var order = new PurchaseOrder { SupplierId = supplier.Id, SupplierPriceTierId = tier.Id, WarehouseId = warehouse.Id, Number = "TEST-001", TierCode = "small", TierName = "Демо", CreatedBy = "test-actor", Status = PurchaseStatus.PartiallyReceived };
        var line = new PurchaseOrderLine { PurchaseOrderId = order.Id, SupplierOfferId = offer.Id, ExternalCode = offer.ExternalCode, Name = "Демо", SupplierUnit = "шт", Quantity = 5, ReceivedQuantity = 2, UnitPrice = 1 };
        db.AddRange(supplier, tier, offer, order, line); db.UnallocatedStocks.Add(new() { SupplierOfferId = offer.Id, WarehouseId = warehouse.Id, SupplierUnit = "шт", Quantity = 40 }); await db.SaveChangesAsync();
        var v = (await service.ProductAsync(ids.Product))!.Variants.Single(v => v.Id == ids.RedM); Assert.Equal(0, v.Available); Assert.Equal(6, v.Incoming); Assert.Equal("incoming", v.Availability);
        Assert.Equal(ids.RedM, Assert.Single(Assert.Single((await service.ListAsync(new CatalogQuery { Availability = "incoming" })).Items).Variants).Id);
        offer.ConversionConfirmed = false; await db.SaveChangesAsync(); Assert.Equal(0, (await service.ProductAsync(ids.Product))!.Variants.Single(v => v.Id == ids.RedM).Incoming);
        offer.ConversionConfirmed = true; order.Status = PurchaseStatus.Cancelled; await db.SaveChangesAsync(); Assert.Equal(0, (await service.ProductAsync(ids.Product))!.Variants.Single(v => v.Id == ids.RedM).Incoming);
        order.Status = PurchaseStatus.Created; await db.SaveChangesAsync(); Assert.Equal(0, (await service.ProductAsync(ids.Product))!.Variants.Single(v => v.Id == ids.RedM).Incoming);
        order.Status = PurchaseStatus.Submitted; line.SupplierUnit = "пар"; await db.SaveChangesAsync(); Assert.Equal(0, (await service.ProductAsync(ids.Product))!.Variants.Single(v => v.Id == ids.RedM).Incoming);
    }
    /// <summary>Измеряет SQL: полная страница не вызывает отдельный запрос для каждой карточки.</summary>
    [Fact]
    public async Task StorefrontSqlCountDoesNotGrowPerCard()
    {
        await using var db = await factory.CreateDbContextAsync(); await StorefrontFixture.SeedAsync(db); var counter = new StoreCounter();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(db.Database.GetConnectionString()).AddInterceptors(counter).Options;
        var service = new StorefrontService(new StoreFactory(options));
        await service.ListAsync(new CatalogQuery { Search = "футболка" }); var one = counter.Count; counter.Count = 0;
        await service.ListAsync(new CatalogQuery()); Assert.Equal(one, counter.Count); Assert.InRange(counter.Count, 1, 12);
    }
    /// <summary>Реальный apply нового прайса не меняет ручную розницу; чтение витрины не записывает цены и остатки.</summary>
    [Fact]
    public async Task StorefrontManualPriceSurvivesImportAndReadingDoesNotWrite()
    {
        await using var db = await factory.CreateDbContextAsync(); var ids = await StorefrontFixture.SeedAsync(db);
        var initial = new SportsStore.Infrastructure.Import.PriceImportService(factory, new StorePriceParser(Fixtures.Document()));
        await initial.ApplyAsync((await initial.PreviewAsync("ballmarket", "demo-first")).Id);
        var offer = await db.SupplierOffers.SingleAsync(); offer.ProductVariantId = ids.RedS; offer.ConversionConfirmed = true; offer.SaleUnitsPerSupplierUnit = 1; await db.SaveChangesAsync();
        var originalPrice = await db.SalePrices.AsNoTracking().SingleAsync(p => p.ProductVariantId == ids.RedS && p.Segment == CustomerSegment.Retail);
        var originalBalance = await db.InventoryBalances.AsNoTracking().SingleAsync();
        var next = new SportsStore.Infrastructure.Import.PriceImportService(factory, new StorePriceParser(Fixtures.Document(price: "4700", date: "29.07.26")));
        await next.ApplyAsync((await next.PreviewAsync("ballmarket", "demo-next")).Id);
        var service = new StorefrontService(factory); Assert.Equal(1000, (await service.ProductAsync(ids.Product))!.Variants.Single(v => v.Id == ids.RedS).Price);
        await service.ListAsync(new CatalogQuery { Availability = "stock", Sort = "price-asc" });
        var price = await db.SalePrices.AsNoTracking().SingleAsync(p => p.Id == originalPrice.Id); Assert.True(price.IsManual); Assert.Equal(originalPrice.Version, price.Version);
        var balance = await db.InventoryBalances.AsNoTracking().SingleAsync(); Assert.Equal(originalBalance.Version, balance.Version); Assert.Equal(10, balance.OnHand); Assert.Equal(3, balance.Reserved);
        Assert.NotEmpty(await db.SupplierPriceHistories.ToListAsync());
    }
    /// <summary>Контролируемая партия закупочных данных для проверки настоящей транзакции импорта.</summary>
    /// <param name="document">Синтетический разобранный документ.</param>
    private sealed class StorePriceParser(SportsStore.Application.Import.ParsedPriceList document) : SportsStore.Application.Import.ISupplierPriceParser
    {
        /// <summary>Одинаковая версия для последовательных прайсов одного формата.</summary>
        public string Version => "storefront-test/1";
        /// <summary>Возвращает синтетические данные, не обращаясь к рабочему XLS.</summary>
        public SportsStore.Application.Import.ParsedPriceList Parse(string path) => document;
    }
    /// <summary>Тестовый контекст с перехватчиком SQL, не используемый приложением.</summary>
    /// <param name="options">Подключение отдельной тестовой БД.</param>
    private sealed class StoreFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        /// <summary>Создаёт новый короткий контекст.</summary>
        public ApplicationDbContext CreateDbContext() => new(options);
    }
    /// <summary>Считает фактически исполненные SQL-чтения.</summary>
    private sealed class StoreCounter : DbCommandInterceptor
    {
        /// <summary>Количество запросов текущей операции.</summary>
        public int Count;
        /// <summary>Фиксирует выполнение, не изменяя SQL или результат.</summary>
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) { Count++; return ValueTask.FromResult(result); }
    }
}
