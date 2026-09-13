using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Серверная корзина одного покупателя либо гостевого секрета; преобразованная корзина больше не редактируется.</summary>
public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    /// <summary>Настраивает связи, ограничения и русские комментарии PostgreSQL.</summary>
    public void Configure(EntityTypeBuilder<Cart> b)
    {
        ConfigurationDefaults.Entity(b);
        b.ToTable(t => t.HasComment("Серверная корзина одного покупателя либо гостевого секрета; преобразованная корзина больше не редактируется."));
        b.Property(x => x.Id).HasComment("Устойчивый идентификатор записи.");
        b.Property(x => x.Version).HasComment("Версия xmin для конкурентных изменений.");
        b.Property(x => x.CustomerId).HasComment("Владелец-покупатель; null только у гостя.");
        b.Property(x => x.GuestKeyHash).HasComment("SHA-256 случайного гостевого секрета; сам секрет в БД не хранится.");
        b.Property(x => x.State).HasComment("Состояние активной, объединённой или оформленной корзины.");
        b.Property(x => x.CreatedAt).HasComment("Время создания, UTC.");
        b.Property(x => x.UpdatedAt).HasComment("Время изменения состава, UTC.");
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.GuestKeyHash).IsUnique(); b.HasIndex(x => x.CustomerId).IsUnique().HasFilter("\"State\" = 'Active' AND \"CustomerId\" IS NOT NULL"); b.ToTable(t => t.HasCheckConstraint("CK_Cart_Owner", "(\"CustomerId\" IS NULL) <> (\"GuestKeyHash\" IS NULL)"));
    }
}
