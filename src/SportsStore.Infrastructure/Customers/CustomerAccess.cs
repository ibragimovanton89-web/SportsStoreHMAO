using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Customers;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Customers;

/// <summary>Проверяет живую личность и связь покупателя; локальный обход администратора здесь не действует.</summary>
/// <param name="identity">Доверенная сессия хоста.</param>
public sealed class CustomerAccess(ICustomerIdentity identity)
{
    /// <summary>Возвращает только покупателя действующей сессии; наличие роли Customer само по себе недостаточно.</summary>
    /// <param name="db">Короткий контекст вызывающей операции.</param><param name="ct">Отмена запроса.</param>
    /// <exception cref="UnauthorizedAccessException">Нет действующей учётной записи или принадлежащего ей покупателя.</exception>
    public async Task<Customer> RequireAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        var session = await identity.GetAsync(ct);
        if (session.Local || string.IsNullOrEmpty(session.UserId)) throw new UnauthorizedAccessException("Войдите как покупатель.");
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == session.UserId, ct);
        if (user is null || !user.EmailConfirmed || user.SecurityStamp != session.SecurityStamp || user.LockoutEnd > DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Войдите заново и подтвердите почту.");
        var staff = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
            where ur.UserId == user.Id && (r.Name == "Admin" || r.Name == "Manager") select ur).AnyAsync(ct);
        if (staff && (!user.TwoFactorEnabled || !session.Mfa)) throw new UnauthorizedAccessException("Для сотрудника требуется MFA.");
        return await db.Customers.SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id, ct)
            ?? throw new UnauthorizedAccessException("Учётная запись не связана с профилем покупателя.");
    }
}
