using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using Xunit;

namespace SportsStore.Tests;

/// <summary>Удаление закупок проверяется на изолированной PostgreSQL, без изменений пользовательских заказов.</summary>
public sealed partial class AdminTests
{
    /// <summary>Удаление состава не затрагивает карточку, цену, прайс; повтор команды не создаёт второй аудит.</summary>
    [Fact]
    public async Task DeletePurchasePreservesCardPriceAndSupplierAndIsIdempotent()
    {
        var (supplier, offer) = await PurchaseFixture(); var service = Purchases(); var id = await ReadyPurchase(supplier, offer);
        var product = await service.CreateCardAsync(offer, "DEMO сохраняемая карточка", null, null, Guid.NewGuid());
        var variant = Assert.Single(await Catalog().VariantsAsync(product));
        var cost = Assert.Single((await service.RetailAsync(variant.Id)).Costs);
        await service.SetRetailAsync(variant.Id, 0, cost.LineId, null, 20, Guid.NewGuid());
        var order = await service.OrderAsync(id); var operation = Guid.NewGuid();
        await service.DeleteAsync(id, order.Version, operation); await service.DeleteAsync(id, order.Version, operation);
        await using var db = await factory.CreateDbContextAsync();
        Assert.Empty(await db.PurchaseOrders.ToListAsync()); Assert.Empty(await db.PurchaseOrderLines.ToListAsync());
        Assert.Single(await db.Products.ToListAsync()); Assert.Single(await db.ProductVariants.ToListAsync());
        Assert.Equal(120, (await db.SalePrices.SingleAsync()).Amount); Assert.Single(await db.SalePriceHistories.ToListAsync());
        Assert.Single(await db.SupplierOffers.ToListAsync()); Assert.Equal(3, await db.SupplierOfferPrices.CountAsync());
        Assert.Equal(1, await db.AdminAudits.CountAsync(a => a.OperationId == operation && a.Action == "purchase.delete"));
    }
    /// <summary>Старые версии, Manager, отправленные и уже принятые заказы не обходят защиту сервера.</summary>
    [Fact]
    public async Task DeletePurchaseRejectsStaleVersionRoleAndReceipts()
    {
        var (supplier, offer) = await PurchaseFixture(); var service = Purchases(); var id = await ReadyPurchase(supplier, offer);
        var order = await service.OrderAsync(id);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.DeleteAsync(id, 0, Guid.NewGuid()));
        await Role("Manager"); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteAsync(id, order.Version, Guid.NewGuid())); await Role("Admin");
        await PurchaseStatusTo(id, PurchaseStatus.Submitted); order = await service.OrderAsync(id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(id, order.Version, Guid.NewGuid()));
        await service.ReceiveAsync(id, order.Version, null, Guid.NewGuid()); order = await service.OrderAsync(id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(id, order.Version, Guid.NewGuid()));
        await using var db = await factory.CreateDbContextAsync(); Assert.Equal(3, (await db.UnallocatedStocks.SingleAsync()).Quantity);
        Assert.Single(await db.PurchaseReceiptLines.ToListAsync()); Assert.Single(await db.PurchaseOrders.ToListAsync());
    }
    /// <summary>Подборка и отменённая закупка удаляются; после удаления можно начать новый подбор.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteDraftOrCancelledPurchaseAllowsNewDraft(bool cancelled)
    {
        var (supplier, offer) = await PurchaseFixture(); var service = Purchases();
        var id = cancelled ? await ReadyPurchase(supplier, offer) : await service.StartAsync(supplier, Guid.NewGuid());
        if (cancelled) await PurchaseStatusTo(id, PurchaseStatus.Cancelled);
        await service.DeleteAsync(id, (await service.OrderAsync(id)).Version, Guid.NewGuid());
        var next = await service.StartAsync(supplier, Guid.NewGuid()); Assert.NotEqual(id, next);
        Assert.Equal(next, Assert.Single((await service.OrdersAsync(new())).Items).Id);
    }
}
