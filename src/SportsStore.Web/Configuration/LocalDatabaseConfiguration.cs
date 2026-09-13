using Npgsql;

namespace SportsStore.Web.Configuration;

/// <summary>Подключает локальную Docker-БД при запуске Development из IDE без наследования PowerShell-окружения.</summary>
public static class LocalDatabaseConfiguration
{
    /// <summary>Использует .env корня решения только при отсутствии явно настроенного подключения; Production не читает файл.</summary>
    /// <param name="configuration">Конфигурация после загрузки appsettings, User Secrets, окружения и аргументов.</param>
    /// <param name="environment">Среда и корень веб-проекта для фиксированного пути к локальному .env.</param>
    /// <exception cref="InvalidOperationException">Локальный файл существует, но необходимые параметры некорректны.</exception>
    public static void AddDevelopmentDatabaseFallback(this ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        // Явное подключение, включая отдельную тестовую БД, всегда имеет приоритет.
        if (!environment.IsDevelopment() || !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection"))) return;
        var path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", ".env"));
        if (!File.Exists(path)) return;
        if (new FileInfo(path).Length > 65536) throw new InvalidOperationException("Локальный .env превышает допустимый размер.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadLines(path))
        {
            var separator = line.IndexOf('=');
            if (separator <= 0 || line.TrimStart().StartsWith('#')) continue;
            var key = line[..separator].Trim();
            // Значения не исполняются; административный пароль для приложения не читается.
            if (key is "POSTGRES_PORT" or "APP_DB_PASSWORD") values[key] = line[(separator + 1)..];
        }
        if (!values.TryGetValue("POSTGRES_PORT", out var portValue) || !int.TryParse(portValue, out var port) || port is < 1 or > 65535
            || !values.TryGetValue("APP_DB_PASSWORD", out var password) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Укажите POSTGRES_PORT и APP_DB_PASSWORD в локальном .env или настройте ConnectionStrings:DefaultConnection.");
        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1", Port = port, Database = "sportsstorehmao", Username = "sportsstore",
            Password = password, IncludeErrorDetail = false
        };
        configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connection.ConnectionString });
    }
}
