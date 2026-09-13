using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение ImportBatch в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    /// <summary>Задаёт отображение ImportBatch и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<ImportBatch> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SupplierId, x.Sha256, x.ParserVersion }).IsUnique();
        b.Property(x => x.TiersJson).HasColumnType("jsonb").Metadata.SetMaxLength(null); b.Property(x => x.Conditions).HasMaxLength(16000); b.Property(x => x.Sha256).HasMaxLength(64);
    }
}

