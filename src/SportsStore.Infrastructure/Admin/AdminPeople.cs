using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Работа с существующими покупателями и безопасным журналом без публичной регистрации.</summary>
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
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var actor = await access.RequireAsync(db, true, ct); if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) return;
        var customer = await db.Customers.SingleAsync(x => x.Id == data.Id, ct); AdminAccess.Version(customer, data.Version);
        var decision = customer.WholesaleStatus != data.Wholesale || customer.Segment != data.Segment;
        var safeReason = decision ? AdminAccess.Text(reason, "Причина решения", 500) : AdminAccess.Optional(reason);
        customer.DisplayName = AdminAccess.Text(data.Name, "Имя"); customer.Kind = data.Kind; customer.Segment = data.Segment; customer.WholesaleStatus = data.Wholesale;
        var profile = await db.OrganizationProfiles.SingleOrDefaultAsync(x => x.CustomerId == customer.Id, ct);
        if (data.Kind != CustomerKind.Individual)
        {
            if (data.Inn is null || !Regex.IsMatch(data.Inn, @"^([0-9]{10}|[0-9]{12})$", RegexOptions.CultureInvariant) || data.Kpp is not null && !Regex.IsMatch(data.Kpp, @"^[0-9]{9}$"))
                throw new ArgumentException("ИНН должен содержать 10 или 12 цифр; КПП — 9 цифр либо отсутствовать.");
            if (profile is null) { profile = new() { CustomerId = customer.Id }; db.OrganizationProfiles.Add(profile); }
            profile.LegalName = AdminAccess.Text(data.LegalName, "Официальное название"); profile.Inn = data.Inn; profile.Kpp = data.Kpp;
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
