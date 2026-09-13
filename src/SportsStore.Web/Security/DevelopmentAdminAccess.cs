using System.Net;
using System.Security.Claims;

namespace SportsStore.Web.Security;

/// <summary>Локальный тестовый вход без cookie и без создания пользователя в Identity.</summary>
public static class DevelopmentAdminAccess
{
    /// <summary>Ключ явного включения временного режима; читается только при запуске.</summary>
    public const string ConfigurationKey = "Development:BypassAdminAuthentication";

    /// <summary>Запрещает включённый тестовый вход вне Development вместо молчаливого запуска с ослабленной защитой.</summary>
    /// <param name="environment">Среда запуска хоста.</param>
    /// <param name="enabled">Явно установленный флаг тестового входа.</param>
    public static void ValidateEnvironment(string environment, bool enabled)
    {
        if (enabled && !string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Тестовый вход администратора разрешён только в Development. Отключите Development:BypassAdminAuthentication.");
    }

    /// <summary>Проверяет прямое loopback-подключение и локальный Host; заголовки пользователя не заменяют адрес соединения.</summary>
    /// <param name="address">Адрес непосредственного HTTP-клиента.</param>
    /// <param name="host">Имя хоста без порта.</param>
    /// <returns>True только для локального адреса соединения и localhost либо loopback в Host.</returns>
    public static bool IsLocalRequest(IPAddress? address, string host)
    {
        if (address is null || !IPAddress.IsLoopback(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address)) return false;
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || IPAddress.TryParse(host.Trim('[', ']'), out var target) && IPAddress.IsLoopback(target);
    }

    /// <summary>Создаёт доверенную личность в памяти процесса; этот тип не восстанавливается из cookie или claims браузера.</summary>
    /// <returns>Локальный тестовый Admin для SSR и серверного Blazor circuit.</returns>
    internal static ClaimsPrincipal CreatePrincipal() => new(new LocalDevelopmentIdentity());

    /// <summary>Распознаёт только объект, созданный серверным тестовым middleware, а не строковое имя схемы или claim.</summary>
    /// <param name="principal">Личность текущего запроса или circuit.</param>
    /// <returns>True для доверенного локального тестового входа.</returns>
    public static bool IsDevelopmentPrincipal(ClaimsPrincipal principal) => principal.Identity is LocalDevelopmentIdentity;

    /// <summary>Маркер серверного происхождения личности; не регистрируется как схема cookie-аутентификации.</summary>
    private sealed class LocalDevelopmentIdentity : ClaimsIdentity
    {
        /// <summary>Устанавливает отдельное имя для аудита и роль Admin без пароля и постоянной учётной записи.</summary>
        public LocalDevelopmentIdentity() : base(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "local-development-admin"),
            new Claim(ClaimTypes.Name, "Локальный тестовый администратор"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "LocalDevelopment") { }
    }
}
