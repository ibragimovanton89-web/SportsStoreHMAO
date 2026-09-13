using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Admin;
using SportsStore.Infrastructure.Services;
using Xunit;
namespace SportsStore.Tests;

/// <summary>Закупочные сценарии на настоящей изолированной PostgreSQL, включая приёмку и публичную карточку.</summary>
public sealed partial class AdminTests
{
    /// <summary>Создаёт сервис с проверкой тестового сотрудника по БД.</summary>
    private Purchasing Purchases() => new(factory, new AdminAccess(identity));
    /// <summary>Добавляет явно синтетический прайс: три цены 100/90/80, упаковка из шести единиц не разделяется.</summary>
    private async Task<(Guid Supplier, Guid Offer)> PurchaseFixture()
    {
        await using var db = await factory.CreateDbContextAsync(); var supplier = new Supplier { Code = "purchase-test", Name = "DEMO поставщик" }; db.Suppliers.Add(supplier);
        var batch = new ImportBatch { SupplierId = supplier.Id, FileName = "DEMO.xls", Sha256 = new string('a', 64), ParserVersion = "test/1", SourceDate = new DateOnly(2026, 7, 28) }; db.ImportBatches.Add(batch);
        var offer = new SupplierOffer { SupplierId = supplier.Id, ExternalCode = "0000042", SourceName = "DEMO воланы, 6 в упаковке", SupplierUnit = "упак", UnitsPerBox = 12, SourceSection = "DEMO", SourceDate = new DateOnly(2026, 7, 28) }; db.SupplierOffers.Add(offer);
        foreach (var (code, amount) in new[] { ("small", 100m), ("wholesale", 90m), ("large", 80m) })
        { var tier = new SupplierPriceTier { SupplierId = supplier.Id, Code = code, Name = code, MinimumAmount = 150000, Currency = "RUB" }; db.SupplierPriceTiers.Add(tier); db.SupplierOfferPrices.Add(new() { SupplierOfferId = offer.Id, SupplierPriceTierId = tier.Id, Amount = amount, ImportBatchId = batch.Id }); }
        await db.SaveChangesAsync(); return (supplier.Id, offer.Id);
    }
    /// <summary>Меняет этап с актуальной версией, как интерфейс после перечитывания.</summary>
    private async Task PurchaseStatusTo(Guid id, PurchaseStatus status) => await Purchases().ChangeStatusAsync(id, (await Purchases().OrderAsync(id)).Version, status, Guid.NewGuid());
    /// <summary>Создаёт заказ с явно демонстрационным нулевым минимальным порогом для проверки приёмки.</summary>
    private async Task<Guid> ReadyPurchase(Guid supplier, Guid offer, int quantity = 3)
    {
        var service = Purchases(); var tiers = await service.TiersAsync(supplier); await service.SaveTiersAsync(supplier, tiers.Select(t => t with { ManualMinimum = 0 }).ToArray(), Guid.NewGuid());
        var id = await service.StartAsync(supplier, Guid.NewGuid()); await service.SetQuantityAsync(id, (await service.OrderAsync(id)).Version, offer, quantity, Guid.NewGuid()); await PurchaseStatusTo(id, PurchaseStatus.Created); return id;
    }
    /// <summary>Проверяет три цены, счётчик, ручные пороги, недостающую сумму и неизменность снимка после смены прайса.</summary>
    [Fact]
    public async Task PurchaseCountersThresholdsAndHistoricalTierPrices()
    {
        var (supplier, offer) = await PurchaseFixture(); var s = Purchases(); var id = await s.StartAsync(supplier, Guid.NewGuid());
        var reopen = Guid.NewGuid(); Assert.Equal(id, await s.StartAsync(supplier, reopen)); var order = await s.OrderAsync(id);
        var op = Guid.NewGuid(); await s.SetQuantityAsync(id, order.Version, offer, 2, op); await s.SetQuantityAsync(id, order.Version, offer, 2, op);
        var row = Assert.Single((await s.PositionsAsync(id, new(), false)).Items); Assert.Equal("0000042", row.Code); Assert.Equal("упак", row.Unit); Assert.Equal(12, row.Box); Assert.Equal(100, row.Small); Assert.Equal(90, row.Wholesale); Assert.Equal(80, row.Large);
        order = await s.OrderAsync(id); Assert.Equal(200, order.Total); Assert.Equal(149800, order.Remaining);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => s.SetQuantityAsync(id, 0, offer, 3, Guid.NewGuid()));
        var tiers = await s.TiersAsync(supplier); await s.SaveTiersAsync(supplier, tiers.Select(t => t with { ManualMinimum = t.Code == "large" ? 500 : 1000 }).ToArray(), Guid.NewGuid());
        await s.ConfigureAsync(id, order.Version, tiers.Single(t => t.Code == "large").Id, order.WarehouseId, "DEMO", Guid.NewGuid());
        order = await s.OrderAsync(id); Assert.Equal(160, order.Total); Assert.Equal(340, order.Remaining);
        await using (var db = await factory.CreateDbContextAsync()) { foreach (var price in await db.SupplierOfferPrices.ToListAsync()) price.Amount = 999; await db.SaveChangesAsync(); }
        await s.SetQuantityAsync(id, order.Version, offer, 3, Guid.NewGuid()); Assert.Equal(240, (await s.OrderAsync(id)).Total);
        await PurchaseStatusTo(id, PurchaseStatus.Created); Assert.Equal(id, await s.StartAsync(supplier, reopen)); await Assert.ThrowsAsync<InvalidOperationException>(() => PurchaseStatusTo(id, PurchaseStatus.Submitted));
        order = await s.OrderAsync(id); await s.SetQuantityAsync(id, order.Version, offer, 7, Guid.NewGuid()); await PurchaseStatusTo(id, PurchaseStatus.Submitted);
        order = await s.OrderAsync(id); await Assert.ThrowsAsync<InvalidOperationException>(() => s.SetQuantityAsync(id, order.Version, offer, 8, Guid.NewGuid()));
        Assert.Single((await s.OrdersAsync(new("0000042"))).Items);
    }
    /// <summary>Частичная приёмка, повтор команды, перенос в карточку и следующая поставка не дублируют склад.</summary>
    [Fact]
    public async Task PartialReceivingCreatesStockOnceAndCardPublishesHistoricalMarkup()
    {
        var (supplier, offer) = await PurchaseFixture(); var s = Purchases(); var id = await ReadyPurchase(supplier, offer); await PurchaseStatusTo(id, PurchaseStatus.Submitted);
        var order = await s.OrderAsync(id); var row = Assert.Single((await s.PositionsAsync(id, new(), true)).Items); var op = Guid.NewGuid();
        await s.ReceiveAsync(id, order.Version, [new(row.LineId!.Value, 1)], op); await s.ReceiveAsync(id, order.Version, [new(row.LineId.Value, 1)], op);
        Assert.Equal(PurchaseStatus.PartiallyReceived, (await s.OrderAsync(id)).Status); Assert.Equal(1, Assert.Single((await s.UnallocatedAsync(new())).Items).Quantity);
        var brand = await Catalog().SaveLookupAsync(false, new(Guid.Empty, "DEMO бренд", 0), Guid.NewGuid()); var category = await Catalog().SaveLookupAsync(true, new(Guid.Empty, "DEMO категория", 0), Guid.NewGuid());
        var product = await s.CreateCardAsync(offer, "DEMO мой товар", brand, category, Guid.NewGuid()); Assert.Equal(product, await s.CreateCardAsync(offer, "DEMO", null, null, Guid.NewGuid()));
        Assert.Empty((await s.UnallocatedAsync(new())).Items); var owned = Assert.Single((await s.OwnedAsync(new())).Items); Assert.Equal(1, owned.Available); Assert.Equal(2, owned.Incoming); Assert.Equal("упак", owned.Unit);
        var retail = await s.RetailAsync(owned.VariantId); var cost = Assert.Single(retail.Costs); Assert.Equal(100, cost.UnitCost);
        await s.SetRetailAsync(owned.VariantId, retail.Version, cost.LineId, null, 12.345m, Guid.NewGuid()); Assert.Equal(112.35m, (await s.RetailAsync(owned.VariantId)).Amount);
        var storefront = new StorefrontService(factory); Assert.Null(await storefront.ProductAsync(product));
        await using (var db = await factory.CreateDbContextAsync()) { db.ProductImages.Add(new() { ProductId = product, Url = "/media/test.webp" }); await db.SaveChangesAsync(); }
        await Catalog().SetStatusAsync(product, (await Catalog().ProductAsync(product)).Version, ProductStatus.Published, Guid.NewGuid());
        Assert.Equal(112.35m, Assert.Single((await storefront.ProductAsync(product))!.Variants).Price); Assert.Single((await storefront.ListAsync("DEMO", 1)).Items);
        await s.ReceiveAsync(id, (await s.OrderAsync(id)).Version, null, Guid.NewGuid()); Assert.Equal(PurchaseStatus.Received, (await s.OrderAsync(id)).Status);
        owned = Assert.Single((await s.OwnedAsync(new())).Items); Assert.Equal(3, owned.Available); Assert.Equal(0, owned.Incoming);
        var next = await ReadyPurchase(supplier, offer, 2); await PurchaseStatusTo(next, PurchaseStatus.Submitted); await s.ReceiveAsync(next, (await s.OrderAsync(next)).Version, null, Guid.NewGuid()); Assert.Equal(5, Assert.Single((await s.OwnedAsync(new())).Items).Available);
        await using var verify = await factory.CreateDbContextAsync(); Assert.True((await verify.SalePrices.SingleAsync()).IsManual); Assert.Equal(3, await verify.PurchaseReceiptLines.CountAsync());
        Assert.Equal(owned.ProductId, await s.CreateCardAsync(offer, "Повторное открытие", null, null, Guid.NewGuid()));
    }
    /// <summary>Конкурирующие приёмки не превышают заказ; неверная строка откатывает всю операцию.</summary>
    [Fact]
    public async Task ConcurrentReceiptAndInvalidRowsCannotOverCreditStock()
    {
        var (supplier, offer) = await PurchaseFixture(); var s = Purchases(); var id = await ReadyPurchase(supplier, offer); await PurchaseStatusTo(id, PurchaseStatus.Submitted);
        var row = Assert.Single((await s.PositionsAsync(id, new(), true)).Items); var order = await s.OrderAsync(id);
        await Assert.ThrowsAsync<ArgumentException>(() => s.ReceiveAsync(id, order.Version, [new(row.LineId!.Value, 1), new(Guid.NewGuid(), 1)], Guid.NewGuid()));
        Assert.Empty((await s.UnallocatedAsync(new())).Items);
        var calls = Enumerable.Range(0, 2).Select(async _ => { try { await s.ReceiveAsync(id, order.Version, null, Guid.NewGuid()); return true; } catch (DbUpdateConcurrencyException) { return false; } }).ToArray();
        Assert.Single(await Task.WhenAll(calls), ok => ok); Assert.Equal(3, Assert.Single((await s.UnallocatedAsync(new())).Items).Quantity);
        await Assert.ThrowsAsync<InvalidOperationException>(() => PurchaseStatusTo(id, PurchaseStatus.Cancelled));
    }
    /// <summary>Manager собирает заказ, но не подтверждает приёмку, пороги или цену; невыбранный ассортимент не становится карточками.</summary>
    [Fact]
    public async Task PurchasingPermissionsAndUnselectedOfferAreEnforcedOnServer()
    {
        var (supplier, offer) = await PurchaseFixture(); var s = Purchases(); await Assert.ThrowsAsync<InvalidOperationException>(() => s.CreateCardAsync(offer, "DEMO", null, null, Guid.NewGuid()));
        var id = await ReadyPurchase(supplier, offer); await Role("Manager");
        var tiers = await s.TiersAsync(supplier); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => s.SaveTiersAsync(supplier, tiers, Guid.NewGuid()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => PurchaseStatusTo(id, PurchaseStatus.Submitted));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => s.ReceiveAsync(id, 0, null, Guid.NewGuid()));
        var product = await s.CreateCardAsync(offer, "DEMO", null, null, Guid.NewGuid()); var variant = Assert.Single(await Catalog().VariantsAsync(product));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => s.SetRetailAsync(variant.Id, 0, null, 150, null, Guid.NewGuid()));
    }
    /// <summary>Ошибка ограничения PostgreSQL откатывает приёмку, журнал и количество строки целиком.</summary>
    [Fact]
    public async Task PurchaseReceiptDatabaseFailureRollsBackEveryChange()
    {
        var (supplier, offer) = await PurchaseFixture(); var s = Purchases(); var id = await ReadyPurchase(supplier, offer, 1);
        var product = await s.CreateCardAsync(offer, "DEMO переполнение", null, null, Guid.NewGuid()); var variant = Assert.Single(await Catalog().VariantsAsync(product));
        await PurchaseStatusTo(id, PurchaseStatus.Submitted); var order = await s.OrderAsync(id); var op = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync()) { db.InventoryBalances.Add(new() { ProductVariantId = variant.Id, WarehouseId = order.WarehouseId, OnHand = 99999999999999.9999m, Reserved = 2 }); await db.SaveChangesAsync(); }
        await Assert.ThrowsAsync<DbUpdateException>(() => s.ReceiveAsync(id, order.Version, null, op));
        await using var verify = await factory.CreateDbContextAsync(); Assert.Empty(await verify.PurchaseReceiptLines.ToListAsync()); Assert.False(await verify.AdminAudits.AnyAsync(a => a.OperationId == op));
        Assert.Equal(0, (await verify.PurchaseOrderLines.SingleAsync()).ReceivedQuantity); Assert.Equal(2, (await verify.InventoryBalances.SingleAsync()).Reserved); Assert.Equal(order.Version, (await s.OrderAsync(id)).Version);
    }
    /// <summary>Неизвестная цена блокирует создание; неподтверждённые единицы блокируют наценку, но не ручную цену.</summary>
    [Fact]
    public async Task PurchaseUnknownPriceAndUnitsAreNeverGuessed()
    {
        var (supplier, offer) = await PurchaseFixture(); var s = Purchases();
        await using (var db = await factory.CreateDbContextAsync()) { var tier = await db.SupplierPriceTiers.SingleAsync(t => t.Code == "large"); db.SupplierOfferPrices.Remove(await db.SupplierOfferPrices.SingleAsync(p => p.SupplierPriceTierId == tier.Id)); await db.SaveChangesAsync(); }
        var id = await s.StartAsync(supplier, Guid.NewGuid()); var order = await s.OrderAsync(id); var tiers = await s.TiersAsync(supplier);
        await s.ConfigureAsync(id, order.Version, tiers.Single(t => t.Code == "large").Id, order.WarehouseId, null, Guid.NewGuid());
        await s.SetQuantityAsync(id, (await s.OrderAsync(id)).Version, offer, 1, Guid.NewGuid()); Assert.Equal(1, (await s.OrderAsync(id)).MissingPrices);
        await Assert.ThrowsAsync<InvalidOperationException>(() => PurchaseStatusTo(id, PurchaseStatus.Created));
        order = await s.OrderAsync(id); await s.ConfigureAsync(id, order.Version, tiers.Single(t => t.Code == "small").Id, order.WarehouseId, null, Guid.NewGuid()); await PurchaseStatusTo(id, PurchaseStatus.Created);
        var product = await s.CreateCardAsync(offer, "DEMO единицы", null, null, Guid.NewGuid()); var variant = Assert.Single(await Catalog().VariantsAsync(product)); var cost = Assert.Single((await s.RetailAsync(variant.Id)).Costs);
        // Имитируем неподтверждённые исторические данные напрямую в тестовой БД: публичной команды ручного сопоставления больше нет.
        await using (var legacy = await factory.CreateDbContextAsync()) { var row = await legacy.SupplierOffers.SingleAsync(x => x.Id == offer); row.SaleUnitsPerSupplierUnit = null; row.ConversionConfirmed = false; await legacy.SaveChangesAsync(); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.SetRetailAsync(variant.Id, 0, cost.LineId, null, 20, Guid.NewGuid()));
        await s.SetRetailAsync(variant.Id, 0, null, 125, null, Guid.NewGuid()); Assert.Equal(125, (await s.RetailAsync(variant.Id)).Amount);
    }
    /// <summary>Новый прайс не меняет единицу уже принятой упаковки; неверное распределение откатывается.</summary>
    [Fact]
    public async Task ReceivedUnitSnapshotSurvivesSupplierUnitChange()
    {
        var (supplier, offer) = await PurchaseFixture(); var s = Purchases(); var id = await ReadyPurchase(supplier, offer); await PurchaseStatusTo(id, PurchaseStatus.Submitted);
        var row = Assert.Single((await s.PositionsAsync(id, new(), true)).Items); await s.ReceiveAsync(id, (await s.OrderAsync(id)).Version, [new(row.LineId!.Value, 1)], Guid.NewGuid());
        await using (var db = await factory.CreateDbContextAsync()) { (await db.SupplierOffers.SingleAsync()).SupplierUnit = "шт"; await db.SaveChangesAsync(); }
        Assert.Equal("упак", Assert.Single((await s.UnallocatedAsync(new())).Items).Unit);
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.CreateCardAsync(offer, "DEMO", null, null, Guid.NewGuid()));
        await using var verify = await factory.CreateDbContextAsync(); Assert.Empty(await verify.Products.ToListAsync()); Assert.Equal(1, (await verify.UnallocatedStocks.SingleAsync()).Quantity);
    }
}
