using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Infrastructure.Admin;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Web.Security;

/// <summary>Извлекает неизменяемый снимок текущей сессии из серверного состояния Blazor или HTTP.</summary>
/// <param name="authentication">Состояние circuit.</param><param name="http">HTTP-контекст для серверных загрузок.</param>
public sealed class WebAdminIdentity(AuthenticationStateProvider authentication, IHttpContextAccessor http) : IAdminIdentity
{
    /// <summary>Возвращает только доверенные claims; тестовая личность допускается только из локального middleware Development.</summary>
    public async Task<AdminSession> GetAsync(CancellationToken ct = default)
    {
        ClaimsPrincipal principal;
        try { principal = (await authentication.GetAuthenticationStateAsync()).User; }
        catch (InvalidOperationException) { principal = http.HttpContext?.User ?? new ClaimsPrincipal(); }
        return Session(principal);
    }
    /// <summary>Преобразует предъявленную cookie в данные для повторной проверки в БД.</summary>
    internal static AdminSession Session(ClaimsPrincipal principal) => new(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
        principal.FindFirstValue("AspNet.Identity.SecurityStamp") ?? "", principal.HasClaim("amr", "mfa"), Local: DevelopmentAdminAccess.IsDevelopmentPrincipal(principal));
}

/// <summary>Требование действующей MFA-сессии сотрудника с актуальными ролями.</summary>
/// <param name="Administrator">Требуется ли роль Admin.</param>
public sealed record StaffRequirement(bool Administrator) : IAuthorizationRequirement;

/// <summary>Проверяет именованные policies через актуальную БД как при SSR, так и в интерактивных маршрутах.</summary>
/// <param name="factory">Фабрика контекстов.</param>
public sealed class StaffAuthorizationHandler(IDbContextFactory<ApplicationDbContext> factory) : AuthorizationHandler<StaffRequirement>
{
    /// <summary>Не доверяет устаревшим ролям cookie: сверяет Identity, stamp, блокировку и MFA.</summary>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, StaffRequirement requirement)
    {
        await using var db = await factory.CreateDbContextAsync();
        try
        {
            await new AdminAccess(new PrincipalIdentity(context.User)).RequireAsync(db, requirement.Administrator);
            context.Succeed(requirement);
        }
        catch (UnauthorizedAccessException) { context.Fail(); }
    }
    /// <summary>Адаптер только серверного ClaimsPrincipal для проверки policy.</summary>
    /// <param name="principal">Предъявленная серверу личность.</param>
    private sealed class PrincipalIdentity(ClaimsPrincipal principal) : IAdminIdentity
    {
        /// <summary>Возвращает предъявленные claims без локальных привилегий.</summary>
        public Task<AdminSession> GetAsync(CancellationToken ct = default) => Task.FromResult(WebAdminIdentity.Session(principal));
    }
}

/// <summary>Отзывает активный circuit при блокировке, изменении stamp или снятии роли; команды дополнительно проверяются немедленно.</summary>
/// <param name="logger">Журнал жизненного цикла без секретов.</param><param name="factory">Фабрика коротких контекстов.</param>
public sealed class StaffAuthenticationStateProvider(ILoggerFactory logger, IDbContextFactory<ApplicationDbContext> factory)
    : RevalidatingServerAuthenticationStateProvider(logger)
{
    /// <summary>Период фоновой проверки открытой вкладки; не заменяет проверку каждой команды.</summary>
    protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(15);
    /// <summary>Проверяет свежую учётную запись покупателя или сотрудника; административная роль и MFA проверяются отдельной policy.</summary>
    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState state, CancellationToken ct)
    {
        if (DevelopmentAdminAccess.IsDevelopmentPrincipal(state.User)) return true;
        var session = WebAdminIdentity.Session(state.User);
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == session.UserId, ct);
        return user is not null && user.EmailConfirmed && user.SecurityStamp == session.SecurityStamp && !(user.LockoutEnd > DateTimeOffset.UtcNow);
    }
}
