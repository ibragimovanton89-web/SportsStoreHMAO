namespace SportsStore.Domain.Entities;
/// <summary>История опубликованных собственных цен с источником изменения.</summary>
public sealed class SalePriceHistory : Entity
{
    /// <summary>Идентификатор продаваемого варианта собственного каталога (ProductVariant).</summary>
    public Guid ProductVariantId { get; set; }
    /// <summary>Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.</summary>
    public CustomerSegment Segment { get; set; }
    /// <summary>Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.</summary>
    public decimal MinimumQuantity { get; set; }
    /// <summary>Предыдущая цена за единицу продажи магазина; NULL при первой публикации.</summary>
    public decimal? OldAmount { get; set; }
    /// <summary>Новая опубликованная цена за единицу продажи магазина.</summary>
    public decimal NewAmount { get; set; }
    /// <summary>Трёхбуквенный код валюты, например RUB (российский рубль).</summary>
    public string Currency { get; set; } = "RUB";
    /// <summary>Источник изменения цены: описание ручной установки или данные расчёта для аудита.</summary>
    public string Source { get; set; } = "";
    /// <summary>Момент изменения цены, UTC.</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

