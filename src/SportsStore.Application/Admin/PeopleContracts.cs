using SportsStore.Domain.Entities;

namespace SportsStore.Application.Admin;

/// <summary>Сведения покупателя для административного просмотра без произвольной привязки Identity.</summary>
/// <param name="Id">Покупатель.</param><param name="Version">Ожидаемая версия.</param><param name="Name">Отображаемое имя.</param><param name="Kind">Правовой тип.</param>
/// <param name="Segment">Коммерческий сегмент.</param><param name="Wholesale">Статус опта.</param><param name="LegalName">Название организации.</param><param name="Inn">ИНН.</param><param name="Kpp">КПП.</param>
public sealed record CustomerData(Guid Id, uint Version, string Name, CustomerKind Kind, CustomerSegment Segment, WholesaleStatus Wholesale, string? LegalName, string? Inn, string? Kpp);

/// <summary>Безопасная проекция журнала административных действий.</summary>
/// <param name="At">UTC-время.</param><param name="Actor">Идентификатор сотрудника.</param><param name="Action">Код действия.</param><param name="Description">Описание.</param>
/// <param name="Reason">Основание.</param><param name="Operation">Идентификатор для диагностики повторов.</param>
public sealed record AuditData(DateTime At, string Actor, string Action, string Description, string? Reason, Guid Operation);

/// <summary>Контракт чтения покупателей и изменения коммерческих решений.</summary>
public interface IAdminPeople
{
    /// <summary>Возвращает страницу покупателей для сотрудника.</summary>
    Task<AdminPage<CustomerData>> CustomersAsync(AdminQuery query, CancellationToken ct = default);
    /// <summary>Изменяет существующего покупателя только от Admin; решение об опте требует причины.</summary>
    Task SaveCustomerAsync(CustomerData data, string reason, Guid operationId, CancellationToken ct = default);
    /// <summary>Возвращает страницу безопасного аудита только для Admin.</summary>
    Task<AdminPage<AuditData>> AuditAsync(AdminQuery query, CancellationToken ct = default);
}

/// <summary>Снимок сотрудника без хеша пароля, security stamp и токенов.</summary>
/// <param name="Id">Аккаунт.</param><param name="Version">ConcurrencyStamp для проверки формы.</param><param name="Email">Email сотрудника.</param>
/// <param name="Role">Текущая роль сотрудника.</param><param name="Blocked">Блокировка входа.</param><param name="Mfa">Настроена ли MFA.</param><param name="Confirmed">Принято ли приглашение.</param>
public sealed record EmployeeData(string Id, string Version, string Email, string Role, bool Blocked, bool Mfa, bool Confirmed);

/// <summary>Контракт управления сотрудниками с защитой последнего активного Admin.</summary>
public interface IAdminEmployees
{
    /// <summary>Возвращает ограниченный список сотрудников.</summary>
    Task<AdminPage<EmployeeData>> ListAsync(AdminQuery query, CancellationToken ct = default);
    /// <summary>Создаёт новый аккаунт без пароля и возвращает одноразовый токен только администратору для ручной передачи.</summary>
    Task<string> InviteAsync(string email, string role, Guid operationId, CancellationToken ct = default);
    /// <summary>Меняет роль или блокировку, отзывает старые сессии и защищает последнего Admin.</summary>
    Task UpdateAsync(EmployeeData employee, string reason, Guid operationId, CancellationToken ct = default);
}
