namespace SportsStore.Domain.Entities;
/// <summary>Исходные и разобранные строки прайса, сохранённые для проверки импорта.</summary>
public sealed class ImportRow : Entity
{
    /// <summary>Снимок закупочных цен до preview или repreview в порядке тарифов партии; сохраняется после применения и удаления XLS.</summary>
    public string BeforePricesJson { get; set; } = "[]";
    /// <summary>Идентификатор партии импорта прайса (ImportBatch).</summary>
    public Guid ImportBatchId { get; set; }
    /// <summary>Номер строки в исходном листе Excel, начиная с 1.</summary>
    public int RowNumber { get; set; }
    /// <summary>Тип строки: Empty — пустая, Header — заголовок, Section — раздел, Product — позиция, Error — ошибочная.</summary>
    public ImportRowKind Kind { get; set; }
    /// <summary>Исходные значения ячеек в JSON для аудита; содержимое не исполняется.</summary>
    public string RawJson { get; set; } = "[]";
    /// <summary>Нормализованные данные предложения в JSON; NULL для строк без распознанной позиции.</summary>
    public string? ParsedJson { get; set; }
    /// <summary>Ошибки и замечания при разборе и проверке строки.</summary>
    public string Diagnostics { get; set; } = "";
    /// <summary>Результат сопоставления строки с действующими предложениями при предварительной проверке.</summary>
    public string MatchResult { get; set; } = "";
}

