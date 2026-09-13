namespace SportsStore.Domain.Entities;
/// <summary>Жизненный цикл закупки у поставщика; количество на складе меняет только приёмка.</summary>
public enum PurchaseStatus
{
    /// <summary>Список подбирается; ещё не создан заказ.</summary>
    Draft,
    /// <summary>Заказ создан, состав можно редактировать.</summary>
    Created,
    /// <summary>Оператор отметил передачу поставщику; автоматической отправки нет.</summary>
    Submitted,
    /// <summary>Поставка находится в пути.</summary>
    InTransit,
    /// <summary>Принята часть количества; оставшееся ожидается.</summary>
    PartiallyReceived,
    /// <summary>Все строки полностью приняты на склад.</summary>
    Received,
    /// <summary>Закупка отменена до первой приёмки.</summary>
    Cancelled
}
