using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Настраивает хранение Category в PostgreSQL: свойства, связи, индексы и ограничения целостности.</summary>
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>Задаёт отображение Category и ограничения базы данных; применяется при построении модели DbContext.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public void Configure(EntityTypeBuilder<Category> b)
    {
        ConfigurationDefaults.Entity(b);
        b.HasOne<Category>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_Category_Valid", "\"ParentId\" IS NULL OR \"ParentId\" <> \"Id\""));
    }
}

