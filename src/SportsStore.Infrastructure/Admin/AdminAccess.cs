using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Проверяет живую учётную запись и роли при каждой серверной операции, независимо от старого состояния интерфейса.</summary>
/// <param name="identity">Доверенный источник сессии хоста.</param>
public sealed class AdminAccess(IAdminIdentity identity)
{
    /// <summary>Проверяет разрешение и возвращает сотрудника для аудита; браузер не передаёт роли или идентификатор.</summary>
    public async Task<AdminSession> RequireAsync(ApplicationDbContext db, bool administrator, CancellationToken ct = default)
    {
        var session = await identity.GetAsync(ct);
        if (session.Local) return session;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == session.UserId, ct);
        if (user is null || !user.EmailConfirmed || !user.TwoFactorEnabled || !session.Mfa || user.SecurityStamp != session.SecurityStamp
            || user.LockoutEnd > DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Сессия недействительна. Выполните вход и двухфакторную проверку заново.");
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == user.Id select r.Name).ToListAsync(ct);
        if (!roles.Contains("Admin") && (administrator || !roles.Contains("Manager")))
            throw new UnauthorizedAccessException("Для этого действия недостаточно прав.");
        return session;
    }

    /// <summary>Возвращает текущие полномочия администратора, повторно проверяя БД.</summary>
    public async Task<bool> IsAdministratorAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        var session = await RequireAsync(db, false, ct);
        return session.Local || await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
            where ur.UserId == session.UserId && r.Name == "Admin" select ur).AnyAsync(ct);
    }

    /// <summary>Добавляет безопасную запись в текущую транзакцию; вызывающий код сохраняет её вместе с изменением.</summary>
    public static void Audit(ApplicationDbContext db, AdminSession actor, Guid operationId, string action, string objectType, string objectId, string description, string? reason = null)
    {
        if (operationId == Guid.Empty) throw new ArgumentException("Отсутствует идентификатор операции.");
        db.AdminAudits.Add(new() { ActorId = actor.UserId, OperationId = operationId, Action = action,
            ObjectType = objectType, ObjectId = objectId, Description = description, Reason = reason });
    }

    /// <summary>Проверяет версию из формы до изменения записи; данные клиента остаются в форме при конфликте.</summary>
    public static void Version(Entity entity, uint expected)
    {
        if (entity.Version != expected) throw new DbUpdateConcurrencyException("Запись изменена другим сотрудником. Скопируйте введённые данные и обновите сведения.");
    }

    /// <summary>Нормализует обязательный текст и проверяет длину до передачи EF.</summary>
    public static string Text(string? value, string label, int max = 500)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > max) throw new ArgumentException($"{label}: требуется от 1 до {max} символов.");
        return value;
    }

    /// <summary>Ограничивает необязательный текст; пустая строка превращается в null.</summary>
    public static string? Optional(string? value, int max = 500)
    {
        if (value?.Length > max) throw new ArgumentException($"Допустимо не более {max} символов.");
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
