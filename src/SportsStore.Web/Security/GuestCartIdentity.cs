using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using SportsStore.Application.Commerce;
namespace SportsStore.Web.Security;
/// <summary>Случайная защищённая гостевая cookie; в сервисы передаётся только хеш секрета.</summary>
/// <param name="http">Текущий HTTP-запрос.</param><param name="protection">Постоянный key ring сайта.</param>
public sealed class GuestCartIdentity(IHttpContextAccessor http, IDataProtectionProvider protection) : IGuestCartIdentity
{
    /// <summary>Host-only cookie, недоступная JavaScript и передаваемая только через HTTPS.</summary>
    public const string CookieName = "__Host-SportsStore.Cart";
    /// <summary>Изолированное назначение Data Protection для гостевой корзины.</summary>
    private readonly IDataProtector protector = protection.CreateProtector("SportsStore.GuestCart.v1");
    /// <inheritdoc />
    public string? KeyHash
    {
        get
        {
            var value = http.HttpContext?.Request.Cookies[CookieName]; if (value is null) return null;
            try { var raw = protector.Unprotect(value); return raw.Length == 64 ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))) : null; }
            catch (CryptographicException) { return null; }
        }
    }
    /// <summary>Выдаёт новый секрет до отправки HTTP-заголовков; не создаёт корзину в БД.</summary>
    public void EnsureCookie(HttpContext context)
    {
        if (KeyHash is not null) return;
        context.Response.Cookies.Append(CookieName, protector.Protect(Convert.ToHexString(RandomNumberGenerator.GetBytes(32))), Options());
    }
    /// <summary>Удаляет гостевой секрет после успешного объединения, не раскрывая покупательскую корзину после выхода.</summary>
    public void Clear(HttpContext context) => context.Response.Cookies.Delete(CookieName, Options());
    /// <summary>Безопасные флаги cookie; срок не продлевает время резервирования.</summary>
    private static CookieOptions Options() => new() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = "/", MaxAge = TimeSpan.FromDays(30), IsEssential = true };
}
