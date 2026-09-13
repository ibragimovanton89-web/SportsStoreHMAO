using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsStore.Application.Customers;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Identity;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Customers;

/// <summary>Создаёт Identity и покупателя в одном scoped DbContext; токены писем генерируются по запросу и не сохраняются в outbox.</summary>
/// <param name="scopes">Независимая область Identity на операцию.</param><param name="transport">Транспорт письма.</param>
/// <param name="settings">Проверенный базовый адрес писем.</param><param name="throttle">Ограничение действий по нормализованной почте.</param>
public sealed class CustomerRegistration(IServiceScopeFactory scopes, ICustomerEmailTransport transport, CustomerEmailTransport settings,
    AccountEmailThrottle throttle) : ICustomerRegistration
{
    /// <summary>Проверяет синтаксис адреса до нормализации Identity, не выводя введённое значение в ошибку.</summary>
    private static string Email(string email)
    {
        email = email.Trim();
        if (email.Length is < 3 or > 254 || !MailAddress.TryCreate(email, out var address) || address.Address != email)
            throw new ArgumentException("Введите корректный адрес электронной почты.");
        return email;
    }
    /// <summary>Атомарно создаёт только новую связь; существующие пользователь и покупатель никогда не присваиваются по совпадению почты.</summary>
    public async Task RegisterAsync(string email, string password, string displayName, CancellationToken ct = default)
    {
        email = Email(email); displayName = Admin.AdminAccess.Text(displayName, "Имя", 200);
        if (password.Length is < 12 or > 1024) throw new ArgumentException("Пароль должен содержать от 12 до 1024 символов.");
        await using var scope = scopes.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var normalized = users.NormalizeEmail(email);
        if (!throttle.Allow(normalized, "register")) return;
        // Проверяем пароль до поиска существующей почты, чтобы политика не раскрывала занятый адрес.
        var user = new ApplicationUser { UserName = email, Email = email, LockoutEnabled = true };
        foreach (var validator in users.PasswordValidators)
            if (!(await validator.ValidateAsync(users, user, password)).Succeeded) throw new ArgumentException("Используйте минимум 12 символов, прописные и строчные буквы, цифры и специальный символ.");
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            // Сериализация регистрации одного адреса; уникальное нормализованное имя Identity дополнительно защищает гонку.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({normalized}, 74041))", ct);
            if (await users.FindByEmailAsync(email) is not null) return;
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded) return;
            if (!(await users.AddToRoleAsync(user, "Customer")).Succeeded) throw new InvalidOperationException("Роль покупателя не настроена.");
            db.Customers.Add(new() { ApplicationUserId = user.Id, DisplayName = displayName, Kind = CustomerKind.Individual,
                Segment = CustomerSegment.Retail, WholesaleStatus = WholesaleStatus.NotRequested });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        await SendTokenAsync(users, user, false, ct);
    }
    /// <summary>Повторное подтверждение имеет нейтральный результат для неизвестной и уже подтверждённой почты.</summary>
    public Task ResendAsync(string email, CancellationToken ct = default) => RequestAsync(email, false, ct);
    /// <summary>Восстановление допускается только для подтверждённого покупателя.</summary>
    public Task ForgotAsync(string email, CancellationToken ct = default) => RequestAsync(email, true, ct);
    /// <summary>Применяет лимит к каждому адресу до проверки существования пользователя.</summary>
    private async Task RequestAsync(string email, bool reset, CancellationToken ct)
    {
        email = Email(email);
        await using var scope = scopes.CreateAsyncScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (!throttle.Allow(users.NormalizeEmail(email), reset ? "reset" : "confirm")) return;
        var user = await users.FindByEmailAsync(email);
        if (user is null || user.EmailConfirmed != reset || await users.IsInRoleAsync(user, "Admin") || await users.IsInRoleAsync(user, "Manager")) return;
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await db.Customers.AnyAsync(x => x.ApplicationUserId == user.Id, ct)) return;
        await SendTokenAsync(users, user, reset, ct);
    }
    /// <summary>Отправляет одноразовую ссылку только после фиксации пользователя; ошибка транспорта не меняет публичный ответ.</summary>
    private async Task SendTokenAsync(UserManager<ApplicationUser> users, ApplicationUser user, bool reset, CancellationToken ct)
    {
        var token = reset ? await users.GeneratePasswordResetTokenAsync(user) : await users.GenerateEmailConfirmationTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        try
        {
            var link = new Uri(settings.PublicOrigin(), $"customer/{(reset ? "reset-password" : "confirm-email")}?userId={Uri.EscapeDataString(user.Id)}&token={encoded}");
            await transport.SendAsync(new(user.Email!, reset ? "Восстановление пароля SportsStoreHMAO" : "Подтвердите почту SportsStoreHMAO",
                $"{(reset ? "Для смены пароля" : "Для подтверждения почты")} откройте ссылку:\n{link}\n\nСсылка действует один час. Если вы не запрашивали действие, проигнорируйте письмо.", Guid.NewGuid()), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or IOException or InvalidOperationException or ArgumentException)
        { /* Без токена в журнале и очереди: покупатель может повторить отправку после восстановления транспорта. */ }
    }
    /// <summary>Безопасно декодирует ограниченный URL-токен, не принимая исходные ошибки в пользовательский ответ.</summary>
    private static string? Token(string encoded)
    {
        if (encoded.Length is < 1 or > 6000) return null;
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded)); } catch (FormatException) { return null; }
    }
    /// <summary>Подтверждает только покупателя без действующего подтверждения; повтор ссылки не выполняет изменение.</summary>
    public async Task<bool> ConfirmAsync(string userId, string encodedToken, CancellationToken ct = default)
    {
        if (userId.Length > 450 || Token(encodedToken) is not string token) return false;
        await using var scope = scopes.CreateAsyncScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var user = await users.FindByIdAsync(userId);
        if (user is null || user.EmailConfirmed || !await db.Customers.AnyAsync(x => x.ApplicationUserId == userId, ct)) return false;
        return (await users.ConfirmEmailAsync(user, token)).Succeeded;
    }
    /// <summary>Стандартный ResetPasswordAsync обновляет stamp и делает использованный токен недействительным.</summary>
    public async Task<bool> ResetAsync(string userId, string encodedToken, string password, CancellationToken ct = default)
    {
        if (userId.Length > 450 || password.Length is < 12 or > 1024 || Token(encodedToken) is not string token) return false;
        await using var scope = scopes.CreateAsyncScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var user = await users.FindByIdAsync(userId);
        if (user is null || !user.EmailConfirmed || !await db.Customers.AnyAsync(x => x.ApplicationUserId == userId, ct)
            || await users.IsInRoleAsync(user, "Admin") || await users.IsInRoleAsync(user, "Manager")) return false;
        return (await users.ResetPasswordAsync(user, token, password)).Succeeded;
    }
}
