using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
namespace SportsStore.Infrastructure.Pricing;
/// <summary>Общий выбор сохранённой цены в контексте вызывающей операции, включая атомарное оформление.</summary>
internal static class SalePriceSelection
{
    /// <summary>Возвращает допустимые положительные RUB-ступени; публичность проверяется вызывающим кодом.</summary>
    internal static IQueryable<SalePrice> Eligible(ApplicationDbContext db, CustomerSegment segment) =>
        db.SalePrices.Where(x => x.Segment == segment && x.Currency == "RUB" && x.Amount > 0);
    /// <summary>Выбирает максимальную нижнюю границу для одного SKU без перехода между сегментами.</summary>
    internal static SalePrice? Choose(IEnumerable<SalePrice> prices, Guid variant, decimal quantity) =>
        prices.Where(x => x.ProductVariantId == variant && x.MinimumQuantity <= quantity).OrderByDescending(x => x.MinimumQuantity).FirstOrDefault();
}
