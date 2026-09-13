namespace SportsStore.Domain.Entities;

/// <summary>Неизменяемая запись успешной административной операции; сохраняется в транзакции изменения без секретов и персональных реквизитов.</summary>
public sealed class AdminAudit : Entity
{
    /// <summary>Идентификатор сотрудника Identity либо обозначение доверенного локального оператора.</summary>
    public string ActorId { get; set; } = "";
    /// <summary>Момент успешного изменения, UTC.</summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    /// <summary>Стабильный код действия для поиска и расследования.</summary>
    public string Action { get; set; } = "";
    /// <summary>Тип изменённого бизнес-объекта.</summary>
    public string ObjectType { get; set; } = "";
    /// <summary>Идентификатор изменённого объекта; не содержит содержимого объекта.</summary>
    public string ObjectId { get; set; } = "";
    /// <summary>Краткое безопасное описание без адресов, реквизитов, паролей и токенов.</summary>
    public string Description { get; set; } = "";
    /// <summary>Основание решения; сотрудник не должен вводить здесь персональные данные.</summary>
    public string? Reason { get; set; }
    /// <summary>Идентификатор действия клиента для обнаружения повторной отправки.</summary>
    public Guid OperationId { get; set; }
}
