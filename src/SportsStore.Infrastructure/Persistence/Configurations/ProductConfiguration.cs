using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение Product в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>Задаёт отображение Product и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<Product> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<Brand>().WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Description).HasMaxLength(8000); b.HasIndex(x => new { x.Status, x.CategoryId });
    }
}

