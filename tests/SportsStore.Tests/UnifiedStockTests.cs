using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using Xunit;

namespace SportsStore.Tests;

/// <summary>Единый склад показывает карточки без вариантов и сохраняет серверные фильтры публикации.</summary>
public sealed partial class AdminTests
{
    /// <summary>Черновик без варианта не исчезает и не получает ожидаемые остатки несопоставленного прайса.</summary>
    [Fact]
    public async Task UnifiedStockIncludesEmptyCardsAndFiltersPublication()
    {
        var (supplier, offer) = await PurchaseFixture(); var order = await ReadyPurchase(supplier, offer); await PurchaseStatusTo(order, PurchaseStatus.Submitted);
        var id = await Catalog().SaveProductAsync(new(Guid.Empty, 0, "DEMO пустая карточка", null, null, null, ProductStatus.Draft), Guid.NewGuid());
        var stock = Purchases(); var row = Assert.Single((await stock.OwnedAsync(new())).Items);
        Assert.Equal(id, row.ProductId); Assert.Equal(Guid.Empty, row.VariantId); Assert.Equal(0, row.Available); Assert.Equal(0, row.Incoming);
        Assert.Equal(ProductStatus.Draft, row.Status); Assert.Single((await stock.OwnedAsync(new(Filter: "Draft"))).Items);
        Assert.Empty((await stock.OwnedAsync(new(Filter: "Published"))).Items);
        await using (var db = await factory.CreateDbContextAsync()) { (await db.Products.SingleAsync()).Status = ProductStatus.Archived; await db.SaveChangesAsync(); }
        Assert.Empty((await stock.OwnedAsync(new(Filter: "Draft"))).Items);
        Assert.Single((await stock.OwnedAsync(new("пустая", Sort: "new", Filter: "Archived"))).Items);
    }
}
