using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using SportsStore.Application.Storefront;
namespace SportsStore.Web.Storefront;

/// <summary>Единый ограниченный формат URL каталога, без выбора оптового сегмента и внешних возвратов.</summary>
public static class CatalogUrls
{
    /// <summary>Читает только известные параметры; неверные числа и идентификаторы заменяются безопасными значениями.</summary>
    /// <param name="uri">Абсолютный адрес текущей страницы.</param>
    public static CatalogQuery Parse(string uri)
    {
        var args = QueryHelpers.ParseQuery(new Uri(uri).Query);
        string One(string key) => args.TryGetValue(key, out var values) ? values.FirstOrDefault() ?? "" : "";
        string[] Many(string key) => args.TryGetValue(key, out var values) ? values.Take(20).Select(x => x ?? "").ToArray() : [];
        decimal? Money(string key) => decimal.TryParse(One(key), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ? value : null;
        return new CatalogQuery { Search = One("q"), Category = Guid.TryParse(One("category"), out var category) ? category : null,
            Brands = Many("brand").Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty).ToArray(), Sizes = Many("size"), Colors = Many("color"),
            MinPrice = Money("min"), MaxPrice = Money("max"), Availability = One("availability"), Sort = One("sort"), Page = int.TryParse(One("page"), out var page) ? page : 1 }.Normalize();
    }
    /// <summary>Создаёт канонический относительный URL выбранного состояния, кодируя каждое значение.</summary>
    /// <param name="query">Состояние фильтров и страницы.</param>
    public static string Catalog(CatalogQuery query)
    {
        query = query.Normalize(); var pairs = new List<KeyValuePair<string, string?>>();
        void Add(string key, string? value) { if (!string.IsNullOrEmpty(value)) pairs.Add(new(key, value)); }
        Add("q", query.Search); Add("category", query.Category?.ToString());
        foreach (var id in query.Brands) Add("brand", id.ToString()); foreach (var size in query.Sizes) Add("size", size); foreach (var color in query.Colors) Add("color", color);
        Add("min", query.MinPrice?.ToString(CultureInfo.InvariantCulture)); Add("max", query.MaxPrice?.ToString(CultureInfo.InvariantCulture));
        if (query.Availability != "all") Add("availability", query.Availability); if (query.Sort != "name") Add("sort", query.Sort); if (query.Page > 1) Add("page", query.Page.ToString(CultureInfo.InvariantCulture));
        return QueryHelpers.AddQueryString("/catalog", pairs);
    }
    /// <summary>Ссылка сразу выбирает подходящий фильтрам вариант и сохраняет безопасный возврат.</summary>
    public static string Product(Guid product, Guid variant, CatalogQuery query) => QueryHelpers.AddQueryString($"/catalog/{product}", new Dictionary<string, string?> { ["variant"] = variant.ToString(), ["return"] = Catalog(query) });
    /// <summary>Разрешает возврат только к собственному каталогу и его известным параметрам.</summary>
    public static string SafeReturn(string? value) => value == "/catalog" || value?.StartsWith("/catalog?", StringComparison.Ordinal) == true ? Catalog(Parse("https://catalog.invalid" + value)) : "/catalog";
    /// <summary>Подпись приоритета доступности без обещаний поставки.</summary>
    public static string Availability(string value) => value switch { "stock" => "На складе", "incoming" => "В пути", _ => "Нет в наличии" };
    /// <summary>Canonical из явно настроенного HTTPS-адреса; для локальной разработки допускается проверенный loopback.</summary>
    public static string? Canonical(string? configured, string current, string path)
    {
        if (Uri.TryCreate(configured, UriKind.Absolute, out var origin) && origin.Scheme == "https" && string.IsNullOrEmpty(origin.UserInfo) && origin.AbsolutePath == "/" && origin.Query.Length == 0 && origin.Fragment.Length == 0) return origin.GetLeftPart(UriPartial.Authority) + path;
        var local = new Uri(current); return local.IsLoopback && local.Scheme is "http" or "https" ? local.GetLeftPart(UriPartial.Authority) + path : null;
    }
}
