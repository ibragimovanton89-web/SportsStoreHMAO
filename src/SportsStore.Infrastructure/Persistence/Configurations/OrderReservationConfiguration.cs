using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Распределение резерва конкретной строки заказа по собственному складу.</summary>
public sealed class OrderReservationConfiguration : IEntityTypeConfiguration<OrderReservation>
{
    /// <summary>Настраивает связи, ограничения и русские комментарии PostgreSQL.</summary>
    public void Configure(EntityTypeBuilder<OrderReservation> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Распределение резерва конкретной строки заказа по собственному складу."));
        b.Property(x => x.Id).HasComment("Устойчивый идентификатор записи.");
        b.Property(x => x.Version).HasComment("Версия xmin для конкурентных изменений.");
        b.Property(x => x.OrderId).HasComment("Заказ-владелец резерва.");
        b.Property(x => x.OrderItemId).HasComment("Строка, для которой выделено количество.");
        b.Property(x => x.ProductVariantId).HasComment("Вариант резервируемого товара.");
        b.Property(x => x.WarehouseId).HasComment("Склад, на котором увеличен Reserved.");
        b.Property(x => x.Quantity).HasComment("Положительное зарезервированное количество в единицах продажи.");
        b.Property(x => x.Active).HasComment("True до однократного освобождения.");
        b.Property(x => x.CreatedAt).HasComment("Момент резервирования, UTC.");
        b.Property(x => x.ReleasedAt).HasComment("Момент освобождения, UTC; null у активного резерва.");
        b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OrderItem>().WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OrderItemId, x.WarehouseId }).IsUnique(); b.ToTable(t => t.HasCheckConstraint("CK_OrderReservation_Positive", "\"Quantity\" > 0"));
    }
}
