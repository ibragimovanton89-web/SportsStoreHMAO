namespace SportsStore.Domain.Entities;
/// <summary>Неизменяемая история переходов покупательского заказа и идемпотентных команд.</summary>
public sealed class OrderEvent : Entity
{
    /// <summary>Заказ, к которому относится событие.</summary>
    public Guid OrderId { get; set; }
    /// <summary>Уникальный ключ команды изменения.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Отпечаток типа команды и её аргументов для безопасного повтора.</summary>
    public string CommandFingerprint { get; set; } = "";
    /// <summary>Состояние после команды.</summary>
    public CustomerOrderStatus Status { get; set; }
    /// <summary>Срок резерва после команды, UTC.</summary>
    public DateTime? ReserveUntil { get; set; } = null;
    /// <summary>Время события, UTC.</summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    /// <summary>Служебный автор события; не выдаётся покупателю.</summary>
    public string ActorId { get; set; } = "";
    /// <summary>Публичное объяснение перехода без внутренних заметок.</summary>
    public string Reason { get; set; } = "";
}
