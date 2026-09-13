using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Ключ повторяемого добавления в корзину с проверкой неизменности команды.</summary>
public sealed class CartOperationConfiguration : IEntityTypeConfiguration<CartOperation>
{
    /// <summary>Настраивает связи, ограничения и русские комментарии PostgreSQL.</summary>
    public void Configure(EntityTypeBuilder<CartOperation> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Ключ повторяемого добавления в корзину с проверкой неизменности команды."));
        b.Property(x => x.Id).HasComment("Устойчивый идентификатор записи.");
        b.Property(x => x.Version).HasComment("Версия xmin для конкурентных изменений.");
        b.Property(x => x.CartId).HasComment("Корзина, к которой относилась команда.");
        b.Property(x => x.OperationId).HasComment("Уникальный ключ добавления.");
        b.Property(x => x.VariantId).HasComment("Идентификатор варианта команды.");
        b.Property(x => x.Quantity).HasComment("Добавляемое количество для проверки повторного запроса.");
        b.HasOne<Cart>().WithMany().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OperationId).IsUnique();
    }
}
