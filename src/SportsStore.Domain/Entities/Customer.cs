namespace SportsStore.Domain.Entities;
/// <summary>Покупатели: правовой тип, коммерческий сегмент и независимое подтверждение права на опт.</summary>
public sealed class Customer : Entity
{
    /// <summary>Необязательная уникальная ссылка на учётную запись Identity; собственных паролей у покупателя нет.</summary>
    public string? ApplicationUserId { get; set; }
    /// <summary>Правовой тип: Individual — физлицо, SoleProprietor — ИП, Organization — организация.</summary>
    public CustomerKind Kind { get; set; }
    /// <summary>Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.</summary>
    public CustomerSegment Segment { get; set; }
    /// <summary>Подтверждение опта: NotRequested — не запрошен, Pending — проверяется, Approved — одобрен, Rejected — отклонён. Право проверяется сервером.</summary>
    public WholesaleStatus WholesaleStatus { get; set; }
    /// <summary>Отображаемое имя покупателя; может содержать персональные данные.</summary>
    public string DisplayName { get; set; } = "";
}

