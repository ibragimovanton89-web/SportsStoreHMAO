using SportsStore.Domain.Entities;

namespace SportsStore.Application.Customers;

/// <summary>Предъявленная серверу сессия; не принимается из формы покупателя.</summary>
/// <param name="UserId">Идентификатор Identity.</param><param name="SecurityStamp">Метка подписанной сессии.</param>
/// <param name="Mfa">Подтверждена ли MFA.</param><param name="Local">Локальный обход администратора, запрещённый для покупательских операций.</param>
public sealed record CustomerSession(string UserId, string SecurityStamp, bool Mfa, bool Local = false);
/// <summary>Доверенный адаптер серверной HTTP- или Blazor-сессии.</summary>
public interface ICustomerIdentity
{
    /// <summary>Получает личность без доверия к пользовательским идентификаторам формы.</summary>
    Task<CustomerSession> GetAsync(CancellationToken ct = default);
}
/// <summary>Разрешённые для изменения сведения; условия цены и связь с Identity отсутствуют.</summary>
/// <param name="Version">Ожидаемая версия покупателя.</param><param name="Name">Имя покупателя.</param>
/// <param name="Phone">Неподтверждённый контактный телефон.</param><param name="Kind">Правовой тип.</param>
/// <param name="LegalName">Официальное название, если применимо.</param><param name="Inn">ИНН, если применим.</param><param name="Kpp">КПП, если применим.</param>
public sealed record ProfileInput(uint Version, string Name, string? Phone, CustomerKind Kind, string? LegalName, string? Inn, string? Kpp);
/// <summary>Собственные сведения покупателя, включая только его текущие условия.</summary>
/// <param name="Profile">Редактируемые поля.</param><param name="Email">Подтверждённая почта, недоступная для изменения.</param>
/// <param name="Status">Результат проверки опта.</param><param name="HasLegacyApproval">Одобрение существовало без заявки.</param>
public sealed record CustomerProfile(ProfileInput Profile, string Email, WholesaleStatus Status, bool HasLegacyApproval);
/// <summary>Адрес без CustomerId: владелец всегда определяется сервером.</summary>
/// <param name="Id">Идентификатор; Guid.Empty для нового адреса.</param><param name="Version">Ожидаемая версия.</param>
/// <param name="Recipient">Получатель.</param><param name="Address">Текст адреса.</param><param name="PostalCode">Необязательный индекс.</param>
public sealed record AddressInput(Guid Id, uint Version, string Recipient, string Address, string? PostalCode);
/// <summary>Безопасный снимок заявки без идентификатора сотрудника или внутренних заметок.</summary>
/// <param name="Id">Заявка.</param><param name="Version">Версия решения.</param><param name="Status">Состояние.</param><param name="SubmittedAt">Время подачи UTC.</param>
/// <param name="Kind">Тип в момент подачи.</param><param name="LegalName">Снимок названия.</param><param name="Inn">Снимок ИНН.</param><param name="Kpp">Снимок КПП.</param>
/// <param name="Comment">Обращение покупателя.</param><param name="PublicReason">Публичный результат.</param>
public sealed record WholesaleApplicationData(Guid Id, uint Version, WholesaleApplicationStatus Status, DateTimeOffset SubmittedAt,
    CustomerKind Kind, string? LegalName, string? Inn, string? Kpp, string? Comment, string? PublicReason);
/// <summary>Элемент истории, доступный владельцу; источник без заявки обозначается отдельно.</summary>
/// <param name="At">Время UTC.</param><param name="Outcome">Результат.</param><param name="Reason">Публичная причина.</param><param name="WithoutApplication">Решение без заявки.</param>
public sealed record WholesaleHistory(DateTimeOffset At, WholesaleApplicationStatus Outcome, string Reason, bool WithoutApplication);
/// <summary>Количество одного SKU, начиная с которого действует сохранённая цена.</summary>
/// <param name="MinimumQuantity">Включительная граница в единицах продажи.</param><param name="Amount">Цена одной единицы, RUB.</param>
public sealed record WholesaleTier(decimal MinimumQuantity, decimal Amount);
/// <summary>Персональный результат для подтверждённого покупателя; отсутствие суммы не означает розничный fallback.</summary>
/// <param name="Amount">Оптовая цена либо null при отсутствии ступени.</param><param name="Tiers">Доступные ступени данного варианта.</param>
public sealed record CustomerPrice(decimal? Amount, IReadOnlyList<WholesaleTier> Tiers);
/// <summary>Покупательские операции с повторной проверкой сессии и владельца при каждом вызове.</summary>
public interface ICustomerAccount
{
    /// <summary>Читает собственный профиль; при отсутствии действующей сессии запрещает доступ.</summary>
    Task<CustomerProfile> ProfileAsync(CancellationToken ct = default);
    /// <summary>Меняет разрешённые сведения; существенная смена реквизитов отзывает опт только с явным подтверждением.</summary>
    Task SaveProfileAsync(ProfileInput input, bool acknowledgeWholesaleRevocation, Guid operationId, CancellationToken ct = default);
    /// <summary>Возвращает только собственные адреса.</summary>
    Task<IReadOnlyList<AddressInput>> AddressesAsync(CancellationToken ct = default);
    /// <summary>Создаёт или изменяет собственный адрес с проверкой версии.</summary>
    Task SaveAddressAsync(AddressInput input, CancellationToken ct = default);
    /// <summary>Удаляет собственный адрес с проверкой версии.</summary>
    Task DeleteAddressAsync(Guid id, uint version, CancellationToken ct = default);
    /// <summary>Читает собственные заявки, от новых к старым.</summary>
    Task<IReadOnlyList<WholesaleApplicationData>> ApplicationsAsync(CancellationToken ct = default);
    /// <summary>Читает публичную историю собственных решений.</summary>
    Task<IReadOnlyList<WholesaleHistory>> HistoryAsync(CancellationToken ct = default);
    /// <summary>Подаёт одну ожидающую заявку со снимком текущих реквизитов.</summary>
    Task SubmitAsync(string? comment, Guid operationId, CancellationToken ct = default);
    /// <summary>Отзывает собственную ожидающую заявку, проверяя версию.</summary>
    Task WithdrawAsync(Guid id, uint version, Guid operationId, CancellationToken ct = default);
    /// <summary>Возвращает опт только действующему подтверждённому покупателю для публичного варианта; null без права на опт.</summary>
    Task<CustomerPrice?> PriceAsync(Guid productId, Guid variantId, int quantity, CancellationToken ct = default);
}
/// <summary>Проверка заявок в существующей административной области.</summary>
public interface IWholesaleAdministration
{
    /// <summary>Сотрудник читает заявки выбранного покупателя.</summary>
    Task<IReadOnlyList<WholesaleApplicationData>> ApplicationsAsync(Guid customerId, CancellationToken ct = default);
    /// <summary>Сотрудник читает историю решений.</summary>
    Task<IReadOnlyList<WholesaleHistory>> HistoryAsync(Guid customerId, CancellationToken ct = default);
    /// <summary>Администратор решает ожидающую заявку; версия и снимок проверяются транзакционно.</summary>
    Task DecideAsync(Guid applicationId, uint version, bool approve, string reason, Guid operationId, CancellationToken ct = default);
    /// <summary>Администратор отзывает ранее предоставленный опт с причиной.</summary>
    Task RevokeAsync(Guid customerId, uint version, string reason, Guid operationId, CancellationToken ct = default);
}
