using System.Text.RegularExpressions;
namespace SportsStore.Infrastructure.Import;

/// <summary>Признаки закупочного подбора из текста BallMarket; не создают и не изменяют собственные справочники магазина.</summary>
public static class BallMarketFacets
{
    /// <summary>Явное значение для отсутствующего или неоднозначного признака.</summary>
    public const string Unknown = "Не определён";
    /// <summary>Отдельные бренды из проверенных заголовков прайса; линейки и страна производства сюда не входят.</summary>
    private static readonly string[] Brands = ["ADIDAS", "ADRENALINA", "ASICS", "BABOLAT", "NEVA", "TECNIFIBRE", "BIG BOY", "IB HOCKEY", "BULLPADEL", "DIADEM", "DOUBLE FISH", "DUNLOP", "ERGOFORCE", "GALA", "GILBERT", "HEAD", "INDIGO", "KELME", "KV.REZAC", "MACRON", "MIKASA", "MITRE", "MIZUNO", "MOLTEN", "NIKE", "PENALTY", "PUMA", "SALVAS", "SELECT", "SPALDING", "SPEEDO", "STIGA", "TORRES", "TYR", "UNDER ARMOUR", "WARRIOR", "BLUE SPORT", "TEXSTYLE", "WILSON", "WISH", "COMPEX", "CURETAPE", "DISPOTECH", "DYNAMIC", "PHYSIOTAPE", "REHABMEDIC", "TMAX", "WINNER MEDICAL"];
    /// <summary>Поиск полных названий, исключающий ложные совпадения HEAD в HEADBAND и NIKE в длинном слове.</summary>
    private static readonly Regex BrandPattern = new(@"(?<![\p{L}\p{N}])(?:" + string.Join("|", Brands.Select(Regex.Escape)) + @")(?![\p{L}\p{N}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    /// <summary>Нормализует пробелы, сохраняя исходное название группы для отображения.</summary>
    private static string Normalize(string value) => Regex.Replace(value.Replace('\u00a0', ' '), @"\s+", " ").Trim();
    /// <summary>Определяет бренд и последнюю товарную группу; несколько брендов в названии остаются неопределёнными.</summary>
    /// <param name="name">Исходное название позиции поставщика.</param>
    /// <param name="section">Сохранённые заголовки импорта, разделённые « / ».</param>
    /// <returns>Признаки только для фильтрации закупочного прайса.</returns>
    public static (string Brand, string Category) Classify(string name, string section)
    {
        var matches = BrandPattern.Matches(Normalize(name)).Select(x => x.Value.ToUpperInvariant()).Distinct().ToArray();
        var parts = section.Split(" / ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Normalize).ToArray();
        var brand = matches.Length == 1 ? matches[0] : Unknown;
        if (matches.Length == 0)
        {
            var headings = parts.Where(p => Brands.Contains(p, StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (headings.Length == 1) brand = headings[0].ToUpperInvariant();
        }
        var category = parts.LastOrDefault() ?? Unknown;
        if (Brands.Contains(category, StringComparer.OrdinalIgnoreCase) || category.Equals("MADE IN RUSSIA", StringComparison.OrdinalIgnoreCase)
            || category.Split(',').All(p => Brands.Contains(p.Trim(), StringComparer.OrdinalIgnoreCase))) category = Unknown;
        return (brand, category);
    }
}
