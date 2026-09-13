using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using Xunit;
namespace SportsStore.Tests;

/// <summary>Генерация внутренних кодов не создаёт варианты заранее и соблюдает права редактирования.</summary>
public sealed partial class AdminTests
{
    /// <summary>Разные вызовы предлагают разные коды, код сохраняется стандартной командой, повторный SKU отклоняется.</summary>
    [Fact]
    public async Task GeneratedSkuIsPersistedOnlyOnSaveAndCannotBeDuplicated()
    {
        var id = await Catalog().SaveProductAsync(new(Guid.Empty, 0, "DEMO генерация", null, null, null, ProductStatus.Draft), Guid.NewGuid());
        var first = await Catalog().GenerateSkuAsync(id);
        var second = await Catalog().GenerateSkuAsync(id);
        Assert.Matches("^SS-[0-9A-F]{16}$", first);
        Assert.NotEqual(first, second);
        Assert.Empty(await Catalog().VariantsAsync(id));
        var data = new SportsStore.Application.Admin.VariantData(Guid.Empty, 0, id, first, "шт", null, null, null, null);
        await Catalog().SaveVariantAsync(data, Guid.NewGuid());
        Assert.Equal(first, Assert.Single(await Catalog().VariantsAsync(id)).Sku);
        await Assert.ThrowsAsync<ArgumentException>(() => Catalog().SaveVariantAsync(data, Guid.NewGuid()));
        await Role("Customer");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Catalog().GenerateSkuAsync(id));
    }
}

