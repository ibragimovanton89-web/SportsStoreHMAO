using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SportsStore.Application.Customers;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Customers;

/// <summary>Обрабатывает зафиксированные уведомления с короткой арендой; SMTP выполняется после коммита аренды.</summary>
/// <param name="factory">Фабрика контекстов.</param><param name="transport">Почтовый транспорт.</param>
public sealed class NotificationProcessor(IDbContextFactory<ApplicationDbContext> factory, ICustomerEmailTransport transport)
{
    /// <summary>Обрабатывает одно письмо; не изменяет решение по опту даже при сбое доставки.</summary>
    /// <returns>Была ли найдена запись для обработки.</returns>
    public async Task<bool> ProcessOneAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        Domain.Entities.NotificationOutbox? item;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            var now = DateTimeOffset.UtcNow;
            item = await db.NotificationOutbox.FromSqlInterpolated($"SELECT *, xmin FROM \"NotificationOutbox\" WHERE \"SentAt\" IS NULL AND \"Attempts\" < 5 AND \"NextAttemptAt\" <= {now} AND (\"LeaseUntil\" IS NULL OR \"LeaseUntil\" < {now}) ORDER BY \"CreatedAt\", \"Id\" LIMIT 1 FOR UPDATE SKIP LOCKED").SingleOrDefaultAsync(ct);
            if (item is null) return false;
            item.Attempts++; item.LeaseUntil = now.AddMinutes(2);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        try
        {
            var recipient = await (from d in db.WholesaleDecisions join c in db.Customers on d.CustomerId equals c.Id
                join u in db.Users on c.ApplicationUserId equals u.Id
                where d.Id == item.DecisionId && u.EmailConfirmed select new { u.Email, d.Outcome, d.PublicReason }).SingleOrDefaultAsync(ct);
            if (recipient is null) throw new InvalidOperationException("RecipientUnavailable");
            var result = recipient.Outcome switch
            {
                Domain.Entities.WholesaleApplicationStatus.Approved => "Оптовые условия одобрены.",
                Domain.Entities.WholesaleApplicationStatus.Rejected => "Заявка на опт отклонена.",
                Domain.Entities.WholesaleApplicationStatus.Revoked => "Оптовые условия отозваны.",
                _ => "Заявка на опт отозвана."
            };
            await transport.SendAsync(new(recipient.Email!, "Результат заявки на опт — SportsStoreHMAO",
                result + "\n" + recipient.PublicReason + "\nПодробности доступны в личном кабинете.", item.Id), ct);
            item.SentAt = DateTimeOffset.UtcNow; item.LastErrorCode = null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or IOException or InvalidOperationException or ArgumentException)
        {
            item.LastErrorCode = "DeliveryFailed";
            item.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(Math.Pow(2, item.Attempts));
        }
        item.LeaseUntil = null;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

/// <summary>Фоновый запуск ограниченной очереди; ошибки БД не раскрывают письма и не останавливают веб-приложение.</summary>
/// <param name="processor">Обработчик коротких операций.</param><param name="logger">Журнал только безопасного кода ошибки.</param>
public sealed class NotificationWorker(NotificationProcessor processor, ILogger<NotificationWorker> logger) : BackgroundService
{
    /// <summary>Проверяет очередь каждые 15 секунд, обрабатывая не более 20 писем за проход.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                for (var i = 0; i < 20 && await processor.ProcessOneAsync(stoppingToken); i++) { }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Обработчик уведомлений временно недоступен; повтор будет выполнен позже."); }
            try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
