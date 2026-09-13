using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Серверные условия оформления; персональные данные доступны только владельцу.</summary>
public sealed class CheckoutPreviewConfiguration : IEntityTypeConfiguration<CheckoutPreview>
{
    /// <summary>Настраивает связи, ограничения и русские комментарии PostgreSQL.</summary>
    public void Configure(EntityTypeBuilder<CheckoutPreview> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Серверные условия оформления; персональные данные доступны только владельцу."));
        b.Property(x => x.Id).HasComment("Устойчивый идентификатор записи.");
        b.Property(x => x.Version).HasComment("Версия xmin для конкурентных изменений.");
        b.Property(x => x.CustomerId).HasComment("Покупатель, подтвердивший условия.");
        b.Property(x => x.CartId).HasComment("Корзина, состав которой проверялся.");
        b.Property(x => x.AddressId).HasComment("Выбранный собственный адрес для повторной проверки.");
        b.Property(x => x.CartVersion).HasComment("Версия корзины при подготовке; сравнение фактических условий имеет приоритет.");
        b.Property(x => x.ConditionsJson).HasComment("Неизменяемый снимок подтверждаемых условий, включая контакты; не для журналов.");
        b.Property(x => x.Fingerprint).HasComment("SHA-256 нормализованных условий, не самостоятельное средство авторизации.");
        b.Property(x => x.ExpiresAt).HasComment("Срок действия подтверждения, UTC.");
        b.Property(x => x.CreatedAt).HasComment("Время создания, UTC.");
        b.HasOne<Cart>().WithMany().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.ConditionsJson).HasColumnType("text"); b.Property(x => x.ConditionsJson).Metadata.SetMaxLength(null); b.HasIndex(x => new { x.CustomerId, x.ExpiresAt });
    }
}
