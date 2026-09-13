namespace SportsStore.Domain.Entities;

/// <summary>Состояние заявки; изменение сохраняется также отдельным решением в истории.</summary>
public enum WholesaleApplicationStatus
{
    /// <summary>Ожидает проверки неизменяемого снимка реквизитов.</summary>
    Pending,
    /// <summary>Одобрена администратором.</summary>
    Approved,
    /// <summary>Отклонена с публичной причиной.</summary>
    Rejected,
    /// <summary>Отозвана покупателем до решения.</summary>
    Withdrawn,
    /// <summary>Ранее предоставленное право отозвано.</summary>
    Revoked
}

/// <summary>Заявка покупателя на опт со снимком данных на момент подачи; не даёт права на цену до одобрения.</summary>
public sealed class WholesaleApplication : Entity
{
    /// <summary>Владелец заявки; определяется серверной сессией.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Состояние обработки заявки.</summary>
    public WholesaleApplicationStatus Status { get; set; }
    /// <summary>Правовой тип в момент подачи.</summary>
    public CustomerKind Kind { get; set; }
    /// <summary>Официальное название в момент подачи; null для физлица.</summary>
    public string? LegalName { get; set; }
    /// <summary>ИНН в момент подачи; null для физлица.</summary>
    public string? Inn { get; set; }
    /// <summary>КПП в момент подачи; null, если неприменим.</summary>
    public string? Kpp { get; set; }
    /// <summary>Обращение покупателя без внутренних заметок сотрудников.</summary>
    public string? Comment { get; set; }
    /// <summary>Момент подачи, UTC.</summary>
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Момент последнего решения, UTC; null до обработки.</summary>
    public DateTimeOffset? ReviewedAt { get; set; }
    /// <summary>Служебный идентификатор рассмотревшего сотрудника; не включается в DTO покупателя.</summary>
    public string? ReviewedBy { get; set; }
    /// <summary>Объяснение результата, доступное покупателю.</summary>
    public string? PublicReason { get; set; }
    /// <summary>Идентификатор команды подачи для безопасного повтора.</summary>
    public Guid OperationId { get; set; }
}
