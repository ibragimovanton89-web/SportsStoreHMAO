using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExcelDataReader;
using SportsStore.Application.Import;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Import;

/// <summary>Читает бинарный XLS BallMarket без Excel COM; проверяет сигнатуру, ограничения, заголовки и данные.</summary>
public sealed class BallMarketParser : ISupplierPriceParser
{
    /// <summary>Версия формата парсера для воспроизводимости импорта.</summary>
    public string Version => "ballmarket-biff/1.0.0";
    /// <summary>Максимальный размер входного XLS в байтах: 10 МиБ.</summary>
    public const int MaxBytes = 10 * 1024 * 1024;
    /// <summary>Максимальное количество обрабатываемых строк листа: 20 000.</summary>
    public const int MaxRows = 20000;
    /// <summary>Обязательные заголовки для поиска таблицы независимо от номера строки.</summary>
    private static readonly string[] Headers = ["Наименование товаров", "Ед.", "Кол. в кор.", "Мелкий Опт", "Опт", "Крупный Опт", "Остаток на складе"];
    /// <summary>Читает файл по пути, проверяет бинарную сигнатуру и пределы размера; возвращает нормализованный прайс без записи в БД.</summary>
    /// <param name="path">Путь к локальному XLS-файлу; содержимое рассматривается только как данные.</param>
    public ParsedPriceList Parse(string path)
    {
        using var file = File.OpenRead(path);
        if (file.Length is <= 0 or > MaxBytes) throw new InvalidDataException("File must be 1 byte..10 MiB.");
        var bytes = new byte[checked((int)file.Length)];
        file.ReadExactly(bytes);
        if (!bytes.AsSpan(0, Math.Min(8, bytes.Length)).SequenceEqual(new byte[] { 0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1 }))
            throw new InvalidDataException("Expected an OLE/BIFF .xls file; extension is not trusted.");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateBinaryReader(new MemoryStream(bytes));
        var raw = new List<(string[] Values, string? CodeFormat)>();
        while (reader.Read())
        {
            if (raw.Count >= MaxRows || reader.FieldCount > 64) throw new InvalidDataException("Worksheet exceeds limits.");
            var values = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.GetValue(i) is double numeric && Regex.IsMatch(reader.GetNumberFormatString(i) ?? "", "^0{2,}$")
                    ? numeric.ToString(reader.GetNumberFormatString(i), CultureInfo.InvariantCulture)
                    : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture) ?? "").ToArray();
            if (values.Any(x => x.Length > 8000)) throw new InvalidDataException("Cell exceeds 8000 characters.");
            raw.Add((values, null));
        }
        if (reader.NextResult()) throw new InvalidDataException("Multiple sheets require explicit parser review.");
        while (raw.Count > 0 && raw[^1].Values.All(string.IsNullOrWhiteSpace)) raw.RemoveAt(raw.Count - 1);
        return ParseRows(Path.GetFileName(path), Convert.ToHexString(SHA256.HashData(bytes)), raw.Select(x => x.Values).ToArray());
    }

    // Общая нормализация для бинарного чтения и тестов; формулы не вычисляются.
    /// <summary>Находит таблицу по заголовкам, извлекает дату и закупочные тарифы; классифицирует строки, сохраняя ведущие нули и неизвестные значения.</summary>
    /// <param name="name">Имя исходного файла для отчёта.</param>
    /// <param name="hash">SHA-256 исходного содержимого.</param>
    /// <param name="rows">Строки исходного листа; индекс массива начинается с нуля.</param>
    public ParsedPriceList ParseRows(string name, string hash, IReadOnlyList<string[]> rows)
    {
        if (rows.Count > MaxRows) throw new InvalidDataException("Too many rows.");
        var header = -1;
        int[] columns = [];
        for (int r = 0; r < Math.Min(rows.Count, 200); r++)
        {
            var values = rows[r].Select(Normalize).ToArray();
            var indices = Headers.Select(h => Array.FindIndex(values, v => v.Equals(h, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (indices.All(x => x >= 0)) { header = r; columns = indices; break; }
        }
        if (header < 0 || columns[0] == 0) throw new InvalidDataException("Table headings or external-code column not found.");
        var conditions = string.Join("\n", rows.Take(header).SelectMany(x => x).Where(x => !string.IsNullOrWhiteSpace(x)));
        if (conditions.Length > 16000) throw new InvalidDataException("Header text exceeds limit.");
        var dateMatch = Regex.Match(conditions, @"(?:от|на)\s+(\d{2}\.\d{2}\.\d{2,4})", RegexOptions.IgnoreCase);
        if (!dateMatch.Success || !DateOnly.TryParseExact(dateMatch.Groups[1].Value, ["dd.MM.yyyy","dd.MM.yy"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw new InvalidDataException("Source date not found.");
        if (!conditions.Contains("руб", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("RUB currency not confirmed.");
        var tiers = new[] {
            new ParsedTier("small", "Мелкий Опт", Threshold(conditions, @"Мелкий\s+опт")),
            new ParsedTier("wholesale", "Опт", Threshold(conditions, @"(?<![А-Яа-я]\s)\bОпт")),
            new ParsedTier("large", "Крупный Опт", Threshold(conditions, @"Крупный\s+опт"))
        };
        var result = new List<ParsedRow>();
        var seen = new Dictionary<string, ParsedOffer>(StringComparer.Ordinal);
        string section = "";
        bool afterProduct = true;
        for (int r = 0; r < rows.Count; r++)
        {
            var values = rows[r];
            // Читает нормализованную ячейку; короткая строка означает пустую ячейку, а не выход за границы.
            string Cell(int col) => col < values.Length ? Normalize(values[col]) : "";
            var rawJson = JsonSerializer.Serialize(values);
            if (r <= header) { result.Add(new(r+1, ImportRowKind.Header, rawJson, null, "")); continue; }
            if (values.All(string.IsNullOrWhiteSpace)) { result.Add(new(r+1, ImportRowKind.Empty, rawJson, null, "")); continue; }
            var code = Cell(columns[0]-1);
            var productName = Cell(columns[0]);
            if (code.Length == 0 && columns.Skip(1).All(c => Cell(c).Length == 0))
            {
                if (productName.Length > 0)
                    section = afterProduct || section.Length == 0 ? productName : section + " / " + productName;
                afterProduct = false;
                result.Add(new(r+1, ImportRowKind.Section, rawJson, null, "")); continue;
            }
            var diagnostics = new List<string>();
            afterProduct = true;
            if (!Regex.IsMatch(code, @"^\d{1,64}$")) diagnostics.Add("Invalid external code.");
            if (productName.Length is 0 or > 4000) diagnostics.Add("Invalid product name.");
            var unit = Cell(columns[1]).ToLowerInvariant().TrimEnd('.');
            if (unit is not ("шт" or "пар" or "компл" or "упак")) diagnostics.Add("Unknown supplier unit.");
            var box = Number(Cell(columns[2]), false, "UnitsPerBox", diagnostics);
            var prices = columns.Skip(3).Take(3).Select(c => Number(Cell(c), false, "Price", diagnostics)).ToArray();
            var stock = Number(Cell(columns[6]), true, "Stock", diagnostics);
            var suggestions = JsonSerializer.Serialize(new { NeedsReview = true, Section = section,
                IsNewMarker = productName.Contains("НОВИНКА", StringComparison.OrdinalIgnoreCase),
                SupplierSpecialMarker = productName.Contains("СЦ*", StringComparison.OrdinalIgnoreCase) });
            var offer = new ParsedOffer(code, productName, section, unit, box, stock, prices, suggestions);
            if (seen.TryGetValue(code, out var prior))
            {
                if (JsonSerializer.Serialize(prior) == JsonSerializer.Serialize(offer))
                { result.Add(new(r+1, ImportRowKind.Header, rawJson, null, "Identical duplicate ignored.")); continue; }
                diagnostics.Add("Conflicting duplicate external code.");
            }
            else seen[code] = offer;
            result.Add(new(r+1, diagnostics.Count == 0 ? ImportRowKind.Product : ImportRowKind.Error,
                rawJson, offer, string.Join(" ", diagnostics)));
        }
        if (!result.Any(x => x.Kind == ImportRowKind.Product)) throw new InvalidDataException("No valid products.");
        return new(name, hash, Version, date, "RUB", conditions, tiers, result);
    }
    /// <summary>Заменяет неразрывные и повторные пробелы обычным пробелом и убирает пробелы по краям.</summary>
    /// <param name="value">Исходное значение для нормализации или разбора.</param>
    private static string Normalize(string value) => Regex.Replace(value.Replace('\u00a0',' ').Replace('\u202f',' '), @"\s+", " ").Trim();
    /// <summary>Разбирает decimal с запятой или точкой; пустое значение оставляет неизвестным, неверное добавляет в диагностику.</summary>
    /// <param name="value">Исходное значение для нормализации или разбора.</param>
    /// <param name="zeroAllowed">Допустим ли явный ноль; для остатков допустим, для цены запрещён.</param>
    /// <param name="field">Название поля для диагностического сообщения.</param>
    /// <param name="errors">Список, в который добавляется ошибка разбора.</param>
    private static decimal? Number(string value, bool zeroAllowed, string field, List<string> errors)
    {
        if (value.Length == 0) return null;
        if (!decimal.TryParse(value.Replace(" ", "").Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var number) || number < 0 || (!zeroAllowed && number == 0) || number > 99999999999999m || decimal.Round(number, 4) != number)
        { errors.Add(field + ": invalid number."); return null; }
        return number;
    }
    /// <summary>Извлекает описательный порог закупочного тарифа из шапки; не определяет условия покупателей магазина.</summary>
    /// <param name="conditions">Текст условий из шапки прайса.</param>
    /// <param name="label">Шаблон названия закупочного тарифа для поиска порога.</param>
    private static decimal? Threshold(string conditions, string label)
    {
        // Пороги описывают закупки у поставщика и не определяют сегмент покупателя магазина.
        var m = Regex.Match(conditions, label + @"\s*-\s*(?:(?:заказы|закупка)\s+)?от\s+([\d\s]+)\s*руб", RegexOptions.IgnoreCase);
        return m.Success && decimal.TryParse(m.Groups[1].Value.Replace(" ", ""), out var value) ? value : null;
    }
}
