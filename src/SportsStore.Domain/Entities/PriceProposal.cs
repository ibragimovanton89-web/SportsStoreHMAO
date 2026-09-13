namespace SportsStore.Domain.Entities;
/// <summary>Предложения пересчёта цен; меняют опубликованные цены только после применения, если явно не включён автоматический режим.</summary>
public sealed class PriceProposal : Entity
{
    /// <summary>Идентификатор закупочного предложения поставщика (SupplierOffer).</summary>
    public Guid SupplierOfferId { get; set; }
    /// <summary>Идентификатор продаваемого варианта собственного каталога (ProductVariant).</summary>
    public Guid ProductVariantId { get; set; }
    /// <summary>Идентификатор партии импорта прайса (ImportBatch).</summary>
    public Guid? ImportBatchId { get; set; }
    /// <summary>Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.</summary>
    public CustomerSegment Segment { get; set; }
    /// <summary>Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.</summary>
    public decimal MinimumQuantity { get; set; }
    /// <summary>Предлагаемая цена за единицу продажи магазина после расчёта наценки и округления.</summary>
    public decimal Amount { get; set; }
    /// <summary>Трёхбуквенный код валюты, например RUB (российский рубль).</summary>
    public string Currency { get; set; } = "RUB";
    /// <summary>Отпечаток исходных параметров расчёта для обнаружения устаревшего предложения пересчёта.</summary>
    public string Fingerprint { get; set; } = "";
    /// <summary>Источник изменения цены: описание ручной установки или данные расчёта для аудита.</summary>
    public string Source { get; set; } = "";
    /// <summary>Статус пересчёта: Pending — ожидает применения, Applied — применён, Superseded — заменён или устарел.</summary>
    public ProposalStatus Status { get; set; }
    /// <summary>Момент создания записи, UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

