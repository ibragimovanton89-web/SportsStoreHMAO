namespace SportsStore.Domain.Entities;
/// <summary>Отдельная очередь писем о заказах; не использует фиктивные решения об опте.</summary>
public sealed class OrderNotification : Entity
{
    /// <summary>Уникальное событие, намерение отправки которого создано в той же транзакции.</summary>
    public Guid EventId { get; set; }
    /// <summary>Время постановки, UTC.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Число начатых попыток; не более пяти.</summary>
    public int Attempts { get; set; }
    /// <summary>Ближайшее время повтора, UTC.</summary>
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Аренда обработчика, UTC; null вне обработки.</summary>
    public DateTimeOffset? LeaseUntil { get; set; } = null;
    /// <summary>Момент подтверждения отправки транспортом, UTC.</summary>
    public DateTimeOffset? SentAt { get; set; } = null;
    /// <summary>Безопасный код ошибки без адресов, писем и секретов.</summary>
    public string? LastErrorCode { get; set; } = null;
}
