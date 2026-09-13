using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение ProductImage в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    /// <summary>Задаёт отображение ProductImage и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<ProductImage> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProductId, x.SortOrder }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_ProductImage_Valid", "\"SortOrder\" >= 0"));
    }
}

