using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using Xunit;

namespace SportsStore.Tests;

/// <summary>Цены из карточки товара: независимость опта, история, версия и права сотрудника.</summary>
public sealed partial class AdminTests
{
    /// <summary>Оптовая цена не меняет розницу; повтор команды не создаёт историю, устаревшая версия и Manager отклоняются.</summary>
    [Fact]
    public async Task CardWholesalePriceIsIndependentVersionedAndAdminOnly()
    {
        var (supplier, offer) = await PurchaseFixture();
        await ReadyPurchase(supplier, offer);
        var service = Purchases();
        var product = await service.CreateCardAsync(offer, "DEMO цены карточки", null, null, Guid.NewGuid());
        var variant = Assert.Single(await Catalog().VariantsAsync(product));
        await service.SetRetailAsync(variant.Id, 0, null, 150m, null, Guid.NewGuid());
        var wholesale = await service.SalePriceAsync(variant.Id, CustomerSegment.Wholesale);
        Assert.Null(wholesale.Amount);
        var cost = Assert.Single(wholesale.Costs);
        var op = Guid.NewGuid();
        await service.SetSalePriceAsync(variant.Id, CustomerSegment.Wholesale, 0, cost.LineId, null, 12.345m, op);
        await service.SetSalePriceAsync(variant.Id, CustomerSegment.Wholesale, 0, cost.LineId, null, 12.345m, op);
        Assert.Equal(112.35m, (await service.SalePriceAsync(variant.Id, CustomerSegment.Wholesale)).Amount);
        Assert.Equal(150m, (await service.RetailAsync(variant.Id)).Amount);
        await using var db = await factory.CreateDbContextAsync();
        Assert.Equal(2, await db.SalePriceHistories.CountAsync(x => x.ProductVariantId == variant.Id));
        Assert.Equal(2, await db.SalePrices.CountAsync(x => x.ProductVariantId == variant.Id && x.IsManual));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.SetSalePriceAsync(variant.Id, CustomerSegment.Wholesale, 0, null, 120m, null, Guid.NewGuid()));
        var current = await service.SalePriceAsync(variant.Id, CustomerSegment.Wholesale);
        await service.SetSalePriceAsync(variant.Id, CustomerSegment.Wholesale, current.Version, null, 125m, null, Guid.NewGuid());
        Assert.Equal(125m, (await service.SalePriceAsync(variant.Id, CustomerSegment.Wholesale)).Amount);
        await service.SetSalePriceAsync(variant.Id, CustomerSegment.Wholesale, 0, null, 110m, null, Guid.NewGuid(), minimumQuantity: 10);
        Assert.Equal(new decimal[] { 1, 10 }, await service.WholesaleQuantitiesAsync(variant.Id));
        Assert.Equal(110m, (await service.SalePriceAsync(variant.Id, CustomerSegment.Wholesale, minimumQuantity: 10)).Amount);
        Assert.Equal(125m, (await service.SalePriceAsync(variant.Id, CustomerSegment.Wholesale)).Amount);
        await Role("Manager");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetSalePriceAsync(variant.Id, CustomerSegment.Wholesale, current.Version, null, 130m, null, Guid.NewGuid()));
    }
}
