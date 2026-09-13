using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Отдельная очередь писем о заказах; не использует фиктивные решения об опте.</summary>
public sealed class OrderNotificationConfiguration : IEntityTypeConfiguration<OrderNotification>
{
    /// <summary>Настраивает связи, ограничения и русские комментарии PostgreSQL.</summary>
    public void Configure(EntityTypeBuilder<OrderNotification> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Отдельная очередь писем о заказах; не использует фиктивные решения об опте."));
        b.Property(x => x.Id).HasComment("Устойчивый идентификатор записи.");
        b.Property(x => x.Version).HasComment("Версия xmin для конкурентных изменений.");
        b.Property(x => x.EventId).HasComment("Уникальное событие, намерение отправки которого создано в той же транзакции.");
        b.Property(x => x.CreatedAt).HasComment("Время постановки, UTC.");
        b.Property(x => x.Attempts).HasComment("Число начатых попыток; не более пяти.");
        b.Property(x => x.NextAttemptAt).HasComment("Ближайшее время повтора, UTC.");
        b.Property(x => x.LeaseUntil).HasComment("Аренда обработчика, UTC; null вне обработки.");
        b.Property(x => x.SentAt).HasComment("Момент подтверждения отправки транспортом, UTC.");
        b.Property(x => x.LastErrorCode).HasComment("Безопасный код ошибки без адресов, писем и секретов.");
        b.HasOne<OrderEvent>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.EventId).IsUnique(); b.HasIndex(x => new { x.SentAt, x.NextAttemptAt }); b.ToTable(t => t.HasCheckConstraint("CK_OrderNotification_Attempts", "\"Attempts\" >= 0"));
    }
}
