using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Import;
using Xunit;
namespace SportsStore.Tests;

/// <summary>Классификация текста прайса не принимает смешанные бренды, линейки и страны за один бренд.</summary>
public sealed class SupplierFacetTests
{
    /// <summary>Проверяет границы названий, однозначные заголовки и исходные товарные группы.</summary>
    /// <param name="name">Название позиции.</param><param name="section">Раздел прайса.</param>
    /// <param name="brand">Ожидаемый бренд.</param><param name="category">Ожидаемая категория.</param>
    [Theory]
    [InlineData("Мяч adidas JD8036", "ADIDAS / Мячи футбольные и футзальные", "ADIDAS", "Мячи футбольные и футзальные")]
    [InlineData("Ракетка BABOLAT", "BABOLAT, NEVA, TECNIFIBRE / Ракетки", "BABOLAT", "Ракетки")]
    [InlineData("Мяч", "BABOLAT, NEVA, TECNIFIBRE / Мячи", "Не определён", "Мячи")]
    [InlineData("HEADBAND", "Аксессуары", "Не определён", "Аксессуары")]
    [InlineData("Мяч NIKE / PUMA", "Мячи", "Не определён", "Мячи")]
    [InlineData("Мяч", "ADIDAS / Мячи", "ADIDAS", "Мячи")]
    [InlineData("Коврик TORRES", "Power Line", "TORRES", "Power Line")]
    [InlineData("Шашки", "MADE IN RUSSIA", "Не определён", "Не определён")]
    [InlineData("Ракетка STIGA", "STIGA", "STIGA", "Не определён")]
    public void ClassifiesConservatively(string name, string section, string brand, string category)
    {
        var result = BallMarketFacets.Classify(name, section);
        Assert.Equal(brand, result.Brand); Assert.Equal(category, result.Category);
    }
}

/// <summary>Серверные фильтры применяются совместно, до пагинации, не изменяя сохранённый заказ.</summary>
public sealed partial class AdminTests
{
    /// <summary>Бренд, категория, текст и выбранные позиции пересекаются; скрытые строки остаются в итоговой сумме.</summary>
    [Fact]
    public async Task PurchaseFacetsFilterTogetherWithoutChangingOrder()
    {
        var (supplier, offer) = await PurchaseFixture();
        await using (var db = await factory.CreateDbContextAsync())
        {
            var first = await db.SupplierOffers.SingleAsync(x => x.Id == offer); first.SourceName = "Мяч ADIDAS"; first.SourceSection = "Мячи";
            db.SupplierOffers.Add(new() { SupplierId = supplier, ExternalCode = "002", SourceName = "Сумка ADIDAS", SourceSection = "Сумки", SupplierUnit = "шт", SourceDate = first.SourceDate });
            db.SupplierOffers.Add(new() { SupplierId = supplier, ExternalCode = "003", SourceName = "Мяч PUMA", SourceSection = "Мячи", SupplierUnit = "шт", SourceDate = first.SourceDate });
            await db.SaveChangesAsync();
        }
        var service = Purchases(); var order = await service.StartAsync(supplier, Guid.NewGuid());
        await service.SetQuantityAsync(order, (await service.OrderAsync(order)).Version, offer, 1, Guid.NewGuid());
        var before = await service.OrderAsync(order);
        var facets = await service.FacetsAsync(order);
        Assert.Equal(new[] { "ADIDAS", "PUMA" }, facets.Brands);
        Assert.Equal(2, (await service.PositionsAsync(order, new(), false, brand: "ADIDAS")).Total);
        var rows = await service.PositionsAsync(order, new(), false, brand: "ADIDAS", category: "Мячи");
        Assert.Equal(offer, Assert.Single(rows.Items).OfferId);
        Assert.Equal("ADIDAS", rows.Items[0].Brand);
        Assert.Empty((await service.PositionsAsync(order, new(), true, brand: "PUMA")).Items);
        Assert.Empty((await service.PositionsAsync(order, new("002"), false, brand: "PUMA")).Items);
        Assert.Empty((await service.PositionsAsync(order, new(), false, brand: "unknown-brand")).Items);
        var after = await service.OrderAsync(order);
        Assert.Equal(before.Total, after.Total); Assert.Equal(before.Version, after.Version);
        Assert.Equal(1, (await service.PositionsAsync(order, new(), true)).Total);
    }
}
