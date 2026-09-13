using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение Supplier в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    /// <summary>Задаёт отображение Supplier и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<SupplierPriceTier>().WithMany().HasForeignKey(x => x.SelectedPriceTierId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

