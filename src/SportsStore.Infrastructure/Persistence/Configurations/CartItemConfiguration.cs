using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Выбранное количество одного варианта; цена проверяется заново и здесь не хранится.</summary>
public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    /// <summary>Настраивает связи, ограничения и русские комментарии PostgreSQL.</summary>
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Выбранное количество одного варианта; цена проверяется заново и здесь не хранится."));
        b.Property(x => x.Id).HasComment("Устойчивый идентификатор записи.");
        b.Property(x => x.Version).HasComment("Версия xmin для конкурентных изменений.");
        b.Property(x => x.CartId).HasComment("Корзина-владелец строки.");
        b.Property(x => x.ProductVariantId).HasComment("Выбранный вариант; связь сохраняет недоступную строку для явного удаления.");
        b.Property(x => x.Quantity).HasComment("Целое количество; после объединения превышение лимита требует исправления покупателем.");
        b.HasOne<Cart>().WithMany().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CartId, x.ProductVariantId }).IsUnique(); b.ToTable(t => t.HasCheckConstraint("CK_CartItem_Quantity", "\"Quantity\" > 0 AND trunc(\"Quantity\") = \"Quantity\""));
    }
}
