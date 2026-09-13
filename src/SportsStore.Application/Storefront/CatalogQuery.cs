namespace SportsStore.Application.Storefront;

/// <summary>Собственный справочник публичной навигации, не связанный с оформлением прайса.</summary>
/// <param name="Id">Идентификатор бренда или категории.</param><param name="Name">Название.</param><param name="ParentId">Родитель категории или null.</param>
public sealed record StoreLookup(Guid Id, string Name, Guid? ParentId = null);
/// <summary>Возможные значения в рамках поиска и категории; скрытые товары не участвуют.</summary>
/// <param name="Brands">Бренды.</param><param name="Categories">Категории с публичными потомками.</param><param name="Sizes">Размеры.</param><param name="Colors">Цвета.</param>
public sealed record CatalogFacets(IReadOnlyList<StoreLookup> Brands, IReadOnlyList<StoreLookup> Categories, IReadOnlyList<string> Sizes, IReadOnlyList<string> Colors);

/// <summary>Ограниченный розничный запрос: OR внутри группы и AND между группами, без выбора оптового сегмента.</summary>
public sealed record CatalogQuery
{
    /// <summary>Буквальный поиск по названию, SKU и артикулу производителя, до 200 символов.</summary>
    public string Search { get; init; } = "";
    /// <summary>Категория вместе с потомками; null снимает ограничение.</summary>
    public Guid? Category { get; init; }
    /// <summary>До 20 собственных брендов.</summary>
    public IReadOnlyList<Guid> Brands { get; init; } = [];
    /// <summary>До 20 размеров одного подходящего варианта.</summary>
    public IReadOnlyList<string> Sizes { get; init; } = [];
    /// <summary>До 20 цветов того же варианта.</summary>
    public IReadOnlyList<string> Colors { get; init; } = [];
    /// <summary>Нижняя включительная граница розничной цены RUB.</summary>
    public decimal? MinPrice { get; init; }
    /// <summary>Верхняя включительная граница розничной цены RUB.</summary>
    public decimal? MaxPrice { get; init; }
    /// <summary>all, stock, incoming или absent; поступление не заменяет собственный остаток.</summary>
    public string Availability { get; init; } = "all";
    /// <summary>name, price-asc, price-desc или newest, всегда с дополнительным порядком по Id.</summary>
    public string Sort { get; init; } = "name";
    /// <summary>Страница от единицы; выход за результаты ограничивается последней страницей.</summary>
    public int Page { get; init; } = 1;
    /// <summary>Есть ли ограничения помимо поиска и сортировки.</summary>
    public bool HasFilters => Category is not null || Brands.Count > 0 || Sizes.Count > 0 || Colors.Count > 0 || MinPrice is not null || MaxPrice is not null || Availability != "all";
    /// <summary>Нормализует недоверенные границы и списки; перевёрнутый диапазон меняет местами.</summary>
    public CatalogQuery Normalize()
    {
        var min = MinPrice is >= 0 and <= 9999999999999999m ? MinPrice : null;
        var max = MaxPrice is >= 0 and <= 9999999999999999m ? MaxPrice : null;
        if (min > max) (min, max) = (max, min);
        return this with { Search = Text(Search, 200), Brands = Brands.Where(x => x != Guid.Empty).Distinct().Take(20).ToArray(),
            Sizes = Values(Sizes), Colors = Values(Colors), Category = Category == Guid.Empty ? null : Category,
            MinPrice = min, MaxPrice = max, Availability = Availability is "stock" or "incoming" or "absent" ? Availability : "all",
            Sort = Sort is "price-asc" or "price-desc" or "newest" ? Sort : "name", Page = Math.Clamp(Page, 1, 100000) };
    }
    /// <summary>Убирает повторы и крайние пробелы, не извлекая характеристики из свободного текста.</summary>
    private static string[] Values(IEnumerable<string> values) => values.Select(x => x.Trim()).Where(x => x.Length is > 0 and <= 500).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();
    /// <summary>Нормализует последовательности пробелов и ограничивает длину текста.</summary>
    /// <param name="value">Исходный поисковый текст.</param><param name="limit">Максимальная длина.</param>
    public static string Text(string? value, int limit) { var text = string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)); return text.Length > limit ? text[..limit] : text; }
}
