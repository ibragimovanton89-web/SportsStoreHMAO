// Точка входа: сборка веб-приложения, регистрация зависимостей и порядок защитного HTTP-конвейера.
using Microsoft.AspNetCore.DataProtection;
using SportsStore.Application;
using SportsStore.Infrastructure;
using SportsStore.Web.Components;
using SportsStore.Web.Configuration;
using StoreOptions = SportsStore.Web.Configuration.StoreOptions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using SportsStore.Application.Admin;
using SportsStore.Infrastructure.Admin;
using SportsStore.Web.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddDevelopmentDatabaseFallback(builder.Environment);
// Ошибочно включённый обход на опубликованном хосте останавливает запуск.
var developmentAdmin = builder.Configuration.GetValue<bool>(DevelopmentAdminAccess.ConfigurationKey);
DevelopmentAdminAccess.ValidateEnvironment(builder.Environment.EnvironmentName, developmentAdmin);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// URL приглашения содержит одноразовый токен: стандартное журналирование полного запроса отключено.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
// Ключи защиты cookie изолированы именем приложения; эфемерный режим допустим только при разработке.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("SportsStoreHMAO");
// Постоянные ключи и их сертификат задаются окружением; локальный HTTPS-тест использует отдельное хранилище.
if (builder.Configuration["DataProtection:KeyPath"] is string keyPath)
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(keyPath)));
if (builder.Configuration["DataProtection:CertificatePath"] is string certificatePath)
    dataProtection.ProtectKeysWithCertificate(System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
        certificatePath, builder.Configuration["DataProtection:CertificatePassword"],
        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet));
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Development:UseEphemeralDataProtection"))
    dataProtection.UseEphemeralDataProtectionProvider();
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
// Ошибочные публичные настройки останавливают запуск, а не проявляются пустой шапкой сайта.
builder.Services.AddOptions<StoreOptions>()
    .Bind(builder.Configuration.GetSection(StoreOptions.SectionName))
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();
builder.Services.AddScoped<AuthenticationStateProvider, StaffAuthenticationStateProvider>();
builder.Services.AddScoped<IAdminIdentity, WebAdminIdentity>();
builder.Services.AddScoped<AdminAccess>();
builder.Services.AddScoped<IAdminCatalog, AdminCatalog>();
builder.Services.AddScoped<IPurchasing, Purchasing>();
builder.Services.AddScoped<SportsStore.Application.Storefront.IStorefront, SportsStore.Infrastructure.Services.StorefrontService>();
builder.Services.AddScoped<IAdminCommerce, AdminCommerce>();
builder.Services.AddScoped<IAdminImport, AdminImport>();
builder.Services.AddScoped<IAdminImages, AdminImages>();
builder.Services.AddScoped<IAdminPeople, AdminPeople>();
builder.Services.AddScoped<IAdminEmployees, AdminEmployees>();
builder.Services.AddScoped<IAuthorizationHandler, StaffAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminPolicies.Staff, policy => policy.RequireAuthenticatedUser().AddRequirements(new StaffRequirement(false)));
    options.AddPolicy(AdminPolicies.Administrator, policy => policy.RequireAuthenticatedUser().AddRequirements(new StaffRequirement(true)));
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
    // Перестроение principal после успешной проверки stamp не должно терять подтверждённый способ входа.
    // Значение берётся из проверенной cookie, а не из формы; при изменении stamp сессия отклоняется раньше.
    options.OnRefreshingPrincipal = context =>
    {
        if (context.CurrentPrincipal?.HasClaim("amr", "mfa") == true && context.NewPrincipal?.Identity is System.Security.Claims.ClaimsIdentity identity)
            identity.AddClaim(new System.Security.Claims.Claim("amr", "mfa"));
        return Task.CompletedTask;
    };
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/denied";
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 15, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
});
var app = builder.Build();
// Опциональное завершение TLS на локальном reverse proxy: доверяем только loopback, одному переходу.
// Прямое размещение Kestrel по HTTPS не требует этой настройки.
if (builder.Configuration.GetValue<bool>("ReverseProxy:UseLoopbackProxy"))
    app.UseForwardedHeaders(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    });
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
// Заголовки запрещают встраивание сайта и ограничивают источники ресурсов браузера.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = context.Request.Path.StartsWithSegments("/account")
        ? "no-referrer" : "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    // Текущая серверная оболочка не использует встроенные скрипты или карты импорта.
    context.Response.Headers["Content-Security-Policy"] =
        $"default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self' wss://{context.Request.Host}; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
    await next();
});
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRateLimiter();
// Порядок важен: сначала установление личности, затем права доступа и защита от CSRF.
app.UseAuthentication();
if (developmentAdmin)
{
    app.Logger.LogWarning("Включён локальный тестовый вход Admin без пароля. Только Development и loopback.");
    app.Use(async (context, next) =>
    {
        if (DevelopmentAdminAccess.IsLocalRequest(context.Connection.RemoteIpAddress, context.Request.Host.Host))
            context.User = DevelopmentAdminAccess.CreatePrincipal();
        await next();
    });
}
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorPages();
// Опубликованные изображения доступны покупателю; черновики требуют действующей сессии сотрудника.
app.MapGet("/media/{name}", async (string name, SportsStore.Infrastructure.Persistence.ApplicationDbContext db, IConfiguration config, HttpContext context, IAuthorizationService authorization) =>
{
    if (!name.EndsWith(".webp", StringComparison.Ordinal) || !Guid.TryParseExact(name[..^5], "N", out var id)) return Results.NotFound();
    var image = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(db.ProductImages, x => x.Id == id);
    if (image is null) return Results.NotFound();
    var published = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Products, p => p.Id == image.ProductId && p.Status == SportsStore.Domain.Entities.ProductStatus.Published);
    if (!published && !(await authorization.AuthorizeAsync(context.User, null, AdminPolicies.Staff)).Succeeded) return Results.NotFound();
    context.Response.Headers.CacheControl = "no-store";
    var root = config["Storage:RootPath"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SportsStoreHMAO", "storage");
    var path = Path.Combine(Path.GetFullPath(root), "images", id.ToString("N") + ".webp");
    return File.Exists(path) ? Results.File(path, "image/webp") : Results.NotFound();
});
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();


