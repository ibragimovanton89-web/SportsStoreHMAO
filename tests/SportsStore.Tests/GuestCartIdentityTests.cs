using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using SportsStore.Web.Security;
using Xunit;
namespace SportsStore.Tests;
/// <summary>Проверяет реальную защиту гостевой cookie, а не доверенный тестовый GUID.</summary>
public sealed class GuestCartIdentityTests
{
    /// <summary>Выданная cookie защищена, имеет обязательные флаги и не принимается после подмены.</summary>
    [Fact]
    public void CookieIsProtectedAndTamperingIsRejected()
    {
        var http = new DefaultHttpContext(); var access = new HttpContextAccessor { HttpContext = http };
        var protection = new EphemeralDataProtectionProvider(); var identity = new GuestCartIdentity(access, protection);
        identity.EnsureCookie(http); var header = http.Response.Headers.SetCookie.ToString();
        Assert.Contains("secure",header); Assert.Contains("httponly",header); Assert.Contains("samesite=lax",header); Assert.Contains("path=/",header);
        http.Request.Headers.Cookie = header.Split(';')[0]; var hash = identity.KeyHash; Assert.NotNull(hash); Assert.Equal(64,hash.Length);
        var pair = header.Split(';')[0]; var split = pair.IndexOf('='); var token = pair[(split+1)..];
        http.Request.Headers.Cookie = pair[..(split+1)] + token[..20] + (token[20]=='A'?'B':'A') + token[21..]; Assert.Null(identity.KeyHash);
        http.Request.Headers.Cookie = pair; Assert.Null(new GuestCartIdentity(access,new EphemeralDataProtectionProvider()).KeyHash);
    }
}
