namespace SportsStore.Domain.Entities;
/// <summary>Допустимые состояния без имитации оплаты и отгрузки.</summary>
public enum CustomerOrderStatus
{
    /// <summary>Прежняя запись без нового резерва.</summary>
    Historical,
    /// <summary>Ожидает сотрудника; товар зарезервирован.</summary>
    AwaitingConfirmation,
    /// <summary>Подтверждён сотрудником, но не оплачен и не отгружен.</summary>
    Confirmed,
    /// <summary>Отменён, собственный резерв освобождён.</summary>
    Cancelled,
    /// <summary>Срок резерва истёк.</summary>
    Expired,
}
