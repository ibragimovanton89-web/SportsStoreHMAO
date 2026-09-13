using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение Customer в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <summary>Задаёт отображение Customer и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasIndex(x => x.ApplicationUserId).IsUnique();
        b.HasOne<SportsStore.Infrastructure.Identity.ApplicationUser>().WithOne().HasForeignKey<Customer>(x => x.ApplicationUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

