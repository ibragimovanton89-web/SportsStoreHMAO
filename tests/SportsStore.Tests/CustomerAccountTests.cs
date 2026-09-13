using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SportsStore.Application.Customers;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Admin;
using SportsStore.Infrastructure.Customers;
using SportsStore.Infrastructure.Identity;
using SportsStore.Infrastructure.Pricing;
using Xunit;

namespace SportsStore.Tests;

/// <summary>Реальные PostgreSQL-проверки покупателей, владения, опта и очереди; синтетические адреса не отправляются наружу.</summary>
public sealed partial class AdminTests
{
    /// <summary>Создаёт подтверждённого тестового покупателя стандартным Identity, не меняя пользователя администратора.</summary>
    private async Task<(Customer Customer, BuyerIdentity Identity)> BuyerAsync(string email = "buyer@example.invalid")
    {
        await using var scope = provider.CreateAsyncScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, LockoutEnabled = true };
        Assert.True((await users.CreateAsync(user, "Synthetic-Test_7404!")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, "Customer")).Succeeded);
        await using var db = await factory.CreateDbContextAsync(); var customer = new Customer { ApplicationUserId = user.Id, DisplayName = "Тестовый покупатель" };
        db.Customers.Add(customer); await db.SaveChangesAsync();
        return (customer, new(new(user.Id, user.SecurityStamp!, false)));
    }
    /// <summary>Собирает сервис с предъявленной синтетической сессией.</summary>
    private CustomerAccount Account(BuyerIdentity buyer) => new(factory, new CustomerAccess(buyer), new PricingService(factory));
    /// <summary>Собирает регистрацию с тестовым транспортом без внешней отправки.</summary>
    private CustomerRegistration Registration(CaptureMail mail)
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Email:PublicOrigin"] = "https://localhost:7393" }).Build();
        return new(provider.GetRequiredService<IServiceScopeFactory>(), mail, new(settings, new TestEnvironment()), new());
    }
    /// <summary>Проверяет конкурентную регистрацию одной почты, одну связь и роль без самостоятельного присвоения опта.</summary>
    [Fact]
    public async Task CustomerRegistrationIsAtomicAndConcurrentDuplicateIsNeutral()
    {
        var mail = new CaptureMail(); var registration = Registration(mail);
        await Task.WhenAll(registration.RegisterAsync("new@example.invalid", "Synthetic-Test_7404!", "Новый"), registration.RegisterAsync("NEW@example.invalid", "Synthetic-Test_7404!", "Новый"));
        await using var db = await factory.CreateDbContextAsync(); var user = await db.Users.SingleAsync(x => x.NormalizedEmail == "NEW@EXAMPLE.INVALID");
        Assert.False(user.EmailConfirmed); Assert.Single(await db.Customers.Where(x => x.ApplicationUserId == user.Id).ToListAsync());
        var customer = await db.Customers.SingleAsync(); Assert.Equal(WholesaleStatus.NotRequested, customer.WholesaleStatus); Assert.Equal(CustomerSegment.Retail, customer.Segment);
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == user.Id select r.Name).ToListAsync();
        Assert.Equal(["Customer"], roles); Assert.Single(mail.Messages);
        var original = customer.Id;
        await registration.RegisterAsync("new@example.invalid", "Synthetic-Test_7404!", "Подмена");
        Assert.Equal(original, (await db.Customers.SingleAsync()).Id);
    }
    /// <summary>Проверяет назначение, чужого пользователя, повтор подтверждения и повтор восстановления со сменой stamp.</summary>
    [Fact]
    public async Task CustomerTokensAreUserAndPurposeBoundAndResetRevokesSessions()
    {
        var mail = new CaptureMail(); var registration = Registration(mail);
        await registration.RegisterAsync("token@example.invalid", "Synthetic-Test_7404!", "Тест");
        var confirmation = TokenFrom(mail.Messages.Single());
        Assert.False(await registration.ConfirmAsync("wrong-user", confirmation.Token));
        Assert.False(await registration.ResetAsync(confirmation.User, confirmation.Token, "Synthetic-Changed_7404!"));
        Assert.True(await registration.ConfirmAsync(confirmation.User, confirmation.Token));
        Assert.False(await registration.ConfirmAsync(confirmation.User, confirmation.Token));
        await using var db = await factory.CreateDbContextAsync(); var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == confirmation.User);
        var account = Account(new(new(user.Id, user.SecurityStamp!, false))); Assert.NotNull(await account.ProfileAsync());
        await registration.ForgotAsync("token@example.invalid"); var reset = TokenFrom(mail.Messages.Last());
        Assert.False(await registration.ConfirmAsync(reset.User, reset.Token));
        Assert.True(await registration.ResetAsync(reset.User, reset.Token, "Synthetic-Changed_7404!"));
        Assert.False(await registration.ResetAsync(reset.User, reset.Token, "Synthetic-Again_7404!"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => account.ProfileAsync());
        Assert.True((await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id)).EmailConfirmed);
    }
    /// <summary>Неизвестные адреса, повтор подтверждения и неподтверждённое восстановление не создают письма с обходом проверки.</summary>
    [Fact]
    public async Task UnknownAndUnconfirmedRecoveryDoesNotSendOrConfirm()
    {
        var mail = new CaptureMail(); var registration = Registration(mail);
        await registration.ForgotAsync("missing@example.invalid"); await registration.ResendAsync("missing@example.invalid"); Assert.Empty(mail.Messages);
        await registration.RegisterAsync("unconfirmed@example.invalid", "Synthetic-Test_7404!", "Тест");
        await registration.ForgotAsync("unconfirmed@example.invalid"); Assert.Single(mail.Messages);
        await using var db = await factory.CreateDbContextAsync(); Assert.False((await db.Users.SingleAsync(x => x.Email == "unconfirmed@example.invalid")).EmailConfirmed);
    }
    /// <summary>Владение адресами, конкурентное редактирование и запрет локального admin-обхода проверяются прикладным сервисом.</summary>
    [Fact]
    public async Task BuyerAddressOwnershipAndConcurrencyAreEnforced()
    {
        var a = await BuyerAsync(); var b = await BuyerAsync("other@example.invalid"); var account = Account(a.Identity);
        await account.SaveAddressAsync(new(Guid.Empty, 0, "Получатель", "Тестовый адрес", "000000"));
        var address = Assert.Single(await account.AddressesAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Account(b.Identity).DeleteAddressAsync(address.Id, address.Version));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Account(b.Identity).SaveAddressAsync(address with { Address = "Подмена" }));
        await account.SaveAddressAsync(address with { Address = "Изменённый адрес" });
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => account.SaveAddressAsync(address with { Address = "Устаревшая вкладка" }));
        a.Identity.Session = a.Identity.Session with { Local = true };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => account.ProfileAsync());
    }
    /// <summary>Параллельные заявки сериализуются; повтор операции не создаёт вторую запись и историю.</summary>
    [Fact]
    public async Task PendingWholesaleApplicationIsUniqueAndIdempotent()
    {
        var buyer = await BuyerAsync(); var account = Account(buyer.Identity); var operation = Guid.NewGuid();
        await Task.WhenAll(account.SubmitAsync("Опт", operation), account.SubmitAsync("Опт", operation));
        var application = Assert.Single(await account.ApplicationsAsync());
        await Assert.ThrowsAsync<ArgumentException>(() => account.SubmitAsync("Другая", Guid.NewGuid()));
        var other = await BuyerAsync("other@example.invalid");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Account(other.Identity).WithdrawAsync(application.Id, application.Version, Guid.NewGuid()));
        var withdrawal = Guid.NewGuid(); await account.WithdrawAsync(application.Id, application.Version, withdrawal); await account.WithdrawAsync(application.Id, application.Version, withdrawal);
        Assert.Single(await account.HistoryAsync()); await account.SubmitAsync("Повторная заявка", Guid.NewGuid()); Assert.Equal(2, (await account.ApplicationsAsync()).Count);
    }
    /// <summary>Решение доступно Admin, создаёт outbox после коммита и отзывается при подтверждённой смене реквизитов.</summary>
    [Fact]
    public async Task WholesaleDecisionOutboxAndLegalRevocationAreAtomic()
    {
        var buyer = await BuyerAsync(); var account = Account(buyer.Identity); await account.SubmitAsync("Проверка", Guid.NewGuid());
        var application = Assert.Single(await account.ApplicationsAsync()); var administration = new WholesaleAdministration(factory, new AdminAccess(identity));
        await Role("Manager"); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => administration.DecideAsync(application.Id, application.Version, true, "Одобрено", Guid.NewGuid()));
        await Role("Admin"); var operation = Guid.NewGuid(); await administration.DecideAsync(application.Id, application.Version, true, "Одобрено", operation);
        await administration.DecideAsync(application.Id, application.Version, true, "Одобрено", operation);
        await using var db = await factory.CreateDbContextAsync(); Assert.Single(await db.NotificationOutbox.ToListAsync());
        var p = await account.ProfileAsync(); Assert.Equal(WholesaleStatus.Approved, p.Status);
        await account.SaveProfileAsync(p.Profile with { Phone = "+70000000000" }, false, Guid.NewGuid()); p = await account.ProfileAsync(); Assert.Equal(WholesaleStatus.Approved, p.Status);
        var changed = p.Profile with { Kind = CustomerKind.SoleProprietor, LegalName = "Тестовый ИП", Inn = "000000000000" };
        await Assert.ThrowsAsync<ArgumentException>(() => account.SaveProfileAsync(changed, false, Guid.NewGuid()));
        await account.SaveProfileAsync(changed, true, Guid.NewGuid()); Assert.Equal(WholesaleStatus.Rejected, (await account.ProfileAsync()).Status);
        Assert.Equal(2, await db.NotificationOutbox.CountAsync()); Assert.Equal(2, (await account.HistoryAsync()).Count);
        var mail = new CaptureMail { Fail = true }; var processor = new NotificationProcessor(factory, mail); Assert.True(await processor.ProcessOneAsync());
        Assert.Equal(WholesaleStatus.Rejected, (await account.ProfileAsync()).Status); Assert.Equal(2, await db.WholesaleDecisions.CountAsync());
        Assert.Contains(await db.NotificationOutbox.AsNoTracking().ToListAsync(), x => x.LastErrorCode == "DeliveryFailed" && x.SentAt == null);
    }
    /// <summary>Старая форма администратора также создаёт историю и очередь вместо обхода нового сценария.</summary>
    [Fact]
    public async Task ExistingAdminFormUsesWholesaleHistoryAndOutbox()
    {
        var buyer = await BuyerAsync(); var people = new AdminPeople(factory, new AdminAccess(identity));
        await people.SaveCustomerAsync(new(buyer.Customer.Id, buyer.Customer.Version, "Покупатель", CustomerKind.Individual, CustomerSegment.Wholesale,
            WholesaleStatus.Approved, null, null, null), "Ручное решение", Guid.NewGuid());
        await using var db = await factory.CreateDbContextAsync(); Assert.Single(await db.WholesaleDecisions.ToListAsync()); Assert.Single(await db.NotificationOutbox.ToListAsync());
        Assert.Empty(await db.WholesaleApplications.ToListAsync()); Assert.True((await Account(buyer.Identity).ProfileAsync()).HasLegacyApproval);
    }
    /// <summary>Извлекает синтетическую ссылку из захваченного письма только внутри тестового процесса.</summary>
    private static (string User, string Token) TokenFrom(CustomerEmail email)
    {
        var link = email.Body.Split('\n').Single(x => x.StartsWith("https://", StringComparison.Ordinal));
        var values = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(link).Query);
        return (values["userId"].ToString(), values["token"].ToString());
    }
    /// <summary>Оптовая цена ограничена владельцем, публичным вариантом и ступенью; отзыв действует без нового входа.</summary>
    [Fact]
    public async Task CustomerPriceRequiresApprovedLiveAccountAndPublicMatchingVariant()
    {
        var buyer = await BuyerAsync(); var account = Account(buyer.Identity);
        await using var db = await factory.CreateDbContextAsync(); var ids = await StorefrontFixture.SeedAsync(db);
        Assert.Null(await account.PriceAsync(ids.Product, ids.RedS, 1));
        var c = await db.Customers.SingleAsync(); c.Segment = CustomerSegment.Wholesale; c.WholesaleStatus = WholesaleStatus.Approved;
        db.SalePrices.Add(new() { ProductVariantId = ids.RedS, Segment = CustomerSegment.Wholesale, MinimumQuantity = 10, Amount = 650, IsManual = true }); await db.SaveChangesAsync();
        Assert.Equal(777, (await account.PriceAsync(ids.Product, ids.RedS, 9))!.Amount); Assert.Equal(650, (await account.PriceAsync(ids.Product, ids.RedS, 10))!.Amount);
        Assert.Null((await account.PriceAsync(ids.Product, ids.BlueM, 1))!.Amount);
        Assert.Null(await account.PriceAsync(ids.Hidden, ids.RedS, 1));
        await Assert.ThrowsAsync<ArgumentException>(() => account.PriceAsync(ids.Product, ids.RedS, 0));
        c.WholesaleStatus = WholesaleStatus.Rejected; c.Segment = CustomerSegment.Retail; await db.SaveChangesAsync();
        Assert.Null(await account.PriceAsync(ids.Product, ids.RedS, 10));
        c.WholesaleStatus = WholesaleStatus.Approved; c.Segment = CustomerSegment.Wholesale; await db.SaveChangesAsync();
        var user = await db.Users.SingleAsync(x => x.Id == c.ApplicationUserId); user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1); await db.SaveChangesAsync();
        Assert.Null(await account.PriceAsync(ids.Product, ids.RedS, 10));
    }
    /// <summary>Смена реквизитов Pending делает старую заявку непригодной для одобрения и сохраняет её исходный снимок.</summary>
    [Fact]
    public async Task ChangedPendingProfileCannotBeApprovedFromOldSnapshot()
    {
        var buyer = await BuyerAsync(); var account = Account(buyer.Identity); await account.SubmitAsync("Тест", Guid.NewGuid());
        var application = Assert.Single(await account.ApplicationsAsync()); var profile = await account.ProfileAsync();
        await account.SaveProfileAsync(profile.Profile with { Kind = CustomerKind.Organization, LegalName = "Тестовая организация", Inn = "0000000000" }, true, Guid.NewGuid());
        var administration = new WholesaleAdministration(factory, new AdminAccess(identity));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => administration.DecideAsync(application.Id, application.Version, true, "Устаревшее решение", Guid.NewGuid()));
        var old = Assert.Single(await account.ApplicationsAsync()); Assert.Equal(CustomerKind.Individual, old.Kind); Assert.Null(old.Inn);
        Assert.Equal(WholesaleApplicationStatus.Withdrawn, old.Status);
        await account.SubmitAsync("Новые реквизиты", Guid.NewGuid()); Assert.Equal("0000000000", (await account.ApplicationsAsync())[0].Inn);
    }
    /// <summary>Частичный уникальный индекс запрещает вторую ожидающую заявку даже при обходе прикладного сервиса.</summary>
    [Fact]
    public async Task PostgreSqlRejectsTwoPendingApplicationsAndRollsBackWholeTransaction()
    {
        var buyer = await BuyerAsync(); await using var db = await factory.CreateDbContextAsync();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            db.WholesaleApplications.Add(new() { CustomerId = buyer.Customer.Id, OperationId = Guid.NewGuid() });
            db.WholesaleApplications.Add(new() { CustomerId = buyer.Customer.Id, OperationId = Guid.NewGuid() });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); await tx.RollbackAsync();
        }
        db.ChangeTracker.Clear(); Assert.Empty(await db.WholesaleApplications.ToListAsync()); Assert.Empty(await db.NotificationOutbox.ToListAsync());
    }
    /// <summary>Реальный фоновый таймер не разлогинивает покупателя без Staff-роли, но отзывает заблокированную сессию.</summary>
    [Fact]
    public async Task CustomerCircuitSurvivesRevalidationAndRejectsLockout()
    {
        var buyer = await BuyerAsync();
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, buyer.Identity.Session.UserId),
            new System.Security.Claims.Claim("AspNet.Identity.SecurityStamp", buyer.Identity.Session.SecurityStamp),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Customer")
        }, "Test"));
        using var state = new SportsStore.Web.Security.StaffAuthenticationStateProvider(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance, factory);
        state.SetAuthenticationState(Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(principal)));
        await Task.Delay(TimeSpan.FromSeconds(17)); Assert.True((await state.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated);
        await using var db = await factory.CreateDbContextAsync(); var user = await db.Users.SingleAsync(x => x.Id == buyer.Identity.Session.UserId);
        user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1); await db.SaveChangesAsync();
        await Task.Delay(TimeSpan.FromSeconds(17)); Assert.False((await state.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated);
    }
    /// <summary>Истёкший срок стандартного провайдера делает код непригодным, не меняя подтверждение пользователя.</summary>
    [Fact]
    public async Task ExpiredCustomerTokenIsRejected()
    {
        var mail = new CaptureMail(); var registration = Registration(mail);
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CustomerTokenOptions>>();
        options.Value.TokenLifespan = TimeSpan.FromMilliseconds(1);
        await registration.RegisterAsync("expired@example.invalid", "Synthetic-Test_7404!", "Тест");
        await Task.Delay(30); var link = TokenFrom(mail.Messages.Single());
        Assert.False(await registration.ConfirmAsync(link.User, link.Token));
        await using var db = await factory.CreateDbContextAsync(); Assert.False((await db.Users.SingleAsync(x => x.Id == link.User)).EmailConfirmed);
    }
    /// <summary>Ошибка записи профиля откатывает Identity и назначение роли из той же транзакции.</summary>
    [Fact]
    public async Task RegistrationProfileFailureRollsBackIdentityAndDoesNotSendMail()
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Customer\" ADD CONSTRAINT \"CK_TestRejectProfile\" CHECK (\"DisplayName\" <> 'Запрет')");
        var mail = new CaptureMail(); var registration = Registration(mail);
        await Assert.ThrowsAsync<DbUpdateException>(() => registration.RegisterAsync("rollback@example.invalid", "Synthetic-Test_7404!", "Запрет"));
        Assert.False(await db.Users.AnyAsync(x => x.NormalizedEmail == "ROLLBACK@EXAMPLE.INVALID")); Assert.Empty(await db.Customers.ToListAsync()); Assert.Empty(mail.Messages);
    }
    /// <summary>Незафиксированное намерение не видно обработчику; после коммита письмо отправляется один раз.</summary>
    [Fact]
    public async Task NotificationIsInvisibleBeforeCommitAndRetryDoesNotRepeatDecision()
    {
        var buyer = await BuyerAsync(); var mail = new CaptureMail(); var processor = new NotificationProcessor(factory, mail);
        await using var db = await factory.CreateDbContextAsync();
        var decision = new WholesaleDecision { CustomerId = buyer.Customer.Id, ActorId = "test-actor", OperationId = Guid.NewGuid(), Outcome = WholesaleApplicationStatus.Approved, PublicReason = "Тестовое решение" };
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            db.WholesaleDecisions.Add(decision); db.NotificationOutbox.Add(new() { DecisionId = decision.Id }); await db.SaveChangesAsync();
            Assert.False(await processor.ProcessOneAsync()); Assert.Empty(mail.Messages); await tx.CommitAsync();
        }
        Assert.True(await processor.ProcessOneAsync()); Assert.False(await processor.ProcessOneAsync()); Assert.Single(mail.Messages); Assert.Single(await db.WholesaleDecisions.ToListAsync());
    }
    /// <summary>Гонка одобрения и смены реквизитов не оставляет одобрение для непроверенного нового профиля.</summary>
    [Fact]
    public async Task ConcurrentApprovalAndLegalChangeCannotApproveDifferentSnapshot()
    {
        var buyer = await BuyerAsync(); var account = Account(buyer.Identity); await account.SubmitAsync("Тест", Guid.NewGuid());
        var profile = await account.ProfileAsync(); var application = Assert.Single(await account.ApplicationsAsync());
        var administration = new WholesaleAdministration(factory, new AdminAccess(identity));
        var approval = Record.ExceptionAsync(() => administration.DecideAsync(application.Id, application.Version, true, "Одобрено", Guid.NewGuid()));
        var edit = Record.ExceptionAsync(() => account.SaveProfileAsync(profile.Profile with { Kind = CustomerKind.Organization, LegalName = "Новый субъект", Inn = "0000000000" }, true, Guid.NewGuid()));
        var results = await Task.WhenAll(approval, edit);
        Assert.Contains(results, x => x is null);
        var current = await account.ProfileAsync();
        Assert.False(current.Status == WholesaleStatus.Approved && current.Profile.Kind == CustomerKind.Organization);
        Assert.All(results.Where(x => x is not null), x => Assert.True(x is ArgumentException or DbUpdateConcurrencyException));
    }
    /// <summary>Обновление прежней схемы сохраняет клиента, Identity-связь и одобрение без выдуманных заявок.</summary>
    [Fact]
    public async Task CustomerMigrationPreservesLegacyApprovalWithoutInventingApplications()
    {
        await using var db = await factory.CreateDbContextAsync();
        var migrations = db.Database.GetMigrations().ToArray();
        var migrator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(db);
        await migrator.MigrateAsync(migrations[^2]);
        var c = new Customer { DisplayName = "Ранее одобренный", Kind = CustomerKind.Individual, Segment = CustomerSegment.Wholesale, WholesaleStatus = WholesaleStatus.Approved };
        db.Customers.Add(c); await db.SaveChangesAsync(); var version = c.Version;
        await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
        var actual = await db.Customers.SingleAsync(x => x.Id == c.Id); Assert.Equal(version, actual.Version); Assert.Equal(WholesaleStatus.Approved, actual.WholesaleStatus);
        Assert.Empty(await db.WholesaleApplications.ToListAsync()); Assert.Empty(await db.WholesaleDecisions.ToListAsync());
    }
    /// <summary>Доверенный тестовый источник сессии без HTTP-параметров.</summary>
    /// <param name="session">Синтетическая предъявленная сессия.</param>
    private sealed class BuyerIdentity(CustomerSession session) : ICustomerIdentity
    {
        /// <summary>Сессия, которую тест намеренно отзывает или подменяет для проверки отказа.</summary>
        public CustomerSession Session { get; set; } = session;
        /// <summary>Возвращает сессию только тестовому прикладному сервису.</summary>
        public Task<CustomerSession> GetAsync(CancellationToken ct = default) => Task.FromResult(Session);
    }
    /// <summary>Транспорт тестов, никогда не отправляющий письма в сеть.</summary>
    private sealed class CaptureMail : ICustomerEmailTransport
    {
        /// <summary>Захваченные письма, включая синтетические одноразовые ссылки.</summary>
        public ConcurrentQueue<CustomerEmail> Messages { get; } = new();
        /// <summary>Имитирует отказ транспорта после фиксации решения.</summary>
        public bool Fail { get; set; }
        /// <summary>Сохраняет письмо в памяти либо имитирует безопасную ошибку.</summary>
        public Task SendAsync(CustomerEmail message, CancellationToken ct = default)
        { if (Fail) throw new IOException("Synthetic failure"); Messages.Enqueue(message); return Task.CompletedTask; }
    }
    /// <summary>Изолированное окружение для проверки разрешённых адресов тестовых писем.</summary>
    private sealed class TestEnvironment : IHostEnvironment
    {
        /// <summary>Тест всегда использует Development, не производственный захват.</summary>
        public string EnvironmentName { get; set; } = Environments.Development;
        /// <summary>Имя тестового приложения.</summary>
        public string ApplicationName { get; set; } = "SportsStore.Tests";
        /// <summary>Корень синтетического окружения.</summary>
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        /// <summary>Не выдаёт файлы через тестовый провайдер.</summary>
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
