namespace SportsStore.Domain.Entities;
/// <summary>Основа заказов со снимками контактов, адреса и реквизитов на момент оформления.</summary>
public sealed class Order : Entity
{
    /// <summary>Необязательная ссылка на покупателя; исполнение и история используют снимки данных в заказе.</summary>
    public Guid? CustomerId { get; set; }
    /// <summary>Уникальный номер заказа магазина.</summary>
    public string Number { get; set; } = "";
    /// <summary>Момент создания записи, UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Трёхбуквенный код валюты, например RUB (российский рубль).</summary>
    public string Currency { get; set; } = "RUB";
    /// <summary>Фактически применённый формат продажи: Retail — розница, Wholesale — опт.</summary>
    public CustomerSegment SalesFormat { get; set; }
    /// <summary>Снимок имени контактного лица на момент заказа; персональные данные.</summary>
    public string ContactName { get; set; } = "";
    /// <summary>Снимок контактного адреса электронной почты на момент заказа; персональные данные.</summary>
    public string ContactEmail { get; set; } = "";
    /// <summary>Снимок контактного телефона на момент заказа; персональные данные.</summary>
    public string ContactPhone { get; set; } = "";
    /// <summary>Снимок адреса доставки на момент заказа; изменения адресов покупателя его не меняют.</summary>
    public string ShippingAddress { get; set; } = "";
    /// <summary>Снимок наименования организации или ИП на момент заказа; NULL, если неприменимо.</summary>
    public string? OrganizationName { get; set; }
    /// <summary>Снимок ИНН на момент заказа; NULL, если неприменимо.</summary>
    public string? Inn { get; set; }
    /// <summary>Снимок КПП на момент заказа; NULL, если неприменимо.</summary>
    public string? Kpp { get; set; }
    /// <summary>Исторические записи не получают фиктивного резерва.</summary>
    public CustomerOrderStatus Status { get; set; } = CustomerOrderStatus.Historical;
    /// <summary>Время последнего перехода, UTC.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Конечный срок активного резерва, UTC.</summary>
    public DateTime? ReserveUntil { get; set; } = null;
    /// <summary>Сумма сохранённых строк без неопределённой доставки.</summary>
    public decimal GoodsTotal { get; set; }
    /// <summary>Оформленная корзина; уникальна, null у исторических заказов.</summary>
    public Guid? CartId { get; set; } = null;
    /// <summary>Ключ оформления; null у исторических записей.</summary>
    public Guid? CheckoutOperationId { get; set; } = null;
    /// <summary>Подтверждённый серверный preview.</summary>
    public Guid? PreviewId { get; set; } = null;
    /// <summary>Отпечаток подтверждённых условий.</summary>
    public string? ConditionsFingerprint { get; set; } = null;
    /// <summary>Снимок получателя выбранного адреса.</summary>
    public string? RecipientName { get; set; } = null;
    /// <summary>Снимок индекса; null, если не указан.</summary>
    public string? PostalCode { get; set; } = null;
    /// <summary>Снимок правового типа покупателя.</summary>
    public CustomerKind CustomerKind { get; set; } = CustomerKind.Individual;
}

