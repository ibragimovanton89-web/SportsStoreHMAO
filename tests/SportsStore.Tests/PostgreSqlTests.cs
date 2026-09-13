using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SportsStore.Application.Import;
using SportsStore.Application.Orders;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Import;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Pricing;
using SportsStore.Infrastructure.Services;
using Xunit;
namespace SportsStore.Tests;

// Каждый тест создаёт собственную PostgreSQL; рабочая БД разработки не используется.
/// <summary>Интеграционные проверки на отдельной временной PostgreSQL с настоящими миграциями и ограничениями.</summary>
[Trait("Category","PostgreSQL")]
public sealed class PostgreSqlTests : IAsyncLifetime
{
    /// <summary>Административная строка подключения только для создания тестовой БД; не выводится в журналы.</summary>
    private string admin = "";
    /// <summary>Уникальное имя временной тестовой базы, исключающее конфликт с пользовательскими данными.</summary>
    private readonly string databaseName = "sportsstore_test_" + Guid.NewGuid().ToString("N");
    /// <summary>Фабрика контекстов изолированной тестовой базы.</summary>
    private IDbContextFactory<ApplicationDbContext> factory = null!;
    /// <summary>Признак успешного создания тестовой базы; предотвращает удаление не созданной этим тестом БД.</summary>
    private bool created;
    /// <summary>Создаёт уникальную временную PostgreSQL и применяет миграции перед интеграционным тестом.</summary>
    public async Task InitializeAsync()
    {
        admin = Environment.GetEnvironmentVariable("SPORTSSTORE_TEST_ADMIN_CONNECTION")
            ?? throw new InvalidOperationException("Set SPORTSSTORE_TEST_ADMIN_CONNECTION for isolated PostgreSQL integration tests.");
        var app = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Set application connection; it must use the non-superuser sportsstore.");
        await using var connection = new NpgsqlConnection(admin);
        await connection.OpenAsync();
        await using(var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\" OWNER sportsstore",connection)) await create.ExecuteNonQueryAsync();
        created=true;
        var cs = new NpgsqlConnectionStringBuilder(app) { Database=databaseName };
        factory = new Factory(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(cs.ConnectionString).Options);
        await using var db=factory.CreateDbContext();
        await db.Database.MigrateAsync();
        await db.Database.MigrateAsync(); // Повторное применение миграций не должно изменять схему.
        Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT rolsuper AS \"Value\" FROM pg_roles WHERE rolname=current_user").SingleAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
    /// <summary>Закрывает подключения и удаляет только временную базу, созданную этим экземпляром теста.</summary>
    public async Task DisposeAsync()
    {
        if(!created)return;
        if(!databaseName.StartsWith("sportsstore_test_",StringComparison.Ordinal) || databaseName.Length!=49)throw new InvalidOperationException("Unsafe test DB name.");
        await using var connection=new NpgsqlConnection(admin);
        await connection.OpenAsync();
        await using var cmd=new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)",connection);
        await cmd.ExecuteNonQueryAsync();
    }
    /// <summary>Создаёт сервис импорта с заданным синтетическим документом и тестовой фабрикой контекстов.</summary>
    /// <param name="document">Синтетический документ для тестовой реализации парсера.</param>
    private PriceImportService Import(ParsedPriceList document)=>new(factory,new Parser(document));
    /// <summary>Проверяет сохранение прежней цены, исчезнувшего предложения и собственного остатка при обновлении прайса.</summary>
    [Fact]
    public async Task MissingPriceAndDisappearedOfferPreservePriorDataAndOwnStock()
    {
        var first=Import(Fixtures.Document(extra:["","00099999","Second","шт","1","10","9","8","0"]));
        var initial=await first.PreviewAsync("ballmarket","initial");await first.ApplyAsync(initial.Id);
        var next=Import(Fixtures.Document(price:"",date:"29.07.26"));
        var preview=await next.PreviewAsync("ballmarket","next");await next.ApplyAsync(preview.Id);
        await using var db=factory.CreateDbContext();
        Assert.Equal(2,await db.SupplierOffers.CountAsync());
        Assert.Equal(6,await db.SupplierOfferPrices.CountAsync());
        Assert.Equal(6,await db.SupplierPriceHistories.CountAsync());
        var firstOffer=await db.SupplierOffers.SingleAsync(x=>x.ExternalCode=="00024387");
        Assert.Contains(await db.SupplierOfferPrices.Where(x=>x.SupplierOfferId==firstOffer.Id).ToListAsync(),x=>x.Amount==4633);
        Assert.Equal(new DateOnly(2026,7,28),(await db.SupplierOffers.SingleAsync(x=>x.ExternalCode=="00099999")).SourceDate);
    }
    /// <summary>Проверяет, что две конкурирующие партии не перезаписывают одну ревизию поставщика.</summary>
    [Fact]
    public async Task TwoDifferentConcurrentBatchesCannotBothOverwriteSameRevision()
    {
        var left=Import(Fixtures.Document());var a=await left.PreviewAsync("ballmarket","a");
        var right=Import(Fixtures.Document(price:"4700"));var b=await right.PreviewAsync("ballmarket","b");
        // Каждая конкурентная попытка отдельно сообщает успех или ожидаемый отказ устаревшей партии.
        static async Task<bool> TryApply(PriceImportService service,Guid id)
        {try{await service.ApplyAsync(id);return true;}catch(InvalidOperationException){return false;}}
        var results=await Task.WhenAll(TryApply(left,a.Id),TryApply(right,b.Id));
        Assert.Single(results,x=>x);
        await using var db=factory.CreateDbContext();Assert.Equal(3,await db.SupplierPriceHistories.CountAsync());
    }
    /// <summary>Проверяет предварительный импорт, применение и отсутствие дублей при повторе файла и команды.</summary>
    [Fact]
    public async Task PreviewApplyRepeatedFileAndRepeatedApplyDoNotDuplicate()
    {
        var service=Import(Fixtures.Document());
        var preview=await service.PreviewAsync("ballmarket","test.xls");
        await using(var db=factory.CreateDbContext()){Assert.Equal(0,await db.SupplierOffers.CountAsync());Assert.Equal(0,await db.Products.CountAsync());}
        var applied=await service.ApplyAsync(preview.Id);
        Assert.Equal(ImportStatus.Applied,applied.Status);
        Assert.Equal(preview.Id,(await service.PreviewAsync("ballmarket","renamed.xls")).Id);
        await service.ApplyAsync(preview.Id);
        await using(var db=factory.CreateDbContext())
        {
            var offer=await db.SupplierOffers.SingleAsync();
            Assert.Equal("00024387",offer.ExternalCode); Assert.Null(offer.Stock); Assert.Null(offer.ProductVariantId);
            Assert.Equal(3,await db.SupplierOfferPrices.CountAsync()); Assert.Equal(3,await db.SupplierPriceHistories.CountAsync());
            Assert.Equal(0,await db.InventoryBalances.CountAsync()); Assert.Equal(0,await db.Products.CountAsync());
        }
    }
    /// <summary>Проверяет единственную запись истории на изменение цены и запрет более старого прайса.</summary>
    [Fact]
    public async Task PriceChangeHasOneHistoryEntryAndOlderDocumentCannotApply()
    {
        var first=Import(Fixtures.Document()); var b=await first.PreviewAsync("ballmarket","1");await first.ApplyAsync(b.Id);
        var next=Import(Fixtures.Document(price:"4700",date:"29.07.26"));var n=await next.PreviewAsync("ballmarket","2");
        Assert.Equal(1,n.Changed);await next.ApplyAsync(n.Id);
        await using(var db=factory.CreateDbContext())
        {
            Assert.Equal(4,await db.SupplierPriceHistories.CountAsync());
            var change=await db.SupplierPriceHistories.SingleAsync(x=>x.ImportBatchId==n.Id);
            Assert.Equal(4633m,change.OldAmount);Assert.Equal(4700m,change.NewAmount);
        }
        var old=Import(Fixtures.Document(price:"4000",date:"27.07.26"));var o=await old.PreviewAsync("ballmarket","old");
        Assert.Equal(ImportStatus.Invalid,o.Status);await Assert.ThrowsAsync<InvalidOperationException>(()=>old.ApplyAsync(o.Id));
    }
    /// <summary>Проверяет блокировку партии с ошибкой и полный откат при нарушении ограничения PostgreSQL.</summary>
    [Fact]
    public async Task InvalidRowBlocksBatchAndConstraintFailureRollsBackEverything()
    {
        var invalid=Import(Fixtures.Document(extra:["","BAD","Broken","шт","1","-5","2","3","0"]));
        var b=await invalid.PreviewAsync("ballmarket","bad");Assert.Equal(ImportStatus.Invalid,b.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>invalid.ApplyAsync(b.Id));
        var valid=Import(Fixtures.Document(extra:["","00099999","Second","шт","1","10","9","8","0"]));
        var v=await valid.PreviewAsync("ballmarket","valid");
        // Намеренно повреждаем staging: нарушение ограничения БД должно откатить все подготовленные строки и цены.
        await using(var db=factory.CreateDbContext())
        {
            var row=await db.ImportRows.Where(x=>x.ImportBatchId==v.Id && x.Kind==ImportRowKind.Product).OrderByDescending(x=>x.RowNumber).FirstAsync();
            var parsed=JsonSerializer.Deserialize<ParsedOffer>(row.ParsedJson!)!;
            row.ParsedJson=JsonSerializer.Serialize(parsed with {Prices=[-1,9,8]});await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<DbUpdateException>(()=>valid.ApplyAsync(v.Id));
        await using(var db=factory.CreateDbContext())
        {
            Assert.Equal(0,await db.SupplierOffers.CountAsync());Assert.Equal(0,await db.SupplierOfferPrices.CountAsync());
            Assert.Equal(ImportStatus.Preview,(await db.ImportBatches.SingleAsync(x=>x.Id==v.Id)).Status);
        }
    }
    /// <summary>Проверяет сериализацию применения и необходимость повторной проверки устаревшей партии.</summary>
    [Fact]
    public async Task ParallelApplyIsSerializedAndStalePreviewRequiresRevalidation()
    {
        var s1=Import(Fixtures.Document());var a=await s1.PreviewAsync("ballmarket","a");
        var s2=Import(Fixtures.Document(price:"4700"));var b=await s2.PreviewAsync("ballmarket","b");
        await Task.WhenAll(s1.ApplyAsync(a.Id),s1.ApplyAsync(a.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>s2.ApplyAsync(b.Id));
        var refreshed=await s2.RepreviewAsync(b.Id);Assert.Equal(1,refreshed.Changed);
        await s2.ApplyAsync(b.Id);
        await using var db=factory.CreateDbContext();Assert.Equal(4,await db.SupplierPriceHistories.CountAsync());
    }
    /// <summary>Проверяет ограничения остатков, оптимистическую конкуренцию xmin и защиту категорий от циклов.</summary>
    [Fact]
    public async Task StockConstraintsXminAndCategoryCycleAreEnforced()
    {
        Guid balanceId;Guid parentId;Guid childId;
        await using(var db=factory.CreateDbContext())
        {
            var p=new Product{Name="Test"};var v=new ProductVariant{ProductId=p.Id,Sku="TEST",SaleUnit="шт"};var w=new Warehouse{Name="Test"};
            var stock=new InventoryBalance{ProductVariantId=v.Id,WarehouseId=w.Id,OnHand=5,Reserved=1};balanceId=stock.Id;
            var parent=new Category{Name="Parent"};var child=new Category{Name="Child",ParentId=parent.Id};parentId=parent.Id;childId=child.Id;
            db.AddRange(p,v,w,stock,parent,child);await db.SaveChangesAsync();
        }
        await using(var first=factory.CreateDbContext())
        await using(var second=factory.CreateDbContext())
        {
            var a=await first.InventoryBalances.SingleAsync(x=>x.Id==balanceId);var b=await second.InventoryBalances.SingleAsync(x=>x.Id==balanceId);
            a.Reserved=2;await first.SaveChangesAsync();b.Reserved=3;await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>second.SaveChangesAsync());
        }
        await using(var db=factory.CreateDbContext())
        {var stock=await db.InventoryBalances.SingleAsync();stock.Reserved=6;await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());}
        await using(var db=factory.CreateDbContext())
        {var parent=await db.Categories.SingleAsync(x=>x.Id==parentId);parent.ParentId=childId;await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());}
    }
    /// <summary>Проверяет подтверждение опта, защиту ручной цены и сохранность заказа при изменениях каталога.</summary>
    [Fact]
    public async Task WholesaleApprovalManualProtectionAndOrderSnapshotSurviveCatalogChanges()
    {
        await using(var db=factory.CreateDbContext())
        {
            await DemoData.SeedAsync(db);
            db.PricingSettings.Add(new(){Id=PricingService.SettingsId,MinimumWholesaleOrder=1000});await db.SaveChangesAsync();
        }
        Guid offerId;
        await using(var db=factory.CreateDbContext())offerId=(await db.SupplierOffers.SingleAsync()).Id;
        var pricing=new PricingService(factory);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>pricing.QuoteAsync("demo-Approved",DemoData.VariantId,1));
        var proposals=await pricing.ProposeAsync(offerId);Assert.Equal(3,proposals.Count);
        foreach(var id in proposals)await pricing.ApplyAsync(id);
        Assert.Equal(130m,(await pricing.QuoteAsync("demo-Pending",DemoData.VariantId,1)).Amount);
        Assert.Equal(120m,(await pricing.QuoteAsync("demo-Approved",DemoData.VariantId,9)).Amount);
        Assert.Equal(110m,(await pricing.QuoteAsync("demo-Approved",DemoData.VariantId,10)).Amount);
        Assert.Null((await pricing.QuoteAsync("demo-Approved",DemoData.VariantId,10)).MinimumWholesaleOrder);
        await using (var retired = factory.CreateDbContext()) { retired.PricingSettings.RemoveRange(await retired.PricingSettings.ToListAsync()); await retired.SaveChangesAsync(); }
        Assert.Equal(120m, (await pricing.QuoteAsync("demo-Approved",DemoData.VariantId,1)).Amount);

        await using(var db=factory.CreateDbContext())
        {
            var product=await db.Products.SingleAsync();var variant=await db.ProductVariants.SingleAsync();
            var order=new Order{Number="TEST-1",ContactName="DEMO",ContactEmail="demo@example.invalid",ContactPhone="DEMO",ShippingAddress="DEMO"};
            db.Orders.Add(order);db.OrderItems.Add(OrderSnapshots.Item(order.Id,product,variant,1,130,0));await db.SaveChangesAsync();
            product.Name="Changed";await db.SaveChangesAsync();
        }
        await pricing.SetManualAsync(DemoData.VariantId,CustomerSegment.Retail,1,200);
        await using(var db=factory.CreateDbContext())
        {var price=await db.SupplierOfferPrices.SingleAsync();price.Amount=150;await db.SaveChangesAsync();}
        var updated=await pricing.ProposeAsync(offerId);
        foreach(var id in updated)await pricing.ApplyAsync(id);
        Assert.Equal(200m,(await pricing.QuoteAsync("demo-Pending",DemoData.VariantId,1)).Amount);
        await using(var db=factory.CreateDbContext())
        {
            var item=await db.OrderItems.SingleAsync();Assert.Equal(130m,item.UnitPrice);Assert.StartsWith("DEMO",item.Name);
            var offer=await db.SupplierOffers.SingleAsync();offer.ConversionConfirmed=false;await db.SaveChangesAsync();
        }
        Assert.Empty(await pricing.ProposeAsync(offerId));
    }
    /// <summary>Проверяет устаревание пересчёта после изменения наценки и запрет расчёта без подтверждённого тарифа.</summary>
    [Fact]
    public async Task ChangedRuleInvalidatesPendingProposalAndUnknownTariffProducesNone()
    {
        await using(var db=factory.CreateDbContext())await DemoData.SeedAsync(db);
        Guid offerId;await using(var db=factory.CreateDbContext())offerId=(await db.SupplierOffers.SingleAsync()).Id;
        var service=new PricingService(factory);var ids=await service.ProposeAsync(offerId);
        await using(var db=factory.CreateDbContext())
        {foreach(var r in await db.MarkupRules.ToListAsync())r.MarkupPercent+=1;await db.SaveChangesAsync();}
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.ApplyAsync(ids[0]));
        await using(var db=factory.CreateDbContext())
        {var supplier=await db.Suppliers.SingleAsync();supplier.PriceTierConfirmed=false;await db.SaveChangesAsync();}
        Assert.Empty(await service.ProposeAsync(offerId));
    }
    /// <summary>Создаёт контексты изолированной тестовой базы данных.</summary>
    /// <param name="options">Параметры подключения и модели создаваемых контекстов.</param>
    private sealed class Factory(DbContextOptions<ApplicationDbContext> options):IDbContextFactory<ApplicationDbContext>
    {
        /// <summary>Создаёт новый независимый контекст; вызывающий код обязан освободить его после операции.</summary>
        public ApplicationDbContext CreateDbContext()=>new(options);}
    /// <summary>Тестовая реализация парсера, возвращающая заранее подготовленный документ.</summary>
    /// <param name="doc">Заранее подготовленный документ, возвращаемый тестовым парсером.</param>
    private sealed class Parser(ParsedPriceList doc):ISupplierPriceParser
    {
        /// <summary>Версия формата парсера для воспроизводимости импорта.</summary>
        public string Version=>doc.ParserVersion;
        /// <summary>Возвращает подготовленный тестовый документ без чтения файла; позволяет проверять сценарий импорта независимо от XLS.</summary>
        /// <param name="path">Путь к локальному XLS-файлу; содержимое рассматривается только как данные.</param>
        public ParsedPriceList Parse(string path)=>doc;}
}
