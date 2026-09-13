using System.Net;
using System.Security.Claims;
using SportsStore.Web.Security;
using Xunit;
namespace SportsStore.Tests;

/// <summary>Границы временного входа: среда, адрес соединения и неподделываемый claims-полем маркер.</summary>
public sealed class DevelopmentAdminAccessTests
{
    /// <summary>Включённый обход не подменяет покупателя, сотрудника или вторую аутентифицированную identity.</summary>
    [Theory]
    [InlineData("Customer")]
    [InlineData("Admin")]
    [InlineData("Manager")]
    public void AuthenticatedSessionHasPriorityOverBypass(string role)
    {
        var user = new ClaimsPrincipal(new[] { new ClaimsIdentity(), new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Identity.Application") });
        Assert.False(DevelopmentAdminAccess.ShouldBypass(user, IPAddress.Loopback, "localhost"));
        Assert.True(DevelopmentAdminAccess.ShouldBypass(new ClaimsPrincipal(), IPAddress.Loopback, "localhost"));
        Assert.False(DevelopmentAdminAccess.ShouldBypass(new ClaimsPrincipal(), IPAddress.Parse("192.168.1.20"), "localhost"));
    }
    /// <summary>Обход запрещён в Production и Staging, даже если флаг ошибочно оставлен включённым.</summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void NonDevelopmentRejectsEnabledBypass(string environment)
    {
        Assert.Throws<InvalidOperationException>(() => DevelopmentAdminAccess.ValidateEnvironment(environment, true));
        DevelopmentAdminAccess.ValidateEnvironment(environment, false);
    }
    /// <summary>Одного подставленного Host localhost недостаточно: соединение должно быть loopback.</summary>
    [Fact]
    public void OnlyDirectLocalRequestsAreEligible()
    {
        DevelopmentAdminAccess.ValidateEnvironment("Development", true);
        Assert.True(DevelopmentAdminAccess.IsLocalRequest(IPAddress.Loopback, "localhost"));
        Assert.True(DevelopmentAdminAccess.IsLocalRequest(IPAddress.IPv6Loopback, "[::1]"));
        Assert.True(DevelopmentAdminAccess.IsLocalRequest(IPAddress.Parse("::ffff:127.0.0.1"), "127.0.0.1"));
        Assert.False(DevelopmentAdminAccess.IsLocalRequest(IPAddress.Parse("192.168.1.20"), "localhost"));
        Assert.False(DevelopmentAdminAccess.IsLocalRequest(IPAddress.Loopback, "store.example"));
        Assert.False(DevelopmentAdminAccess.IsLocalRequest(null, "localhost"));
    }
    /// <summary>Обычная cookie с теми же claims или названием схемы не получает локальных привилегий.</summary>
    [Fact]
    public void ClaimsCannotForgeDevelopmentIdentity()
    {
        var trusted = DevelopmentAdminAccess.CreatePrincipal();
        Assert.True(trusted.Identity!.IsAuthenticated);
        Assert.True(trusted.IsInRole("Admin"));
        Assert.True(DevelopmentAdminAccess.IsDevelopmentPrincipal(trusted));
        var copy = new ClaimsPrincipal(new ClaimsIdentity(trusted.Claims, "LocalDevelopment"));
        Assert.False(DevelopmentAdminAccess.IsDevelopmentPrincipal(copy));
    }
}
