using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение SupplierPriceTier в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class SupplierPriceTierConfiguration : IEntityTypeConfiguration<SupplierPriceTier>
{
    /// <summary>Задаёт отображение SupplierPriceTier и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<SupplierPriceTier> b)
    {
        ConfigurationDefaults.Entity(b);
        b.Property(x => x.ManualMinimumAmount).HasPrecision(18, 2).HasComment("Порог закупки, вручную заданный владельцем; null использует исходный порог прайса. Импорт не перезаписывает.");
        b.ToTable(t => t.HasCheckConstraint("CK_SupplierPriceTier_ManualMinimum", "\"ManualMinimumAmount\" IS NULL OR \"ManualMinimumAmount\" >= 0"));
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SupplierId, x.Code }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_SupplierPriceTier_Valid", "\"MinimumAmount\" IS NULL OR \"MinimumAmount\" >= 0"));
        b.Property(x => x.SourceConditions).HasMaxLength(16000); b.Property(x => x.Currency).HasMaxLength(3);
    }
}

