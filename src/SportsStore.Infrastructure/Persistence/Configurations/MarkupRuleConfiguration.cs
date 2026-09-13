using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение MarkupRule в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class MarkupRuleConfiguration : IEntityTypeConfiguration<MarkupRule>
{
    /// <summary>Задаёт отображение MarkupRule и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<MarkupRule> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.Segment, x.CategoryId, x.ProductVariantId, x.MinimumQuantity }).IsUnique().AreNullsDistinct(false);
        b.ToTable(t => t.HasCheckConstraint("CK_MarkupRule_Valid", "NOT (\"CategoryId\" IS NOT NULL AND \"ProductVariantId\" IS NOT NULL) AND \"MarkupPercent\" >= 0 AND \"MinimumQuantity\" >= 1 AND (\"Segment\" <> 'Retail' OR \"MinimumQuantity\" = 1)"));
    }
}

