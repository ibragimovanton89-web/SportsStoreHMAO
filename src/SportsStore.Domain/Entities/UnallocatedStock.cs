namespace SportsStore.Domain.Entities;
/// <summary>Фактически полученный товар до сопоставления с карточкой; при создании карточки переносится в InventoryBalance.</summary>
public sealed class UnallocatedStock : Entity
{
    /// <summary>Единица фактически принятого товара; не меняется вместе с новым прайсом. Пусто только для старых несверенных записей.</summary>
    public string SupplierUnit { get; set; } = "";
    /// <summary>Полученная позиция поставщика.</summary>
    public Guid SupplierOfferId { get; set; }
    /// <summary>Склад хранения.</summary>
    public Guid WarehouseId { get; set; }
    /// <summary>Количество в единицах поставщика, ещё не зачисленное варианту; перенос обнуляет этот остаток.</summary>
    public decimal Quantity { get; set; }
}
