using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Ограничения, связи и русские комментарии PurchaseReceiptLine.</summary>
public sealed class PurchaseReceiptLineConfiguration : IEntityTypeConfiguration<PurchaseReceiptLine>
{
    /// <summary>Настраивает хранение закупочного объекта и защиту истории.</summary>
    /// <param name="b">Построитель отображения EF.</param>
    public void Configure(EntityTypeBuilder<PurchaseReceiptLine> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Неизменяемая запись приёмки; повтор той же операции не увеличивает остаток повторно."));
        b.Property(x => x.Id).HasComment("Уникальный идентификатор записи.");
        b.Property(x => x.PurchaseOrderLineId).HasComment("Строка закупки, по которой принято количество.");
        b.Property(x => x.WarehouseId).HasComment("Склад фактического поступления.");
        b.Property(x => x.OperationId).HasComment("Идентификатор приёмки; уникален вместе со строкой закупки.");
        b.Property(x => x.Quantity).HasComment("Принято в этой операции, в единицах поставщика.");
        b.Property(x => x.ProductVariantId).HasComment("Вариант, получивший остаток; null означает приёмку до создания карточки.");
        b.Property(x => x.Conversion).HasComment("Зафиксированный перевод единиц при зачислении варианту; null для нераспределённого остатка.");
        b.Property(x => x.ReceivedAt).HasComment("Момент приёмки, UTC.");
        b.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OperationId, x.PurchaseOrderLineId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_PurchaseReceiptLine_Quantity", "\"Quantity\" > 0 AND (\"Conversion\" IS NULL OR \"Conversion\" > 0)"));
    }
}
