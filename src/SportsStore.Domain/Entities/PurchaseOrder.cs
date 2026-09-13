namespace SportsStore.Domain.Entities;
/// <summary>Закупочный заказ магазина поставщику; не является заказом покупателя.</summary>
public sealed class PurchaseOrder : Entity
{
    /// <summary>Номер закупки для сотрудника.</summary>
    public string Number { get; set; } = "";
    /// <summary>Поставщик закупки.</summary>
    public Guid SupplierId { get; set; }
    /// <summary>Склад назначения приёмки.</summary>
    public Guid WarehouseId { get; set; }
    /// <summary>Выбранный закупочный тариф.</summary>
    public Guid SupplierPriceTierId { get; set; }
    /// <summary>Снимок кода тарифа: small, wholesale или large.</summary>
    public string TierCode { get; set; } = "";
    /// <summary>Снимок названия выбранного тарифа.</summary>
    public string TierName { get; set; } = "";
    /// <summary>Снимок порога выбранного тарифа в рублях; null означает неизвестный порог.</summary>
    public decimal? MinimumAmount { get; set; }
    /// <summary>Валюта закупки, RUB.</summary>
    public string Currency { get; set; } = "RUB";
    /// <summary>Идентификатор сотрудника, собравшего заказ.</summary>
    public string CreatedBy { get; set; } = "";
    /// <summary>Этап закупки; поступление оформляется отдельной транзакционной командой.</summary>
    public PurchaseStatus Status { get; set; }
    /// <summary>Примечание оператора без секретов.</summary>
    public string? Note { get; set; }
    /// <summary>Момент начала сборки закупки, UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Момент последнего изменения, UTC.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
