using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение Order в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>Задаёт отображение Order и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<Order> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => x.Number).IsUnique();
    }
}

