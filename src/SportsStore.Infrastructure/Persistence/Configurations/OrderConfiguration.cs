using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение Order в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>Задаёт отображение Order и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<Order> b)
    {
        ConfigurationDefaults.Entity(b);
        b.Property(x => x.Status).HasComment("Исторические записи не получают фиктивного резерва.");
        b.Property(x => x.UpdatedAt).HasComment("Время последнего перехода, UTC.");
        b.Property(x => x.ReserveUntil).HasComment("Конечный срок активного резерва, UTC.");
        b.Property(x => x.GoodsTotal).HasComment("Сумма сохранённых строк без неопределённой доставки.");
        b.Property(x => x.CartId).HasComment("Оформленная корзина; уникальна, null у исторических заказов.");
        b.Property(x => x.CheckoutOperationId).HasComment("Ключ оформления; null у исторических записей.");
        b.Property(x => x.PreviewId).HasComment("Подтверждённый серверный preview.");
        b.Property(x => x.ConditionsFingerprint).HasComment("Отпечаток подтверждённых условий.");
        b.Property(x => x.RecipientName).HasComment("Снимок получателя выбранного адреса.");
        b.Property(x => x.PostalCode).HasComment("Снимок индекса; null, если не указан.");
        b.Property(x => x.CustomerKind).HasComment("Снимок правового типа покупателя.");
        b.HasIndex(x => x.CartId).IsUnique(); b.HasIndex(x => x.CheckoutOperationId).IsUnique();
        b.HasOne<Cart>().WithMany().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CheckoutPreview>().WithMany().HasForeignKey(x => x.PreviewId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.Status, x.ReserveUntil });

        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => x.Number).IsUnique();
    }
}

