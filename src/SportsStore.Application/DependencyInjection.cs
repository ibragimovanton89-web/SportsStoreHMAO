using Microsoft.Extensions.DependencyInjection;
namespace SportsStore.Application;

/// <summary>Точка регистрации зависимостей слоя в контейнере приложения.</summary>
public static class DependencyInjection
{
    /// <summary>Регистрирует зависимости прикладного слоя и возвращает контейнер для дальнейшей настройки.</summary>
    /// <param name="services">Контейнер регистрации зависимостей приложения.</param>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Здесь регистрируются прикладные зависимости; реализации с доступом к БД подключаются в Infrastructure.
        return services;
    }
}

