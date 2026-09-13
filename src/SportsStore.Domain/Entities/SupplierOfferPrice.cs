namespace SportsStore.Domain.Entities;
/// <summary>Действующие закупочные цены предложений по тарифам поставщика.</summary>
public sealed class SupplierOfferPrice : Entity
{
    /// <summary>Идентификатор закупочного предложения поставщика (SupplierOffer).</summary>
    public Guid SupplierOfferId { get; set; }
    /// <summary>Идентификатор закупочного тарифа поставщика (SupplierPriceTier).</summary>
    public Guid SupplierPriceTierId { get; set; }
    /// <summary>Закупочная цена за одну единицу поставщика в валюте закупочного тарифа.</summary>
    public decimal Amount { get; set; }
    /// <summary>Идентификатор партии импорта прайса (ImportBatch).</summary>
    public Guid ImportBatchId { get; set; }
}

