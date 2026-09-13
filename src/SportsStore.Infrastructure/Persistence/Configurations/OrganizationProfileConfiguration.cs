using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение OrganizationProfile в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class OrganizationProfileConfiguration : IEntityTypeConfiguration<OrganizationProfile>
{
    /// <summary>Задаёт отображение OrganizationProfile и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<OrganizationProfile> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.CustomerId).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_OrganizationProfile_Valid", "\"Inn\" ~ '^([0-9]{10}|[0-9]{12})$' AND (\"Kpp\" IS NULL OR \"Kpp\" ~ '^[0-9]{9}$')"));
    }
}

