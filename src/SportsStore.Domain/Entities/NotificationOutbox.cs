namespace SportsStore.Domain.Entities;

/// <summary>Очередь уведомлений о решениях по опту; сохраняется с решением, не содержит токенов подтверждения или реквизитов.</summary>
public sealed class NotificationOutbox : Entity
{
    /// <summary>Решение, о котором отправляется уведомление; одно логическое письмо на решение.</summary>
    public Guid DecisionId { get; set; }
    /// <summary>Момент постановки в очередь, UTC.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Количество начатых попыток, включая прерванные процессом.</summary>
    public int Attempts { get; set; }
    /// <summary>Следующая допустимая попытка, UTC.</summary>
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Конец аренды обработчиком, UTC; null, когда запись свободна.</summary>
    public DateTimeOffset? LeaseUntil { get; set; }
    /// <summary>Момент успешной отправки, UTC; null до подтверждения транспорта.</summary>
    public DateTimeOffset? SentAt { get; set; }
    /// <summary>Безопасный код ошибки без адресов, секретов или текста SMTP-ответа.</summary>
    public string? LastErrorCode { get; set; }
}
