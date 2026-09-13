using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Customers;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Identity;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Web.Pages.Customer;

/// <summary>HTTP-формы покупателя с antiforgery; выдача cookie и смена пароля выполняются вне Blazor circuit.</summary>
/// <param name="accounts">Защищённые операции кабинета.</param><param name="registration">Регистрация и письма Identity.</param>
/// <param name="users">Стандартный менеджер Identity.</param><param name="signIn">Стандартная выдача и отзыв cookie.</param><param name="db">HTTP-контекст Identity для проверки связи покупателя.</param>
/// <param name="throttle">Ограничитель входа по нормализованному адресу.</param>
[EnableRateLimiting("customer-account")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IndexModel(ICustomerAccount accounts, ICustomerRegistration registration, UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn, ApplicationDbContext db, SportsStore.Infrastructure.Customers.AccountEmailThrottle throttle) : PageModel
{
    /// <summary>Имя разрешённой страницы из маршрута.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty(SupportsGet = true)] public string ActionName { get; set; } = "login";
    /// <summary>Адрес возврата; используются только собственные маршруты магазина.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    /// <summary>Почта в публичной форме; в профиле не редактируется.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string Email { get; set; } = "";
    /// <summary>Пароль только текущего POST; не выводится обратно в HTML.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string Password { get; set; } = "";
    /// <summary>Новый пароль регистрации или смены; не журналируется.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string NewPassword { get; set; } = "";
    /// <summary>Повтор пароля для обнаружения опечатки.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string ConfirmPassword { get; set; } = "";
    /// <summary>Покупательское отображаемое имя.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string DisplayName { get; set; } = "";
    /// <summary>Неподтверждённый контактный телефон Identity.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string? Phone { get; set; }
    /// <summary>Разрешённый для самостоятельного изменения правовой тип.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public CustomerKind Kind { get; set; }
    /// <summary>Официальное наименование для ИП или организации.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string? LegalName { get; set; }
    /// <summary>ИНН строкой без потери ведущих нулей.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string? Inn { get; set; }
    /// <summary>Необязательный КПП организации.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string? Kpp { get; set; }
    /// <summary>Версия редактируемой записи для обнаружения конфликта.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public uint Version { get; set; }
    /// <summary>Явное согласие отозвать опт при смене реквизитов.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public bool AcknowledgeRevocation { get; set; }
    /// <summary>Идентификатор собственной записи адреса или заявки; сервис проверяет владение.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public Guid RecordId { get; set; }
    /// <summary>Получатель по адресу.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string Recipient { get; set; } = "";
    /// <summary>Текст собственного адреса.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string Address { get; set; } = "";
    /// <summary>Необязательный индекс.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string? PostalCode { get; set; }
    /// <summary>Текст обращения покупателя на опт.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public string? Comment { get; set; }
    /// <summary>Идентификатор команды, сохраняемый при повторном POST.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty] public Guid OperationId { get; set; } = Guid.NewGuid();
    /// <summary>Пользователь ссылки Identity; не используется как владелец операций кабинета.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty(SupportsGet = true)] public string UserId { get; set; } = "";
    /// <summary>Ограниченный одноразовый код из письма.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindProperty(SupportsGet = true)] public string Token { get; set; } = "";
    /// <summary>Безопасное сообщение результата без сведений о существовании чужой учётной записи.</summary>
    public string? Message { get; private set; }
    /// <summary>Безопасная ошибка ввода или конфликта; исходные исключения не выводятся.</summary>
    public string? Error { get; private set; }
    /// <summary>Собственный профиль для вывода условий и подтверждённой почты.</summary>
    public CustomerProfile? Profile { get; private set; }
    /// <summary>Список собственных адресов.</summary>
    public IReadOnlyList<AddressInput> Addresses { get; private set; } = [];
    /// <summary>Последние собственные заявки.</summary>
    public IReadOnlyList<WholesaleApplicationData> Applications { get; private set; } = [];
    /// <summary>Публичная история собственных решений.</summary>
    public IReadOnlyList<WholesaleHistory> History { get; private set; } = [];
    /// <summary>Нейтральное сообщение всех почтовых запросов.</summary>
    private const string MailMessage = "Если действие доступно для указанной почты, письмо будет отправлено. Проверьте входящие и папку «Спам». При необходимости повторите запрос позже.";
    /// <summary>Названия страниц для заголовка и семантического h1.</summary>
    public string Title => ActionName switch
    {
        "register" => "Создать аккаунт", "confirm-email" => "Подтверждение почты", "resend-confirmation" => "Повторить подтверждение",
        "forgot-password" => "Восстановить пароль", "reset-password" => "Новый пароль", "profile" => "Мой профиль",
        "addresses" => "Мои адреса", "wholesale" => "Оптовые условия", "security" => "Безопасность", _ => "Вход покупателя"
    };
    /// <summary>Разрешённая защищённая область, не определяемая параметром обработчика формы.</summary>
    public bool IsPrivate => ActionName is "profile" or "addresses" or "wholesale" or "security";
    /// <summary>Возвращает русское название состояния заявки.</summary>
    public static string StatusName(WholesaleApplicationStatus status) => status switch
    {
        WholesaleApplicationStatus.Pending => "Ожидает проверки", WholesaleApplicationStatus.Approved => "Одобрено",
        WholesaleApplicationStatus.Rejected => "Отклонено", WholesaleApplicationStatus.Withdrawn => "Заявка отозвана", _ => "Опт отозван"
    };
    /// <summary>Загружает только разрешённый маршрут; подтверждение выполняется отдельным POST, чтобы почтовый сканер не расходовал ссылку.</summary>
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (ActionName is not ("register" or "login" or "confirm-email" or "resend-confirmation" or "forgot-password" or "reset-password" or "profile" or "addresses" or "wholesale" or "security" or "logout")) return NotFound();
        if (ActionName == "logout") return StatusCode(405);
        try { if (IsPrivate) await LoadAsync(true, ct); }
        catch (UnauthorizedAccessException) { return Redirect("/customer/login?returnUrl=" + Uri.EscapeDataString(Request.Path)); }
        catch (Exception ex) when (ex is DbUpdateException or Npgsql.NpgsqlException or InvalidOperationException)
        { Error = "Кабинет временно недоступен. Попробуйте обновить страницу позже."; Response.StatusCode = 503; }
        return Page();
    }
    /// <summary>Обрабатывает формы с allowlist действий; ASP.NET Core проверяет antiforgery до вызова.</summary>
    public async Task<IActionResult> OnPostAsync(string? command, CancellationToken ct)
    {
        // Пустые form-поля binder может преобразовать в null несмотря на CLR-аннотацию.
        Email ??= ""; Password ??= ""; NewPassword ??= ""; ConfirmPassword ??= ""; DisplayName ??= "";
        UserId ??= ""; Token ??= ""; Recipient ??= ""; Address ??= "";
        // Одна PageModel обслуживает разные формы: обязательность строк проверяется только в соответствующем сценарии.
        // Ошибки привязки чисел, enum, bool и Guid остаются в ModelState и запрещают выполнение команды.
        foreach (var field in new[] { nameof(Email), nameof(Password), nameof(NewPassword), nameof(ConfirmPassword), nameof(DisplayName), nameof(UserId), nameof(Token), nameof(Recipient), nameof(Address) })
            if (ModelState.TryGetValue(field, out var entry)) { entry.Errors.Clear(); entry.ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid; }
        try
        {
            if (!ModelState.IsValid) throw new ArgumentException("Проверьте формат введённых данных.");
            switch (ActionName)
            {
                case "register":
                    MatchPasswords(); await registration.RegisterAsync(Email, NewPassword, DisplayName, ct); Message = MailMessage; break;
                case "resend-confirmation": await registration.ResendAsync(Email, ct); Message = MailMessage; break;
                case "forgot-password": await registration.ForgotAsync(Email, ct); Message = MailMessage; break;
                case "confirm-email":
                    Message = await registration.ConfirmAsync(UserId, Token, ct) ? "Почта подтверждена. Теперь можно войти." : "Ссылка недействительна или уже использована. Можно запросить новое подтверждение."; break;
                case "reset-password":
                    MatchPasswords();
                    Message = await registration.ResetAsync(UserId, Token, NewPassword, ct) ? "Пароль изменён. Войдите заново." : "Не удалось изменить пароль. Проверьте пароль или запросите новую ссылку."; break;
                case "login":
                    if (Email.Length > 254 || Password.Length > 1024) throw new ArgumentException("Не удалось войти. Проверьте данные и подтверждение почты.");
                    if (!throttle.Allow(users.NormalizeEmail(Email.Trim()), "login", 10)) throw new ArgumentException("Не удалось войти. Попробуйте позже.");
                    var user = await users.FindByEmailAsync(Email.Trim());
                    if (user is null || await users.IsInRoleAsync(user, "Admin") || await users.IsInRoleAsync(user, "Manager")
                        || !await db.Customers.AnyAsync(x => x.ApplicationUserId == user.Id, ct)) throw new ArgumentException("Не удалось войти. Проверьте данные и подтверждение почты. Сотрудникам доступен отдельный вход.");
                    var result = await signIn.PasswordSignInAsync(user, Password, false, true);
                    if (!result.Succeeded) throw new ArgumentException("Не удалось войти. Проверьте данные и подтверждение почты или попробуйте позже.");
                    // Cookie уже выдана, а principal текущего POST нужно обновить до проверки владельца корзины.
                    HttpContext.User = await signIn.CreateUserPrincipalAsync(user);
                    await HttpContext.RequestServices.GetRequiredService<SportsStore.Application.Commerce.ICommerce>().MergeAsync(ct);
                    HttpContext.RequestServices.GetRequiredService<SportsStore.Web.Security.GuestCartIdentity>().Clear(HttpContext);
                    return LocalRedirect(SafeReturn());
                case "logout": await signIn.SignOutAsync(); return Redirect("/catalog");
                case "profile":
                    await accounts.SaveProfileAsync(new(Version, DisplayName, Phone, Kind, LegalName, Inn, Kpp), AcknowledgeRevocation, OperationId, ct);
                    return Redirect("/customer/profile");
                case "addresses":
                    if (command == "delete") await accounts.DeleteAddressAsync(RecordId, Version, ct);
                    else await accounts.SaveAddressAsync(new(RecordId, Version, Recipient, Address, PostalCode), ct);
                    return Redirect("/customer/addresses");
                case "wholesale":
                    if (command == "withdraw") await accounts.WithdrawAsync(RecordId, Version, OperationId, ct);
                    else await accounts.SubmitAsync(Comment, OperationId, ct);
                    return Redirect("/customer/wholesale");
                case "security":
                    if (Password.Length > 1024) throw new ArgumentException("Проверьте текущий пароль.");
                    await accounts.ProfileAsync(ct); MatchPasswords();
                    var current = await users.GetUserAsync(User) ?? throw new UnauthorizedAccessException();
                    if (!(await users.ChangePasswordAsync(current, Password, NewPassword)).Succeeded) throw new ArgumentException("Проверьте текущий пароль и требования к новому паролю.");
                    await signIn.RefreshSignInAsync(current); Message = "Пароль изменён. Остальные сессии отозваны."; break;
                default: return NotFound();
            }
        }
        catch (UnauthorizedAccessException) { return Redirect("/customer/login"); }
        catch (DbUpdateConcurrencyException) { Error = "Данные изменились в другой вкладке. Введённый текст сохранён в форме: скопируйте его и обновите страницу перед повтором."; }
        catch (ArgumentException ex) { Error = ex.Message; }
        catch (Exception ex) when (ex is DbUpdateException or Npgsql.NpgsqlException or InvalidOperationException) { Error = "Операция временно недоступна. Введённые данные сохранены в форме. Попробуйте позже."; }
        Password = NewPassword = ConfirmPassword = "";
        ModelState.Remove(nameof(Password)); ModelState.Remove(nameof(NewPassword)); ModelState.Remove(nameof(ConfirmPassword));
        if (IsPrivate)
        {
            try { await LoadAsync(false, ct); } catch (UnauthorizedAccessException) { return Redirect("/customer/login"); }
            catch (Exception ex) when (ex is DbUpdateException or Npgsql.NpgsqlException or InvalidOperationException) { Error = "Кабинет временно недоступен. Попробуйте позже."; Response.StatusCode = 503; }
        }
        return Page();
    }
    /// <summary>Проверяет повтор и разумную длину пароля до обращения к Identity.</summary>
    private void MatchPasswords()
    {
        if (NewPassword != ConfirmPassword || NewPassword.Length is < 12 or > 1024) throw new ArgumentException("Пароли должны совпадать и содержать минимум 12 символов.");
    }
    /// <summary>Разрешает возврат только в каталог и кабинет, исключая внешние адреса и административные маршруты.</summary>
    private string SafeReturn() => Url.IsLocalUrl(ReturnUrl) && ((ReturnUrl == "/cart" || ReturnUrl == "/checkout" || ReturnUrl!.StartsWith("/catalog", StringComparison.Ordinal)) || ReturnUrl.StartsWith("/customer/", StringComparison.Ordinal)) ? ReturnUrl : "/customer/profile";
    /// <summary>Читает защищённые данные; после ошибки не перезаписывает введённые поля или ожидаемую версию.</summary>
    private async Task LoadAsync(bool populate, CancellationToken ct)
    {
        Profile = await accounts.ProfileAsync(ct);
        if (populate)
        {
            var p = Profile.Profile; DisplayName = p.Name; Phone = p.Phone; Kind = p.Kind; LegalName = p.LegalName; Inn = p.Inn; Kpp = p.Kpp; Version = p.Version;
        }
        if (ActionName == "addresses") Addresses = await accounts.AddressesAsync(ct);
        if (ActionName == "wholesale") { Applications = await accounts.ApplicationsAsync(ct); History = await accounts.HistoryAsync(ct); }
    }
}
