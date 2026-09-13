using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Ограничения, связи и русские комментарии PurchaseOrderLine.</summary>
public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    /// <summary>Настраивает хранение закупочного объекта и защиту истории.</summary>
    /// <param name="b">Построитель отображения EF.</param>
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Выбранная позиция закупки со снимками исходных данных и цен; новый прайс не меняет заказ."));
        b.Property(x => x.Id).HasComment("Уникальный идентификатор записи.");
        b.Property(x => x.PurchaseOrderId).HasComment("Закупочный заказ.");
        b.Property(x => x.SupplierOfferId).HasComment("Предложение поставщика, выбранное вручную.");
        b.Property(x => x.ExternalCode).HasComment("Код поставщика с ведущими нулями.");
        b.Property(x => x.Name).HasComment("Название позиции на момент выбора.");
        b.Property(x => x.SupplierUnit).HasComment("Единица закупки: плюс один означает одну такую единицу, а не коробку.");
        b.Property(x => x.UnitsPerBox).HasComment("Справочное количество в коробке; не задаёт минимальный заказ.");
        b.Property(x => x.SmallPrice).HasComment("Снимок цены мелкого опта за единицу поставщика; null не равен нулю.");
        b.Property(x => x.WholesalePrice).HasComment("Снимок цены опта за единицу поставщика.");
        b.Property(x => x.LargePrice).HasComment("Снимок цены крупного опта за единицу поставщика.");
        b.Property(x => x.UnitPrice).HasComment("Зафиксированная цена выбранного тарифа за единицу поставщика.");
        b.Property(x => x.Quantity).HasComment("Заказанное количество единиц поставщика, от 1 до 1000000.");
        b.Property(x => x.ReceivedQuantity).HasComment("Суммарно принятое количество; увеличивается только командой приёмки.");
        b.HasOne<PurchaseOrder>().WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierOffer>().WithMany().HasForeignKey(x => x.SupplierOfferId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PurchaseOrderId, x.SupplierOfferId }).IsUnique();
        b.Property(x => x.Name).HasMaxLength(16000);
        b.ToTable(t => t.HasCheckConstraint("CK_PurchaseOrderLine_Quantities", "\"Quantity\" BETWEEN 1 AND 1000000 AND \"ReceivedQuantity\" BETWEEN 0 AND \"Quantity\""));
        b.ToTable(t => t.HasCheckConstraint("CK_PurchaseOrderLine_Prices", "(\"UnitPrice\" IS NULL OR \"UnitPrice\" > 0) AND (\"SmallPrice\" IS NULL OR \"SmallPrice\" > 0) AND (\"WholesalePrice\" IS NULL OR \"WholesalePrice\" > 0) AND (\"LargePrice\" IS NULL OR \"LargePrice\" > 0)"));
    }
}
