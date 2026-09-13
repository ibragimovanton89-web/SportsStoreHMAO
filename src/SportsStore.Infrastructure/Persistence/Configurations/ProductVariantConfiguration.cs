using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение ProductVariant в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    /// <summary>Задаёт отображение ProductVariant и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<ProductVariant> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<SupplierOffer>().WithMany().HasForeignKey(x => x.PricingSupplierOfferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Sku).IsUnique();
    }
}
