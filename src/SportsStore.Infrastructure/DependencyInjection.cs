using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsStore.Infrastructure.Identity;
using SportsStore.Infrastructure.Persistence;
namespace SportsStore.Infrastructure;

/// <summary>Точка регистрации зависимостей слоя в контейнере приложения.</summary>
public static class DependencyInjection
{
    /// <summary>Регистрирует парсер, сервисы, фабрику PostgreSQL и Identity; настраивает парольную политику и защищённые cookie без вывода секретов.</summary>
    /// <param name="services">Контейнер регистрации зависимостей приложения.</param>
    /// <param name="configuration">Конфигурация приложения с подключением из защищённого источника.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<SportsStore.Infrastructure.Commerce.CommerceService>();
        services.AddScoped<SportsStore.Application.Commerce.ICommerce>(sp => sp.GetRequiredService<SportsStore.Infrastructure.Commerce.CommerceService>());
        services.AddSingleton<SportsStore.Infrastructure.Commerce.OrderNotificationProcessor>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddSingleton<SportsStore.Application.Import.ISupplierPriceParser, SportsStore.Infrastructure.Import.BallMarketParser>();
        services.AddTransient<SportsStore.Application.Import.IPriceImportService, SportsStore.Infrastructure.Import.PriceImportService>();
        services.AddTransient<SportsStore.Application.Pricing.IPricingService, SportsStore.Infrastructure.Pricing.PricingService>();
        services.AddScoped<SportsStore.Infrastructure.Customers.CustomerAccess>();
        services.AddScoped<SportsStore.Application.Customers.ICustomerAccount, SportsStore.Infrastructure.Customers.CustomerAccount>();
        services.AddScoped<SportsStore.Application.Customers.IWholesaleAdministration, SportsStore.Infrastructure.Customers.WholesaleAdministration>();
        services.AddSingleton<SportsStore.Infrastructure.Customers.AccountEmailThrottle>();
        services.AddSingleton<SportsStore.Infrastructure.Customers.CustomerEmailTransport>();
        services.AddSingleton<SportsStore.Application.Customers.ICustomerEmailTransport>(sp => sp.GetRequiredService<SportsStore.Infrastructure.Customers.CustomerEmailTransport>());
        services.AddTransient<SportsStore.Application.Customers.ICustomerRegistration, SportsStore.Infrastructure.Customers.CustomerRegistration>();
        services.AddSingleton<SportsStore.Infrastructure.Customers.NotificationProcessor>();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection using user-secrets or environment variables.");

        // Отдельный контекст на операцию предотвращает совместное использование DbContext между событиями Blazor.
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString).EnableSensitiveDataLogging(false).EnableDetailedErrors(false));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequiredUniqueChars = 4;
            options.SignIn.RequireConfirmedEmail = true;
            options.Tokens.EmailConfirmationTokenProvider = "CustomerAccounts";
            options.Tokens.PasswordResetTokenProvider = "CustomerAccounts";
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders()
            .AddTokenProvider<SportsStore.Infrastructure.Customers.CustomerTokenProvider>("CustomerAccounts");

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "__Host-SportsStore.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
        });
        return services;
    }
}
