using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение Warehouse в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    /// <summary>Задаёт отображение Warehouse и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

