using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Import;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Подстановка признаков прайса и создание справочников только при сохранении собственной карточки.</summary>
public sealed partial class AdminCatalog
{
    /// <summary>Читает однозначные признаки предложения или связанных вариантов, не создавая справочники.</summary>
    /// <param name="id">Идентификатор предложения или карточки.</param>
    /// <param name="product">True обрабатывает предложения всех вариантов карточки.</param>
    /// <param name="ct">Отмена чтения.</param>
    /// <returns>Названия для предварительного заполнения; null означает отсутствие однозначного признака.</returns>
    public async Task<(string? Brand, string? Category)> SuggestLookupsAsync(Guid id, bool product, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await access.RequireAsync(db, false, ct);
        var query = db.SupplierOffers.AsNoTracking().Where(o => product
            ? db.ProductVariants.Any(v => v.ProductId == id && v.Id == o.ProductVariantId)
            : o.Id == id);
        var rows = await query.OrderBy(o => o.Id).Select(o => new { o.SourceName, o.SourceSection }).Take(1001).ToListAsync(ct);
        if (rows.Count > 1000) return (null, null);
        var facets = rows.Select(o => BallMarketFacets.Classify(o.SourceName, o.SourceSection)).ToArray();
        return (UniqueSuggestion(facets.Select(x => x.Brand)), UniqueSuggestion(facets.Select(x => x.Category)));
    }

    /// <summary>Возвращает единственное известное название без учёта регистра; конфликтующие признаки не угадываются.</summary>
    /// <param name="values">Результаты распознавания всех связанных предложений.</param>
    /// <returns>Однозначное название либо null.</returns>
    private static string? UniqueSuggestion(IEnumerable<string> values)
    {
        var names = values.Where(x => x != BallMarketFacets.Unknown).Distinct(StringComparer.OrdinalIgnoreCase).Take(2).ToArray();
        return names.Length == 1 ? names[0] : null;
    }

    /// <summary>Находит либо добавляет справочники в транзакции вызывающей команды; выбранный ID имеет приоритет над текстом.</summary>
    /// <param name="db">Контекст с открытой транзакцией; сохранением и откатом владеет вызывающая команда.</param>
    /// <param name="brandId">Явно выбранный бренд или null.</param>
    /// <param name="categoryId">Явно выбранная категория или null.</param>
    /// <param name="brandName">Подтверждённое пользователем название бренда; пустое не создаёт запись.</param>
    /// <param name="categoryName">Подтверждённое название категории; новая категория создаётся без родителя.</param>
    /// <param name="ct">Отмена запросов.</param>
    /// <returns>Ссылки на существующие или добавленные в контекст записи.</returns>
    /// <exception cref="ArgumentException">Название слишком длинное или неоднозначно в справочнике.</exception>
    internal static async Task<(Guid? Brand, Guid? Category)> ResolveLookupsAsync(ApplicationDbContext db,
        Guid? brandId, Guid? categoryId, string? brandName, string? categoryName, CancellationToken ct)
    {
        // Общая блокировка с редактором справочников исключает дубли от параллельного сохранения карточек.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861001)", ct);
        if (brandId is null && NormalizeLookupName(brandName) is string brand)
        {
            var matches = await db.Brands.Where(x => x.Name.ToLower() == brand.ToLower()).OrderBy(x => x.Id).Take(2).ToListAsync(ct);
            if (matches.Count > 1) throw new ArgumentException("Несколько брендов с таким названием. Выберите запись из списка.");
            var item = matches.SingleOrDefault();
            if (item is null) { item = new Brand { Name = brand }; db.Brands.Add(item); }
            brandId = item.Id;
        }
        if (categoryId is null && NormalizeLookupName(categoryName) is string category)
        {
            var matches = await db.Categories.Where(x => x.Name.ToLower() == category.ToLower()).OrderBy(x => x.Id).Take(2).ToListAsync(ct);
            if (matches.Count > 1) throw new ArgumentException("Название категории встречается в нескольких ветках. Выберите нужную запись из списка.");
            var item = matches.SingleOrDefault();
            if (item is null) { item = new Category { Name = category }; db.Categories.Add(item); }
            categoryId = item.Id;
        }
        return (brandId, categoryId);
    }

    /// <summary>Убирает лишние пробелы и не допускает записи служебного обозначения неизвестного признака.</summary>
    /// <param name="value">Ввод сотрудника либо распознанное название.</param>
    /// <returns>Проверенное название до 500 символов либо null.</returns>
    private static string? NormalizeLookupName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var name = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return name.Equals(BallMarketFacets.Unknown, StringComparison.OrdinalIgnoreCase) ? null : AdminAccess.Text(name, "Название справочника");
    }
}
