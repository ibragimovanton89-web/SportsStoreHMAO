using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence.Configurations;
/// <summary>Общие правила EF: ключи, xmin, длины строк, точность decimal и строковые перечисления с ограничениями.</summary>
internal static class ConfigurationDefaults
{
    /// <summary>Настраивает общие ключи, точность чисел, длины строк и допустимые значения перечислений.</summary>
    /// <param name="b">Построитель отображения сущности в реляционную таблицу.</param>
    public static void Entity<T>(EntityTypeBuilder<T> b) where T : Entity
    {
        b.ToTable(typeof(T).Name);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsRowVersion(); // PostgreSQL xmin
        foreach (var p in b.Metadata.GetProperties())
        {
            if (p.ClrType == typeof(string)) p.SetMaxLength(p.Name == "Currency" ? 3 : 500);
            if (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
            { p.SetPrecision(18); p.SetScale(4); }
            if (p.ClrType.IsEnum)
            {
                b.Property(p.Name).HasConversion<string>().HasMaxLength(32);
                var values = string.Join(", ", Enum.GetNames(p.ClrType).Select(x => "'" + x + "'"));
                b.ToTable(t => t.HasCheckConstraint("CK_" + typeof(T).Name + "_" + p.Name, "\"" + p.Name + "\" IN (" + values + ")"));
            }
        }
    }
}
