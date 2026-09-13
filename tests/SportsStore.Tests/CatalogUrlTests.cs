using SportsStore.Application.Storefront;
using SportsStore.Web.Storefront;
using Xunit;
namespace SportsStore.Tests;
/// <summary>Проверяет границы доверия URL и воспроизводимость ссылок витрины.</summary>
public sealed class CatalogUrlTests
{
    /// <summary>«От» не показывается при одинаковой цене даже у разных SKU и единиц продажи.</summary>
    [Fact]
    public void PricePrefixRequiresDifferentMatchingPrices()
    {
        var one = new StoreVariant("A", "шт", null, null, 100, 0, 0);
        var product = new StoreProduct(Guid.NewGuid(), "Тест", null, null, null, [], [one, one with { Sku = "B", Unit = "упак" }]);
        Assert.False(product.HasDifferentPrices);
        Assert.True((product with { Variants = [one, one with { Price = 101 }] }).HasDifferentPrices);
        Assert.False((product with { Variants = [one] }).HasDifferentPrices);
    }
    /// <summary>Несколько значений, кириллица и специальные символы сохраняются буквально.</summary>
    [Fact]
    public void RoundTripPreservesFiltersWithoutWholesaleParameter()
    {
        var query = new CatalogQuery { Search = "100%_ <мяч>", Brands = [Guid.NewGuid(), Guid.NewGuid()], Sizes = ["S", "M"], Colors = ["Красный", "Синий"], MinPrice = 12.34m, MaxPrice = 9999, Sort = "price-desc", Page = 3 };
        var result = CatalogUrls.Parse("https://shop.invalid" + CatalogUrls.Catalog(query));
        Assert.Equal(query.Search, result.Search); Assert.Equal(query.Brands, result.Brands); Assert.Equal(query.Sizes, result.Sizes); Assert.Equal(query.Colors, result.Colors); Assert.Equal(query.MinPrice, result.MinPrice); Assert.Equal(3, result.Page);
        Assert.DoesNotContain("Wholesale", CatalogUrls.Catalog(CatalogUrls.Parse("https://shop.invalid/catalog?segment=Wholesale&sort=Wholesale")));
    }
    /// <summary>Внешний возврат, невалидные значения и подмена Host не становятся редиректом или canonical.</summary>
    [Fact]
    public void InvalidParametersAndExternalOriginsAreRejected()
    {
        foreach (var value in new[] { "https://evil.invalid", "//evil.invalid", "/catalog/../admin", "/catalog%3fnext=evil", "/catalog?return=https://evil.invalid" }) Assert.Equal("/catalog", CatalogUrls.SafeReturn(value));
        var query = CatalogUrls.Parse("https://shop.invalid/catalog?page=bad&category=bad&min=bad&sort=Wholesale"); Assert.Equal(1, query.Page); Assert.Null(query.Category); Assert.Null(query.MinPrice); Assert.Equal("name", query.Sort);
        Assert.Null(CatalogUrls.Canonical(null, "https://evil.invalid/catalog", "/catalog")); Assert.Equal("https://shop.invalid/catalog", CatalogUrls.Canonical("https://shop.invalid", "https://evil.invalid", "/catalog")); Assert.Null(CatalogUrls.Canonical("https://user:pass@evil.invalid", "https://evil.invalid", "/catalog"));
    }
}
