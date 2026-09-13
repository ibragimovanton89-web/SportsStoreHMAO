namespace SportsStore.Domain.Entities;
/// <summary>Серверные условия оформления; персональные данные доступны только владельцу.</summary>
public sealed class CheckoutPreview : Entity
{
    /// <summary>Покупатель, подтвердивший условия.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Корзина, состав которой проверялся.</summary>
    public Guid CartId { get; set; }
    /// <summary>Выбранный собственный адрес для повторной проверки.</summary>
    public Guid AddressId { get; set; }
    /// <summary>Версия корзины при подготовке; сравнение фактических условий имеет приоритет.</summary>
    public uint CartVersion { get; set; }
    /// <summary>Неизменяемый снимок подтверждаемых условий, включая контакты; не для журналов.</summary>
    public string ConditionsJson { get; set; } = "";
    /// <summary>SHA-256 нормализованных условий, не самостоятельное средство авторизации.</summary>
    public string Fingerprint { get; set; } = "";
    /// <summary>Срок действия подтверждения, UTC.</summary>
    public DateTime ExpiresAt { get; set; }
    /// <summary>Время создания, UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
