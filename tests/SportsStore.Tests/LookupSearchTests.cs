using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using Xunit;

namespace SportsStore.Tests;

/// <summary>Проверяет серверный поиск справочников и чтение сохранённого выбора вне страницы подсказок.</summary>
public sealed partial class AdminTests
{
    /// <summary>Начало названия ищется без учёта регистра; поиск ограничен страницей, но выбранная запись доступна по ID.</summary>
    [Fact]
    public async Task LookupSearchUsesCaseInsensitivePrefixAndResolvesOffPageSelection()
    {
        await using var db = await factory.CreateDbContextAsync();
        var brands = Enumerable.Range(0, 30).Select(i => new Brand { Name = $"Adidas {i:00}" }).ToArray();
        db.Brands.AddRange(brands);
        db.Brands.Add(new Brand { Name = "Other Adidas" });
        var category = new Category { Name = "Мячи" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var result = await Catalog().LookupsAsync(false, new(" adi "));
        Assert.Equal(30, result.Total);
        Assert.Equal(25, result.Items.Count);
        Assert.DoesNotContain(result.Items, x => x.Name == "Other Adidas");
        Assert.Equal("Adidas 29", (await Catalog().LookupAsync(false, brands[29].Id))!.Name);
        Assert.Single((await Catalog().LookupsAsync(true, new("мЯ"))).Items);
        Assert.Null(await Catalog().LookupAsync(false, category.Id));
        Assert.Empty((await Catalog().LookupsAsync(false, new("missing"))).Items);
    }
}
