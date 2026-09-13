namespace SportsStore.Domain.Entities;
/// <summary>Серверная корзина одного покупателя либо гостевого секрета; преобразованная корзина больше не редактируется.</summary>
public sealed class Cart : Entity
{
    /// <summary>Владелец-покупатель; null только у гостя.</summary>
    public Guid? CustomerId { get; set; } = null;
    /// <summary>SHA-256 случайного гостевого секрета; сам секрет в БД не хранится.</summary>
    public string? GuestKeyHash { get; set; } = null;
    /// <summary>Состояние активной, объединённой или оформленной корзины.</summary>
    public CartState State { get; set; } = CartState.Active;
    /// <summary>Время создания, UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Время изменения состава, UTC.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
