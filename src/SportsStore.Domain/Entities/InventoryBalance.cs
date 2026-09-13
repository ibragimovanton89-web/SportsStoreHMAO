namespace SportsStore.Domain.Entities;
/// <summary>Собственные остатки и резервы по варианту и складу. Импорт остатков поставщика их не изменяет.</summary>
public sealed class InventoryBalance : Entity
{
    /// <summary>Идентификатор продаваемого варианта собственного каталога (ProductVariant).</summary>
    public Guid ProductVariantId { get; set; }
    /// <summary>Идентификатор собственного склада (Warehouse).</summary>
    public Guid WarehouseId { get; set; }
    /// <summary>Физическое количество на собственном складе в единицах продажи; неотрицательное.</summary>
    public decimal OnHand { get; set; }
    /// <summary>Зарезервированное количество в единицах продажи; от 0 до OnHand. Доступно OnHand − Reserved.</summary>
    public decimal Reserved { get; set; }
}

