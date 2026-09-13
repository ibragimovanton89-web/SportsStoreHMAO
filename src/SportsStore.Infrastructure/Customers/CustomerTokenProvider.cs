using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportsStore.Infrastructure.Identity;

namespace SportsStore.Infrastructure.Customers;

/// <summary>Отдельный срок покупательских ссылок, не меняющий провайдер приглашений сотрудников.</summary>
public sealed class CustomerTokenOptions : DataProtectionTokenProviderOptions
{
    /// <summary>Изолирует назначение ключа и ограничивает подтверждение/восстановление одним часом.</summary>
    public CustomerTokenOptions() { Name = "SportsStore.CustomerAccounts"; TokenLifespan = TimeSpan.FromHours(1); }
}

/// <summary>Стандартный Data Protection токен Identity с отдельной конфигурацией покупательских писем.</summary>
/// <param name="protection">Защищённое хранилище ключей приложения.</param><param name="options">Срок и назначение покупательских ссылок.</param>
/// <param name="logger">Стандартный журнал проверки без вывода самого токена.</param>
public sealed class CustomerTokenProvider(IDataProtectionProvider protection, IOptions<CustomerTokenOptions> options,
    ILogger<DataProtectorTokenProvider<ApplicationUser>> logger) : DataProtectorTokenProvider<ApplicationUser>(protection, options, logger);
