using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение OrderItem в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    /// <summary>Задаёт отображение OrderItem и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.SetNull);
        b.ToTable(t => t.HasCheckConstraint("CK_OrderItem_Valid", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"UnitDiscount\" >= 0 AND \"UnitDiscount\" <= \"UnitPrice\""));
        b.Property(x => x.UnitPrice).HasPrecision(18,2); b.Property(x => x.UnitDiscount).HasPrecision(18,2);
    }
}

