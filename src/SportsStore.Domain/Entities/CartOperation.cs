namespace SportsStore.Domain.Entities;
/// <summary>Ключ повторяемого добавления в корзину с проверкой неизменности команды.</summary>
public sealed class CartOperation : Entity
{
    /// <summary>Корзина, к которой относилась команда.</summary>
    public Guid CartId { get; set; }
    /// <summary>Уникальный ключ добавления.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Идентификатор варианта команды.</summary>
    public Guid VariantId { get; set; }
    /// <summary>Добавляемое количество для проверки повторного запроса.</summary>
    public int Quantity { get; set; }
}
