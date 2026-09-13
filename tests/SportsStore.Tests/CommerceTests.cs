using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SportsStore.Application.Commerce;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Admin;
using SportsStore.Infrastructure.Commerce;
using SportsStore.Infrastructure.Customers;
using SportsStore.Infrastructure.Pricing;
using Xunit;
namespace SportsStore.Tests;
/// <summary>Корзина и резерв проверяются настоящими независимыми PostgreSQL-транзакциями.</summary>
public sealed partial class AdminTests
{
    /// <summary>Изолированный гостевой секрет; чужая cookie моделируется другим значением.</summary>
    private sealed class TestGuest(string? hash) : IGuestCartIdentity { /// <inheritdoc />
        public string? KeyHash => hash; }
    /// <summary>Управляемые часы для истечения без ожидания суток.</summary>
    private sealed class OrderClock : TimeProvider
    {
        /// <summary>Текущее время теста UTC.</summary>
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => Now;
    }
    /// <summary>Собирает прикладной сервис с настоящей фабрикой отдельных соединений.</summary>
    private CommerceService Shop(BuyerIdentity buyer, string? guest = null, TimeProvider? clock = null) => new(factory, new CustomerAccess(buyer), buyer, new TestGuest(guest), new AdminAccess(identity), clock ?? TimeProvider.System,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Orders:ReservationHours"] = "24" }).Build());
    /// <summary>Готовит синтетический каталог, подтверждённого владельца, телефон и адрес.</summary>
    private async Task<(StorefrontFixture.Ids Ids, BuyerIdentity Buyer, Guid Address, CommerceService Shop)> CheckoutFixture()
    {
        var b = await BuyerAsync(); await using var db = await factory.CreateDbContextAsync(); var ids = await StorefrontFixture.SeedAsync(db);
        var u = await db.Users.SingleAsync(x => x.Id == b.Customer.ApplicationUserId); u.PhoneNumber = "+70000000000";
        var address = new CustomerAddress { CustomerId = b.Customer.Id, Recipient = "Тестовый получатель", Address = "Синтетический адрес", PostalCode = "000000" }; db.Add(address); await db.SaveChangesAsync();
        return (ids, b.Identity, address.Id, Shop(b.Identity));
    }
    /// <summary>Гостевая корзина недоступна по чужому секрету; объединение повторяется без удвоения.</summary>
    [Fact]
    public async Task CartGuestOwnershipAndMergeAreIdempotent()
    {
        var f = await CheckoutFixture(); var anonymous = new BuyerIdentity(new("", "", false)); var guest = Shop(anonymous, "guest-secret-hash");
        var op = Guid.NewGuid(); await guest.AddAsync(f.Ids.RedS, 2, op); await guest.AddAsync(f.Ids.RedS, 2, op);
        Assert.Empty((await Shop(anonymous, "other-hash").CartAsync()).Lines);
        await f.Shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); var merged = Shop(f.Buyer, "guest-secret-hash");
        await merged.MergeAsync(); await merged.MergeAsync(); Assert.Equal(3, Assert.Single((await merged.CartAsync()).Lines).Quantity);
        Assert.Empty((await guest.CartAsync()).Lines);
        await using var db = await factory.CreateDbContextAsync(); Assert.Equal(3, (await db.InventoryBalances.SingleAsync()).Reserved);
    }
    /// <summary>Две вкладки не перезаписывают количество, скрытая карточка остаётся безопасной удаляемой строкой.</summary>
    [Fact]
    public async Task CartVersionAndHiddenVariantAreChecked()
    {
        var f = await CheckoutFixture(); await f.Shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); var cart = await f.Shop.CartAsync();
        await f.Shop.SetAsync(f.Ids.RedS, 2, cart.Version); await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => f.Shop.SetAsync(f.Ids.RedS, 3, cart.Version));
        await using var db = await factory.CreateDbContextAsync(); (await db.Products.SingleAsync(x => x.Id == f.Ids.Product)).Status = ProductStatus.Archived; await db.SaveChangesAsync();
        var line = Assert.Single((await f.Shop.CartAsync()).Lines); Assert.Null(line.ProductId); Assert.Equal("", line.Sku); Assert.Null(line.Price); Assert.NotNull(line.Error);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Shop.PreviewAsync(f.Address));
    }
    /// <summary>Оптовые ступени применяются к одному SKU; отсутствие цены не заменяется розницей.</summary>
    [Fact]
    public async Task CheckoutWholesaleTiersAndMissingPrice()
    {
        var f = await CheckoutFixture(); await using var db = await factory.CreateDbContextAsync(); var c = await db.Customers.SingleAsync(); c.Segment = CustomerSegment.Wholesale; c.WholesaleStatus = WholesaleStatus.Approved;
        db.SalePrices.Add(new() { ProductVariantId = f.Ids.RedS, Segment = CustomerSegment.Wholesale, MinimumQuantity = 5, Amount = 650 }); await db.SaveChangesAsync();
        await f.Shop.AddAsync(f.Ids.RedS, 5, Guid.NewGuid()); Assert.Equal(650, (await f.Shop.CartAsync()).Lines[0].Price);
        await f.Shop.SetAsync(f.Ids.RedS, 4, (await f.Shop.CartAsync()).Version); Assert.Equal(777, (await f.Shop.CartAsync()).Lines[0].Price);
        db.SalePrices.RemoveRange(await db.SalePrices.Where(x => x.Segment == CustomerSegment.Wholesale).ToListAsync()); await db.SaveChangesAsync();
        Assert.Null((await f.Shop.CartAsync()).Total); await Assert.ThrowsAsync<ArgumentException>(() => f.Shop.PreviewAsync(f.Address));
    }
    /// <summary>Изменение цены или выбранного адреса требует повторного подтверждения, не создавая резерв.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CheckoutRequiresNewConfirmationWhenConditionsChange(bool price)
    {
        var f = await CheckoutFixture(); await f.Shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); var p = await f.Shop.PreviewAsync(f.Address);
        await using var db = await factory.CreateDbContextAsync();
        if (price) (await db.SalePrices.SingleAsync(x => x.ProductVariantId == f.Ids.RedS && x.Segment == CustomerSegment.Retail)).Amount = 1100;
        else (await db.CustomerAddresss.SingleAsync(x => x.Id == f.Address)).Address = "Другой синтетический адрес";
        await db.SaveChangesAsync(); var result = await f.Shop.SubmitAsync(p.Id, Guid.NewGuid()); Assert.Null(result.OrderId); Assert.NotNull(result.NewPreviewId); Assert.Empty(await db.Orders.ToListAsync()); Assert.Equal(3, (await db.InventoryBalances.SingleAsync()).Reserved);
    }
    /// <summary>Отзыв одобренного опта меняет подтверждаемые условия, но не создаёт заказ по розничной цене молча.</summary>
    [Fact]
    public async Task CheckoutWholesaleRevocationRequiresConfirmation()
    {
        var f = await CheckoutFixture(); await using var db = await factory.CreateDbContextAsync(); var c = await db.Customers.SingleAsync(); c.Segment = CustomerSegment.Wholesale; c.WholesaleStatus = WholesaleStatus.Approved; await db.SaveChangesAsync();
        await f.Shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); var p = await f.Shop.PreviewAsync(f.Address);
        c.WholesaleStatus = WholesaleStatus.Rejected; await db.SaveChangesAsync(); Assert.NotNull((await f.Shop.SubmitAsync(p.Id, Guid.NewGuid())).NewPreviewId);
    }
    /// <summary>Состав резервируется по нескольким складам; повтор отмены сохраняет прежний чужой резерв.</summary>
    [Fact]
    public async Task CheckoutAllocatesMultipleWarehousesAndCancelsOnce()
    {
        var f = await CheckoutFixture(); await using var db = await factory.CreateDbContextAsync(); var original = await db.InventoryBalances.SingleAsync(); original.OnHand = 5;
        var second = new Warehouse { Name = "Второй тестовый склад" }; db.Add(second); db.InventoryBalances.Add(new() { ProductVariantId = f.Ids.RedS, WarehouseId = second.Id, OnHand = 4 }); await db.SaveChangesAsync();
        await f.Shop.AddAsync(f.Ids.RedS, 5, Guid.NewGuid()); var p = await f.Shop.PreviewAsync(f.Address); var key = Guid.NewGuid(); var result = await f.Shop.SubmitAsync(p.Id, key);
        Assert.Equal(result.OrderId, (await f.Shop.SubmitAsync(p.Id, key)).OrderId);
        var order = await f.Shop.OrderAsync(result.OrderId!.Value); Assert.Equal(5000, order.GoodsTotal); Assert.Empty(order.Reservations);
        Assert.Equal(2, (await f.Shop.OrderAsync(order.Id, true)).Reservations.Count);
        var cancel = Guid.NewGuid(); await f.Shop.ChangeOrderAsync(order.Id, order.Version, "cancel", "", null, cancel, false); await f.Shop.ChangeOrderAsync(order.Id, order.Version, "cancel", "", null, cancel, false);
        db.ChangeTracker.Clear(); Assert.Equal(3, await db.InventoryBalances.SumAsync(x => x.Reserved)); Assert.Equal(9, await db.InventoryBalances.SumAsync(x => x.OnHand)); Assert.Equal(2, await db.OrderEvents.CountAsync());
    }
    /// <summary>Два отдельных соединения одновременно оформляют одну корзину; уникальность и retry дают один заказ.</summary>
    [Fact]
    public async Task CheckoutParallelSameCartCreatesOneOrder()
    {
        var f = await CheckoutFixture(); await f.Shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); var p = await f.Shop.PreviewAsync(f.Address); var key = Guid.NewGuid();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<CheckoutResult> Run() { await start.Task; return await Shop(f.Buyer).SubmitAsync(p.Id, key); }
        var a = Run(); var b = Run(); start.SetResult(); var results = await Task.WhenAll(a, b); Assert.Equal(results[0].OrderId, results[1].OrderId);
        await using var db = await factory.CreateDbContextAsync(); Assert.Single(await db.Orders.ToListAsync()); Assert.Single(await db.OrderNotifications.ToListAsync());
        Assert.Equal(4, (await db.InventoryBalances.SingleAsync()).Reserved);
    }
    /// <summary>Два покупателя одновременно претендуют на последнюю единицу; полный rollback у проигравшего.</summary>
    [Fact]
    public async Task CheckoutTwoBuyersCannotOversellLastUnit()
    {
        var f = await CheckoutFixture(); var other = await BuyerAsync("second@example.invalid"); await using var db = await factory.CreateDbContextAsync();
        (await db.InventoryBalances.SingleAsync()).OnHand = 4; (await db.Users.SingleAsync(x => x.Id == other.Customer.ApplicationUserId)).PhoneNumber = "+70000000000";
        var address = new CustomerAddress { CustomerId = other.Customer.Id, Recipient = "Тест", Address = "Тест" }; db.Add(address); await db.SaveChangesAsync();
        var shop = Shop(other.Identity); await f.Shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); await shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid());
        var p1 = await f.Shop.PreviewAsync(f.Address); var p2 = await shop.PreviewAsync(address.Id); var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Run(CommerceService service, Guid preview) { await start.Task; try { await service.SubmitAsync(preview, Guid.NewGuid()); return true; } catch (ArgumentException) { return false; } }
        var a = Run(f.Shop, p1.Id); var b = Run(shop, p2.Id); start.SetResult(); Assert.Single(await Task.WhenAll(a,b), x => x);
        Assert.Single(await db.Orders.AsNoTracking().ToListAsync()); Assert.Equal(4, await db.InventoryBalances.AsNoTracking().Select(x => x.Reserved).SingleAsync());
    }
    /// <summary>Истечение после нового экземпляра сервиса освобождает только собственный резерв и блокирует позднее подтверждение.</summary>
    [Fact]
    public async Task CheckoutExpirySurvivesProcessorRestart()
    {
        var f = await CheckoutFixture(); var clock = new OrderClock(); var shop = Shop(f.Buyer, clock: clock); await shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); var p = await shop.PreviewAsync(f.Address); var r = await shop.SubmitAsync(p.Id, Guid.NewGuid()); var o = await shop.OrderAsync(r.OrderId!.Value);
        clock.Now = clock.Now.AddHours(25);
        await Assert.ThrowsAsync<ArgumentException>(() => shop.ChangeOrderAsync(o.Id, o.Version, "confirm", "Тест", null, Guid.NewGuid(), true));
        Assert.True(await Shop(f.Buyer, clock:clock).ExpireOneAsync()); Assert.False(await shop.ExpireOneAsync());
        Assert.Equal(CustomerOrderStatus.Expired, (await shop.OrderAsync(o.Id)).Status);
        await using var db = await factory.CreateDbContextAsync(); Assert.Equal(3, (await db.InventoryBalances.SingleAsync()).Reserved);
    }
    /// <summary>Чужие адрес, preview и номер заказа не дают доступа; тот же ключ с другим preview отклоняется.</summary>
    [Fact]
    public async Task CheckoutOwnerAndIdempotencyContentAreEnforced()
    {
        var f = await CheckoutFixture(); await f.Shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); var p = await f.Shop.PreviewAsync(f.Address); var p2 = await f.Shop.PreviewAsync(f.Address); var key = Guid.NewGuid(); var r = await f.Shop.SubmitAsync(p.Id,key);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Shop.SubmitAsync(p2.Id,key));
        var other = await BuyerAsync("other-order@example.invalid"); var shop = Shop(other.Identity);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => shop.SubmitAsync(p.Id,key));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => shop.OrderAsync(r.OrderId!.Value));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => shop.PreviewAsync(p.Id,true));
    }
    /// <summary>Снимки не меняются после изменения каталога, цены и контактов; OnHand при оформлении не списывается.</summary>
    [Fact]
    public async Task CheckoutSnapshotsRemainIndependent()
    {
        var f = await CheckoutFixture(); await f.Shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); var p = await f.Shop.PreviewAsync(f.Address); var r = await f.Shop.SubmitAsync(p.Id, Guid.NewGuid());
        await using var db = await factory.CreateDbContextAsync(); (await db.Products.SingleAsync(x => x.Id == f.Ids.Product)).Name = "Изменено"; (await db.ProductVariants.SingleAsync(x => x.Id == f.Ids.RedS)).Size = "XXL";
        (await db.Customers.SingleAsync()).DisplayName = "Другое имя"; await db.SaveChangesAsync();
        var o = await f.Shop.OrderAsync(r.OrderId!.Value); Assert.Equal("S", o.Lines[0].Size); Assert.NotEqual("Изменено",o.Lines[0].Name); Assert.Equal("Тестовый покупатель",o.ContactName); Assert.Equal(10,(await db.InventoryBalances.SingleAsync()).OnHand);
    }
}
