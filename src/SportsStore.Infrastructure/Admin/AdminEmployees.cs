using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsStore.Application.Admin;
using SportsStore.Infrastructure.Identity;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Управляет сотрудниками через стандартный UserManager; короткая область DI обеспечивает общий контекст Identity и аудита.</summary>
/// <param name="scopes">Фабрика независимых областей операции.</param><param name="factory">Фабрика чтения.</param><param name="access">Проверка прав текущего сотрудника.</param>
public sealed class AdminEmployees(IServiceScopeFactory scopes, IDbContextFactory<ApplicationDbContext> factory, AdminAccess access) : IAdminEmployees
{
    /// <summary>Возвращает страницу аккаунтов сотрудников без секретных полей.</summary>
    public async Task<AdminPage<EmployeeData>> ListAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, true, ct);
        var q = db.Users.AsNoTracking().Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && (r.Name == "Admin" || r.Name == "Manager"))));
        if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(x => x.Email != null && x.Email.Contains(query.Search));
        var total = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        return new(await q.OrderBy(x => x.Email).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25).Select(u => new EmployeeData(u.Id, u.ConcurrencyStamp ?? "", u.Email ?? "",
            db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")) ? "Admin" : "Manager",
            u.LockoutEnd > DateTimeOffset.UtcNow, u.TwoFactorEnabled, u.EmailConfirmed)).ToListAsync(ct), total, page);
    }
    /// <summary>Создаёт новый приглашённый аккаунт; повтор не сбрасывает пароль и не перевыдаёт токен автоматически.</summary>
    public async Task<string> InviteAsync(string email, string role, Guid operationId, CancellationToken ct = default)
    {
        email = AdminAccess.Text(email, "Email", 256);
        if (!new EmailAddressAttribute().IsValid(email) || role is not ("Admin" or "Manager")) throw new ArgumentException("Проверьте email и роль сотрудника.");
        await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await using var tx = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861002)", ct);
        var actor = await access.RequireAsync(db, true, ct);
        if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) throw new InvalidOperationException("Приглашение уже создано. Повтор не создаёт новый аккаунт или токен.");
        if (await users.FindByEmailAsync(email) is not null || await users.FindByNameAsync(email) is not null) throw new ArgumentException("Такой аккаунт уже существует; автоматическое повышение или сброс запрещены.");
        var user = new ApplicationUser { Email = email, UserName = email, LockoutEnabled = true };
        Ensure(await users.CreateAsync(user)); Ensure(await users.AddToRoleAsync(user, role));
        var token = await users.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "StaffInvitation");
        AdminAccess.Audit(db, actor, operationId, "staff.invite", "ApplicationUser", user.Id, $"Создано приглашение сотрудника с ролью {role}; ссылка не сохраняется в аудите.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return "/account/invite?UserId=" + Uri.EscapeDataString(user.Id) + "&Token=" + Uri.EscapeDataString(token);
    }
    /// <summary>Меняет роль и блокировку, сериализуя защиту последнего активного администратора.</summary>
    public async Task UpdateAsync(EmployeeData employee, string reason, Guid operationId, CancellationToken ct = default)
    {
        if (employee.Role is not ("Admin" or "Manager")) throw new ArgumentException("Допустимы роли Admin и Manager.");
        reason = AdminAccess.Text(reason, "Причина изменения");
        await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await using var tx = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861002)", ct);
        var actor = await access.RequireAsync(db, true, ct); if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) return;
        var user = await users.FindByIdAsync(employee.Id) ?? throw new ArgumentException("Сотрудник не найден.");
        if (user.ConcurrencyStamp != employee.Version) throw new DbUpdateConcurrencyException("Сотрудник уже изменён. Обновите сведения.");
        var roles = await users.GetRolesAsync(user);
        if (!roles.Contains("Admin") && !roles.Contains("Manager")) throw new ArgumentException("Этот аккаунт не является сотрудником.");
        if (roles.Contains("Admin") && (employee.Blocked || employee.Role != "Admin"))
        {
            var other = await db.Users.AnyAsync(u => u.Id != user.Id && u.EmailConfirmed && u.PasswordHash != null && (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow)
                && db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")), ct);
            if (!other) throw new InvalidOperationException("Нельзя заблокировать или понизить последнего активного Admin.");
        }
        foreach (var role in roles.Where(x => x is "Admin" or "Manager")) if (role != employee.Role) Ensure(await users.RemoveFromRoleAsync(user, role));
        if (!roles.Contains(employee.Role)) Ensure(await users.AddToRoleAsync(user, employee.Role));
        Ensure(await users.SetLockoutEndDateAsync(user, employee.Blocked ? DateTimeOffset.MaxValue : null));
        Ensure(await users.UpdateSecurityStampAsync(user));
        AdminAccess.Audit(db, actor, operationId, "staff.update", "ApplicationUser", user.Id, $"Роль {employee.Role}; блокировка {employee.Blocked}; прежние сессии отозваны.", reason);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    /// <summary>Останавливает транзакцию при отказе Identity без вывода чувствительных подробностей.</summary>
    private static void Ensure(IdentityResult result) { if (!result.Succeeded) throw new InvalidOperationException("Identity отклонил изменение учётной записи."); }
}
