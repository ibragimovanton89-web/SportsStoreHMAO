using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using Xunit;

namespace SportsStore.Tests;

/// <summary>Проверяет отложенное создание справочников и ручную корректировку подсказок прайса на PostgreSQL.</summary>
public sealed partial class AdminTests
{
    /// <summary>Открытие не пишет справочники; сохранение создаёт ссылки, а ручной бренд имеет приоритет над прайсом.</summary>
    [Fact]
    public async Task SuggestedLookupsAreReadOnlyAndPurchaseSaveCreatesReferences()
    {
        var (supplier, offer) = await PurchaseFixture();
        await using (var db = await factory.CreateDbContextAsync())
        {
            var source = await db.SupplierOffers.SingleAsync(x => x.Id == offer);
            source.SourceName = "Мяч ADIDAS DEMO"; source.SourceSection = "ADIDAS / Футбольные мячи";
            await db.SaveChangesAsync();
        }
        var suggested = await Catalog().SuggestLookupsAsync(offer, false);
        Assert.Equal("ADIDAS", suggested.Brand); Assert.Equal("Футбольные мячи", suggested.Category);
        await using (var db = await factory.CreateDbContextAsync())
        {
            Assert.Empty(await db.Brands.ToListAsync()); Assert.Empty(await db.Categories.ToListAsync());
        }
        await ReadyPurchase(supplier, offer);
        // Ошибка карточки откатывает и новые справочники, добавленные в той же транзакции.
        await Assert.ThrowsAsync<ArgumentException>(() => Purchases().CreateCardAsync(offer, "", null, null,
            Guid.NewGuid(), brandName: "Не сохранять", categoryName: "Не сохранять"));
        var operation = Guid.NewGuid();
        var id = await Purchases().CreateCardAsync(offer, "DEMO карточка", null, null, operation,
            brandName: "Мой бренд", categoryName: suggested.Category);
        Assert.Equal(id, await Purchases().CreateCardAsync(offer, "DEMO карточка", null, null, operation,
            brandName: "Не заменять", categoryName: "Не заменять"));
        Assert.Equal(suggested, await Catalog().SuggestLookupsAsync(id, true));
        await using var check = await factory.CreateDbContextAsync();
        var product = await check.Products.SingleAsync();
        Assert.Equal("Мой бренд", (await check.Brands.SingleAsync()).Name);
        Assert.Equal((await check.Brands.SingleAsync()).Id, product.BrandId);
        Assert.Equal((await check.Categories.SingleAsync()).Id, product.CategoryId);
        Assert.Equal("Мяч ADIDAS DEMO", (await check.SupplierOffers.SingleAsync()).SourceName);
    }

    /// <summary>Параллельные сохранения с разным регистром не дублируют справочники, а устаревшая версия не создаёт лишние записи.</summary>
    [Fact]
    public async Task ConcurrentCardsReuseLookupNamesAndStaleSaveDoesNotCreateOrphans()
    {
        ProductData first = new(Guid.Empty, 0, "DEMO один", null, null, null, ProductStatus.Draft)
            { BrandName = "  DEMO   Brand  ", CategoryName = "DEMO Category" };
        var ids = await Task.WhenAll(Catalog().SaveProductAsync(first, Guid.NewGuid()),
            Catalog().SaveProductAsync(first with { Name = "DEMO два", BrandName = "demo brand", CategoryName = "demo category" }, Guid.NewGuid()));
        var saved = await Catalog().ProductAsync(ids[0]);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Catalog().SaveProductAsync(saved with
            { Version = 0, BrandId = null, BrandName = "Не сохранять" }, Guid.NewGuid()));
        await using var db = await factory.CreateDbContextAsync();
        Assert.Single(await db.Brands.ToListAsync()); Assert.Single(await db.Categories.ToListAsync());
        Assert.Equal(saved.BrandId, (await Catalog().ProductAsync(ids[1])).BrandId);
    }

    /// <summary>Неизвестные признаки не становятся справочниками; одноимённые категории разных веток требуют явного выбора.</summary>
    [Fact]
    public async Task UnknownAndAmbiguousCategoriesAreNotGuessed()
    {
        var (_, offer) = await PurchaseFixture();
        await using (var sourceDb = await factory.CreateDbContextAsync())
        {
            (await sourceDb.SupplierOffers.SingleAsync(x => x.Id == offer)).SourceSection = "";
            await sourceDb.SaveChangesAsync();
        }
        var unknown = await Catalog().SuggestLookupsAsync(offer, false);
        Assert.Null(unknown.Brand); Assert.Null(unknown.Category);
        var parent = await Catalog().SaveLookupAsync(true, new(Guid.Empty, "DEMO родитель", 0), Guid.NewGuid());
        var category = await Catalog().SaveLookupAsync(true, new(Guid.Empty, "Мячи", 0), Guid.NewGuid());
        await Catalog().SaveLookupAsync(true, new(Guid.Empty, "Мячи", 0, parent), Guid.NewGuid());
        ProductData input = new(Guid.Empty, 0, "DEMO", null, null, null, ProductStatus.Draft)
            { BrandName = "Не определён", CategoryName = "Мячи" };
        await Assert.ThrowsAsync<ArgumentException>(() => Catalog().SaveProductAsync(input, Guid.NewGuid()));
        var id = await Catalog().SaveProductAsync(input with { CategoryId = category }, Guid.NewGuid());
        var product = await Catalog().ProductAsync(id);
        Assert.Null(product.BrandId); Assert.Equal(category, product.CategoryId);
    }
}
