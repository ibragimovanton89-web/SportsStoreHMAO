using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение SupplierPriceHistory в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class SupplierPriceHistoryConfiguration : IEntityTypeConfiguration<SupplierPriceHistory>
{
    /// <summary>Задаёт отображение SupplierPriceHistory и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<SupplierPriceHistory> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<SupplierOffer>().WithMany().HasForeignKey(x => x.SupplierOfferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierPriceTier>().WithMany().HasForeignKey(x => x.SupplierPriceTierId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ImportBatchId, x.SupplierOfferId, x.SupplierPriceTierId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_SupplierPriceHistory_Valid", "\"NewAmount\" > 0 AND (\"OldAmount\" IS NULL OR \"OldAmount\" > 0)"));
    }
}

