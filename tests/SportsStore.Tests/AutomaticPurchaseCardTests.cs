using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using Xunit;
namespace SportsStore.Tests;

/// <summary>Автоматическая связь закупки с карточкой заменяет удалённый сценарий ручного сопоставления.</summary>
public sealed partial class AdminTests
{
    /// <summary>Карточка создаётся только после выбора в заказе, атомарно и без дублей при повторном открытии.</summary>
    [Fact]
    public async Task PurchaseCardAutomaticallyLinksOnceAndRejectsUnorderedOffer()
    {
        var (supplier, offer) = await PurchaseFixture();
        var service = Purchases();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateCardAsync(offer, "DEMO", null, null, Guid.NewGuid()));
        await ReadyPurchase(supplier, offer);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCardAsync(offer, "", null, null, Guid.NewGuid()));
        await using (var check = await factory.CreateDbContextAsync()) Assert.Empty(await check.Products.ToListAsync());
        var operation = Guid.NewGuid();
        var product = await service.CreateCardAsync(offer, "DEMO карточка", null, null, operation);
        Assert.Equal(product, await service.CreateCardAsync(offer, "DEMO карточка", null, null, operation));
        Assert.Equal(product, await service.CreateCardAsync(offer, "Не заменять название", null, null, Guid.NewGuid()));
        await using var db = await factory.CreateDbContextAsync();
        var variant = Assert.Single(await db.ProductVariants.ToListAsync());
        var linked = await db.SupplierOffers.SingleAsync(x => x.Id == offer);
        Assert.Equal(variant.Id, linked.ProductVariantId);
        Assert.Equal(1m, linked.SaleUnitsPerSupplierUnit);
        Assert.True(linked.ConversionConfirmed);
        Assert.Equal("DEMO карточка", (await db.Products.SingleAsync()).Name);
        Assert.Equal(ProductStatus.Draft, (await db.Products.SingleAsync()).Status);
    }
}
