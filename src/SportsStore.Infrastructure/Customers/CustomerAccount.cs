using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Customers;
using SportsStore.Application.Pricing;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Admin;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Customers;

/// <summary>Кабинет покупателя: короткие контексты, разрешённые поля и проверка владельца каждой операции.</summary>
/// <param name="factory">Фабрика контекстов.</param><param name="access">Живая проверка сессии.</param><param name="pricing">Общий выбор сохранённой цены.</param>
public sealed class CustomerAccount(IDbContextFactory<ApplicationDbContext> factory, CustomerAccess access, IPricingService pricing) : ICustomerAccount
{
    /// <summary>Читает профиль текущего покупателя и отмечает историческое одобрение без заявки.</summary>
    public async Task<CustomerProfile> ProfileAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var c = await access.RequireAsync(db, ct);
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == c.ApplicationUserId, ct);
        var org = await db.OrganizationProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.CustomerId == c.Id, ct);
        return new(new(c.Version, c.DisplayName, user.PhoneNumber, c.Kind, org?.LegalName, org?.Inn, org?.Kpp), user.Email!, c.WholesaleStatus,
            c.WholesaleStatus == WholesaleStatus.Approved && !await db.WholesaleApplications.AnyAsync(x => x.CustomerId == c.Id, ct));
    }
    /// <summary>Сохраняет профиль; смена реквизитов отзывает действующий опт или ожидающую заявку в той же транзакции.</summary>
    public async Task SaveProfileAsync(ProfileInput input, bool acknowledgeWholesaleRevocation, Guid operationId, CancellationToken ct = default)
    {
        var name = AdminAccess.Text(input.Name, "Имя", 200); var phone = AdminAccess.Optional(input.Phone, 32);
        var legal = AdminAccess.Optional(input.LegalName); var inn = AdminAccess.Optional(input.Inn, 12); var kpp = AdminAccess.Optional(input.Kpp, 9);
        WholesaleWorkflow.ValidateLegal(input.Kind, legal, inn, kpp);
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var c = await access.RequireAsync(db, ct); c = await WholesaleWorkflow.LockAsync(db, c.Id, ct);
        if (await WholesaleWorkflow.PriorAsync(db, c.Id, operationId, ct)) return;
        AdminAccess.Version(c, input.Version);
        var org = await db.OrganizationProfiles.SingleOrDefaultAsync(x => x.CustomerId == c.Id, ct);
        var changed = !WholesaleWorkflow.SameLegal(c.Kind, org, input.Kind, legal, inn, kpp);
        if (changed && c.WholesaleStatus is WholesaleStatus.Approved or WholesaleStatus.Pending)
        {
            if (!acknowledgeWholesaleRevocation) throw new ArgumentException("Изменение реквизитов отзовёт опт или текущую заявку. Подтвердите это действие.");
            var application = await db.WholesaleApplications.Where(x => x.CustomerId == c.Id && (x.Status == WholesaleApplicationStatus.Pending || x.Status == WholesaleApplicationStatus.Approved))
                .OrderByDescending(x => x.SubmittedAt).FirstOrDefaultAsync(ct);
            WholesaleWorkflow.Decide(db, c, application, c.WholesaleStatus == WholesaleStatus.Approved ? WholesaleApplicationStatus.Revoked : WholesaleApplicationStatus.Withdrawn,
                c.ApplicationUserId!, "Изменены существенные реквизиты. Можно подать новую заявку.", operationId);
        }
        c.DisplayName = name; c.Kind = input.Kind;
        if (input.Kind == CustomerKind.Individual) { if (org is not null) db.OrganizationProfiles.Remove(org); }
        else
        {
            if (org is null) { org = new() { CustomerId = c.Id }; db.OrganizationProfiles.Add(org); }
            org.LegalName = legal!; org.Inn = inn!; org.Kpp = kpp;
        }
        var user = await db.Users.SingleAsync(x => x.Id == c.ApplicationUserId, ct);
        if (user.PhoneNumber != phone) { user.PhoneNumber = phone; user.PhoneNumberConfirmed = false; }
        // Даже изменение только телефона должно менять версию общей формы профиля.
        db.Entry(c).Property(x => x.DisplayName).IsModified = true;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    /// <summary>Читает адреса только текущего владельца.</summary>
    public async Task<IReadOnlyList<AddressInput>> AddressesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var c = await access.RequireAsync(db, ct);
        return await db.CustomerAddresss.AsNoTracking().Where(x => x.CustomerId == c.Id).OrderBy(x => x.Id)
            .Select(x => new AddressInput(x.Id, x.Version, x.Recipient, x.Address, x.PostalCode)).ToListAsync(ct);
    }
    /// <summary>Сохраняет собственный адрес; чужой идентификатор не раскрывает существование записи.</summary>
    public async Task SaveAddressAsync(AddressInput input, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var c = await access.RequireAsync(db, ct);
        CustomerAddress address;
        if (input.Id == Guid.Empty)
        {
            if (await db.CustomerAddresss.CountAsync(x => x.CustomerId == c.Id, ct) >= 20) throw new ArgumentException("Допустимо до 20 адресов.");
            address = new() { CustomerId = c.Id }; db.CustomerAddresss.Add(address);
        }
        else
        {
            address = await db.CustomerAddresss.SingleOrDefaultAsync(x => x.Id == input.Id && x.CustomerId == c.Id, ct)
                ?? throw new UnauthorizedAccessException("Адрес недоступен.");
            AdminAccess.Version(address, input.Version);
        }
        address.Recipient = AdminAccess.Text(input.Recipient, "Получатель", 200);
        address.Address = AdminAccess.Text(input.Address, "Адрес"); address.PostalCode = AdminAccess.Optional(input.PostalCode, 20);
        await db.SaveChangesAsync(ct);
    }
    /// <summary>Удаляет только собственный адрес указанной версии.</summary>
    public async Task DeleteAddressAsync(Guid id, uint version, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var c = await access.RequireAsync(db, ct);
        var address = await db.CustomerAddresss.SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == c.Id, ct)
            ?? throw new UnauthorizedAccessException("Адрес недоступен.");
        AdminAccess.Version(address, version); db.CustomerAddresss.Remove(address); await db.SaveChangesAsync(ct);
    }
    /// <summary>Возвращает последние собственные заявки без внутренних данных сотрудников.</summary>
    public async Task<IReadOnlyList<WholesaleApplicationData>> ApplicationsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var c = await access.RequireAsync(db, ct);
        return (await db.WholesaleApplications.AsNoTracking().Where(x => x.CustomerId == c.Id).OrderByDescending(x => x.SubmittedAt).Take(100).ToListAsync(ct))
            .Select(WholesaleWorkflow.Data).ToArray();
    }
    /// <summary>Возвращает публичную историю собственных решений.</summary>
    public async Task<IReadOnlyList<WholesaleHistory>> HistoryAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var c = await access.RequireAsync(db, ct);
        return await db.WholesaleDecisions.AsNoTracking().Where(x => x.CustomerId == c.Id).OrderByDescending(x => x.OccurredAt).Take(100)
            .Select(x => new WholesaleHistory(x.OccurredAt, x.Outcome, x.PublicReason, x.ApplicationId == null)).ToListAsync(ct);
    }
    /// <summary>Создаёт снимок под блокировкой покупателя, сохраняя одну ожидающую заявку.</summary>
    public async Task SubmitAsync(string? comment, Guid operationId, CancellationToken ct = default)
    {
        if (operationId == Guid.Empty) throw new ArgumentException("Отсутствует идентификатор операции.");
        comment = AdminAccess.Optional(comment);
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var c = await access.RequireAsync(db, ct); c = await WholesaleWorkflow.LockAsync(db, c.Id, ct);
        var prior = await db.WholesaleApplications.AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == operationId, ct);
        if (prior is not null) { if (prior.CustomerId != c.Id) throw new UnauthorizedAccessException("Недоступная операция."); return; }
        if (c.WholesaleStatus == WholesaleStatus.Approved) throw new ArgumentException("Опт уже одобрен.");
        if (await db.WholesaleApplications.AnyAsync(x => x.CustomerId == c.Id && x.Status == WholesaleApplicationStatus.Pending, ct))
            throw new ArgumentException("Заявка уже ожидает проверки. Для изменения сначала отзовите её.");
        var org = await db.OrganizationProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.CustomerId == c.Id, ct);
        WholesaleWorkflow.ValidateLegal(c.Kind, org?.LegalName, org?.Inn, org?.Kpp);
        db.WholesaleApplications.Add(new() { CustomerId = c.Id, Kind = c.Kind, LegalName = org?.LegalName, Inn = org?.Inn, Kpp = org?.Kpp,
            Comment = comment, OperationId = operationId, Status = WholesaleApplicationStatus.Pending });
        c.Segment = CustomerSegment.Retail; c.WholesaleStatus = WholesaleStatus.Pending;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    /// <summary>Отзывает только свою ожидающую заявку; повтор команды не создаёт историю повторно.</summary>
    public async Task WithdrawAsync(Guid id, uint version, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var c = await access.RequireAsync(db, ct); c = await WholesaleWorkflow.LockAsync(db, c.Id, ct);
        if (await WholesaleWorkflow.PriorAsync(db, c.Id, operationId, ct)) return;
        var application = await db.WholesaleApplications.SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == c.Id, ct)
            ?? throw new UnauthorizedAccessException("Заявка недоступна.");
        AdminAccess.Version(application, version);
        if (application.Status != WholesaleApplicationStatus.Pending) throw new ArgumentException("Можно отозвать только ожидающую заявку.");
        WholesaleWorkflow.Decide(db, c, application, WholesaleApplicationStatus.Withdrawn, c.ApplicationUserId!, "Заявка отозвана покупателем.", operationId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    /// <summary>Проверяет публичность варианта и право текущей сессии до обращения к общему сервису цены.</summary>
    public async Task<CustomerPrice?> PriceAsync(Guid productId, Guid variantId, int quantity, CancellationToken ct = default)
    {
        if (quantity is < 1 or > 1000000) throw new ArgumentException("Количество должно быть целым числом от 1 до 1 000 000.");
        await using var db = await factory.CreateDbContextAsync(ct);
        Customer c;
        try { c = await access.RequireAsync(db, ct); } catch (UnauthorizedAccessException) { return null; }
        if (PriceCalculator.AvailableSegment(c) != CustomerSegment.Wholesale) return null;
        if (!await db.ProductVariants.AnyAsync(v => v.Id == variantId && v.ProductId == productId && db.Products.Any(p => p.Id == productId && p.Status == ProductStatus.Published)
            && db.SalePrices.Any(p => p.ProductVariantId == v.Id && p.Segment == CustomerSegment.Retail && p.MinimumQuantity == 1 && p.Currency == "RUB" && p.Amount > 0), ct)) return null;
        var tiers = await db.SalePrices.AsNoTracking().Where(x => x.ProductVariantId == variantId && x.Segment == CustomerSegment.Wholesale && x.Currency == "RUB" && x.Amount > 0)
            .OrderBy(x => x.MinimumQuantity).Select(x => new WholesaleTier(x.MinimumQuantity, x.Amount)).ToListAsync(ct);
        decimal? amount = null;
        if (tiers.Any(x => x.MinimumQuantity <= quantity))
        {
            var quote = await pricing.QuoteAsync(c.ApplicationUserId, variantId, quantity, ct);
            if (quote.Segment != CustomerSegment.Wholesale) return null;
            amount = quote.Amount;
        }
        // Смена stamp, блокировка или отзыв опта во время чтения не оставляют разрешение от первого запроса.
        db.ChangeTracker.Clear();
        try { c = await access.RequireAsync(db, ct); } catch (UnauthorizedAccessException) { return null; }
        if (PriceCalculator.AvailableSegment(c) != CustomerSegment.Wholesale) return null;
        return new(amount, tiers);
    }
}
