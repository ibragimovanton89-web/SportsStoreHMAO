using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение SalePriceHistory в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class SalePriceHistoryConfiguration : IEntityTypeConfiguration<SalePriceHistory>
{
    /// <summary>Задаёт отображение SalePriceHistory и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<SalePriceHistory> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Source).HasMaxLength(4000); b.Property(x => x.OldAmount).HasPrecision(18,2); b.Property(x => x.NewAmount).HasPrecision(18,2);
    }
}

