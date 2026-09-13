namespace SportsStore.Domain.Entities;
/// <summary>Выбранное количество одного варианта; цена проверяется заново и здесь не хранится.</summary>
public sealed class CartItem : Entity
{
    /// <summary>Корзина-владелец строки.</summary>
    public Guid CartId { get; set; }
    /// <summary>Выбранный вариант; связь сохраняет недоступную строку для явного удаления.</summary>
    public Guid ProductVariantId { get; set; }
    /// <summary>Целое количество; после объединения превышение лимита требует исправления покупателем.</summary>
    public decimal Quantity { get; set; }
}
