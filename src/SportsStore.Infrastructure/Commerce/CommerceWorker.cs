using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SportsStore.Application.Customers;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
namespace SportsStore.Infrastructure.Commerce;
/// <summary>Письма заказов с собственной типизированной очередью; старые уведомления опта не меняются.</summary>
/// <param name="factory">Фабрика контекстов.</param><param name="transport">Общий Capture/SMTP транспорт.</param><param name="clock">UTC-часы.</param><param name="configuration">Доверенный origin ссылок.</param>
public sealed class OrderNotificationProcessor(IDbContextFactory<ApplicationDbContext> factory, ICustomerEmailTransport transport, TimeProvider clock, IConfiguration configuration)
{
    /// <summary>Берёт одно уведомление в аренду; доставка происходит после commit, повторы ограничены пятью попытками.</summary>
    public async Task<bool> ProcessOneAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); OrderNotification? item;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            var now = clock.GetUtcNow();
            item = await db.OrderNotifications.FromSqlInterpolated($"SELECT *, xmin FROM \"OrderNotification\" WHERE \"SentAt\" IS NULL AND \"Attempts\" < 5 AND \"NextAttemptAt\" <= {now} AND (\"LeaseUntil\" IS NULL OR \"LeaseUntil\" < {now}) ORDER BY \"CreatedAt\", \"Id\" LIMIT 1 FOR UPDATE SKIP LOCKED").SingleOrDefaultAsync(ct);
            if (item is null) return false;
            item.Attempts++; item.LeaseUntil = now.AddMinutes(2); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        try
        {
            var e = await db.OrderEvents.AsNoTracking().SingleAsync(x => x.Id == item.EventId, ct); var o = await db.Orders.AsNoTracking().SingleAsync(x => x.Id == e.OrderId, ct);
            if (!Uri.TryCreate(configuration["Email:PublicOrigin"], UriKind.Absolute, out var origin) || origin.Scheme != "https" || origin.UserInfo.Length > 0 || origin.AbsolutePath != "/" || origin.Query.Length > 0 || origin.Fragment.Length > 0) throw new InvalidOperationException("OriginUnavailable");
            var link = new Uri(origin, "/customer/orders/" + o.Id);
            await transport.SendAsync(new(o.ContactEmail, "Заказ " + o.Number + " — SportsStoreHMAO", "Заказ " + o.Number + "\nСтатус: " + StatusText(e.Status) + "\n" + e.Reason + "\nСумма товаров: " + o.GoodsTotal.ToString("N2") + " RUB\nСрок резерва (UTC): " + e.ReserveUntil?.ToString("u") + "\n" + link, item.Id), ct);
            item.SentAt = clock.GetUtcNow(); item.LastErrorCode = null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or IOException or InvalidOperationException or ArgumentException)
        { item.LastErrorCode = "DeliveryFailed"; item.NextAttemptAt = clock.GetUtcNow().AddMinutes(Math.Pow(2, item.Attempts)); }
        item.LeaseUntil = null; await db.SaveChangesAsync(ct); return true;
    }
    /// <summary>Понятный статус письма; подтверждение не означает оплату или отгрузку.</summary>
    private static string StatusText(CustomerOrderStatus status) => status switch
    {
        CustomerOrderStatus.AwaitingConfirmation => "Ожидает подтверждения",
        CustomerOrderStatus.Confirmed => "Подтверждён сотрудником",
        CustomerOrderStatus.Cancelled => "Отменён",
        CustomerOrderStatus.Expired => "Резерв истёк",
        _ => "Исторический заказ"
    };
}
/// <summary>Обрабатывает просроченные резервы и письма после перезапуска; несколько процессов используют общие блокировки БД.</summary>
/// <param name="scopes">Короткая DI-область каждой итерации.</param><param name="logger">Журнал только безопасных сообщений.</param>
public sealed class CommerceWorker(IServiceScopeFactory scopes, ILogger<CommerceWorker> logger) : BackgroundService
{
    /// <summary>Каждые 15 секунд выполняет ограниченные проходы; отсутствие схемы не останавливает приложение.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope(); var commerce = scope.ServiceProvider.GetRequiredService<CommerceService>();
                for (var i = 0; i < 20 && await commerce.ExpireOneAsync(stoppingToken); i++) { }
                var mail = scope.ServiceProvider.GetRequiredService<OrderNotificationProcessor>();
                for (var i = 0; i < 20 && await mail.ProcessOneAsync(stoppingToken); i++) { }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Обработка заказов временно недоступна; повтор позже."); }
            try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
}
