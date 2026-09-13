using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Customers;
using SportsStore.Infrastructure.Customers;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Web.Security;

/// <summary>Передаёт покупательскому слою только личность серверной сессии, исключая произвольные claims из формы.</summary>
/// <param name="authentication">Состояние интерактивного соединения.</param><param name="http">HTTP-контекст Razor Pages.</param>
public sealed class WebCustomerIdentity(AuthenticationStateProvider authentication, IHttpContextAccessor http) : ICustomerIdentity
{
    /// <summary>Использует principal circuit либо HTTP principal вне компонента.</summary>
    public async Task<CustomerSession> GetAsync(CancellationToken ct = default)
    {
        System.Security.Claims.ClaimsPrincipal principal;
        try { principal = (await authentication.GetAuthenticationStateAsync()).User; }
        catch (InvalidOperationException) { principal = http.HttpContext?.User ?? new(); }
        var session = WebAdminIdentity.Session(principal);
        return new(session.UserId, session.SecurityStamp, session.Mfa, session.Local);
    }
}

/// <summary>Требование действующей сессии с существующим профилем покупателя.</summary>
public sealed class CustomerRequirement : IAuthorizationRequirement;

/// <summary>Проверяет покупательскую policy по БД, не полагаясь на роль Customer в cookie.</summary>
/// <param name="factory">Фабрика контекстов.</param>
public sealed class CustomerAuthorizationHandler(IDbContextFactory<ApplicationDbContext> factory) : AuthorizationHandler<CustomerRequirement>
{
    /// <summary>Проверяет владельца через тот же механизм, что прикладные команды.</summary>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CustomerRequirement requirement)
    {
        await using var db = await factory.CreateDbContextAsync();
        var s = WebAdminIdentity.Session(context.User);
        try { await new CustomerAccess(new PrincipalIdentity(new(s.UserId, s.SecurityStamp, s.Mfa, s.Local))).RequireAsync(db); context.Succeed(requirement); }
        catch (UnauthorizedAccessException) { context.Fail(); }
    }
    /// <summary>Адаптер проверяемого policy principal.</summary>
    /// <param name="session">Сессия, составленная сервером.</param>
    private sealed class PrincipalIdentity(CustomerSession session) : ICustomerIdentity
    {
        /// <summary>Возвращает серверную сессию для единообразной проверки доступа.</summary>
        public Task<CustomerSession> GetAsync(CancellationToken ct = default) => Task.FromResult(session);
    }
}
