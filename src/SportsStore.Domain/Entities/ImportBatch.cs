namespace SportsStore.Domain.Entities;
/// <summary>Партии загрузки прайсов: предварительная проверка, диагностика и отдельное транзакционное применение.</summary>
public sealed class ImportBatch : Entity
{
    /// <summary>Идентификатор поставщика (Supplier).</summary>
    public Guid SupplierId { get; set; }
    /// <summary>Имя загруженного файла поставщика.</summary>
    public string FileName { get; set; } = "";
    /// <summary>Контрольная сумма SHA-256 содержимого файла для обнаружения повторной загрузки.</summary>
    public string Sha256 { get; set; } = "";
    /// <summary>Версия парсера, разобравшего файл.</summary>
    public string ParserVersion { get; set; } = "";
    /// <summary>Дата документа прайса поставщика; не время его загрузки.</summary>
    public DateOnly SourceDate { get; set; }
    /// <summary>Трёхбуквенный код валюты, например RUB (российский рубль).</summary>
    public string Currency { get; set; } = "RUB";
    /// <summary>Служебные условия закупок из шапки исходного прайса.</summary>
    public string Conditions { get; set; } = "";
    /// <summary>Снимок распознанных закупочных тарифов в JSON до применения партии.</summary>
    public string TiersJson { get; set; } = "[]";
    /// <summary>Момент загрузки файла, UTC.</summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Момент применения партии, UTC; NULL до применения.</summary>
    public DateTime? AppliedAt { get; set; }
    /// <summary>Статус партии: Preview — предварительная проверка, Invalid — есть ошибки, Applied — применена.</summary>
    public ImportStatus Status { get; set; }
    /// <summary>Ревизия поставщика на момент предварительной проверки; несовпадение при применении означает устаревшую партию.</summary>
    public long ExpectedSupplierRevision { get; set; }
    /// <summary>Количество новых предложений по результату предварительной проверки.</summary>
    public int NewCount { get; set; }
    /// <summary>Количество изменившихся предложений по результату предварительной проверки.</summary>
    public int ChangedCount { get; set; }
    /// <summary>Количество неизменившихся предложений по результату предварительной проверки.</summary>
    public int UnchangedCount { get; set; }
    /// <summary>Количество ошибок, обнаруженных при предварительной проверке.</summary>
    public int ErrorCount { get; set; }
}

