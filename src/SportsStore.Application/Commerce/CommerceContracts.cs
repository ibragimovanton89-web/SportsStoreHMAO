using SportsStore.Domain.Entities;
namespace SportsStore.Application.Commerce;
/// <summary>Строка текущей корзины; скрытый вариант не раскрывает сведения каталога.</summary>
/// <param name="VariantId">Выбранный вариант.</param>
/// <param name="ProductId">Публичная карточка; null у скрытой позиции.</param>
/// <param name="Name">Публичное название или безопасная заглушка.</param>
/// <param name="Sku">SKU только доступной карточки.</param>
/// <param name="Unit">Единица продажи.</param>
/// <param name="Quantity">Желаемое целое количество.</param>
/// <param name="Available">Собственный свободный остаток.</param>
/// <param name="Price">Текущая цена; null блокирует оформление.</param>
/// <param name="Tier">Применённая количественная ступень.</param>
/// <param name="Error">Причина невозможности оформления; null при готовности.</param>
/// <param name="Size">Подтверждаемый размер варианта.</param>
/// <param name="Color">Подтверждаемый цвет варианта.</param>
public sealed record CartLine(Guid VariantId, Guid? ProductId, string Name, string Sku, string? Size, string? Color, string Unit, decimal Quantity, decimal Available, decimal? Price, decimal? Tier, string? Error);
/// <summary>Пересчитанная корзина без доверия к клиентской цене.</summary>
/// <param name="Id">Корзина; пустой Guid до первого добавления.</param>
/// <param name="Version">Версия состава для редактирования.</param>
/// <param name="Segment">Сегмент доверенного покупателя либо Retail гостя.</param>
/// <param name="Lines">Строки, включая требующие исправления.</param>
/// <param name="Total">Сумма товаров; null при ошибке строки.</param>
public sealed record CartView(Guid Id, uint Version, CustomerSegment Segment, IReadOnlyList<CartLine> Lines, decimal? Total);
/// <summary>Серверное подтверждение условий с ограниченным сроком.</summary>
/// <param name="Id">Идентификатор preview.</param>
/// <param name="ExpiresAt">Срок подтверждения, UTC.</param>
/// <param name="Cart">Состав и текущие цены.</param>
/// <param name="ContactName">Имя контактного лица.</param>
/// <param name="Email">Подтверждённая почта.</param>
/// <param name="Phone">Контактный телефон.</param>
/// <param name="Recipient">Получатель выбранного адреса.</param>
/// <param name="Address">Снимок адреса.</param>
/// <param name="PostalCode">Индекс, если указан.</param>
/// <param name="Organization">Реквизиты юридического покупателя.</param>
/// <param name="Inn">ИНН, если применим.</param>
/// <param name="Kpp">КПП, если применим.</param>
public sealed record PreviewView(Guid Id, DateTime ExpiresAt, CartView Cart, string ContactName, string Email, string Phone, string Recipient, string Address, string? PostalCode, string? Organization, string? Inn, string? Kpp);
/// <summary>Результат оформления либо новые условия для отдельного подтверждения.</summary>
/// <param name="OrderId">Созданный заказ; null, если требуется подтверждение.</param>
/// <param name="NewPreviewId">Новый preview при изменении условий.</param>
/// <param name="Message">Пояснение необходимости повторного подтверждения.</param>
public sealed record CheckoutResult(Guid? OrderId, Guid? NewPreviewId, string? Message);
/// <summary>Неизменяемый снимок заказанной позиции.</summary>
/// <param name="Sku">Снимок SKU.</param>
/// <param name="Name">Снимок названия.</param>
/// <param name="Size">Снимок размера.</param>
/// <param name="Color">Снимок цвета.</param>
/// <param name="Unit">Единица продажи.</param>
/// <param name="Quantity">Заказанное количество.</param>
/// <param name="UnitPrice">Цена до скидки.</param>
/// <param name="UnitDiscount">Скидка единицы; новые заказы без скидки.</param>
/// <param name="Tier">Применённая ступень.</param>
public sealed record OrderLineView(string Sku, string Name, string? Size, string? Color, string Unit, decimal Quantity, decimal UnitPrice, decimal UnitDiscount, decimal Tier);
/// <summary>Публичная история заказа без служебного автора.</summary>
/// <param name="At">Время UTC.</param>
/// <param name="Status">Статус после действия.</param>
/// <param name="ReserveUntil">Срок после действия UTC.</param>
/// <param name="Reason">Публичная причина.</param>
public sealed record OrderHistoryView(DateTime At, CustomerOrderStatus Status, DateTime? ReserveUntil, string Reason);
/// <summary>Распределение по складам только для сотрудника.</summary>
/// <param name="Sku">SKU строки.</param>
/// <param name="Warehouse">Название склада.</param>
/// <param name="Quantity">Количество.</param>
/// <param name="Active">Действующий резерв.</param>
public sealed record ReservationView(string Sku, string Warehouse, decimal Quantity, bool Active);
/// <summary>Доступный владельцу снимок; распределения выдаются только сотруднику.</summary>
/// <param name="Id">Идентификатор заказа.</param>
/// <param name="Version">Ожидаемая версия команды.</param>
/// <param name="Number">Человекочитаемый номер.</param>
/// <param name="CreatedAt">Время создания UTC.</param>
/// <param name="Status">Состояние заказа.</param>
/// <param name="ReserveUntil">Срок UTC.</param>
/// <param name="Segment">Применённый сегмент.</param>
/// <param name="GoodsTotal">Сумма товаров.</param>
/// <param name="ContactName">Снимок имени.</param>
/// <param name="Email">Снимок почты.</param>
/// <param name="Phone">Снимок телефона.</param>
/// <param name="Recipient">Снимок получателя.</param>
/// <param name="Address">Снимок адреса.</param>
/// <param name="PostalCode">Индекс.</param>
/// <param name="Organization">Организация.</param>
/// <param name="Inn">ИНН.</param>
/// <param name="Kpp">КПП.</param>
/// <param name="Lines">Сохранённые строки.</param>
/// <param name="History">Публичные события.</param>
/// <param name="Reservations">Только сотрудникам; пусто покупателю.</param>
public sealed record CustomerOrderView(Guid Id, uint Version, string Number, DateTime CreatedAt, CustomerOrderStatus Status, DateTime? ReserveUntil, CustomerSegment Segment, decimal GoodsTotal, string ContactName, string Email, string Phone, string? Recipient, string Address, string? PostalCode, string? Organization, string? Inn, string? Kpp, IReadOnlyList<OrderLineView> Lines, IReadOnlyList<OrderHistoryView> History, IReadOnlyList<ReservationView> Reservations);
/// <summary>Строка серверного списка заказов.</summary>
/// <param name="Id">Заказ.</param>
/// <param name="Number">Номер.</param>
/// <param name="CreatedAt">Создание UTC.</param>
/// <param name="Customer">Снимок имени.</param>
/// <param name="Status">Статус.</param>
/// <param name="Segment">Сегмент.</param>
/// <param name="Total">Сумма товаров.</param>
/// <param name="ReserveUntil">Срок UTC.</param>
public sealed record OrderListRow(Guid Id, string Number, DateTime CreatedAt, string Customer, CustomerOrderStatus Status, CustomerSegment Segment, decimal Total, DateTime? ReserveUntil);
/// <summary>Ограниченная страница списка заказов.</summary>
/// <param name="Page">Номер страницы.</param>
/// <param name="Count">Число результатов.</param>
/// <param name="Items">Заказы текущей страницы.</param>
public sealed record OrderPage(int Page, int Count, IReadOnlyList<OrderListRow> Items);
/// <summary>Доверенный хост читает защищённую cookie; GUID корзины не определяет доступ.</summary>
public interface IGuestCartIdentity
{
    /// <summary>Хеш случайного гостевого секрета либо null без cookie.</summary>
    string? KeyHash { get; }
}
/// <summary>Серверные операции корзины, оформления и заказов; цена и владелец не принимаются от клиента.</summary>
public interface ICommerce
{
    /// <summary>Читает состав с актуальными ценами и публичностью.</summary>
    Task<CartView> CartAsync(CancellationToken ct = default);
    /// <summary>Идемпотентно добавляет количество выбранного доступного варианта.</summary>
    Task AddAsync(Guid variantId, int quantity, Guid operationId, CancellationToken ct = default);
    /// <summary>Задаёт желаемое количество по версии; ноль означает явное удаление строки.</summary>
    Task SetAsync(Guid variantId, int quantity, uint version, CancellationToken ct = default);
    /// <summary>Однократно объединяет гостевую корзину с текущим покупателем после входа.</summary>
    Task MergeAsync(CancellationToken ct = default);
    /// <summary>Создаёт серверный preview по собственному адресу покупателя.</summary>
    Task<PreviewView> PreviewAsync(Guid addressId, CancellationToken ct = default);
    /// <summary>Читает ранее созданные условия только владельцу.</summary>
    Task<PreviewView> PreviewAsync(Guid previewId, bool saved, CancellationToken ct = default);
    /// <summary>Атомарно создаёт заказ и резерв либо возвращает новые условия.</summary>
    Task<CheckoutResult> SubmitAsync(Guid previewId, Guid operationId, CancellationToken ct = default);
    /// <summary>Серверная страница; staff=true требует действующие права сотрудника.</summary>
    Task<OrderPage> OrdersAsync(bool staff, string? search, CustomerOrderStatus? status, int page, CancellationToken ct = default);
    /// <summary>Снимок заказа владельцу либо сотруднику с проверкой прав.</summary>
    Task<CustomerOrderView> OrderAsync(Guid id, bool staff = false, CancellationToken ct = default);
    /// <summary>Переход или продление с версией и ключом; действия confirm/cancel/extend.</summary>
    Task ChangeOrderAsync(Guid id, uint version, string action, string reason, DateTime? reserveUntil, Guid operationId, bool staff, CancellationToken ct = default);
}
