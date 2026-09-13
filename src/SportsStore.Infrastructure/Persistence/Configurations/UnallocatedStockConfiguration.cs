using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Ограничения, связи и русские комментарии UnallocatedStock.</summary>
public sealed class UnallocatedStockConfiguration : IEntityTypeConfiguration<UnallocatedStock>
{
    /// <summary>Настраивает хранение закупочного объекта и защиту истории.</summary>
    /// <param name="b">Построитель отображения EF.</param>
    public void Configure(EntityTypeBuilder<UnallocatedStock> b)
    {
        ConfigurationDefaults.Entity(b);
        b.Property(x => x.SupplierUnit).HasMaxLength(32).HasComment("Снимок единицы приёмки; новый прайс его не меняет. Пустое значение требует сверки до распределения.");
        b.ToTable(t => t.HasComment("Фактически полученный товар до сопоставления с карточкой; при создании карточки переносится в InventoryBalance."));
        b.Property(x => x.Id).HasComment("Уникальный идентификатор записи.");
        b.Property(x => x.SupplierOfferId).HasComment("Полученная позиция поставщика.");
        b.Property(x => x.WarehouseId).HasComment("Склад хранения.");
        b.Property(x => x.Quantity).HasComment("Количество в единицах поставщика, ещё не зачисленное варианту; перенос обнуляет этот остаток.");
        b.HasOne<SupplierOffer>().WithMany().HasForeignKey(x => x.SupplierOfferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SupplierOfferId, x.WarehouseId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_UnallocatedStock_Quantity", "\"Quantity\" >= 0"));
    }
}
