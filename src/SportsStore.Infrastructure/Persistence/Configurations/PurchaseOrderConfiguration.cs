using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Ограничения, связи и русские комментарии PurchaseOrder.</summary>
public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    /// <summary>Настраивает хранение закупочного объекта и защиту истории.</summary>
    /// <param name="b">Построитель отображения EF.</param>
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Закупочный заказ магазина поставщику; не является заказом покупателя."));
        b.Property(x => x.Id).HasComment("Уникальный идентификатор записи.");
        b.Property(x => x.Number).HasComment("Номер закупки для сотрудника.");
        b.Property(x => x.SupplierId).HasComment("Поставщик закупки.");
        b.Property(x => x.WarehouseId).HasComment("Склад назначения приёмки.");
        b.Property(x => x.SupplierPriceTierId).HasComment("Выбранный закупочный тариф.");
        b.Property(x => x.TierCode).HasComment("Снимок кода тарифа: small, wholesale или large.");
        b.Property(x => x.TierName).HasComment("Снимок названия выбранного тарифа.");
        b.Property(x => x.MinimumAmount).HasComment("Снимок порога выбранного тарифа в рублях; null означает неизвестный порог.");
        b.Property(x => x.Currency).HasComment("Валюта закупки, RUB.");
        b.Property(x => x.CreatedBy).HasComment("Идентификатор сотрудника, собравшего заказ.");
        b.Property(x => x.Status).HasComment("Этап закупки; поступление оформляется отдельной транзакционной командой.");
        b.Property(x => x.Note).HasComment("Примечание оператора без секретов.");
        b.Property(x => x.CreatedAt).HasComment("Момент начала сборки закупки, UTC.");
        b.Property(x => x.UpdatedAt).HasComment("Момент последнего изменения, UTC.");
        b.HasIndex(x => x.Number).IsUnique();
        b.HasIndex(x => new { x.CreatedBy, x.SupplierId }).IsUnique().HasFilter("\"Status\" = 'Draft'");
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierPriceTier>().WithMany().HasForeignKey(x => x.SupplierPriceTierId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Note).HasMaxLength(1000);
        b.ToTable(t => t.HasCheckConstraint("CK_PurchaseOrder_Minimum", "\"MinimumAmount\" IS NULL OR \"MinimumAmount\" >= 0"));
    }
}
