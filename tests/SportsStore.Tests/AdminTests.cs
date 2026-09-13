using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SkiaSharp;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure;
using SportsStore.Infrastructure.Admin;
using SportsStore.Infrastructure.Identity;
using SportsStore.Infrastructure.Import;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Pricing;
using SportsStore.Infrastructure.Services;
using Xunit;

namespace SportsStore.Tests;

/// <summary>Проверяет административные границы доверия и транзакции в собственной изолированной PostgreSQL, не меняя БД пользователя.</summary>
[Trait("Category", "PostgreSQL")]
public sealed partial class AdminTests : IAsyncLifetime
{
    /// <summary>Уникальная тестовая БД, разрешённая для удаления только этим экземпляром.</summary>
    private readonly string database = "sportsstore_admin_test_" + Guid.NewGuid().ToString("N");
    /// <summary>Административное подключение используется только для создания и удаления тестовой БД.</summary>
    private string admin = "";
    /// <summary>Признак созданной этим тестом базы.</summary>
    private bool created;
    /// <summary>Изолированный контейнер зависимостей с эфемерными ключами только для теста.</summary>
    private ServiceProvider provider = null!;
    /// <summary>Фабрика контекстов ограниченной роли приложения.</summary>
    private IDbContextFactory<ApplicationDbContext> factory = null!;
    /// <summary>Доверенный источник тестовой сессии; не регистрируется в Web.</summary>
    private readonly TestIdentity identity = new();
    /// <summary>Конфигурация изолированной БД и временного каталога изображений.</summary>
    private IConfiguration configuration = null!;
    /// <summary>Физическое тестовое хранилище.</summary>
    private readonly string storage = Path.Combine(Path.GetTempPath(), "sportsstore-images-test-" + Guid.NewGuid().ToString("N"));
    /// <summary>Создаёт чистую PostgreSQL, применяет миграции и добавляет синтетического сотрудника.</summary>
    public async Task InitializeAsync()
    {
        admin = Environment.GetEnvironmentVariable("SPORTSSTORE_TEST_ADMIN_CONNECTION") ?? throw new InvalidOperationException("Нужно тестовое административное подключение.");
        var cs = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? throw new InvalidOperationException("Нужно подключение приложения.")) { Database = database };
        await using (var connection = new NpgsqlConnection(admin)) { await connection.OpenAsync(); await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{database}\" OWNER sportsstore", connection); await cmd.ExecuteNonQueryAsync(); created = true; }
        configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = cs.ConnectionString, ["Storage:RootPath"] = storage }).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddDataProtection().UseEphemeralDataProtectionProvider(); services.AddInfrastructure(configuration);
        provider = services.BuildServiceProvider(); factory = provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var db = await factory.CreateDbContextAsync(); await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
        var user = new ApplicationUser { Id = "test-actor", UserName = "test-actor", Email = "actor@example.invalid", EmailConfirmed = true, TwoFactorEnabled = true, SecurityStamp = "test-stamp", ConcurrencyStamp = "version-1", LockoutEnabled = true, PasswordHash = "test-only-non-login-hash" };
        db.Users.Add(user); db.UserRoles.Add(new() { UserId = user.Id, RoleId = await db.Roles.Where(x => x.Name == "Admin").Select(x => x.Id).SingleAsync() }); await db.SaveChangesAsync();
        identity.Session = new(user.Id, user.SecurityStamp, true);
    }
    /// <summary>Удаляет только собственную тестовую БД и проверенный временный каталог.</summary>
    public async Task DisposeAsync()
    {
        if (provider is not null) await provider.DisposeAsync();
        if (created)
        {
            Assert.StartsWith("sportsstore_admin_test_", database); Assert.Equal(55, database.Length);
            await using var connection = new NpgsqlConnection(admin); await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", connection); await cmd.ExecuteNonQueryAsync();
        }
        var full = Path.GetFullPath(storage); Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), full);
        if (Directory.Exists(full)) Directory.Delete(full, true);
    }
    /// <summary>Сервис каталога с реальной проверкой тестовых claims по PostgreSQL.</summary>
    private AdminCatalog Catalog() => new(factory, new AdminAccess(identity));
    /// <summary>Сервис цен с реальной проверкой полномочий.</summary>
    private AdminCommerce Commerce() => new(factory, new AdminAccess(identity));
    /// <summary>Меняет роль в БД, оставляя старую предъявленную сессию для проверки отзыва.</summary>
    private async Task Role(string role)
    {
        await using var db = await factory.CreateDbContextAsync(); db.UserRoles.RemoveRange(await db.UserRoles.Where(x => x.UserId == "test-actor").ToListAsync());
        db.UserRoles.Add(new() { UserId = "test-actor", RoleId = await db.Roles.Where(x => x.Name == role).Select(x => x.Id).SingleAsync() }); await db.SaveChangesAsync();
    }
    /// <summary>Проверяет немедленный отказ для гостя, Customer, отсутствия MFA и отозванного stamp.</summary>
    [Fact]
    public async Task LiveIdentityRejectsGuestCustomerMissingMfaAndRevokedStamp()
    {
        var original = identity.Session; identity.Session = new("", "", false); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Catalog().OverviewAsync());
        identity.Session = original with { Mfa = false }; await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Catalog().OverviewAsync());
        identity.Session = original; await Role("Customer"); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Catalog().OverviewAsync());
        await Role("Admin"); await using var db = await factory.CreateDbContextAsync(); (await db.Users.SingleAsync()).SecurityStamp = "revoked"; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Catalog().OverviewAsync());
    }
    /// <summary>Проверяет прямую команду Manager, подмену статуса DTO и редактирование уже опубликованной карточки.</summary>
    [Fact]
    public async Task ManagerCannotPublishChangeSettingsOrEditPublishedEvenWithExtraStatus()
    {
        await Role("Manager"); var id = await Catalog().SaveProductAsync(new(Guid.Empty, 0, "DEMO", null, null, null, ProductStatus.Published), Guid.NewGuid());
        var p = await Catalog().ProductAsync(id); Assert.Equal(ProductStatus.Draft, p.Status);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Catalog().SetStatusAsync(id, p.Version, ProductStatus.Published, Guid.NewGuid()));
        await using (var db = await factory.CreateDbContextAsync()) { (await db.Products.SingleAsync()).Status = ProductStatus.Published; await db.SaveChangesAsync(); }
        p = await Catalog().ProductAsync(id); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Catalog().SaveProductAsync(p with { Name = "Подмена" }, Guid.NewGuid()));
    }
    /// <summary>Проверяет конфликт версии двух вкладок без потери первого сохранения и без ложного аудита.</summary>
    [Fact]
    public async Task StaleVersionCannotOverwriteAndPublicationExplainsMissingData()
    {
        var id = await Catalog().SaveProductAsync(new(Guid.Empty, 0, "DEMO", null, null, null, ProductStatus.Draft), Guid.NewGuid()); var old = await Catalog().ProductAsync(id);
        await Catalog().SaveProductAsync(old with { Name = "Первое изменение" }, Guid.NewGuid());
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Catalog().SaveProductAsync(old with { Name = "Старое окно" }, Guid.NewGuid()));
        Assert.Equal("Первое изменение", (await Catalog().ProductAsync(id)).Name);
        var problems = await Catalog().PublicationProblemsAsync(id); Assert.Contains(problems, x => x.Contains("бренд")); Assert.Contains(problems, x => x.Contains("изображение"));
        await using var db = await factory.CreateDbContextAsync(); Assert.Equal(2, await db.AdminAudits.CountAsync());
    }
    /// <summary>Проверяет косвенный путь обхода: Manager готовит расчёт без публикации даже при AutoApply=true.</summary>
    [Fact]
    public async Task ManagerProposalsNeverAutoPublishAndManualPriceStaysProtected()
    {
        Guid offerId;
        await using (var db = await factory.CreateDbContextAsync()) { await DemoData.SeedAsync(db); offerId = (await db.SupplierOffers.SingleAsync()).Id; db.PricingSettings.Add(new() { Id = PricingService.SettingsId, AutoApplyProposals = true, MinimumWholesaleOrder = 100 }); await db.SaveChangesAsync(); }
        await Role("Manager"); var result = await Commerce().ProposeAsync(offerId, Guid.NewGuid()); Assert.NotEmpty(result.Ids);
        await using (var db = await factory.CreateDbContextAsync()) Assert.Empty(await db.SalePrices.ToListAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Commerce().ApplyPriceAsync(result.Ids[0], Guid.NewGuid()));
        await Role("Admin");
        await Commerce().ProposeAsync(offerId, Guid.NewGuid());
        await using (var noAuto = await factory.CreateDbContextAsync()) Assert.Empty(await noAuto.SalePrices.ToListAsync());
        await Commerce().ManualAsync(DemoData.VariantId, CustomerSegment.Retail, 1, 199, Guid.NewGuid());
        var result2 = await Commerce().ProposeAsync(offerId, Guid.NewGuid()); Assert.Contains(result2.Problems, x => x.Contains("Ручные"));
        await using var verify = await factory.CreateDbContextAsync(); Assert.Equal(199, (await verify.SalePrices.SingleAsync(x => x.Segment == CustomerSegment.Retail)).Amount);
    }
    /// <summary>Проверяет реальные декодирование и перекодирование, отказ HTML и слишком больших изображений.</summary>
    [Fact]
    public async Task ImageValidationRejectsSpoofedContentAndPreservesDecodedPixels()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => AdminImages.SanitizeAsync(new MemoryStream("<svg onload='attack'/>"u8.ToArray())));
        using var bitmap = new SKBitmap(24, 24); bitmap.Erase(SKColors.Green); using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        var output = await AdminImages.SanitizeAsync(new MemoryStream(png.ToArray())); using var decoded = SKBitmap.Decode(output); Assert.Equal(24, decoded.Width);
        await Assert.ThrowsAsync<InvalidDataException>(() => AdminImages.SanitizeAsync(new MemoryStream(new byte[AdminImages.MaxBytes + 1])));
    }
    /// <summary>Проверяет создание приглашения, запрет повторного повышения и защиту последнего Admin.</summary>
    [Fact]
    public async Task EmployeesProtectLastAdminAndInvitationCannotPromoteExistingAccount()
    {
        var service = new AdminEmployees(provider.GetRequiredService<IServiceScopeFactory>(), factory, new AdminAccess(identity));
        var self = (await service.ListAsync(new())).Items.Single();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(self with { Blocked = true }, "Тест", Guid.NewGuid()));
        var invitation = await service.InviteAsync("manager@example.invalid", "Manager", Guid.NewGuid()); Assert.StartsWith("/account/invite?", invitation);
        await Assert.ThrowsAsync<ArgumentException>(() => service.InviteAsync("manager@example.invalid", "Admin", Guid.NewGuid()));
        await using var db = await factory.CreateDbContextAsync(); Assert.DoesNotContain(await db.AdminAudits.Select(x => x.Description).ToListAsync(), x => x.Contains("Token="));
        Assert.Null((await db.Users.SingleAsync(x => x.Email == "manager@example.invalid")).PasswordHash);
    }
    /// <summary>Проверяет, что изменение опта не назначает роли и немедленно влияет на доступный сегмент.</summary>
    [Fact]
    public async Task WholesaleDecisionAndRevocationAreAuditedWithoutIdentityRoleChanges()
    {
        await using (var db = await factory.CreateDbContextAsync()) await DemoData.SeedAsync(db);
        var people = new AdminPeople(factory, new AdminAccess(identity)); var c = (await people.CustomersAsync(new())).Items.First(x => x.Wholesale == WholesaleStatus.Approved);
        await people.SaveCustomerAsync(c with { Wholesale = WholesaleStatus.Rejected, Kind = CustomerKind.Individual }, "Учебный отзыв", Guid.NewGuid());
        await using var verify = await factory.CreateDbContextAsync(); var customer = await verify.Customers.SingleAsync(x => x.Id == c.Id);
        Assert.Equal(SportsStore.Domain.Entities.CustomerSegment.Retail, SportsStore.Application.Pricing.PriceCalculator.AvailableSegment(customer));
        Assert.False(await verify.UserRoles.AnyAsync(x => x.UserId == customer.ApplicationUserId)); Assert.Contains(await verify.AdminAudits.ToListAsync(), x => x.Action == "customer.save");
    }
    /// <summary>Проверяет реальный SQL справочников, пагинацию, цикл и атомарный отказ удаления используемого бренда.</summary>
    [Fact]
    public async Task LookupQueriesTranslateAndConstraintsPreserveCatalogAndAudit()
    {
        var brand = await Catalog().SaveLookupAsync(false, new(Guid.Empty, "Учебный бренд", 0), Guid.NewGuid());
        var root = await Catalog().SaveLookupAsync(true, new(Guid.Empty, "Корень", 0), Guid.NewGuid());
        var child = await Catalog().SaveLookupAsync(true, new(Guid.Empty, "Потомок", 0, root), Guid.NewGuid());
        Assert.Single((await Catalog().LookupsAsync(false, new("Учебный"))).Items);
        Assert.Empty((await Catalog().LookupsAsync(false, new("", 2))).Items);
        var categories = (await Catalog().LookupsAsync(true, new())).Items;
        Assert.Equal(2, categories.Count);
        var ancestor = categories.Single(x => x.Id == root);
        await Assert.ThrowsAsync<ArgumentException>(() => Catalog().SaveLookupAsync(true, ancestor with { ParentId = child }, Guid.NewGuid()));
        await Catalog().SaveProductAsync(new(Guid.Empty, 0, "Учебный товар", null, brand, child, ProductStatus.Draft), Guid.NewGuid());
        var selected = (await Catalog().LookupsAsync(false, new())).Items.Single();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Catalog().DeleteLookupAsync(false, brand, selected.Version, Guid.NewGuid()));
        await using var db = await factory.CreateDbContextAsync();
        Assert.Equal(4, await db.AdminAudits.CountAsync());
        Assert.Null((await db.Categories.SingleAsync(x => x.Id == root)).ParentId);
    }
    /// <summary>Проверяет адаптер загрузки, очистку временных файлов, повтор, права apply и откат при отказе аудита.</summary>
    [Fact]
    public async Task ImportAdapterCleansFilesAndCommitsAuditWithOffersAtomically()
    {
        var service = new AdminImport(factory, new AdminAccess(identity), new FakeParser(), configuration);
        await Role("Manager");
        await using var input = new MemoryStream([1, 2, 3]);
        var batch = await service.UploadAsync(input, "../учебный.xls", Guid.NewGuid());
        Assert.Empty(Directory.GetFiles(Path.Combine(storage, "imports")));
        await using var repeated = new MemoryStream([1, 2, 3]);
        Assert.Equal(batch.Id, (await service.UploadAsync(repeated, "учебный.xls", Guid.NewGuid())).Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApplyAsync(batch.Id, Guid.NewGuid()));
        var failing = new PriceImportService(factory, new FakeParser(), (_, _, _) => throw new InvalidOperationException("Имитированный отказ аудита"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failing.ApplyAsync(batch.Id));
        await using (var db = await factory.CreateDbContextAsync())
        {
            Assert.Empty(await db.SupplierOffers.ToListAsync());
            Assert.Equal(ImportStatus.Preview, (await db.ImportBatches.SingleAsync()).Status);
            Assert.Single(await db.AdminAudits.ToListAsync());
        }
        await Role("Admin"); await service.ApplyAsync(batch.Id, Guid.NewGuid()); await service.ApplyAsync(batch.Id, Guid.NewGuid());
        await using var verify = await factory.CreateDbContextAsync();
        Assert.Single(await verify.SupplierOffers.ToListAsync()); Assert.Equal(2, await verify.AdminAudits.CountAsync());
        await using var tooLarge = new MemoryStream(new byte[BallMarketParser.MaxBytes + 1]);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.UploadAsync(tooLarge, "слишком-большой.xls", Guid.NewGuid()));
        Assert.Empty(Directory.GetFiles(Path.Combine(storage, "imports")));
    }
    /// <summary>Тестовый источник предъявленной сессии, позволяющий моделировать устаревшие claims.</summary>
    private sealed class TestIdentity : IAdminIdentity
    {
        /// <summary>Текущая тестовая сессия.</summary>
        public AdminSession Session { get; set; } = new("", "", false);
        /// <summary>Возвращает тестовый снимок; реальные права всегда читает AdminAccess.</summary>
        public Task<AdminSession> GetAsync(CancellationToken ct = default) => Task.FromResult(Session);
    }
    /// <summary>Синтетический прайс для проверки транзакции отдельно от бинарного парсера.</summary>
    private sealed class FakeParser : SportsStore.Application.Import.ISupplierPriceParser
    {
        /// <summary>Версия синтетического источника.</summary>
        public string Version => "test/1";
        /// <summary>Возвращает неизменный документ из фикстуры.</summary>
        public SportsStore.Application.Import.ParsedPriceList Parse(string path) => Fixtures.Document();
    }
}
