using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение SupplierOfferPrice в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class SupplierOfferPriceConfiguration : IEntityTypeConfiguration<SupplierOfferPrice>
{
    /// <summary>Задаёт отображение SupplierOfferPrice и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<SupplierOfferPrice> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<SupplierOffer>().WithMany().HasForeignKey(x => x.SupplierOfferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierPriceTier>().WithMany().HasForeignKey(x => x.SupplierPriceTierId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SupplierOfferId, x.SupplierPriceTierId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_SupplierOfferPrice_Valid", "\"Amount\" > 0"));
    }
}

