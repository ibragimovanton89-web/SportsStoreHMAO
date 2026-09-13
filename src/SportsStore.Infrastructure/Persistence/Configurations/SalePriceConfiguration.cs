using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение SalePrice в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class SalePriceConfiguration : IEntityTypeConfiguration<SalePrice>
{
    /// <summary>Задаёт отображение SalePrice и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<SalePrice> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProductVariantId, x.Segment, x.MinimumQuantity }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_SalePrice_Valid", "\"Amount\" > 0 AND \"MinimumQuantity\" >= 1 AND (\"Segment\" <> 'Retail' OR \"MinimumQuantity\" = 1)"));
        b.Property(x => x.Source).HasMaxLength(4000); b.Property(x => x.Amount).HasPrecision(18,2);
    }
}

