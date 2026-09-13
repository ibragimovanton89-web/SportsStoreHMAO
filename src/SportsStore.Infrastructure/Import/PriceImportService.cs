using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Import;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Pricing;
using SportsStore.Infrastructure.Admin;
namespace SportsStore.Infrastructure.Import;

/// <summary>Сохраняет staging и транзакционно обновляет предложения; блокировка по поставщику предотвращает параллельную перезапись.</summary>
/// <param name="factory">Фабрика нового контекста на каждую прикладную операцию.</param>
/// <param name="parser">Парсер формата поставщика без побочных изменений БД.</param>
/// <param name="administrativeCommit">Внутренний обработчик аудита перед фиксацией; Web не может передать его через форму.</param>
public sealed class PriceImportService(IDbContextFactory<ApplicationDbContext> factory, ISupplierPriceParser parser, AdministrativeCommit? administrativeCommit = null) : IPriceImportService
{
    /// <summary>Повторно сравнивает сохранённые строки с текущими предложениями и обновляет ожидаемую ревизию; применённую партию не меняет.</summary>
    /// <param name="batchId">Идентификатор партии импорта; в расчёте цены может отсутствовать.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    public async Task<BatchReport> RepreviewAsync(Guid batchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var code = await (from b in db.ImportBatches join s in db.Suppliers on b.SupplierId equals s.Id where b.Id == batchId select s.Code).SingleAsync(ct);
        await LockAsync(db, code, ct);
        var batch = await db.ImportBatches.SingleAsync(x => x.Id == batchId, ct);
        if (batch.Status == ImportStatus.Applied) { await tx.CommitAsync(ct); return await ReportAsync(batchId, ct); }
        var supplier = await db.Suppliers.SingleAsync(x => x.Id == batch.SupplierId, ct);
        var rows = await db.ImportRows.Where(x => x.ImportBatchId == batchId).ToListAsync(ct);
        var offers = await db.SupplierOffers.Where(x => x.SupplierId == supplier.Id).ToDictionaryAsync(x => x.ExternalCode, ct);
        var tiers = await db.SupplierPriceTiers.Where(x => x.SupplierId == supplier.Id).ToDictionaryAsync(x => x.Code, ct);
        var prices = await db.SupplierOfferPrices.Where(x => db.SupplierOffers.Any(o => o.Id == x.SupplierOfferId && o.SupplierId == supplier.Id)).ToListAsync(ct);
        var tierData = JsonSerializer.Deserialize<ParsedTier[]>(batch.TiersJson)!;
        batch.NewCount = 0; batch.ChangedCount = 0; batch.UnchangedCount = 0;
        batch.ErrorCount = rows.Count(x => x.Kind == ImportRowKind.Error);
        foreach (var row in rows.Where(x => x.Kind == ImportRowKind.Product))
        {
            var parsed = JsonSerializer.Deserialize<ParsedOffer>(row.ParsedJson!)!;
            if (!offers.TryGetValue(parsed.ExternalCode, out var offer)) { batch.NewCount++; row.MatchResult = "New; unmapped"; continue; }
            bool changed = MetadataChanged(offer, parsed);
            row.BeforePricesJson = JsonSerializer.Serialize(tierData.Select(t => tiers.TryGetValue(t.Code, out var oldTier)
                ? prices.SingleOrDefault(p => p.SupplierOfferId == offer.Id && p.SupplierPriceTierId == oldTier.Id)?.Amount : null));
            for (int i = 0; i < tierData.Length; i++)
                if (parsed.Prices[i] is decimal amount && (!tiers.TryGetValue(tierData[i].Code, out var tier) ||
                    prices.SingleOrDefault(x => x.SupplierOfferId == offer.Id && x.SupplierPriceTierId == tier.Id)?.Amount != amount)) changed = true;
            if (changed) batch.ChangedCount++; else batch.UnchangedCount++;
            row.MatchResult = (changed ? "Changed" : "Unchanged") + (offer.ProductVariantId is null ? "; unmapped" : "; mapped");
        }
        if (supplier.LatestSourceDate > batch.SourceDate) batch.ErrorCount++;
        batch.ExpectedSupplierRevision = supplier.Revision;
        batch.Status = batch.ErrorCount == 0 ? ImportStatus.Preview : ImportStatus.Invalid;
        await db.SaveChangesAsync(ct);
        if (administrativeCommit is not null) { await administrativeCommit(db, batch.Id.ToString(), ct); await db.SaveChangesAsync(ct); }
        await tx.CommitAsync(ct);
        return await ReportAsync(batchId, ct);
    }
    /// <summary>Сохраняет проверенную партию и staging без изменения действующих предложений. Идентичный файл той же версии парсера возвращает существующую партию.</summary>
    /// <param name="supplierCode">Внутренний код поставщика, для текущего входа — ballmarket.</param>
    /// <param name="path">Путь к локальному XLS-файлу; содержимое рассматривается только как данные.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    public async Task<BatchReport> PreviewAsync(string supplierCode, string path, CancellationToken ct = default)
    {
        var parsed = parser.Parse(path);
        if (supplierCode != "ballmarket") throw new ArgumentException("This entry point is configured for ballmarket only.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(db, supplierCode, ct);
        var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Code == supplierCode, ct);
        if (supplier is null)
        {
            supplier = new Supplier { Code = supplierCode, Name = "BallMarket" };
            db.Suppliers.Add(supplier); await db.SaveChangesAsync(ct);
        }
        var prior = await db.ImportBatches.SingleOrDefaultAsync(x => x.SupplierId == supplier.Id && x.Sha256 == parsed.Sha256 && x.ParserVersion == parsed.ParserVersion, ct);
        if (prior is not null) { await tx.CommitAsync(ct); return await ReportAsync(prior.Id, ct); }
        var batch = new ImportBatch { SupplierId = supplier.Id, FileName = parsed.FileName, Sha256 = parsed.Sha256,
            ParserVersion = parsed.ParserVersion, SourceDate = parsed.SourceDate, Currency = parsed.Currency,
            Conditions = parsed.Conditions, TiersJson = JsonSerializer.Serialize(parsed.Tiers), ExpectedSupplierRevision = supplier.Revision };
        var offers = await db.SupplierOffers.Where(x => x.SupplierId == supplier.Id).ToDictionaryAsync(x => x.ExternalCode, ct);
        var tiers = await db.SupplierPriceTiers.Where(x => x.SupplierId == supplier.Id).ToDictionaryAsync(x => x.Code, ct);
        var prices = await db.SupplierOfferPrices.Where(x => db.SupplierOffers.Any(o => o.Id == x.SupplierOfferId && o.SupplierId == supplier.Id)).ToListAsync(ct);
        foreach (var row in parsed.Rows)
        {
            var staging = new ImportRow { ImportBatchId = batch.Id, RowNumber = row.RowNumber, Kind = row.Kind, RawJson = row.RawJson,
                ParsedJson = row.Offer is null ? null : JsonSerializer.Serialize(row.Offer), Diagnostics = row.Diagnostics };
            if (row.Kind == ImportRowKind.Error) batch.ErrorCount++;
            if (row.Kind == ImportRowKind.Product && row.Offer is not null)
            {
                if (!offers.TryGetValue(row.Offer.ExternalCode, out var offer)) { batch.NewCount++; staging.MatchResult = "New; unmapped"; }
                else
                {
                    bool changed = MetadataChanged(offer, row.Offer);
                    staging.BeforePricesJson = JsonSerializer.Serialize(parsed.Tiers.Select(t => tiers.TryGetValue(t.Code, out var oldTier)
                        ? prices.SingleOrDefault(p => p.SupplierOfferId == offer.Id && p.SupplierPriceTierId == oldTier.Id)?.Amount : null));
                    for (int i = 0; i < parsed.Tiers.Count; i++)
                        if (row.Offer.Prices[i] is decimal amount &&
                            (!tiers.TryGetValue(parsed.Tiers[i].Code, out var tier) || prices.SingleOrDefault(x => x.SupplierOfferId == offer.Id && x.SupplierPriceTierId == tier.Id)?.Amount != amount)) changed = true;
                    if (changed) batch.ChangedCount++; else batch.UnchangedCount++;
                    staging.MatchResult = (changed ? "Changed" : "Unchanged") + (offer.ProductVariantId is null ? "; unmapped" : "; mapped");
                }
            }
            db.ImportRows.Add(staging);
        }
        if (supplier.LatestSourceDate > batch.SourceDate)
        { batch.ErrorCount++; batch.Conditions += "\nIMPORT ERROR: Source date is older than the applied supplier document."; }
        batch.Status = batch.ErrorCount > 0 ? ImportStatus.Invalid : ImportStatus.Preview;
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(ct);
        if (administrativeCommit is not null) { await administrativeCommit(db, batch.Id.ToString(), ct); await db.SaveChangesAsync(ct); }
        await tx.CommitAsync(ct);
        return await ReportAsync(batch.Id, ct);
    }
    /// <summary>Читает отчёт партии без отслеживания изменений: счётчики позиций, неизвестных остатков и диагностические сообщения.</summary>
    /// <param name="batchId">Идентификатор партии импорта; в расчёте цены может отсутствовать.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    public async Task<BatchReport> ReportAsync(Guid batchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var b = await db.ImportBatches.AsNoTracking().SingleAsync(x => x.Id == batchId, ct);
        var rows = await db.ImportRows.AsNoTracking().Where(x => x.ImportBatchId == batchId).OrderBy(x => x.RowNumber).ToListAsync(ct);
        var products = rows.Where(x => x.Kind == ImportRowKind.Product).Select(x => JsonSerializer.Deserialize<ParsedOffer>(x.ParsedJson!)!).ToArray();
        return new(b.Id, b.Status, b.SourceDate, b.NewCount, b.ChangedCount, b.UnchangedCount, b.ErrorCount,
            products.Length, products.Count(x => x.Stock is null),
            rows.Where(x => x.Diagnostics.Length > 0).Select(x => $"Row {x.RowNumber}: {x.Diagnostics}")
                .Concat(b.Status == ImportStatus.Invalid && b.ErrorCount > rows.Count(x => x.Kind == ImportRowKind.Error) ? ["Batch: source date is older than current."] : []).ToArray());
    }
    /// <summary>Транзакционно применяет проверенную партию: проверяет ревизию и дату, обновляет предложения и историю; повторное применение безопасно.</summary>
    /// <param name="batchId">Идентификатор партии импорта; в расчёте цены может отсутствовать.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    public async Task<BatchReport> ApplyAsync(Guid batchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var code = await (from b in db.ImportBatches join s in db.Suppliers on b.SupplierId equals s.Id where b.Id == batchId select s.Code).SingleAsync(ct);
        await LockAsync(db, code, ct);
        var batch = await db.ImportBatches.SingleAsync(x => x.Id == batchId, ct);
        if (batch.Status == ImportStatus.Applied) { await tx.CommitAsync(ct); return await ReportAsync(batchId, ct); }
        if (batch.Status != ImportStatus.Preview || batch.ErrorCount != 0) throw new InvalidOperationException("Invalid batch cannot be applied.");
        var supplier = await db.Suppliers.SingleAsync(x => x.Id == batch.SupplierId, ct);
        if (supplier.Revision != batch.ExpectedSupplierRevision) throw new InvalidOperationException("Stale preview. Use repreview to revalidate this batch.");
        if (supplier.LatestSourceDate > batch.SourceDate) throw new InvalidOperationException("Older source date is blocked.");
        var tierData = JsonSerializer.Deserialize<ParsedTier[]>(batch.TiersJson)!;
        var tiers = await db.SupplierPriceTiers.Where(x => x.SupplierId == supplier.Id).ToDictionaryAsync(x => x.Code, ct);
        foreach (var tier in tierData)
        {
            if (!tiers.TryGetValue(tier.Code, out var entity))
            { entity = new SupplierPriceTier { SupplierId = supplier.Id, Code = tier.Code }; tiers[tier.Code] = entity; db.SupplierPriceTiers.Add(entity); }
            if (supplier.SelectedPriceTierId == entity.Id && (entity.MinimumAmount != tier.MinimumAmount || entity.Currency != batch.Currency))
                supplier.PriceTierConfirmed = false;
            entity.Name = tier.Name; entity.MinimumAmount = tier.MinimumAmount; entity.Currency = batch.Currency; entity.SourceConditions = batch.Conditions;
        }
        var offers = await db.SupplierOffers.Where(x => x.SupplierId == supplier.Id).ToDictionaryAsync(x => x.ExternalCode, ct);
        var currentPrices = await db.SupplierOfferPrices.Where(x => db.SupplierOffers.Any(o => o.Id == x.SupplierOfferId && o.SupplierId == supplier.Id)).ToListAsync(ct);
        var priceLookup = currentPrices.ToDictionary(x => (x.SupplierOfferId, x.SupplierPriceTierId));
        var rows = await db.ImportRows.Where(x => x.ImportBatchId == batchId && x.Kind == ImportRowKind.Product).OrderBy(x => x.RowNumber).ToListAsync(ct);
        var changedMappedOffers = new HashSet<Guid>();
        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            var parsed = JsonSerializer.Deserialize<ParsedOffer>(row.ParsedJson!)!;
            if (!offers.TryGetValue(parsed.ExternalCode, out var offer))
            {
                offer = new SupplierOffer { SupplierId = supplier.Id, ExternalCode = parsed.ExternalCode };
                offers[parsed.ExternalCode] = offer; db.SupplierOffers.Add(offer);
            }
            if (offer.SupplierUnit.Length > 0 && offer.SupplierUnit != parsed.Unit)
                offer.ConversionConfirmed = false;
            offer.SourceName = parsed.Name; offer.SourceSection = parsed.Section; offer.SupplierUnit = parsed.Unit;
            offer.UnitsPerBox = parsed.UnitsPerBox; offer.Stock = parsed.Stock; offer.SourceDate = batch.SourceDate;
            offer.LastSeenAt = now; offer.ReviewSuggestionsJson = parsed.SuggestionsJson;
            for (int i = 0; i < tierData.Length; i++)
            {
                if (parsed.Prices[i] is not decimal amount) continue; // Отсутствующая цена не стирает действующую.
                var tier = tiers[tierData[i].Code];
                priceLookup.TryGetValue((offer.Id, tier.Id), out var price);
                if (price?.Amount == amount) continue;
                db.SupplierPriceHistories.Add(new() { SupplierOfferId = offer.Id, SupplierPriceTierId = tier.Id, OldAmount = price?.Amount,
                    NewAmount = amount, ImportBatchId = batch.Id, ChangedAt = now });
                if (price is null)
                {
                    price = new SupplierOfferPrice { SupplierOfferId = offer.Id, SupplierPriceTierId = tier.Id };
                    priceLookup[(offer.Id, tier.Id)] = price; db.SupplierOfferPrices.Add(price);
                }
                price.Amount = amount; price.ImportBatchId = batch.Id;
                if (offer.ProductVariantId is not null) changedMappedOffers.Add(offer.Id);
            }
        }
        supplier.Revision++; supplier.LatestSourceDate = batch.SourceDate;
        batch.Status = ImportStatus.Applied; batch.AppliedAt = now;
        await db.SaveChangesAsync(ct);
        foreach (var offerId in changedMappedOffers)
            await PricingService.ProposeInContextAsync(db, offerId, batch.Id, ct);
        if (administrativeCommit is not null) { await administrativeCommit(db, batch.Id.ToString(), ct); await db.SaveChangesAsync(ct); }
        await tx.CommitAsync(ct);
        return await ReportAsync(batch.Id, ct);
    }
    /// <summary>Берёт транзакционную advisory-блокировку PostgreSQL по коду поставщика; освобождается при завершении транзакции.</summary>
    /// <param name="db">Контекст текущей операции; его жизненным циклом управляет вызывающий код.</param>
    /// <param name="code">Код поставщика для общей транзакционной блокировки.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    internal static Task LockAsync(ApplicationDbContext db, string code, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({code}, 0))", ct);
    /// <summary>Сравнивает только импортируемые данные предложения; собственный каталог и ручные настройки не участвуют.</summary>
    /// <param name="offer">Существующее закупочное предложение.</param>
    /// <param name="row">Нормализованная строка прайса для сравнения.</param>
    private static bool MetadataChanged(SupplierOffer offer, ParsedOffer row) =>
        offer.SourceName != row.Name || offer.SourceSection != row.Section || offer.SupplierUnit != row.Unit ||
        offer.UnitsPerBox != row.UnitsPerBox || offer.Stock != row.Stock || offer.ReviewSuggestionsJson != row.SuggestionsJson;
}
