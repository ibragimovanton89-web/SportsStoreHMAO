using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using Xunit;
namespace SportsStore.Tests;

/// <summary>Удаление карточки проверяется на настоящем PostgreSQL, включая права, версии и сохранность связей.</summary>
public sealed partial class AdminTests
{
    /// <summary>Неиспользуемая карточка удаляется целиком, повтор идемпотентен; старый снимок и Manager не могут удалить её.</summary>
    [Fact]
    public async Task DeleteUnusedProductChecksVersionRoleAndRemovesAllVariantsOnce()
    {
        var id = await Catalog().SaveProductAsync(new(Guid.Empty, 0, "DEMO удаление", null, null, null, ProductStatus.Draft), Guid.NewGuid());
        var old = await Catalog().ProductAsync(id);
        await Catalog().SaveVariantAsync(new(Guid.Empty, 0, id, "DELETE-1", "шт", null, null, null, null), Guid.NewGuid());
        await Catalog().SaveVariantAsync(new(Guid.Empty, 0, id, "DELETE-2", "шт", null, null, null, null), Guid.NewGuid());
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Catalog().DeleteProductAsync(id, old.Version, Guid.NewGuid()));
        var current = await Catalog().ProductAsync(id);
        await Role("Manager");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Catalog().DeleteProductAsync(id, current.Version, Guid.NewGuid()));
        await Role("Admin");
        var operation = Guid.NewGuid();
        await Catalog().DeleteProductAsync(id, current.Version, operation);
        await Catalog().DeleteProductAsync(id, current.Version, operation);
        await using var db = await factory.CreateDbContextAsync();
        Assert.False(await db.Products.AnyAsync(x => x.Id == id));
        Assert.Empty(await db.ProductVariants.ToListAsync());
        Assert.Single(await db.AdminAudits.Where(x => x.OperationId == operation).ToListAsync());
    }
    /// <summary>Связанный с закупкой товар остаётся на месте после отказа, включая варианты и связь поставщика.</summary>
    [Fact]
    public async Task DeletePurchasedProductPreservesLinksAndPriceHistory()
    {
        var (supplier, offer) = await PurchaseFixture();
        await ReadyPurchase(supplier, offer);
        var id = await Purchases().CreateCardAsync(offer, "DEMO сохранить", null, null, Guid.NewGuid());
        var current = await Catalog().ProductAsync(id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Catalog().DeleteProductAsync(id, current.Version, Guid.NewGuid()));
        var variant = Assert.Single(await Catalog().VariantsAsync(id));
        await using var db = await factory.CreateDbContextAsync();
        Assert.Equal(variant.Id, (await db.SupplierOffers.SingleAsync(x => x.Id == offer)).ProductVariantId);
        Assert.True(await db.Products.AnyAsync(x => x.Id == id));
    }
    /// <summary>Остаток с резервом нельзя списать удалением; после обнуления остатка история цены также защищена.</summary>
    [Fact]
    public async Task DeleteProductDoesNotEraseStockOrPriceHistory()
    {
        var id = await Catalog().SaveProductAsync(new(Guid.Empty, 0, "DEMO остаток", null, null, null, ProductStatus.Draft), Guid.NewGuid());
        var variant = await Catalog().SaveVariantAsync(new(Guid.Empty, 0, id, "DELETE-STOCK", "шт", null, null, null, null), Guid.NewGuid());
        await using var db = await factory.CreateDbContextAsync();
        var warehouse = new Warehouse { Name = "DEMO склад" }; db.Warehouses.Add(warehouse);
        var balance = new InventoryBalance { ProductVariantId = variant, WarehouseId = warehouse.Id, OnHand = 1, Reserved = 1 };
        db.InventoryBalances.Add(balance); await db.SaveChangesAsync();
        var product = await Catalog().ProductAsync(id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Catalog().DeleteProductAsync(id, product.Version, Guid.NewGuid()));
        await db.Entry(balance).ReloadAsync(); Assert.Equal(1m, balance.OnHand); Assert.Equal(1m, balance.Reserved);
        balance.OnHand = balance.Reserved = 0; await db.SaveChangesAsync();
        await Purchases().SetRetailAsync(variant, 0, null, 100, null, Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() => Catalog().DeleteProductAsync(id, product.Version, Guid.NewGuid()));
        Assert.Single(await db.SalePriceHistories.Where(x => x.ProductVariantId == variant).ToListAsync());
        Assert.True(await db.Products.AnyAsync(x => x.Id == id));
    }
}
