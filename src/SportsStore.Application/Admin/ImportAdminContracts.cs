using SportsStore.Application.Import;
using SportsStore.Domain.Entities;

namespace SportsStore.Application.Admin;

/// <summary>Безопасное краткое представление партии без исходного файла и JSON.</summary>
/// <param name="Id">Партия.</param><param name="Name">Исходное имя файла.</param><param name="Date">Дата документа.</param><param name="Status">Статус проверки.</param>
/// <param name="New">Новые позиции.</param><param name="Changed">Изменения.</param><param name="Unchanged">Без изменений.</param><param name="Errors">Ошибки.</param>
public sealed record BatchData(Guid Id, string Name, DateOnly Date, ImportStatus Status, int New, int Changed, int Unchanged, int Errors);

/// <summary>Строка сравнения staging и действующих закупочных цен; страницы вычисляются сервером.</summary>
/// <param name="Number">Номер Excel-строки.</param><param name="Code">Код поставщика.</param><param name="Name">Название позиции.</param>
/// <param name="OldPrices">Прежние цены.</param><param name="NewPrices">Цены из staging.</param><param name="Result">Результат сопоставления.</param><param name="Problem">Диагностика.</param>
public sealed record ImportChangeData(int Number, string Code, string Name, IReadOnlyList<decimal?> OldPrices, IReadOnlyList<decimal?> NewPrices, string Result, string Problem);

/// <summary>Административный адаптер импорта: поток файла вместо пути, ограниченные отчёты и атомарный аудит.</summary>
public interface IAdminImport
{
    /// <summary>Принимает поток до 10 МиБ, удаляет временный файл при любом исходе и сохраняет staging.</summary>
    Task<BatchReport> UploadAsync(Stream content, string originalName, Guid operationId, CancellationToken ct = default);
    /// <summary>Возвращает страницу партий.</summary>
    Task<AdminPage<BatchData>> BatchesAsync(AdminQuery query, CancellationToken ct = default);
    /// <summary>Читает сохранённый отчёт после перезапуска без исходного XLS.</summary>
    Task<BatchReport> ReportAsync(Guid id, CancellationToken ct = default);
    /// <summary>Возвращает страницу сравнения цен для проверки.</summary>
    Task<AdminPage<ImportChangeData>> ChangesAsync(Guid id, AdminQuery query, CancellationToken ct = default);
    /// <summary>Повторно проверяет staging по текущей ревизии.</summary>
    Task<BatchReport> RepreviewAsync(Guid id, Guid operationId, CancellationToken ct = default);
    /// <summary>Применяет партию только от Admin; после обрыва соединения следует читать ReportAsync.</summary>
    Task<BatchReport> ApplyAsync(Guid id, Guid operationId, CancellationToken ct = default);
}
