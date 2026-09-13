namespace SportsStore.Domain.Entities;
/// <summary>Неизменяемая запись приёмки; повтор той же операции не увеличивает остаток повторно.</summary>
public sealed class PurchaseReceiptLine : Entity
{
    /// <summary>Строка закупки, по которой принято количество.</summary>
    public Guid PurchaseOrderLineId { get; set; }
    /// <summary>Склад фактического поступления.</summary>
    public Guid WarehouseId { get; set; }
    /// <summary>Идентификатор приёмки; уникален вместе со строкой закупки.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Принято в этой операции, в единицах поставщика.</summary>
    public int Quantity { get; set; }
    /// <summary>Вариант, получивший остаток; null означает приёмку до создания карточки.</summary>
    public Guid? ProductVariantId { get; set; }
    /// <summary>Зафиксированный перевод единиц при зачислении варианту; null для нераспределённого остатка.</summary>
    public decimal? Conversion { get; set; }
    /// <summary>Момент приёмки, UTC.</summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
