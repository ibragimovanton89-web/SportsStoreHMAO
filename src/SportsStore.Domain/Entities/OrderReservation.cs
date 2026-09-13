namespace SportsStore.Domain.Entities;
/// <summary>Распределение резерва конкретной строки заказа по собственному складу.</summary>
public sealed class OrderReservation : Entity
{
    /// <summary>Заказ-владелец резерва.</summary>
    public Guid OrderId { get; set; }
    /// <summary>Строка, для которой выделено количество.</summary>
    public Guid OrderItemId { get; set; }
    /// <summary>Вариант резервируемого товара.</summary>
    public Guid ProductVariantId { get; set; }
    /// <summary>Склад, на котором увеличен Reserved.</summary>
    public Guid WarehouseId { get; set; }
    /// <summary>Положительное зарезервированное количество в единицах продажи.</summary>
    public decimal Quantity { get; set; }
    /// <summary>True до однократного освобождения.</summary>
    public bool Active { get; set; } = true;
    /// <summary>Момент резервирования, UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Момент освобождения, UTC; null у активного резерва.</summary>
    public DateTime? ReleasedAt { get; set; } = null;
}
