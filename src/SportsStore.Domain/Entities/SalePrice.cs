namespace SportsStore.Domain.Entities;
/// <summary>Опубликованные цены продажи за единицу магазина по сегменту и количественной ступени. Ручные цены защищены от пересчёта.</summary>
public sealed class SalePrice : Entity
{
    /// <summary>Идентификатор продаваемого варианта собственного каталога (ProductVariant).</summary>
    public Guid ProductVariantId { get; set; }
    /// <summary>Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.</summary>
    public CustomerSegment Segment { get; set; }
    /// <summary>Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.</summary>
    public decimal MinimumQuantity { get; set; } = 1;
    /// <summary>Опубликованная цена за единицу продажи магазина в указанной валюте.</summary>
    public decimal Amount { get; set; }
    /// <summary>Трёхбуквенный код валюты, например RUB (российский рубль).</summary>
    public string Currency { get; set; } = "RUB";
    /// <summary>Цена установлена вручную и защищена от автоматического пересчёта при импорте.</summary>
    public bool IsManual { get; set; }
    /// <summary>Источник изменения цены: описание ручной установки или данные расчёта для аудита.</summary>
    public string Source { get; set; } = "";
    /// <summary>Момент публикации действующей цены, UTC.</summary>
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}

