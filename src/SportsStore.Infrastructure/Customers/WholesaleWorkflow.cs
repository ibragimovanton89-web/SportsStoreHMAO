using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Customers;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Admin;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Customers;

/// <summary>Общие правила опта для кабинета и старой административной формы; вызывающий код владеет транзакцией.</summary>
internal static class WholesaleWorkflow
{
    /// <summary>Блокирует строку покупателя первой во всех операциях условий, затем проверяет актуальную версию.</summary>
    internal static async Task<Customer> LockAsync(ApplicationDbContext db, Guid id, CancellationToken ct)
    {
        var customer = await db.Customers.FromSqlInterpolated($"SELECT *, xmin FROM \"Customer\" WHERE \"Id\" = {id} FOR UPDATE").SingleAsync(ct);
        // Если проверка доступа уже отслеживала строку, перечитываем её после получения блокировки.
        await db.Entry(customer).ReloadAsync(ct);
        return customer;
    }
    /// <summary>Проверяет формат реквизитов по типу; не выдаёт проверку формата за подтверждение государственного реестра.</summary>
    internal static void ValidateLegal(CustomerKind kind, string? name, string? inn, string? kpp)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentException("Неизвестный правовой тип.");
        if (kind == CustomerKind.Individual) return;
        AdminAccess.Text(name, "Официальное название");
        var pattern = kind == CustomerKind.Organization ? "^[0-9]{10}$" : "^[0-9]{12}$";
        if (inn is null || !Regex.IsMatch(inn, pattern, RegexOptions.CultureInvariant))
            throw new ArgumentException(kind == CustomerKind.Organization ? "ИНН организации должен содержать 10 цифр." : "ИНН ИП должен содержать 12 цифр.");
        if (kpp is not null && (kind != CustomerKind.Organization || !Regex.IsMatch(kpp, "^[0-9]{9}$", RegexOptions.CultureInvariant)))
            throw new ArgumentException("КПП применим только к организации и содержит 9 цифр.");
    }
    /// <summary>Сравнивает только существенные реквизиты; контактный телефон и адрес не влияют на право на опт.</summary>
    internal static bool SameLegal(CustomerKind kind, OrganizationProfile? profile, CustomerKind otherKind, string? name, string? inn, string? kpp) =>
        kind == otherKind && (kind == CustomerKind.Individual || (profile?.LegalName == name && profile?.Inn == inn && profile?.Kpp == kpp));
    /// <summary>Проецирует снимок заявки без служебной личности сотрудника.</summary>
    internal static WholesaleApplicationData Data(WholesaleApplication x) => new(x.Id, x.Version, x.Status, x.SubmittedAt, x.Kind, x.LegalName, x.Inn, x.Kpp, x.Comment, x.PublicReason);
    /// <summary>Добавляет решение и письмо в текущую транзакцию, обновляя условия и состояние заявки; SMTP здесь не вызывается.</summary>
    internal static void Decide(ApplicationDbContext db, Customer customer, WholesaleApplication? application,
        WholesaleApplicationStatus outcome, string actor, string reason, Guid operation)
    {
        if (operation == Guid.Empty) throw new ArgumentException("Отсутствует идентификатор операции.");
        reason = AdminAccess.Text(reason, "Публичная причина", 500);
        customer.Segment = outcome == WholesaleApplicationStatus.Approved ? CustomerSegment.Wholesale : CustomerSegment.Retail;
        customer.WholesaleStatus = outcome switch
        {
            WholesaleApplicationStatus.Approved => WholesaleStatus.Approved,
            WholesaleApplicationStatus.Withdrawn => WholesaleStatus.NotRequested,
            _ => WholesaleStatus.Rejected
        };
        if (application is not null)
        {
            application.Status = outcome; application.ReviewedAt = DateTimeOffset.UtcNow;
            application.ReviewedBy = actor; application.PublicReason = reason;
        }
        var decision = new WholesaleDecision { CustomerId = customer.Id, ApplicationId = application?.Id,
            Outcome = outcome, ActorId = actor, PublicReason = reason, OperationId = operation };
        db.WholesaleDecisions.Add(decision);
        if (customer.ApplicationUserId is not null) db.NotificationOutbox.Add(new() { DecisionId = decision.Id });
    }
    /// <summary>Проверяет повтор команды в пределах покупателя; повтор с чужим идентификатором отклоняется.</summary>
    internal static async Task<bool> PriorAsync(ApplicationDbContext db, Guid customerId, Guid operation, CancellationToken ct)
    {
        if (operation == Guid.Empty) throw new ArgumentException("Отсутствует идентификатор операции.");
        var prior = await db.WholesaleDecisions.AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == operation, ct);
        if (prior is null) return false;
        if (prior.CustomerId != customerId) throw new UnauthorizedAccessException("Недоступная операция.");
        return true;
    }
}
