using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;

namespace SportsStore.Infrastructure.Persistence.Configurations;

/// <summary>Хранение снимков заявок с единственной ожидающей заявкой на покупателя.</summary>
public sealed class WholesaleApplicationConfiguration : IEntityTypeConfiguration<WholesaleApplication>
{
    /// <summary>Настраивает ограничения, ссылки и русские описания новой таблицы.</summary>
    public void Configure(EntityTypeBuilder<WholesaleApplication> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Заявки покупателей на опт с неизменяемыми снимками реквизитов."));
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.CustomerId).IsUnique().HasFilter("\"Status\" = 'Pending'");
        b.HasIndex(x => x.OperationId).IsUnique();
        b.HasIndex(x => new { x.CustomerId, x.SubmittedAt });
        b.Property(x => x.Id).HasComment("Идентификатор заявки.");
        b.Property(x => x.Version).HasComment("Версия xmin для защиты от устаревшего решения.");
        b.Property(x => x.CustomerId).HasComment("Покупатель — владелец заявки.");
        b.Property(x => x.Status).HasComment("Pending: проверка; Approved: одобрено; Rejected: отказ; Withdrawn: отзыв заявки; Revoked: отзыв опта.");
        b.Property(x => x.Kind).HasComment("Снимок правового типа покупателя.");
        b.Property(x => x.LegalName).HasComment("Снимок официального наименования; null для физлица.");
        b.Property(x => x.Inn).HasComment("Снимок ИНН; null для физлица.");
        b.Property(x => x.Kpp).HasComment("Снимок КПП; null, если неприменим.");
        b.Property(x => x.Comment).HasComment("Текст обращения покупателя.");
        b.Property(x => x.SubmittedAt).HasComment("Время подачи, UTC.");
        b.Property(x => x.ReviewedAt).HasComment("Время последнего решения, UTC; null до рассмотрения.");
        b.Property(x => x.ReviewedBy).HasComment("Служебный идентификатор автора решения.");
        b.Property(x => x.PublicReason).HasComment("Доступная покупателю причина результата.");
        b.Property(x => x.OperationId).HasComment("Уникальная команда подачи для защиты от повтора.");
    }
}

/// <summary>История решений, сохраняемая вместе с условиями покупателя.</summary>
public sealed class WholesaleDecisionConfiguration : IEntityTypeConfiguration<WholesaleDecision>
{
    /// <summary>Запрещает каскадное удаление истории и повтор одной команды.</summary>
    public void Configure(EntityTypeBuilder<WholesaleDecision> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Неизменяемая история решений по опту, включая ручные решения без заявки."));
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WholesaleApplication>().WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OperationId).IsUnique();
        b.HasIndex(x => new { x.CustomerId, x.OccurredAt });
        b.Property(x => x.Id).HasComment("Идентификатор решения.");
        b.Property(x => x.Version).HasComment("Системная версия xmin.");
        b.Property(x => x.CustomerId).HasComment("Покупатель, чьи условия изменились.");
        b.Property(x => x.ApplicationId).HasComment("Заявка-основание; null для ручного решения без заявки.");
        b.Property(x => x.Outcome).HasComment("Результат рассмотрения или отзыва права на опт.");
        b.Property(x => x.OccurredAt).HasComment("Время решения, UTC.");
        b.Property(x => x.ActorId).HasComment("Доверенный автор решения; не раскрывается покупателю.");
        b.Property(x => x.PublicReason).HasComment("Объяснение, доступное покупателю.");
        b.Property(x => x.OperationId).HasComment("Уникальный идентификатор команды изменения.");
    }
}

/// <summary>Транзакционная очередь уведомлений с ограниченными повторными попытками.</summary>
public sealed class NotificationOutboxConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    /// <summary>Настраивает уникальность письма на решение и индекс планировщика.</summary>
    public void Configure(EntityTypeBuilder<NotificationOutbox> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => { t.HasComment("Очередь уведомлений по опту без токенов и полных реквизитов."); t.HasCheckConstraint("CK_NotificationOutbox_Attempts", "\"Attempts\" >= 0"); });
        b.HasOne<WholesaleDecision>().WithOne().HasForeignKey<NotificationOutbox>(x => x.DecisionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SentAt, x.NextAttemptAt });
        b.Property(x => x.Id).HasComment("Идентификатор уведомления.");
        b.Property(x => x.Version).HasComment("Версия xmin для защиты аренды обработчика.");
        b.Property(x => x.DecisionId).HasComment("Решение, вызвавшее единственное логическое уведомление.");
        b.Property(x => x.CreatedAt).HasComment("Время постановки в очередь, UTC.");
        b.Property(x => x.Attempts).HasComment("Количество начатых попыток доставки.");
        b.Property(x => x.NextAttemptAt).HasComment("Время следующей допустимой попытки, UTC.");
        b.Property(x => x.LeaseUntil).HasComment("Окончание аренды обработчиком, UTC; null без аренды.");
        b.Property(x => x.SentAt).HasComment("Подтверждённая отправка, UTC; null до отправки.");
        b.Property(x => x.LastErrorCode).HasComment("Безопасный код ошибки без персональных данных и SMTP-ответа.");
    }
}
