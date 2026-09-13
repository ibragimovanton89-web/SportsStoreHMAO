using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение InventoryBalance в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    /// <summary>Задаёт отображение InventoryBalance и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<InventoryBalance> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProductVariantId, x.WarehouseId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_InventoryBalance_Valid", "\"OnHand\" >= 0 AND \"Reserved\" >= 0 AND \"Reserved\" <= \"OnHand\""));
    }
}

