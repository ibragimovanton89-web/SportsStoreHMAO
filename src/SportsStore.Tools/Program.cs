using SportsStore.Application.Admin;
using SportsStore.Infrastructure.Admin;
// Точка входа: локальные команды оператора для импорта и цен; не предоставляется как удалённый API.
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SportsStore.Application.Import;
using SportsStore.Application.Pricing;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Import;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Pricing;
using SportsStore.Infrastructure.Services;

Console.OutputEncoding = Encoding.UTF8;
var jsonOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
// Выводит результат команды в читаемом JSON без секретов.
void Print(object value) => Console.WriteLine(JsonSerializer.Serialize(value, jsonOptions));
// Читает обязательный аргумент по индексу, иначе сообщает об ошибке вызова.
string Arg(int n) => args.Length > n ? args[n] : throw new ArgumentException("Missing argument; run help.");
// Разбирает GUID аргумента.
Guid Id(int n) => Guid.Parse(Arg(n));
// Разбирает decimal с независимой от локали десятичной точкой.
decimal Num(int n) => decimal.Parse(Arg(n), CultureInfo.InvariantCulture);
// Разбирает коммерческий сегмент без учёта регистра.
CustomerSegment Segment(int n) => Enum.Parse<CustomerSegment>(Arg(n), true);
try
{
    var command = args.FirstOrDefault() ?? "help";
    if (command == "help")
    {
        Console.WriteLine("""
SportsStore.Tools — LOCAL operator utility (no remote endpoint)
bootstrap-admin                    Создать первого Admin со скрытым вводом пароля
parse <file.xls>                    Inspect without database
preview <file.xls>                  Save BallMarket staging only
report <batchId>                    Show preview/apply report
repreview <batchId>                 Revalidate existing staging after a stale preview
apply <batchId>                     Transactionally apply a valid preview
offers [externalCode]               List offer IDs (first 30, or exact code)
pricing-source <variantId> <offerId> Explicit purchase source for this variant
rule <Retail|Wholesale> <markupPercent> <minQty> <default|category|variant> [scopeId]
manual <variantId> <segment> <minQty> <amount>
propose <offerId>                   Create proposals; does not publish by default
proposals                          List pending proposals
price-apply <proposalId>            Publish a still-current proposal
quote <authenticatedUserId|-> <variantId> <quantity>
demo                               Seed clearly labelled, draft demo data only
inspect                            Counts and current database role
""");
        return 0;
    }
    var parser = new BallMarketParser();
    if (command == "parse")
    {
        var parsed = parser.Parse(Arg(1));
        Print(new { parsed.FileName, parsed.Sha256, parsed.ParserVersion, parsed.SourceDate, parsed.Currency, parsed.Tiers,
            Rows = parsed.Rows.Count, Products = parsed.Rows.Count(x => x.Kind == ImportRowKind.Product),
            PurchasePrices = parsed.Rows.Where(x => x.Kind == ImportRowKind.Product).Sum(x => x.Offer!.Prices.Count(p => p is not null)),
            UnknownStock = parsed.Rows.Count(x => x.Kind == ImportRowKind.Product && x.Offer!.Stock is null),
            Errors = parsed.Rows.Where(x => x.Kind == ImportRowKind.Error).Select(x => new { x.RowNumber, x.Diagnostics }) });
        return parsed.Rows.Any(x => x.Kind == ImportRowKind.Error) ? 2 : 0;
    }
    var config = new ConfigurationBuilder().AddUserSecrets("SportsStoreHMAO-development-2026").AddEnvironmentVariables().Build();
    if (command == "bootstrap-admin") return await SportsStore.Tools.BootstrapAdmin.RunAsync(config);
    var cs = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection or user-secrets.");
    IDbContextFactory<ApplicationDbContext> factory = new ToolFactory(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(cs).Options);
    var localIdentity = new SportsStore.Tools.LocalAdminIdentity();
    var access = new AdminAccess(localIdentity);
    var commerce = new AdminCommerce(factory, access);
    var catalog = new AdminCatalog(factory, access);
    var import = new PriceImportService(factory, parser, async (context, resultId, ct) =>
    {
        AdminAccess.Audit(context, await localIdentity.GetAsync(ct), Guid.NewGuid(), "import." + command,
            "ImportBatch", resultId, "Импорт выполнен доверенным локальным оператором.");
    });
    var pricing = new PricingService(factory);
    await using var db = await factory.CreateDbContextAsync();
    switch (command)
    {
        case "preview": Print(await import.PreviewAsync("ballmarket", Arg(1))); break;
        case "report": Print(await import.ReportAsync(Id(1))); break;
        case "repreview": Print(await import.RepreviewAsync(Id(1))); break;
        case "apply": Print(await import.ApplyAsync(Id(1))); break;
        case "offers":
            Print(await db.SupplierOffers.Where(x => args.Length < 2 || x.ExternalCode == args[1]).OrderBy(x => x.ExternalCode).Take(30)
                .Select(x => new { x.Id, x.ExternalCode, x.SourceName, x.SupplierUnit, x.Stock, x.ProductVariantId, x.ConversionConfirmed }).ToListAsync()); break;
        case "pricing-source":
        {
            var variantId = Id(1); var offerId = Id(2);
            var variant = await db.ProductVariants.SingleAsync(x => x.Id == variantId);
            var offer = await db.SupplierOffers.SingleAsync(x => x.Id == offerId);
            if (offer.ProductVariantId != variant.Id) throw new InvalidOperationException("Map this offer to the variant first.");
            await commerce.SetSourceAsync(variant.Id, variant.Version, offer.Id, Guid.NewGuid()); Print(new { variant.Id, PricingSupplierOfferId = offer.Id }); break;
        }
        case "rule":
        {
            var segment = Segment(1); var markup = Num(2); var min = Num(3); var scope = Arg(4);
            if (markup < 0 || min < 1 || (segment == CustomerSegment.Retail && min != 1)) throw new ArgumentException("Invalid rule.");
            Guid? category = scope == "category" ? Id(5) : null;
            Guid? variant = scope == "variant" ? Id(5) : null;
            if (scope is not ("default" or "category" or "variant")) throw new ArgumentException("Invalid scope.");
            var rule = await db.MarkupRules.SingleOrDefaultAsync(x => x.Segment == segment && x.CategoryId == category && x.ProductVariantId == variant && x.MinimumQuantity == min);
            var savedId = await commerce.SaveRuleAsync(new(rule?.Id ?? Guid.Empty, rule?.Version ?? 0, segment, category, variant, min, markup), Guid.NewGuid());
            Print(new { Id = savedId }); break;
        }
        case "manual": await commerce.ManualAsync(Id(1), Segment(2), Num(3), Num(4), Guid.NewGuid()); Print(new { Saved = true }); break;
        case "propose":
        {
            var result = await commerce.ProposeAsync(Id(1), Guid.NewGuid()); Print(result); break;
        }
        case "proposals": Print(await db.PriceProposals.Where(x => x.Status == ProposalStatus.Pending).Select(x => new { x.Id, x.ProductVariantId, x.Segment, x.MinimumQuantity, x.Amount, x.Source }).ToListAsync()); break;
        case "price-apply": await commerce.ApplyPriceAsync(Id(1), Guid.NewGuid()); Print(new { Applied = true }); break;
        case "quote": Print(await pricing.QuoteAsync(Arg(1) == "-" ? null : Arg(1), Id(2), Num(3))); break;
        case "demo": Print(await DemoData.SeedAsync(db)); break;
        case "inspect":
            Print(new { Offers = await db.SupplierOffers.CountAsync(), PurchasePrices = await db.SupplierOfferPrices.CountAsync(),
                PurchaseHistory = await db.SupplierPriceHistories.CountAsync(), Products = await db.Products.CountAsync(),
                PublishedProducts = await db.Products.CountAsync(x => x.Status == ProductStatus.Published),
                Batches = await db.ImportBatches.CountAsync(),
                IsSuperuser = await db.Database.SqlQueryRaw<bool>("SELECT rolsuper AS \"Value\" FROM pg_roles WHERE rolname = current_user").SingleAsync() }); break;
        default: throw new ArgumentException("Unknown command; run help.");
    }
    return 0;
}
catch (Exception ex)
{
    // Не выводим строки подключения, содержимое строк и подробности PostgreSQL, которые могут раскрыть секреты.
    Console.Error.WriteLine(ex is Npgsql.PostgresException pg ? $"Database rejected the operation (SQLSTATE {pg.SqlState})."
        : ex is DbUpdateException ? "Database update failed; transaction rolled back. Check constraints and concurrency."
        : ex is Npgsql.NpgsqlException ? "Database connection failed. Check local configuration and container readiness."
        : ex.Message);
    return 1;
}
/// <summary>Фабрика отдельных контекстов PostgreSQL для локальной консольной утилиты.</summary>
/// <param name="options">Параметры подключения и модели создаваемых контекстов.</param>
internal sealed class ToolFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
{
    /// <summary>Создаёт новый независимый контекст; вызывающий код обязан освободить его после операции.</summary>
    public ApplicationDbContext CreateDbContext() => new(options);
}

