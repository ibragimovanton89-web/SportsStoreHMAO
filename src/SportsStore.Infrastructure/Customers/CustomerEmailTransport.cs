using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SportsStore.Application.Customers;

namespace SportsStore.Infrastructure.Customers;

/// <summary>SMTP с обязательным STARTTLS либо захват писем вне wwwroot только в Development.</summary>
/// <param name="configuration">Конфигурация транспорта без секретов в коде.</param><param name="environment">Проверка допустимости тестового захвата.</param>
public sealed class CustomerEmailTransport(IConfiguration configuration, IHostEnvironment environment) : ICustomerEmailTransport
{
    /// <summary>Возвращает доверенный origin для ссылок; заголовок Host запроса не используется.</summary>
    public Uri PublicOrigin()
    {
        if (!Uri.TryCreate(configuration["Email:PublicOrigin"], UriKind.Absolute, out var origin) || origin.UserInfo.Length > 0 || origin.Query.Length > 0
            || origin.Fragment.Length > 0 || origin.AbsolutePath != "/" || (origin.Scheme != "https" && !(environment.IsDevelopment() && origin.IsLoopback && origin.Scheme == "http")))
            throw new InvalidOperationException("Настройте Email:PublicOrigin как доверенный HTTPS origin.");
        return origin;
    }
    /// <summary>Отправляет текст по защищённому SMTP либо пишет отдельный тестовый файл; пароль и SMTP-ошибки не выводятся.</summary>
    public async Task SendAsync(CustomerEmail message, CancellationToken ct = default)
    {
        if (configuration["Email:Mode"] == "Capture")
        {
            if (!environment.IsDevelopment()) throw new InvalidOperationException("Захват писем разрешён только в Development.");
            var path = configuration["Email:CapturePath"];
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)) throw new InvalidOperationException("Укажите абсолютный Email:CapturePath вне сайта.");
            path = Path.GetFullPath(path);
            var root = Path.GetFullPath(environment.ContentRootPath);
            if (path.StartsWith(Path.Combine(root, "wwwroot"), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Захват писем не может находиться в wwwroot.");
            Directory.CreateDirectory(path);
            var body = $"Кому: {message.Recipient}\nТема: {message.Subject}\n\n{message.Body}";
            await File.WriteAllTextAsync(Path.Combine(path, message.MessageId.ToString("N") + ".txt"), body, Encoding.UTF8, ct);
            return;
        }
        if (configuration["Email:Mode"] != "Smtp") throw new InvalidOperationException("Почтовый транспорт не настроен.");
        using var smtp = new SmtpClient(configuration["Email:Smtp:Host"] ?? throw new InvalidOperationException("Не задан SMTP host."), configuration.GetValue("Email:Smtp:Port", 587))
        {
            EnableSsl = true, UseDefaultCredentials = false,
            Credentials = new NetworkCredential(configuration["Email:Smtp:User"], configuration["Email:Smtp:Password"]), Timeout = 15000
        };
        using var mail = new MailMessage(configuration["Email:From"] ?? throw new InvalidOperationException("Не задан адрес отправителя."), message.Recipient, message.Subject, message.Body);
        mail.Headers.Add("X-SportsStore-Message-Id", message.MessageId.ToString("N"));
        await smtp.SendMailAsync(mail, ct);
    }
}
