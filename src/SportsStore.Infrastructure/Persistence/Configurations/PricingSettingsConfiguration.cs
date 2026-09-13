using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение PricingSettings в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class PricingSettingsConfiguration : IEntityTypeConfiguration<PricingSettings>
{
    /// <summary>Задаёт отображение PricingSettings и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<PricingSettings> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasCheckConstraint("CK_PricingSettings_Valid", "\"Id\" = '00000000-0000-0000-0000-000000000001'::uuid AND (\"MinimumWholesaleOrder\" IS NULL OR \"MinimumWholesaleOrder\" >= 0)"));
    }
}

