using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;

namespace SportsStore.Infrastructure.Persistence.Configurations;

/// <summary>Хранение безопасного административного журнала; удаление учётной записи не удаляет историю.</summary>
public sealed class AdminAuditConfiguration : IEntityTypeConfiguration<AdminAudit>
{
    /// <summary>Настраивает ограничения, уникальность команды и русские комментарии журнала.</summary>
    public void Configure(EntityTypeBuilder<AdminAudit> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Журнал успешных административных изменений; сохраняется атомарно с операцией, без секретов и реквизитов."));
        b.HasIndex(x => new { x.ActorId, x.OperationId }).IsUnique();
        b.HasIndex(x => x.OccurredAt);
        b.Property(x => x.Id).HasComment("Уникальный идентификатор записи журнала.");
        b.Property(x => x.ActorId).HasMaxLength(450).HasComment("Идентификатор сотрудника или доверенного локального оператора.");
        b.Property(x => x.OccurredAt).HasComment("Момент успешного изменения, UTC.");
        b.Property(x => x.Action).HasMaxLength(100).HasComment("Стабильный код административного действия.");
        b.Property(x => x.ObjectType).HasMaxLength(100).HasComment("Тип изменённого объекта.");
        b.Property(x => x.ObjectId).HasMaxLength(450).HasComment("Идентификатор объекта без его персонального содержимого.");
        b.Property(x => x.Description).HasMaxLength(1000).HasComment("Безопасное описание изменения без секретов, адресов и реквизитов.");
        b.Property(x => x.Reason).HasMaxLength(500).HasComment("Основание решения; не предназначено для персональных сведений.");
        b.Property(x => x.OperationId).HasComment("Идентификатор отправки команды для аудита и защиты от повторов.");
    }
}
