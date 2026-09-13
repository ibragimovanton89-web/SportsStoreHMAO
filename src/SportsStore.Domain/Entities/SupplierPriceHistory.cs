namespace SportsStore.Domain.Entities;
/// <summary>История закупочных цен с указанием партии импорта, вызвавшей изменение.</summary>
public sealed class SupplierPriceHistory : Entity
{
    /// <summary>Идентификатор закупочного предложения поставщика (SupplierOffer).</summary>
    public Guid SupplierOfferId { get; set; }
    /// <summary>Идентификатор закупочного тарифа поставщика (SupplierPriceTier).</summary>
    public Guid SupplierPriceTierId { get; set; }
    /// <summary>Предыдущая закупочная цена за единицу поставщика; NULL при первом появлении цены.</summary>
    public decimal? OldAmount { get; set; }
    /// <summary>Новая закупочная цена за единицу поставщика в валюте тарифа.</summary>
    public decimal NewAmount { get; set; }
    /// <summary>Идентификатор партии импорта прайса (ImportBatch).</summary>
    public Guid ImportBatchId { get; set; }
    /// <summary>Момент изменения цены, UTC.</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

