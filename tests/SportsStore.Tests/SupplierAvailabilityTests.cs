using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using Xunit;
namespace SportsStore.Tests;

/// <summary>Доступность поставщика при подборе без изменения исходного прайса.</summary>
public sealed partial class AdminTests
{
    /// <summary>Выбор уменьшает отображаемый остаток, удаление возвращает его; ноль блокирует добавление и ручное превышение.</summary>
    [Fact]
    public async Task SupplierAvailabilityTracksSelectionAndRejectsExcess()
    {
        var (supplier, offer) = await PurchaseFixture(); var service = Purchases();
        await using (var db = await factory.CreateDbContextAsync()) { (await db.SupplierOffers.SingleAsync()).Stock = 2; await db.SaveChangesAsync(); }
        var id = await service.StartAsync(supplier, Guid.NewGuid());
        await service.SetQuantityAsync(id, (await service.OrderAsync(id)).Version, offer, 1, Guid.NewGuid());
        Assert.Equal(1, Assert.Single((await service.PositionsAsync(id, new(), false)).Items).RemainingStock);
        await service.SetQuantityAsync(id, (await service.OrderAsync(id)).Version, offer, 2, Guid.NewGuid());
        Assert.Equal(0, Assert.Single((await service.PositionsAsync(id, new(), false)).Items).RemainingStock);
        var selectedVersion = (await service.OrderAsync(id)).Version; await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetQuantityAsync(id, selectedVersion, offer, 3, Guid.NewGuid()));
        await service.SetQuantityAsync(id, (await service.OrderAsync(id)).Version, offer, 0, Guid.NewGuid());
        Assert.Equal(2, Assert.Single((await service.PositionsAsync(id, new(), false)).Items).RemainingStock);
        await using (var db = await factory.CreateDbContextAsync()) { Assert.Equal(2, (await db.SupplierOffers.SingleAsync()).Stock); (await db.SupplierOffers.SingleAsync()).Stock = 0; await db.SaveChangesAsync(); }
        var version = (await service.OrderAsync(id)).Version;
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetQuantityAsync(id, version, offer, 1, Guid.NewGuid()));
    }
    /// <summary>Неизвестный остаток не считается нулём; снижение остатка разрешает уменьшение, но блокирует создание до исправления.</summary>
    [Fact]
    public async Task UnknownStockAndUpdatedStockHaveExplicitBehavior()
    {
        var (supplier, offer) = await PurchaseFixture(); var service = Purchases(); var id = await service.StartAsync(supplier, Guid.NewGuid());
        await service.SetQuantityAsync(id, (await service.OrderAsync(id)).Version, offer, 8, Guid.NewGuid());
        Assert.Null(Assert.Single((await service.PositionsAsync(id, new(), false)).Items).RemainingStock);
        await using (var db = await factory.CreateDbContextAsync()) { (await db.SupplierOffers.SingleAsync()).Stock = 2; await db.SaveChangesAsync(); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => PurchaseStatusTo(id, PurchaseStatus.Created));
        await service.SetQuantityAsync(id, (await service.OrderAsync(id)).Version, offer, 7, Guid.NewGuid());
        await service.SetQuantityAsync(id, (await service.OrderAsync(id)).Version, offer, 2, Guid.NewGuid());
        await PurchaseStatusTo(id, PurchaseStatus.Created);
        var tiers = await service.TiersAsync(supplier);
        await service.SaveTiersAsync(supplier, tiers.Select(t => t with { ManualMinimum = 0 }).ToArray(), Guid.NewGuid());
        var order = await service.OrderAsync(id);
        await service.ConfigureAsync(id, order.Version, order.TierId, order.WarehouseId, null, Guid.NewGuid());
        await PurchaseStatusTo(id, PurchaseStatus.Submitted);
        await using (var db = await factory.CreateDbContextAsync()) { (await db.SupplierOffers.SingleAsync()).Stock = 0; await db.SaveChangesAsync(); }
        // Падение остатка не должно запирать переданный заказ: сотрудник может вернуть его и исправить состав.
        await PurchaseStatusTo(id, PurchaseStatus.Created);
        await service.SetQuantityAsync(id, (await service.OrderAsync(id)).Version, offer, 0, Guid.NewGuid());
    }
}

