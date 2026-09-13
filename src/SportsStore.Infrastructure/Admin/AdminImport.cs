using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SportsStore.Application.Admin;
using SportsStore.Application.Import;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Import;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Авторизованный адаптер импорта с временным хранением вне wwwroot и аудитом в транзакции существующего сервиса.</summary>
/// <param name="factory">Фабрика контекстов.</param><param name="access">Проверка прав.</param><param name="parser">Парсер поставщика.</param><param name="configuration">Путь внешнего хранилища.</param>
public sealed class AdminImport(IDbContextFactory<ApplicationDbContext> factory, AdminAccess access, ISupplierPriceParser parser, IConfiguration configuration) : IAdminImport
{
    /// <summary>Создаёт сервис с внутренним обработчиком аудита; перед фиксацией права проверяются повторно.</summary>
    private PriceImportService Service(bool admin, string action, Guid operationId, ISupplierPriceParser? selected = null) => new(factory, selected ?? parser,
        async (db, objectId, ct) =>
        {
            var actor = await access.RequireAsync(db, admin, ct);
            if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is null)
                AdminAccess.Audit(db, actor, operationId, action, "ImportBatch", objectId, "Сохранён результат операции импорта прайса.");
        });

    /// <summary>Копирует поток с жёстким ограничением объёма; произвольный серверный путь от клиента не принимается.</summary>
    public async Task<BatchReport> UploadAsync(Stream content, string originalName, Guid operationId, CancellationToken ct = default)
    {
        await using (var db = await factory.CreateDbContextAsync(ct)) await access.RequireAsync(db, false, ct);
        originalName = AdminAccess.Text(Path.GetFileName(originalName.Replace('\\', '/')), "Имя файла", 255);
        var folder = Path.Combine(StorageRoot(configuration), "imports"); Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".xls");
        try
        {
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920]; long total = 0; int read;
                while ((read = await content.ReadAsync(buffer, ct)) > 0)
                {
                    total += read; if (total > BallMarketParser.MaxBytes) throw new InvalidDataException("Файл превышает 10 МиБ.");
                    await output.WriteAsync(buffer.AsMemory(0, read), ct);
                }
            }
            ct.ThrowIfCancellationRequested();
            return await Service(false, "import.preview", operationId, new NamedParser(parser, originalName)).PreviewAsync("ballmarket", path, ct);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    /// <summary>Возвращает внешний постоянный каталог хранения, общий для Web-перезапусков; в тестах задаётся временный путь.</summary>
    internal static string StorageRoot(IConfiguration configuration) => Path.GetFullPath(configuration["Storage:RootPath"]
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SportsStoreHMAO", "storage"));

    /// <summary>Подменяет только метаданные имени; реальный парсер получает сгенерированный серверный путь.</summary>
    /// <param name="inner">Реальный парсер.</param><param name="name">Очищенное имя пользователя.</param>
    private sealed class NamedParser(ISupplierPriceParser inner, string name) : ISupplierPriceParser
    {
        /// <summary>Версия реального парсера для дедупликации.</summary>
        public string Version => inner.Version;
        /// <summary>Разбирает локальный временный файл, сохраняя исходное имя только как метаданные.</summary>
        public ParsedPriceList Parse(string path) => inner.Parse(path) with { FileName = name };
    }

    /// <summary>Возвращает ограниченный список партий с фильтрацией на сервере.</summary>
    public async Task<AdminPage<BatchData>> BatchesAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var q = db.ImportBatches.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(x => x.FileName.Contains(query.Search));
        if (Enum.TryParse<ImportStatus>(query.Filter, out var status)) q = q.Where(x => x.Status == status);
        var total = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        return new(await q.OrderByDescending(x => x.UploadedAt).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25)
            .Select(x => new BatchData(x.Id, x.FileName, x.SourceDate, x.Status, x.NewCount, x.ChangedCount, x.UnchangedCount, x.ErrorCount)).ToListAsync(ct), total, page);
    }

    /// <summary>Читает агрегаты без загрузки всех staging-строк; диагностика доступна страницами через ChangesAsync.</summary>
    public async Task<BatchReport> ReportAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var b = await db.ImportBatches.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        var products = await db.ImportRows.CountAsync(x => x.ImportBatchId == id && x.Kind == ImportRowKind.Product, ct);
        // JSON-поле остаётся на сервере; PostgreSQL считает неизвестные остатки без передачи тысяч строк.
        var unknown = await db.Database.SqlQuery<int>($"SELECT count(*)::integer AS \"Value\" FROM \"ImportRow\" WHERE \"ImportBatchId\"={id} AND \"Kind\"='Product' AND (\"ParsedJson\"::jsonb -> 'Stock') = 'null'::jsonb").SingleAsync(ct);
        return new(b.Id, b.Status, b.SourceDate, b.NewCount, b.ChangedCount, b.UnchangedCount, b.ErrorCount, products, unknown, []);
    }

    /// <summary>Возвращает максимум 25 разобранных строк и снимки прежних закупочных цен.</summary>
    public async Task<AdminPage<ImportChangeData>> ChangesAsync(Guid id, AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        var q = db.ImportRows.AsNoTracking().Where(x => x.ImportBatchId == id && (x.Kind == ImportRowKind.Product || x.Kind == ImportRowKind.Error));
        if (query.Filter == "changed") q = q.Where(x => x.MatchResult.StartsWith("Changed"));
        if (query.Filter == "errors") q = q.Where(x => x.Kind == ImportRowKind.Error);
        if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(x => x.ParsedJson != null && x.ParsedJson.Contains(query.Search));
        var total = await q.CountAsync(ct); var page = Math.Clamp(query.Page, 1, 100000);
        var rows = await q.OrderBy(x => x.RowNumber).Skip((page - 1) * 25).Take(25).Select(x => new { x.RowNumber, x.ParsedJson, x.BeforePricesJson, x.MatchResult, x.Diagnostics }).ToListAsync(ct);
        return new(rows.Select(x =>
        {
            var parsed = x.ParsedJson is null ? null : JsonSerializer.Deserialize<ParsedOffer>(x.ParsedJson);
            return new ImportChangeData(x.RowNumber, parsed?.ExternalCode ?? "", parsed?.Name ?? "Строка без позиции",
                JsonSerializer.Deserialize<decimal?[]>(x.BeforePricesJson) ?? [], parsed?.Prices ?? [],
                x.MatchResult.StartsWith("Changed") ? "Изменено" : x.MatchResult.StartsWith("New") ? "Новое" : "Без изменений",
                x.Diagnostics.Length == 0 ? "" : "Строка не прошла проверку: проверьте код, единицу, числа и дубли в исходном прайсе.");
        }).ToArray(), total, page);
    }

    /// <summary>Повторно проверяет партию от Admin или Manager с атомарным аудитом.</summary>
    public async Task<BatchReport> RepreviewAsync(Guid id, Guid operationId, CancellationToken ct = default)
    {
        await using (var db = await factory.CreateDbContextAsync(ct)) await access.RequireAsync(db, false, ct);
        await Service(false, "import.repreview", operationId).RepreviewAsync(id, ct); return await ReportAsync(id, ct);
    }

    /// <summary>Применяет существующий сценарий только после проверки Admin, включая косвенную автоматическую публикацию цен.</summary>
    public async Task<BatchReport> ApplyAsync(Guid id, Guid operationId, CancellationToken ct = default)
    {
        await using (var db = await factory.CreateDbContextAsync(ct)) await access.RequireAsync(db, true, ct);
        await Service(true, "import.apply", operationId).ApplyAsync(id, ct); return await ReportAsync(id, ct);
    }
}
