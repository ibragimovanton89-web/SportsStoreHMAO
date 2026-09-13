using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение SupplierOffer в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class SupplierOfferConfiguration : IEntityTypeConfiguration<SupplierOffer>
{
    /// <summary>Задаёт отображение SupplierOffer и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<SupplierOffer> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SupplierId, x.ExternalCode }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_SupplierOffer_Valid", "(\"Stock\" IS NULL OR \"Stock\" >= 0) AND (\"UnitsPerBox\" IS NULL OR \"UnitsPerBox\" > 0) AND (\"SaleUnitsPerSupplierUnit\" IS NULL OR \"SaleUnitsPerSupplierUnit\" > 0) AND (NOT \"ConversionConfirmed\" OR (\"SaleUnitsPerSupplierUnit\" IS NOT NULL AND \"ProductVariantId\" IS NOT NULL)) AND (\"MinimumOrderQuantity\" IS NULL OR \"MinimumOrderQuantity\" > 0) AND (\"OrderMultiple\" IS NULL OR \"OrderMultiple\" > 0)"));
        b.Property(x => x.ExternalCode).HasMaxLength(64); b.Property(x => x.SourceName).HasMaxLength(4000); b.Property(x => x.SourceSection).HasMaxLength(2000); b.Property(x => x.ReviewSuggestionsJson).HasColumnType("jsonb").Metadata.SetMaxLength(null); b.HasIndex(x => x.LastSeenAt);
    }
}

