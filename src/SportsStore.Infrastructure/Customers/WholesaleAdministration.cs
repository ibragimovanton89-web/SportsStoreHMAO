using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Customers;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Admin;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Customers;

/// <summary>Рассмотрение заявок сотрудниками; решение, аудит и уведомление фиксируются вместе.</summary>
/// <param name="factory">Фабрика коротких контекстов.</param><param name="access">Живая проверка ролей и MFA.</param>
public sealed class WholesaleAdministration(IDbContextFactory<ApplicationDbContext> factory, AdminAccess access) : IWholesaleAdministration
{
    /// <summary>Даёт сотруднику снимки последних заявок выбранного покупателя.</summary>
    public async Task<IReadOnlyList<WholesaleApplicationData>> ApplicationsAsync(Guid customerId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return (await db.WholesaleApplications.AsNoTracking().Where(x => x.CustomerId == customerId).OrderByDescending(x => x.SubmittedAt).Take(100).ToListAsync(ct))
            .Select(WholesaleWorkflow.Data).ToArray();
    }
    /// <summary>Даёт сотруднику публичную историю; не создаёт фиктивных заявок для старых одобрений.</summary>
    public async Task<IReadOnlyList<WholesaleHistory>> HistoryAsync(Guid customerId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await db.WholesaleDecisions.AsNoTracking().Where(x => x.CustomerId == customerId).OrderByDescending(x => x.OccurredAt).Take(100)
            .Select(x => new WholesaleHistory(x.OccurredAt, x.Outcome, x.PublicReason, x.ApplicationId == null)).ToListAsync(ct);
    }
    /// <summary>Только Admin рассматривает актуальную ожидающую заявку с теми же реквизитами, что в снимке.</summary>
    public async Task DecideAsync(Guid applicationId, uint version, bool approve, string reason, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, true, ct);
        var customerId = await db.WholesaleApplications.Where(x => x.Id == applicationId).Select(x => x.CustomerId).SingleAsync(ct);
        var customer = await WholesaleWorkflow.LockAsync(db, customerId, ct);
        if (await WholesaleWorkflow.PriorAsync(db, customer.Id, operationId, ct)) return;
        var application = await db.WholesaleApplications.SingleAsync(x => x.Id == applicationId, ct); AdminAccess.Version(application, version);
        if (application.Status != WholesaleApplicationStatus.Pending) throw new ArgumentException("Заявка уже рассмотрена или отозвана.");
        var profile = await db.OrganizationProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.CustomerId == customer.Id, ct);
        if (!WholesaleWorkflow.SameLegal(customer.Kind, profile, application.Kind, application.LegalName, application.Inn, application.Kpp))
            throw new ArgumentException("Реквизиты изменились. Покупателю нужно подать новую заявку.");
        WholesaleWorkflow.Decide(db, customer, application, approve ? WholesaleApplicationStatus.Approved : WholesaleApplicationStatus.Rejected,
            actor.UserId, reason, operationId);
        AdminAccess.Audit(db, actor, operationId, "wholesale.decide", "WholesaleApplication", application.Id.ToString(), "Рассмотрена заявка на опт.", AdminAccess.Text(reason, "Причина"));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    /// <summary>Отзывает одобрение только от Admin с проверкой версии покупателя.</summary>
    public async Task RevokeAsync(Guid customerId, uint version, string reason, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, true, ct); var customer = await WholesaleWorkflow.LockAsync(db, customerId, ct);
        if (await WholesaleWorkflow.PriorAsync(db, customer.Id, operationId, ct)) return;
        AdminAccess.Version(customer, version);
        if (customer.WholesaleStatus != WholesaleStatus.Approved) throw new ArgumentException("У покупателя нет действующего одобрения.");
        var application = await db.WholesaleApplications.Where(x => x.CustomerId == customerId && x.Status == WholesaleApplicationStatus.Approved).OrderByDescending(x => x.SubmittedAt).FirstOrDefaultAsync(ct);
        WholesaleWorkflow.Decide(db, customer, application, WholesaleApplicationStatus.Revoked, actor.UserId, reason, operationId);
        AdminAccess.Audit(db, actor, operationId, "wholesale.revoke", "Customer", customer.Id.ToString(), "Отозвано право на опт.", AdminAccess.Text(reason, "Причина"));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
