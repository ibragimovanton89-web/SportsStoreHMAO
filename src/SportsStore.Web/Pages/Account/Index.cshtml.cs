using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SportsStore.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Web.Pages.Account;

/// <summary>HTTP-сценарии Identity: cookie устанавливается до отправки ответа, формы защищены antiforgery.</summary>
/// <param name="users">Стандартный менеджер Identity.</param><param name="signIn">Менеджер входа и MFA.</param>
/// <param name="db">Контекст той же HTTP-операции Identity для атомарного принятия приглашения и аудита.</param>
[EnableRateLimiting("account")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IndexModel(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, ApplicationDbContext db) : PageModel
{
    /// <summary>Раздел аккаунта из маршрута; разрешённые значения проверяются явно.</summary>
    [BindProperty(SupportsGet = true)] public string ActionName { get; set; } = "login";
    /// <summary>Адрес возврата; допускается только локальный маршрут.</summary>
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    /// <summary>Адрес сотрудника для входа.</summary>
    [BindProperty] public string Email { get; set; } = "";
    /// <summary>Пароль текущей формы; не сохраняется в журналах или TempData.</summary>
    [BindProperty] public string Password { get; set; } = "";
    /// <summary>Новый пароль при смене или принятии приглашения.</summary>
    [BindProperty] public string NewPassword { get; set; } = "";
    /// <summary>Одноразовый код TOTP либо код восстановления.</summary>
    [BindProperty] public string Code { get; set; } = "";
    /// <summary>Явный выбор входа с резервным кодом.</summary>
    [BindProperty] public bool Recovery { get; set; }
    /// <summary>Идентификатор приглашения; принадлежность проверяется токеном Identity.</summary>
    [BindProperty(SupportsGet = true)] public string? UserId { get; set; }
    /// <summary>Стандартный одноразовый Identity-токен приглашения; не журналируется.</summary>
    [BindProperty(SupportsGet = true)] public string? Token { get; set; }
    /// <summary>Нейтральная ошибка для отображения.</summary>
    public string? Error { get; private set; }
    /// <summary>Секрет настройки TOTP показывается только текущему сотруднику до включения MFA.</summary>
    public string? SharedKey { get; private set; }
    /// <summary>Резервные коды выводятся один раз после генерации, не передаются в URL.</summary>
    public string[] RecoveryCodes { get; private set; } = [];
    /// <summary>Подтверждение выполненного действия.</summary>
    public string? Message { get; private set; }

    /// <summary>Показывает страницу без изменения данных; ключ TOTP создаётся отдельной POST-командой.</summary>
    public async Task<IActionResult> OnGetAsync()
    {
        if (ActionName is not ("login" or "mfa" or "setup" or "password" or "denied" or "invite")) return NotFound();
        if (ActionName is "setup" or "password")
        {
            var user = await CurrentStaffAsync(); if (user is null) return RedirectToPage(new { ActionName = "login" });
            if (ActionName == "password" && (!user.TwoFactorEnabled || !User.HasClaim("amr", "mfa"))) return RedirectToPage(new { ActionName = "setup" });
            if (ActionName == "setup" && !user.TwoFactorEnabled) SharedKey = await users.GetAuthenticatorKeyAsync(user);
        }
        return Page();
    }

    /// <summary>Входит с обязательным lockout; сообщение не раскрывает наличие email или причину отказа.</summary>
    public async Task<IActionResult> OnPostLoginAsync()
    {
        ActionName = "login";
        Email ??= ""; Password ??= "";
        if (Email.Length > 256 || Password.Length > 1024) return Failed();
        var user = await users.FindByEmailAsync(Email.Trim());
        if (user is null || !await IsStaffAsync(user)) return Failed();
        var result = await signIn.PasswordSignInAsync(user, Password, false, lockoutOnFailure: true);
        if (result.RequiresTwoFactor) return RedirectToPage(new { ActionName = "mfa", ReturnUrl = SafeReturn() });
        if (!result.Succeeded) return Failed();
        return LocalRedirect(user.TwoFactorEnabled ? SafeReturn() : "/account/setup");
    }

    /// <summary>Завершает вход через стандартный TOTP либо одноразовый recovery code без запоминания браузера.</summary>
    public async Task<IActionResult> OnPostMfaAsync()
    {
        ActionName = "mfa"; Code ??= ""; var user = await signIn.GetTwoFactorAuthenticationUserAsync();
        if (user is null || !user.EmailConfirmed || await users.IsLockedOutAsync(user) || !await IsStaffAsync(user) || Code.Length > 100) return Failed();
        var result = Recovery ? await signIn.TwoFactorRecoveryCodeSignInAsync(Code.Trim())
            : await signIn.TwoFactorAuthenticatorSignInAsync(Code.Replace(" ", "").Replace("-", ""), false, false);
        if (Recovery && !result.Succeeded) await users.AccessFailedAsync(user);
        return result.Succeeded ? LocalRedirect(SafeReturn()) : Failed();
    }

    /// <summary>Создаёт ключ только до первоначального включения MFA; сброс действующей MFA этим маршрутом запрещён.</summary>
    public async Task<IActionResult> OnPostStartSetupAsync()
    {
        ActionName = "setup"; var user = await CurrentStaffAsync(); if (user is null) return Unauthorized();
        if (user.TwoFactorEnabled) return RedirectToPage(new { ActionName = "login" });
        if (string.IsNullOrEmpty(await users.GetAuthenticatorKeyAsync(user)))
        {
            var reset = await users.ResetAuthenticatorKeyAsync(user);
            if (!reset.Succeeded) return Failed();
            // Identity меняет security stamp при создании ключа: обновляем cookie до следующей POST-формы.
            await signIn.RefreshSignInAsync(user);
        }
        SharedKey = await users.GetAuthenticatorKeyAsync(user); return Page();
    }

    /// <summary>Проверяет TOTP стандартным провайдером, включает MFA и показывает резервные коды только в текущем ответе.</summary>
    public async Task<IActionResult> OnPostSetupAsync()
    {
        Code ??= "";
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861002)");
        ActionName = "setup"; var user = await CurrentStaffAsync(); if (user is null) return Unauthorized();
        if (user.TwoFactorEnabled) return RedirectToPage(new { ActionName = "login" });
        SharedKey = await users.GetAuthenticatorKeyAsync(user);
        if (Code.Length > 20 || !await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, Code.Replace(" ", "").Replace("-", "")))
        {
            Ensure(await users.AccessFailedAsync(user)); await tx.CommitAsync();
            Error = "Код не принят. Проверьте время устройства и повторите попытку."; return Page();
        }
        Ensure(await users.SetTwoFactorEnabledAsync(user, true)); Ensure(await users.ResetAccessFailedCountAsync(user));
        RecoveryCodes = (await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))!.ToArray(); SharedKey = null;
        db.AdminAudits.Add(new() { ActorId = user.Id, Action = "staff.mfa-enabled", ObjectType = "ApplicationUser", ObjectId = user.Id,
            Description = "Сотрудник подтвердил TOTP; секрет и резервные коды не записываются в аудит.", OperationId = Guid.NewGuid() });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        await signIn.SignInWithClaimsAsync(user, false, [new Claim("amr", "mfa")]);
        Message = "Двухфакторная защита включена. Сохраните резервные коды в защищённом месте: повторно они не показываются.";
        return Page();
    }

    /// <summary>Меняет собственный пароль после MFA, инвалидируя старые сессии и обновляя текущую cookie.</summary>
    public async Task<IActionResult> OnPostPasswordAsync()
    {
        ActionName = "password"; var user = await CurrentStaffAsync();
        Password ??= ""; NewPassword ??= "";
        if (user is null || !user.TwoFactorEnabled || !User.HasClaim("amr", "mfa")) return Unauthorized();
        if (Password.Length > 1024 || NewPassword.Length > 1024) return Failed();
        var result = await users.ChangePasswordAsync(user, Password, NewPassword);
        if (!result.Succeeded) { Error = "Пароль не изменён. Проверьте текущий пароль и требования к новому."; return Page(); }
        await signIn.RefreshSignInAsync(user); Message = "Пароль изменён. Остальные сессии будут отозваны."; return Page();
    }

    /// <summary>Принимает одноразовое приглашение без публичной регистрации или повышения существующего аккаунта.</summary>
    public async Task<IActionResult> OnPostInviteAsync()
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861002)");
        ActionName = "invite"; var user = UserId is null ? null : await users.FindByIdAsync(UserId);
        NewPassword ??= "";
        if (user is null || user.EmailConfirmed || await users.HasPasswordAsync(user) || await users.IsLockedOutAsync(user)
            || !await IsStaffAsync(user) || Token is null || Token.Length > 4000 || NewPassword.Length > 1024
            || !await users.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "StaffInvitation", Token)) return Failed();
        var result = await users.AddPasswordAsync(user, NewPassword);
        if (!result.Succeeded) { Error = "Пароль должен содержать не менее 12 символов, разные регистры, цифру и специальный знак."; return Page(); }
        user.EmailConfirmed = true; Ensure(await users.UpdateSecurityStampAsync(user));
        db.AdminAudits.Add(new() { ActorId = user.Id, Action = "staff.accept-invitation", ObjectType = "ApplicationUser", ObjectId = user.Id,
            Description = "Сотрудник принял одноразовое приглашение и задал собственный пароль; токен не сохранялся в журнале.", OperationId = Guid.NewGuid() });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return RedirectToPage(new { ActionName = "login" });
    }

    /// <summary>Выходит только через POST с antiforgery; GET не изменяет аутентификацию.</summary>
    public async Task<IActionResult> OnPostLogoutAsync() { await signIn.SignOutAsync(); return LocalRedirect("/account/login"); }

    /// <summary>Проверяет актуального сотрудника и stamp, разрешая базовую сессию лишь для настройки MFA.</summary>
    private async Task<ApplicationUser?> CurrentStaffAsync()
    {
        var user = await users.GetUserAsync(User);
        return user is not null && user.EmailConfirmed && !await users.IsLockedOutAsync(user) && await IsStaffAsync(user)
            && user.SecurityStamp == User.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType) ? user : null;
    }
    /// <summary>Проверяет роли сотрудника из текущей БД.</summary>
    private async Task<bool> IsStaffAsync(ApplicationUser user) => await users.IsInRoleAsync(user, "Admin") || await users.IsInRoleAsync(user, "Manager");
    /// <summary>Не допускает открытый редирект на чужой сайт.</summary>
    private string SafeReturn() => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/admin";
    /// <summary>Возвращает одинаковую ошибку входа для всех причин отказа.</summary>
    private PageResult Failed() { Error = "Не удалось выполнить вход или подтвердить действие. Проверьте данные и попробуйте позже."; return Page(); }
    /// <summary>Прерывает транзакцию при отказе Identity без раскрытия полей аккаунта или токенов.</summary>
    private static void Ensure(IdentityResult result) { if (!result.Succeeded) throw new InvalidOperationException("Identity отклонил изменение аккаунта."); }
}
