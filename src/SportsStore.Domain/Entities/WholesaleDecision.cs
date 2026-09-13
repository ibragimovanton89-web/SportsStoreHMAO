namespace SportsStore.Domain.Entities;

/// <summary>Неизменяемая история предоставления и отзыва опта, включая решения через прежнюю форму администратора.</summary>
public sealed class WholesaleDecision : Entity
{
    /// <summary>Покупатель, чьи условия изменены.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Заявка-основание; null при прямом решении администратора без заявки.</summary>
    public Guid? ApplicationId { get; set; }
    /// <summary>Результат решения или отзыва.</summary>
    public WholesaleApplicationStatus Outcome { get; set; }
    /// <summary>Момент события, UTC.</summary>
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Доверенный идентификатор автора решения; скрыт из покупательского DTO.</summary>
    public string ActorId { get; set; } = "";
    /// <summary>Публичное объяснение без внутренних заметок.</summary>
    public string PublicReason { get; set; } = "";
    /// <summary>Уникальная команда изменения, не применяемая повторно.</summary>
    public Guid OperationId { get; set; }
}
