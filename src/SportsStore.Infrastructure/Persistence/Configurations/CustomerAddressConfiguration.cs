using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение CustomerAddress в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    /// <summary>Задаёт отображение CustomerAddress и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<CustomerAddress> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

