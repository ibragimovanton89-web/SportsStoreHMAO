using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Pricing;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
namespace SportsStore.Infrastructure.Pricing;

/// <summary>Вычисляет предложения и публикует цены через отдельные контексты и транзакции; защищает ручные цены и право на опт.</summary>
/// <param name="factory">Фабрика нового контекста на каждую прикладную операцию.</param>
public sealed class PricingService(IDbContextFactory<ApplicationDbContext> factory) : IPricingService
{
    /// <summary>Фиксированный идентификатор единственной записи общих настроек цен.</summary>
    public static readonly Guid SettingsId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    /// <summary>Создаёт предложения пересчёта для сопоставленной позиции в сериализуемой транзакции; публикация зависит от явной настройки.</summary>
    /// <param name="offerId">Идентификатор закупочного предложения поставщика.</param>
    /// <param name="batchId">Идентификатор партии импорта; в расчёте цены может отсутствовать.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    public async Task<IReadOnlyList<Guid>> ProposeAsync(Guid offerId, Guid? batchId = null, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var ids = await ProposeInContextAsync(db, offerId, batchId, ct);
        await tx.CommitAsync(ct);
        return ids;
    }
    /// <summary>Создаёт предложения для допустимых ступеней в контексте и транзакции вызывающего кода; пропускает ручные цены и неподтверждённые исходные данные.</summary>
    /// <param name="db">Контекст текущей операции; его жизненным циклом управляет вызывающий код.</param>
    /// <param name="offerId">Идентификатор закупочного предложения поставщика.</param>
    /// <param name="batchId">Идентификатор партии импорта; в расчёте цены может отсутствовать.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    internal static async Task<IReadOnlyList<Guid>> ProposeInContextAsync(ApplicationDbContext db, Guid offerId, Guid? batchId, CancellationToken ct)
    {
        var offer = await db.SupplierOffers.SingleAsync(x => x.Id == offerId, ct);
        if (offer.ProductVariantId is null) return [];
        var rules = await db.MarkupRules.ToListAsync(ct);
        var ids = new List<Guid>();
        foreach (var segment in Enum.GetValues<CustomerSegment>())
        foreach (var min in rules.Where(x => x.Segment == segment).Select(x => x.MinimumQuantity).Distinct().Order())
        {
            Calculation calc;
            try { calc = await CalculateAsync(db, offerId, segment, min, ct); }
            catch (InvalidOperationException) { continue; } // Без правила или подтверждённых исходных данных цена не публикуется.
            if (calc.Rule.MinimumQuantity != min) continue;
            var sale = await db.SalePrices.SingleOrDefaultAsync(x => x.ProductVariantId == offer.ProductVariantId && x.Segment == segment && x.MinimumQuantity == min, ct);
            if (sale?.IsManual == true) continue;
            var existing = await db.PriceProposals.SingleOrDefaultAsync(x => x.SupplierOfferId == offerId && x.Segment == segment && x.MinimumQuantity == min && x.Fingerprint == calc.Fingerprint, ct);
            if (existing is not null) { ids.Add(existing.Id); continue; }
            var proposal = new PriceProposal { SupplierOfferId = offerId, ProductVariantId = offer.ProductVariantId.Value,
                ImportBatchId = batchId, Segment = segment, MinimumQuantity = min, Amount = calc.Amount, Currency = "RUB",
                Fingerprint = calc.Fingerprint, Source = calc.Source };
            db.PriceProposals.Add(proposal);
            await db.SaveChangesAsync(ct);
            ids.Add(proposal.Id);
            // Общий автоматический пересчёт удалён: даже исторический флаг в БД больше не публикует цену.
        }
        return ids;
    }
    /// <summary>Применяет актуальное предложение пересчёта в отдельной транзакции; повторное применение безопасно, ручная цена защищена.</summary>
    /// <param name="proposalId">Идентификатор проверяемого предложения пересчёта.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    public async Task ApplyAsync(Guid proposalId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var proposal = await db.PriceProposals.SingleAsync(x => x.Id == proposalId, ct);
        await ApplyInContextAsync(db, proposal, ct);
        await tx.CommitAsync(ct);
    }
    /// <summary>Проверяет актуальность отпечатка и защиту ручной цены, публикует предложение и отмечает его применённым в текущей транзакции.</summary>
    /// <param name="db">Контекст текущей операции; его жизненным циклом управляет вызывающий код.</param>
    /// <param name="proposal">Предложение пересчёта, которое требуется опубликовать.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    internal static async Task ApplyInContextAsync(ApplicationDbContext db, PriceProposal proposal, CancellationToken ct)
    {
        if (proposal.Status == ProposalStatus.Applied) return;
        var calc = await CalculateAsync(db, proposal.SupplierOfferId, proposal.Segment, proposal.MinimumQuantity, ct);
        if (calc.Fingerprint != proposal.Fingerprint || proposal.Status != ProposalStatus.Pending)
            throw new InvalidOperationException("Stale proposal; calculate a new one.");
        var sale = await db.SalePrices.SingleOrDefaultAsync(x => x.ProductVariantId == proposal.ProductVariantId && x.Segment == proposal.Segment && x.MinimumQuantity == proposal.MinimumQuantity, ct);
        if (sale?.IsManual == true) throw new InvalidOperationException("Manual price is protected.");
        await PublishAsync(db, sale, proposal.ProductVariantId, proposal.Segment, proposal.MinimumQuantity, proposal.Amount, false, proposal.Source, ct);
        proposal.Status = ProposalStatus.Applied;
        await db.SaveChangesAsync(ct);
    }
    /// <summary>Устанавливает фиксированную цену с историей; проверяет сумму и ступень и защищает результат от автоматического пересчёта.</summary>
    /// <param name="variantId">Идентификатор варианта собственного каталога.</param>
    /// <param name="segment">Коммерческий сегмент цены: розница или опт.</param>
    /// <param name="minimumQuantity">Нижняя включительная граница количественной ступени одного SKU.</param>
    /// <param name="amount">Цена за единицу продажи магазина в рублях.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    public async Task SetManualAsync(Guid variantId, CustomerSegment segment, decimal minimumQuantity, decimal amount, CancellationToken ct = default)
    {
        if (amount <= 0 || decimal.Round(amount,2) != amount || minimumQuantity < 1 || (segment == CustomerSegment.Retail && minimumQuantity != 1))
            throw new ArgumentException("Invalid manual price or quantity.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var sale = await db.SalePrices.SingleOrDefaultAsync(x => x.ProductVariantId == variantId && x.Segment == segment && x.MinimumQuantity == minimumQuantity, ct);
        await PublishAsync(db, sale, variantId, segment, minimumQuantity, amount, true, "Manual command", ct);
        await tx.CommitAsync(ct);
    }
    /// <summary>Сохраняет цену и запись истории при фактическом изменении; одинаковые значения не создают ложную историю.</summary>
    /// <param name="db">Контекст текущей операции; его жизненным циклом управляет вызывающий код.</param>
    /// <param name="sale">Текущая опубликованная цена или null при первой публикации.</param>
    /// <param name="variantId">Идентификатор варианта собственного каталога.</param>
    /// <param name="segment">Коммерческий сегмент цены: розница или опт.</param>
    /// <param name="minimumQuantity">Нижняя включительная граница количественной ступени одного SKU.</param>
    /// <param name="amount">Цена за единицу продажи магазина в рублях.</param>
    /// <param name="manual">Установить ли защиту ручной цены от автоматического пересчёта.</param>
    /// <param name="source">Описание источника цены для истории и аудита.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    internal static async Task PublishAsync(ApplicationDbContext db, SalePrice? sale, Guid variantId, CustomerSegment segment,
        decimal minimumQuantity, decimal amount, bool manual, string source, CancellationToken ct)
    {
        if (sale is not null && sale.Amount == amount && sale.IsManual == manual && sale.Source == source) return;
        db.SalePriceHistories.Add(new() { ProductVariantId = variantId, Segment = segment, MinimumQuantity = minimumQuantity,
            OldAmount = sale?.Amount, NewAmount = amount, Source = source });
        if (sale is null)
        {
            sale = new() { ProductVariantId = variantId, Segment = segment, MinimumQuantity = minimumQuantity };
            db.SalePrices.Add(sale);
        }
        sale.Amount = amount; sale.IsManual = manual; sale.Source = source; sale.PublishedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
    /// <summary>Определяет сегмент по доверенному идентификатору вошедшего пользователя и выбирает ступень. При отсутствии цены или настроенного оптового минимума выдаёт ошибку, не подменяя опт розницей.</summary>
    /// <param name="authenticatedUserId">Идентификатор из серверного контекста аутентификации; нельзя доверять произвольному значению клиента. null означает гостя.</param>
    /// <param name="variantId">Идентификатор варианта собственного каталога.</param>
    /// <param name="quantity">Количество одного SKU в единицах продажи магазина.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    public async Task<PriceQuote> QuoteAsync(string? authenticatedUserId, Guid variantId, decimal quantity, CancellationToken ct = default)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        await using var db = await factory.CreateDbContextAsync(ct);
        var customer = authenticatedUserId is null ? null : await db.Customers.SingleOrDefaultAsync(x => x.ApplicationUserId == authenticatedUserId, ct);
        var segment = PriceCalculator.AvailableSegment(customer);
        var price = await db.SalePrices.Where(x => x.ProductVariantId == variantId && x.Segment == segment && x.MinimumQuantity <= quantity)
            .OrderByDescending(x => x.MinimumQuantity).FirstOrDefaultAsync(ct);
        // Для подтверждённого оптового покупателя отсутствие оптовой цены является ошибкой, а не переходом на розницу.
        if (price is null) throw new InvalidOperationException("No published price for the eligible segment and quantity.");
        // Общий минимум оптового заказа удалён. Цена определяется карточкой, количеством SKU и подтверждённым статусом клиента.
        return new(price.Amount, price.Currency, segment, price.MinimumQuantity, null);
    }
    /// <summary>Внутренний результат расчёта с правилом наценки, источником и отпечатком для проверки актуальности.</summary>
    /// <param name="Amount">Цена за одну единицу продажи магазина.</param>
    /// <param name="Rule">Выбранное правило наценки.</param>
    /// <param name="Fingerprint">Отпечаток исходных параметров для проверки актуальности пересчёта.</param>
    /// <param name="Source">Сериализованное объяснение источника расчёта для аудита.</param>
    private sealed record Calculation(decimal Amount, MarkupRule Rule, string Fingerprint, string Source);
    /// <summary>Проверяет сопоставление, выбранный источник, подтверждение тарифа и единиц; выбирает наценку и формирует отпечаток исходных данных.</summary>
    /// <param name="db">Контекст текущей операции; его жизненным циклом управляет вызывающий код.</param>
    /// <param name="offerId">Идентификатор закупочного предложения поставщика.</param>
    /// <param name="segment">Коммерческий сегмент цены: розница или опт.</param>
    /// <param name="quantity">Количество одного SKU в единицах продажи магазина.</param>
    /// <param name="ct">Токен отмены операции и обращений к базе данных.</param>
    private static async Task<Calculation> CalculateAsync(ApplicationDbContext db, Guid offerId, CustomerSegment segment, decimal quantity, CancellationToken ct)
    {
        var offer = await db.SupplierOffers.SingleAsync(x => x.Id == offerId, ct);
        if (offer.ProductVariantId is null) throw new InvalidOperationException("Offer is not mapped.");
        var supplier = await db.Suppliers.SingleAsync(x => x.Id == offer.SupplierId, ct);
        if (!supplier.PriceTierConfirmed || supplier.SelectedPriceTierId is null) throw new InvalidOperationException("Purchase tariff is unconfirmed.");
        var tier = await db.SupplierPriceTiers.SingleAsync(x => x.Id == supplier.SelectedPriceTierId, ct);
        if (tier.SupplierId != supplier.Id || tier.Currency != "RUB") throw new InvalidOperationException("Invalid purchase tariff.");
        var cost = await db.SupplierOfferPrices.SingleOrDefaultAsync(x => x.SupplierOfferId == offerId && x.SupplierPriceTierId == tier.Id, ct)
            ?? throw new InvalidOperationException("Purchase price missing.");
        var variant = await db.ProductVariants.SingleAsync(x => x.Id == offer.ProductVariantId, ct);
        if (variant.PricingSupplierOfferId != offer.Id) throw new InvalidOperationException("Select this offer as the variant pricing source explicitly.");
        var product = await db.Products.SingleAsync(x => x.Id == variant.ProductId, ct);
        var path = new List<Guid>();
        var categories = await db.Categories.ToDictionaryAsync(x => x.Id, ct);
        var cursor = product.CategoryId;
        while (cursor is not null)
        {
            if (path.Contains(cursor.Value)) throw new InvalidOperationException("Category cycle.");
            path.Add(cursor.Value); cursor = categories[cursor.Value].ParentId;
        }
        var rule = PriceCalculator.SelectRule(await db.MarkupRules.ToListAsync(ct), variant.Id, path, segment, quantity)
            ?? throw new InvalidOperationException("Markup rule missing.");
        var amount = PriceCalculator.Calculate(cost.Amount, offer.SaleUnitsPerSupplierUnit, offer.ConversionConfirmed, rule.MarkupPercent);
        var source = JsonSerializer.Serialize(new { Offer = offer.Id, Tier = tier.Id, Purchase = cost.Amount,
            Conversion = offer.SaleUnitsPerSupplierUnit, Rule = rule.Id, Markup = rule.MarkupPercent, Rounding = "2 decimals AwayFromZero" });
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            source + JsonSerializer.Serialize(new { offer.Version, SupplierVersion = supplier.Version,
                VariantVersion = variant.Version, ProductVersion = product.Version, RuleVersion = rule.Version, Path = path, segment, rule.MinimumQuantity }))));
        return new(amount, rule, fingerprint, source);
    }
}

