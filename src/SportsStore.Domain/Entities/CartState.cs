namespace SportsStore.Domain.Entities;
/// <summary>Допустимые состояния без имитации оплаты и отгрузки.</summary>
public enum CartState
{
    /// <summary>Можно редактировать.</summary>
    Active,
    /// <summary>Однократно объединена с покупательской.</summary>
    Merged,
    /// <summary>Оформлена в заказ.</summary>
    Converted,
}
