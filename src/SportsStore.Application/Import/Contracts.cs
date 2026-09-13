using SportsStore.Domain.Entities;
namespace SportsStore.Application.Import;
/// <summary>Распознанный закупочный тариф поставщика до сохранения в действующие предложения.</summary>
/// <param name="Code">Код закупочного тарифа.</param>
/// <param name="Name">Исходное название позиции или закупочного тарифа.</param>
/// <param name="MinimumAmount">Порог суммы закупки у поставщика; null, если не распознан.</param>
public sealed record ParsedTier(string Code, string Name, decimal? MinimumAmount);
/// <summary>Нормализованная позиция прайса; не является карточкой собственного каталога.</summary>
/// <param name="ExternalCode">Строковый код поставщика с сохранёнными ведущими нулями.</param>
/// <param name="Name">Исходное название позиции или закупочного тарифа.</param>
/// <param name="Section">Исходный раздел прайса; не подтверждённая категория или бренд магазина.</param>
/// <param name="Unit">Единица, за которую поставщик указывает цену.</param>
/// <param name="UnitsPerBox">Количество единиц поставщика в коробке; не обязательная кратность заказа.</param>
/// <param name="Stock">Остаток поставщика; null — неизвестен, ноль — явно указан.</param>
/// <param name="Prices">Закупочные цены в порядке тарифов документа; null не заменяется нулём.</param>
/// <param name="SuggestionsJson">JSON с неподтверждёнными признаками для ручной проверки.</param>
public sealed record ParsedOffer(string ExternalCode, string Name, string Section, string Unit,
    decimal? UnitsPerBox, decimal? Stock, decimal?[] Prices, string SuggestionsJson);
/// <summary>Результат разбора одной строки с исходными значениями и диагностикой.</summary>
/// <param name="RowNumber">Номер строки исходного листа, начиная с 1.</param>
/// <param name="Kind">Классификация строки: заголовок, раздел, позиция, пустая или ошибочная.</param>
/// <param name="RawJson">Снимок исходных значений ячеек в JSON.</param>
/// <param name="Offer">Разобранная позиция или null, если строка не содержит позиции.</param>
/// <param name="Diagnostics">Результаты проверки: ошибки и замечания.</param>
public sealed record ParsedRow(int RowNumber, ImportRowKind Kind, string RawJson, ParsedOffer? Offer, string Diagnostics);
/// <summary>Результат разбора целого файла: происхождение, условия, тарифы и строки.</summary>
/// <param name="FileName">Имя исходного файла без пути.</param>
/// <param name="Sha256">Контрольная сумма файла для обнаружения идентичных загрузок.</param>
/// <param name="ParserVersion">Версия парсера, определяющая правила разбора.</param>
/// <param name="SourceDate">Дата документа поставщика, не дата загрузки.</param>
/// <param name="Currency">Трёхбуквенный код валюты; текущий импорт поддерживает RUB.</param>
/// <param name="Conditions">Служебные условия закупки из шапки документа.</param>
/// <param name="Tiers">Упорядоченные закупочные тарифы; порядок совпадает с массивом цен позиций.</param>
/// <param name="Rows">Все сохранённые результаты классификации строк документа.</param>
public sealed record ParsedPriceList(string FileName, string Sha256, string ParserVersion, DateOnly SourceDate,
    string Currency, string Conditions, IReadOnlyList<ParsedTier> Tiers, IReadOnlyList<ParsedRow> Rows);
/// <summary>Контракт разбора прайса поставщика без изменения базы данных; позволяет заменить формат файла.</summary>
public interface ISupplierPriceParser
{
    /// <summary>Версия формата парсера для воспроизводимости импорта.</summary>
    string Version { get; }
    /// <summary>Читает файл по пути, проверяет бинарную сигнатуру и пределы размера; возвращает нормализованный прайс без записи в БД.</summary>
    /// <param name="path">Путь к локальному XLS-файлу; содержимое рассматривается только как данные.</param>
    ParsedPriceList Parse(string path);
}
/// <summary>Отчёт о проверке или применении партии; содержит счётчики и диагностику.</summary>
/// <param name="Id">Идентификатор партии импорта.</param>
/// <param name="Status">Состояние предварительной проверки или применения партии.</param>
/// <param name="SourceDate">Дата документа поставщика, не дата загрузки.</param>
/// <param name="New">Количество новых предложений.</param>
/// <param name="Changed">Количество изменившихся предложений.</param>
/// <param name="Unchanged">Количество предложений без изменений.</param>
/// <param name="Errors">Количество обнаруженных ошибок.</param>
/// <param name="Products">Количество корректных товарных строк; не опубликованных карточек магазина.</param>
/// <param name="UnknownStock">Количество позиций с неизвестным остатком поставщика.</param>
/// <param name="Diagnostics">Результаты проверки: ошибки и замечания.</param>
public sealed record BatchReport(Guid Id, ImportStatus Status, DateOnly SourceDate, int New, int Changed,
    int Unchanged, int Errors, int Products, int UnknownStock, IReadOnlyList<string> Diagnostics);
/// <summary>Прикладной сценарий предварительной проверки, применения и сопоставления закупочных предложений.</summary>
public interface IPriceImportService
{
    /// <summary>Сохраняет проверенную партию и staging без изменения действующих предложений. Идентичный файл той же версии парсера возвращает существующую партию.</summary>
    /// <param name="supplierCode">Внутренний код поставщика, для текущего входа — ballmarket.</param>
    /// <param name="path">Путь к локальному XLS-файлу; содержимое рассматривается только как данные.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    Task<BatchReport> PreviewAsync(string supplierCode, string path, CancellationToken ct = default);
    /// <summary>Читает отчёт партии без отслеживания изменений: счётчики позиций, неизвестных остатков и диагностические сообщения.</summary>
    /// <param name="batchId">Идентификатор партии импорта; в расчёте цены может отсутствовать.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    Task<BatchReport> ReportAsync(Guid batchId, CancellationToken ct = default);
    /// <summary>Повторно сравнивает сохранённые строки с текущими предложениями и обновляет ожидаемую ревизию; применённую партию не меняет.</summary>
    /// <param name="batchId">Идентификатор партии импорта; в расчёте цены может отсутствовать.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    Task<BatchReport> RepreviewAsync(Guid batchId, CancellationToken ct = default);
    /// <summary>Транзакционно применяет проверенную партию: проверяет ревизию и дату, обновляет предложения и историю; повторное применение безопасно.</summary>
    /// <param name="batchId">Идентификатор партии импорта; в расчёте цены может отсутствовать.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    Task<BatchReport> ApplyAsync(Guid batchId, CancellationToken ct = default);
}
