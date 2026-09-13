using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Commerce;
using Xunit;
namespace SportsStore.Tests;
/// <summary>Дополнительные проверки отката, уведомлений и гонок резерва.</summary>
public sealed partial class AdminTests
{
    /// <summary>Отказ записи второй строки откатывает заказ, резерв, корзину и очередь целиком.</summary>
    [Fact]
    public async Task CheckoutDatabaseFailureRollsBackAllRows()
    {
        var f = await CheckoutFixture(); await using var db = await factory.CreateDbContextAsync(); var balance = await db.InventoryBalances.SingleAsync();
        db.InventoryBalances.Add(new() { WarehouseId = balance.WarehouseId, ProductVariantId = f.Ids.RedM, OnHand = 5 }); await db.SaveChangesAsync();
        await f.Shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); await f.Shop.AddAsync(f.Ids.RedM, 1, Guid.NewGuid()); var p = await f.Shop.PreviewAsync(f.Address);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"OrderItem\" ADD CONSTRAINT \"SyntheticFailure\" CHECK (\"Sku\" <> 'DEMO-RED-M')");
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Shop.SubmitAsync(p.Id, Guid.NewGuid()));
        Assert.Empty(await db.Orders.ToListAsync()); Assert.Empty(await db.OrderReservations.ToListAsync()); Assert.Empty(await db.OrderNotifications.ToListAsync()); Assert.Equal(3, await db.InventoryBalances.SumAsync(x => x.Reserved)); Assert.Equal(2,(await f.Shop.CartAsync()).Lines.Count);
    }
    /// <summary>Несущественное обновление версии без изменения условий не требует повторного подтверждения.</summary>
    [Fact]
    public async Task CheckoutIgnoresUnchangedVersionsAndBlocksSecondKey()
    {
        var f = await CheckoutFixture(); await f.Shop.AddAsync(f.Ids.RedS, 1, Guid.NewGuid()); var p = await f.Shop.PreviewAsync(f.Address);
        await f.Shop.SetAsync(f.Ids.RedS, 1,(await f.Shop.CartAsync()).Version); var result = await f.Shop.SubmitAsync(p.Id,Guid.NewGuid()); Assert.NotNull(result.OrderId);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Shop.SubmitAsync(p.Id,Guid.NewGuid()));
    }
    /// <summary>Невыделенный товар и отсутствующий собственный баланс не становятся доступными для добавления.</summary>
    [Fact]
    public async Task CartNeverUsesUnallocatedStockForOrdering()
    {
        var f = await CheckoutFixture();
        await using var db = await factory.CreateDbContextAsync(); var supplier = new Supplier { Name = "Тестовый поставщик" }; db.Add(supplier);
        var offer = new SupplierOffer { SupplierId = supplier.Id, ExternalCode = "001", SourceName = "Тест", SupplierUnit = "шт", Stock = 500, ProductVariantId = f.Ids.RedM };
        offer.ConversionConfirmed = true; offer.SaleUnitsPerSupplierUnit = 1;
        var tier = new SupplierPriceTier { SupplierId = supplier.Id, Code = "small", Name = "Тест" };
        var purchase = new PurchaseOrder { SupplierId = supplier.Id, SupplierPriceTierId = tier.Id, WarehouseId = (await db.Warehouses.SingleAsync()).Id, Number = "CHECKOUT-INCOMING", TierCode = "small", TierName = "Тест", CreatedBy = "test", Status = PurchaseStatus.Submitted };
        db.AddRange(tier,purchase,new PurchaseOrderLine { PurchaseOrderId = purchase.Id, SupplierOfferId = offer.Id, ExternalCode = "001", Name = "Тест", SupplierUnit = "шт", Quantity = 20, UnitPrice = 1 });
        db.Add(offer); db.Add(new UnallocatedStock { SupplierOfferId = offer.Id, WarehouseId = (await db.Warehouses.SingleAsync()).Id, SupplierUnit = "шт", Quantity = 100 }); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => f.Shop.AddAsync(f.Ids.RedM,1,Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentException>(() => f.Shop.AddAsync(f.Ids.BlueM,1,Guid.NewGuid())); Assert.Empty((await f.Shop.CartAsync()).Lines);
    }
    /// <summary>Изменение размера после preview должно быть показано покупателю до снимка заказа.</summary>
    [Fact]
    public async Task CheckoutVariantAttributesAreConfirmed()
    {
        var f = await CheckoutFixture(); await f.Shop.AddAsync(f.Ids.RedS,1,Guid.NewGuid()); var p = await f.Shop.PreviewAsync(f.Address);
        await using var db = await factory.CreateDbContextAsync(); (await db.ProductVariants.SingleAsync(x=>x.Id==f.Ids.RedS)).Color="Зелёный"; await db.SaveChangesAsync();
        Assert.NotNull((await f.Shop.SubmitAsync(p.Id,Guid.NewGuid())).NewPreviewId);
    }
    /// <summary>Отмена и продление одновременно используют разные соединения; устаревшая версия проигравшей команды не применяется.</summary>
    [Fact]
    public async Task CheckoutCancelAndExtendRaceHasSingleWinner()
    {
        var f=await CheckoutFixture(); await f.Shop.AddAsync(f.Ids.RedS,1,Guid.NewGuid()); var p=await f.Shop.PreviewAsync(f.Address); var r=await f.Shop.SubmitAsync(p.Id,Guid.NewGuid()); var o=await f.Shop.OrderAsync(r.OrderId!.Value);
        var start=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Run(string action) { await start.Task; try { await Shop(f.Buyer).ChangeOrderAsync(o.Id,o.Version,action,"Тест гонки",action=="extend"?o.ReserveUntil!.Value.AddHours(1):null,Guid.NewGuid(),true); return true; } catch(DbUpdateConcurrencyException){return false;} catch(ArgumentException){return false;} }
        var a=Run("cancel"); var b=Run("extend"); start.SetResult(); Assert.Single(await Task.WhenAll(a,b),x=>x);
        await using var db=await factory.CreateDbContextAsync(); var updated=await f.Shop.OrderAsync(o.Id); Assert.Equal(updated.Status==CustomerOrderStatus.Cancelled?3:4,await db.InventoryBalances.SumAsync(x=>x.Reserved)); Assert.Equal(2,await db.OrderEvents.CountAsync());
    }
    /// <summary>Два обработчика истечения не освобождают один резерв дважды.</summary>
    [Fact]
    public async Task CheckoutConcurrentExpiryIsIdempotent()
    {
        var f=await CheckoutFixture(); var clock=new OrderClock(); var shop=Shop(f.Buyer,clock:clock); await shop.AddAsync(f.Ids.RedS,1,Guid.NewGuid()); var p=await shop.PreviewAsync(f.Address); await shop.SubmitAsync(p.Id,Guid.NewGuid()); clock.Now=clock.Now.AddDays(2);
        var start=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); async Task<bool> Run(){await start.Task;return await Shop(f.Buyer,clock:clock).ExpireOneAsync();}
        var a=Run();var b=Run();start.SetResult();Assert.Single(await Task.WhenAll(a,b),x=>x);
        await using var db=await factory.CreateDbContextAsync();Assert.Equal(3,await db.InventoryBalances.SumAsync(x=>x.Reserved));
    }
    /// <summary>Очередь невидима до commit; сбой письма не повторяет бизнес-событие, последующий retry отправляет его.</summary>
    [Fact]
    public async Task CheckoutNotificationsCommitAndRetryWithoutDuplicateOrder()
    {
        var f=await CheckoutFixture(); await f.Shop.AddAsync(f.Ids.RedS,1,Guid.NewGuid());var p=await f.Shop.PreviewAsync(f.Address);await f.Shop.SubmitAsync(p.Id,Guid.NewGuid());
        var clock=new OrderClock();var mail=new CaptureMail {Fail=true};var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Email:PublicOrigin"]="https://localhost:7393"}).Build();var processor=new OrderNotificationProcessor(factory,mail,clock,config);
        Assert.True(await processor.ProcessOneAsync());Assert.Empty(mail.Messages);await using var db=await factory.CreateDbContextAsync();Assert.Single(await db.Orders.ToListAsync());Assert.Single(await db.OrderEvents.ToListAsync());
        mail.Fail=false;clock.Now=clock.Now.AddMinutes(3);Assert.True(await processor.ProcessOneAsync());Assert.Single(mail.Messages);Assert.False(await processor.ProcessOneAsync());
        await using var tx=await db.Database.BeginTransactionAsync();var o=await db.Orders.SingleAsync();var e=new OrderEvent{OrderId=o.Id,OperationId=Guid.NewGuid(),CommandFingerprint="test",Status=o.Status,Reason="Тест",ActorId="test"};db.Add(e);db.OrderNotifications.Add(new(){EventId=e.Id});await db.SaveChangesAsync();
        Assert.False(await processor.ProcessOneAsync());await tx.RollbackAsync();Assert.Single(mail.Messages);
    }
    /// <summary>Объединение не обрезает выбранное количество при превышении свободного склада.</summary>
    [Fact]
    public async Task CartMergePreservesOverflowUntilCustomerCorrectsIt()
    {
        var f = await CheckoutFixture(); var anonymous = new BuyerIdentity(new("", "", false));
        await Shop(anonymous,"overflow").AddAsync(f.Ids.RedS,5,Guid.NewGuid()); await f.Shop.AddAsync(f.Ids.RedS,5,Guid.NewGuid());
        var merged = Shop(f.Buyer,"overflow"); await merged.MergeAsync(); await merged.MergeAsync();
        var view = await merged.CartAsync(); Assert.Equal(10,Assert.Single(view.Lines).Quantity); Assert.Null(view.Total);
        await Assert.ThrowsAsync<ArgumentException>(()=>merged.PreviewAsync(f.Address));
    }
}
