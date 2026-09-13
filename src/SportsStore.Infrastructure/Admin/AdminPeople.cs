using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Customers;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Административная работа с покупателями; изменения опта проходят общую историю и очередь уведомлений.</summary>
/// <param name="factory">Фабрика контекстов.</param><param name="access">Проверка актуальных прав.</param>
public sealed class AdminPeople(IDbContextFactory<ApplicationDbContext> factory, AdminAccess access) : IAdminPeople
{
    /// <summary>Проецирует покупателей и необходимые реквизиты с серверной пагинацией.</summary>
    public async Task<AdminPage<CustomerData>> CustomersAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var q = db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(x => x.DisplayName.Contains(query.Search));
        if (Enum.TryParse<WholesaleStatus>(query.Filter, out var status)) q = q.Where(x => x.WholesaleStatus == status);
        var total = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        return new(await q.OrderBy(x => x.DisplayName).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25).Select(x => new CustomerData(x.Id, x.Version, x.DisplayName, x.Kind, x.Segment, x.WholesaleStatus,
            db.OrganizationProfiles.Where(o => o.CustomerId == x.Id).Select(o => o.LegalName).FirstOrDefault(),
            db.OrganizationProfiles.Where(o => o.CustomerId == x.Id).Select(o => o.Inn).FirstOrDefault(),
            db.OrganizationProfiles.Where(o => o.CustomerId == x.Id).Select(o => o.Kpp).FirstOrDefault())).ToListAsync(ct), total, page);
    }
    /// <summary>Редактирует сведения и статус только с версией формы; Identity-связь не доступна во входном контракте.</summary>
    public async Task SaveCustomerAsync(CustomerData data, string reason, Guid operationId, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(data.Kind) || !Enum.IsDefined(data.Segment) || !Enum.IsDefined(data.Wholesale)) throw new ArgumentException("Неизвестный тип или статус покупателя.");
        WholesaleWorkflow.ValidateLegal(data.Kind, data.LegalName, data.Inn, data.Kpp);
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var actor = await access.RequireAsync(db, true, ct); if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) return;
        var customer = await WholesaleWorkflow.LockAsync(db, data.Id, ct); AdminAccess.Version(customer, data.Version);
        var decision = customer.WholesaleStatus != data.Wholesale || customer.Segment != data.Segment;
        var safeReason = decision ? AdminAccess.Text(reason, "Причина решения", 500) : AdminAccess.Optional(reason);
        var profile = await db.OrganizationProfiles.SingleOrDefaultAsync(x => x.CustomerId == customer.Id, ct);
        var legalChanged = !WholesaleWorkflow.SameLegal(customer.Kind, profile, data.Kind, data.LegalName, data.Inn, data.Kpp);
        var oldStatus = customer.WholesaleStatus;
        if (legalChanged && oldStatus is WholesaleStatus.Approved or WholesaleStatus.Pending)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Изменение реквизитов отзывает действующий опт или заявку. Укажите причину.");
            var pending = await db.WholesaleApplications.Where(x => x.CustomerId == customer.Id && (x.Status == WholesaleApplicationStatus.Pending || x.Status == WholesaleApplicationStatus.Approved))
                .OrderByDescending(x => x.SubmittedAt).FirstOrDefaultAsync(ct);
            WholesaleWorkflow.Decide(db, customer, pending, oldStatus == WholesaleStatus.Approved ? WholesaleApplicationStatus.Revoked : WholesaleApplicationStatus.Withdrawn,
                actor.UserId, reason, operationId);
        }
        else if (decision)
        {
            if (data.Wholesale == WholesaleStatus.Pending) throw new ArgumentException("Ожидающая заявка создаётся покупателем со снимком реквизитов.");
            var pending = await db.WholesaleApplications.Where(x => x.CustomerId == customer.Id && (x.Status == WholesaleApplicationStatus.Pending || x.Status == WholesaleApplicationStatus.Approved))
                .OrderByDescending(x => x.SubmittedAt).FirstOrDefaultAsync(ct);
            if (pending?.Status == WholesaleApplicationStatus.Pending && !WholesaleWorkflow.SameLegal(customer.Kind, profile, pending.Kind, pending.LegalName, pending.Inn, pending.Kpp))
                throw new ArgumentException("Реквизиты заявки устарели. Требуется новая заявка.");
            var outcome = data.Wholesale == WholesaleStatus.Approved && data.Segment == CustomerSegment.Wholesale ? WholesaleApplicationStatus.Approved
                : oldStatus == WholesaleStatus.Approved ? WholesaleApplicationStatus.Revoked : WholesaleApplicationStatus.Rejected;
            WholesaleWorkflow.Decide(db, customer, pending, outcome, actor.UserId, reason, operationId);
        }
        customer.DisplayName = AdminAccess.Text(data.Name, "Имя"); customer.Kind = data.Kind;
        if (data.Kind != CustomerKind.Individual)
        {
            if (profile is null) { profile = new() { CustomerId = customer.Id }; db.OrganizationProfiles.Add(profile); }
            profile.LegalName = AdminAccess.Text(data.LegalName, "Официальное название"); profile.Inn = data.Inn!; profile.Kpp = data.Kpp;
        }
        else if (profile is not null) db.OrganizationProfiles.Remove(profile);
        AdminAccess.Audit(db, actor, operationId, "customer.save", "Customer", customer.Id.ToString(), $"Изменены сведения и условия покупателя; сегмент {data.Segment}, статус опта {data.Wholesale}.", safeReason);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    /// <summary>Читает журнал только от Admin, не раскрывая содержимое изменённых реквизитов.</summary>
    public async Task<AdminPage<AuditData>> AuditAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, true, ct);
        var q = db.AdminAudits.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(x => x.Action.Contains(query.Search) || x.Description.Contains(query.Search));
        var total = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        return new(await q.OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25)
            .Select(x => new AuditData(x.OccurredAt, x.ActorId, x.Action, x.Description, x.Reason, x.OperationId)).ToListAsync(ct), total, page);
    }
}
