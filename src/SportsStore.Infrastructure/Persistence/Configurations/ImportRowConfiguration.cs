using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение ImportRow в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class ImportRowConfiguration : IEntityTypeConfiguration<ImportRow>
{
    /// <summary>Задаёт отображение ImportRow и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<ImportRow> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ImportBatchId, x.RowNumber }).IsUnique();
        b.Property(x => x.RawJson).HasColumnType("jsonb").Metadata.SetMaxLength(null); b.Property(x => x.ParsedJson).HasColumnType("jsonb").Metadata.SetMaxLength(null); b.Property(x => x.Diagnostics).HasMaxLength(4000);
    }
}

