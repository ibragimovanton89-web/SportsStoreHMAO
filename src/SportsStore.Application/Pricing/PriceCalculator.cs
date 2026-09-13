using SportsStore.Domain.Entities;
namespace SportsStore.Application.Pricing;
/// <summary>Чистые правила расчёта цен и выбора наценки; не обращается к базе данных.</summary>
public static class PriceCalculator
{
    /// <summary>Рассчитывает закупочную стоимость в единице магазина с наценкой; округляет до двух знаков AwayFromZero. Неподтверждённый перевод единиц запрещён.</summary>
    /// <param name="purchase">Закупочная цена за одну единицу поставщика.</param>
    /// <param name="conversion">Число единиц продажи магазина в одной единице цены поставщика; null, если неизвестно.</param>
    /// <param name="confirmed">Подтверждено ли соответствие единиц поставщика единицам продажи.</param>
    /// <param name="markup">Процент наценки к закупочной стоимости, не маржа.</param>
    /// <returns>Цена за единицу магазина с наценкой, округлённая до копеек.</returns>
    /// <exception cref="InvalidOperationException">Перевод единиц отсутствует, неположителен или не подтверждён.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Закупочная цена неположительна либо наценка отрицательна.</exception>
    public static decimal Calculate(decimal purchase, decimal? conversion, bool confirmed, decimal markup)
    {
        if (!confirmed || conversion is null or <= 0)
            throw new InvalidOperationException("Unit conversion is not confirmed.");
        if (purchase <= 0 || markup < 0) throw new ArgumentOutOfRangeException(nameof(purchase));
        return decimal.Round(purchase / conversion.Value * (1 + markup / 100m), 2, MidpointRounding.AwayFromZero);
    }
    // Сначала выбирается область действия с ближайшей категорией; отсутствие ступени не переключает на менее приоритетную область.
    /// <summary>Выбирает область вариант → ближайшая категория → общее правило, затем максимальную подходящую нижнюю границу количества. Если ступени нет, возвращает null без перехода в более слабую область.</summary>
    /// <param name="rules">Доступные правила наценки.</param>
    /// <param name="variantId">Идентификатор варианта собственного каталога.</param>
    /// <param name="categoryPath">Категории от собственной категории товара к корню; порядок определяет приоритет.</param>
    /// <param name="segment">Коммерческий сегмент цены: розница или опт.</param>
    /// <param name="quantity">Количество одного SKU в единицах продажи магазина.</param>
    /// <returns>Наиболее приоритетное подходящее правило или null, если ступень не определена.</returns>
    public static MarkupRule? SelectRule(IEnumerable<MarkupRule> rules, Guid variantId,
        IReadOnlyList<Guid> categoryPath, CustomerSegment segment, decimal quantity)
    {
        var eligible = rules.Where(r => r.Segment == segment).ToArray();
        var scope = eligible.Where(r => r.ProductVariantId == variantId).ToArray();
        if (scope.Length == 0)
        {
            foreach (var categoryId in categoryPath)
            {
                scope = eligible.Where(r => r.ProductVariantId == null && r.CategoryId == categoryId).ToArray();
                if (scope.Length > 0) break;
            }
        }
        if (scope.Length == 0) scope = eligible.Where(r => r.ProductVariantId == null && r.CategoryId == null).ToArray();
        return scope.Where(r => r.MinimumQuantity <= quantity).OrderByDescending(r => r.MinimumQuantity).FirstOrDefault();
    }
    /// <summary>Возвращает опт только для оптового сегмента со статусом Approved; для остальных покупателей и гостей возвращает розницу.</summary>
    /// <param name="customer">Покупатель из доверенного серверного источника; null для гостя.</param>
    public static CustomerSegment AvailableSegment(Customer? customer) =>
        customer is { Segment: CustomerSegment.Wholesale, WholesaleStatus: WholesaleStatus.Approved }
            ? CustomerSegment.Wholesale : CustomerSegment.Retail;
}
/// <summary>Доступная сервером цена за единицу и применённая количественная ступень; минимум оптового заказа проверяется отдельно.</summary>
/// <param name="Amount">Цена за одну единицу продажи магазина.</param>
/// <param name="Currency">Трёхбуквенный код валюты; текущий импорт поддерживает RUB.</param>
/// <param name="Segment">Коммерческий сегмент, для которого рассчитана или выбрана цена.</param>
/// <param name="MinimumQuantity">Нижняя включительная граница применённой количественной ступени.</param>
/// <param name="MinimumWholesaleOrder">Совместимость прежнего контракта; после удаления коммерческих настроек всегда null, общий минимум не применяется.</param>
public sealed record PriceQuote(decimal Amount, string Currency, CustomerSegment Segment, decimal MinimumQuantity,
    decimal? MinimumWholesaleOrder);
/// <summary>Серверный контракт расчёта, публикации и получения разрешённых покупателю цен.</summary>
public interface IPricingService
{
    /// <summary>Создаёт предложения пересчёта для сопоставленной позиции в сериализуемой транзакции; публикация зависит от явной настройки.</summary>
    /// <param name="offerId">Идентификатор закупочного предложения поставщика.</param>
    /// <param name="batchId">Идентификатор партии импорта; в расчёте цены может отсутствовать.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    Task<IReadOnlyList<Guid>> ProposeAsync(Guid offerId, Guid? batchId = null, CancellationToken ct = default);
    /// <summary>Применяет актуальное предложение пересчёта в отдельной транзакции; повторное применение безопасно, ручная цена защищена.</summary>
    /// <param name="proposalId">Идентификатор проверяемого предложения пересчёта.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    Task ApplyAsync(Guid proposalId, CancellationToken ct = default);
    /// <summary>Устанавливает фиксированную цену с историей; проверяет сумму и ступень и защищает результат от автоматического пересчёта.</summary>
    /// <param name="variantId">Идентификатор варианта собственного каталога.</param>
    /// <param name="segment">Коммерческий сегмент цены: розница или опт.</param>
    /// <param name="minimumQuantity">Нижняя включительная граница количественной ступени одного SKU.</param>
    /// <param name="amount">Цена за единицу продажи магазина в рублях.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    Task SetManualAsync(Guid variantId, CustomerSegment segment, decimal minimumQuantity, decimal amount, CancellationToken ct = default);
    /// <summary>Определяет сегмент по доверенному идентификатору вошедшего пользователя и выбирает ступень. При отсутствии цены или настроенного оптового минимума выдаёт ошибку, не подменяя опт розницей.</summary>
    /// <param name="authenticatedUserId">Идентификатор из серверного контекста аутентификации; нельзя доверять произвольному значению клиента. null означает гостя.</param>
    /// <param name="variantId">Идентификатор варианта собственного каталога.</param>
    /// <param name="quantity">Количество одного SKU в единицах продажи магазина.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    Task<PriceQuote> QuoteAsync(string? authenticatedUserId, Guid variantId, decimal quantity, CancellationToken ct = default);
}

