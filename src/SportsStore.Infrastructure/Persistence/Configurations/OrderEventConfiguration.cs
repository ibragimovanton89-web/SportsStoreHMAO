using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Неизменяемая история переходов покупательского заказа и идемпотентных команд.</summary>
public sealed class OrderEventConfiguration : IEntityTypeConfiguration<OrderEvent>
{
    /// <summary>Настраивает связи, ограничения и русские комментарии PostgreSQL.</summary>
    public void Configure(EntityTypeBuilder<OrderEvent> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Неизменяемая история переходов покупательского заказа и идемпотентных команд."));
        b.Property(x => x.Id).HasComment("Устойчивый идентификатор записи.");
        b.Property(x => x.Version).HasComment("Версия xmin для конкурентных изменений.");
        b.Property(x => x.OrderId).HasComment("Заказ, к которому относится событие.");
        b.Property(x => x.OperationId).HasComment("Уникальный ключ команды изменения.");
        b.Property(x => x.CommandFingerprint).HasComment("Отпечаток типа команды и её аргументов для безопасного повтора.");
        b.Property(x => x.Status).HasComment("Состояние после команды.");
        b.Property(x => x.ReserveUntil).HasComment("Срок резерва после команды, UTC.");
        b.Property(x => x.OccurredAt).HasComment("Время события, UTC.");
        b.Property(x => x.ActorId).HasComment("Служебный автор события; не выдаётся покупателю.");
        b.Property(x => x.Reason).HasComment("Публичное объяснение перехода без внутренних заметок.");
        b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OperationId).IsUnique(); b.HasIndex(x => new { x.OrderId, x.OccurredAt });
    }
}
