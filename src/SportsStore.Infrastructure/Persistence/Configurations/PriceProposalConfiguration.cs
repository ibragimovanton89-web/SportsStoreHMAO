using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение PriceProposal в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class PriceProposalConfiguration : IEntityTypeConfiguration<PriceProposal>
{
    /// <summary>Задаёт отображение PriceProposal и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<PriceProposal> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<SupplierOffer>().WithMany().HasForeignKey(x => x.SupplierOfferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SupplierOfferId, x.Segment, x.MinimumQuantity, x.Fingerprint }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_PriceProposal_Valid", "\"Amount\" > 0 AND \"MinimumQuantity\" >= 1"));
        b.Property(x => x.Source).HasMaxLength(4000); b.Property(x => x.Amount).HasPrecision(18,2);
    }
}

