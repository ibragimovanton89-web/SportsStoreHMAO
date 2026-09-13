using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace SportsStore.Infrastructure.Persistence;
/// <summary>Создаёт контекст для EF CLI: построение модели возможно без секрета, применение миграций требует настройки подключения.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>Создаёт новый независимый контекст; вызывающий код обязан освободить его после операции.</summary>
    /// <param name="args">Аргументы EF CLI; параметры подключения читаются из окружения.</param>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Построение модели не требует подключения. Применение миграций требует секрета из окружения.
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=127.0.0.1;Port=55432;Database=sportsstorehmao;Username=sportsstore";
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(cs).Options);
    }
}

