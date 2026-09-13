using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SportsStore.Application.Commerce;
using SportsStore.Application.Customers;
using SportsStore.Application.Orders;
using SportsStore.Application.Pricing;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Admin;
using SportsStore.Infrastructure.Customers;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Pricing;
namespace SportsStore.Infrastructure.Commerce;
/// <summary>Корзина и оформление с одной транзакцией на команду; резерв не изменяет физическое OnHand.</summary>
/// <param name="factory">Короткие независимые контексты.</param><param name="customerAccess">Проверка покупателя.</param>
/// <param name="identity">Серверная сессия.</param><param name="guest">Защищённый гостевой секрет.</param>
/// <param name="admin">Права сотрудника с MFA.</param><param name="clock">Тестируемое UTC-время.</param><param name="configuration">Явные сроки резерва.</param>
public sealed partial class CommerceService(IDbContextFactory<ApplicationDbContext> factory, CustomerAccess customerAccess,
    ICustomerIdentity identity, IGuestCartIdentity guest, AdminAccess admin, TimeProvider clock, IConfiguration configuration) : ICommerce
{
    /// <summary>Ограничение строк активной корзины; объединение сверх него требует удаления лишних строк.</summary>
    internal const int MaximumLines = 100;
    /// <summary>Ограничение целого количества одного SKU.</summary>
    internal const int MaximumQuantity = 10000;
    /// <summary>UTC-момент из тестируемого источника.</summary>
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    /// <summary>Сериализуемая команда; 861100 берётся до бизнес-строк, совместно с приёмкой/удалением. Повторяется вся операция, не отдельный SaveChanges.</summary>
    private async Task<T> Transaction<T>(Func<ApplicationDbContext, Task<T>> work, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861100)", ct);
                var result = await work(db); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return result;
            }
            catch (Exception ex) when (attempt < 2 && (ex is PostgresException { SqlState: "40001" or "40P01" } || ex.InnerException is PostgresException { SqlState: "40001" or "40P01" }))
            { await tx.RollbackAsync(ct); await Task.Delay(30 * (attempt + 1), ct); }
        }
    }
    /// <summary>Отличает гостя от недействительной cookie; последняя не получает доступ к гостевым данным.</summary>
    private async Task<Customer?> Buyer(ApplicationDbContext db, CancellationToken ct)
    {
        var s = await identity.GetAsync(ct);
        return string.IsNullOrEmpty(s.UserId) || s.Local ? null : await customerAccess.RequireAsync(db, ct);
    }
    /// <summary>Находит корзину только по доверенному владельцу; при команде создаёт новую активную корзину.</summary>
    private async Task<Cart?> Resolve(ApplicationDbContext db, Customer? buyer, bool create, CancellationToken ct)
    {
        var hash = guest.KeyHash;
        Cart? cart = buyer is not null ? await db.Carts.SingleOrDefaultAsync(x => x.CustomerId == buyer.Id && x.State == CartState.Active, ct)
            : hash is null ? null : await db.Carts.SingleOrDefaultAsync(x => x.GuestKeyHash == hash && x.State == CartState.Active, ct);
        if (cart is not null || !create) return cart;
        if (buyer is null && (hash is null || await db.Carts.AnyAsync(x => x.GuestKeyHash == hash, ct)))
            throw new ArgumentException("Гостевая корзина уже объединена. Обновите страницу, чтобы начать новую.");
        cart = new() { CustomerId = buyer?.Id, GuestKeyHash = buyer is null ? hash : null, CreatedAt = Now, UpdatedAt = Now };
        db.Carts.Add(cart); return cart;
    }
    /// <summary>Пересчитывает публичные строки пакетами; скрытая карточка не раскрывает название или SKU.</summary>
    private static async Task<CartView> View(ApplicationDbContext db, Cart? cart, Customer? buyer, CancellationToken ct)
    {
        var segment = PriceCalculator.AvailableSegment(buyer);
        if (cart is null) return new(Guid.Empty, 0, segment, [], 0);
        var items = await db.CartItems.AsNoTracking().Where(x => x.CartId == cart.Id).OrderBy(x => x.ProductVariantId).ToListAsync(ct);
        var ids = items.Select(x => x.ProductVariantId).ToArray();
        var variants = await (from v in db.ProductVariants.AsNoTracking() join p in db.Products.AsNoTracking() on v.ProductId equals p.Id
            where ids.Contains(v.Id) && p.Status == ProductStatus.Published && db.SalePrices.Any(s => s.ProductVariantId == v.Id && s.Segment == CustomerSegment.Retail && s.MinimumQuantity == 1 && s.Currency == "RUB" && s.Amount > 0)
            select new { Variant = v, Product = p }).ToListAsync(ct);
        var prices = await SalePriceSelection.Eligible(db, segment).AsNoTracking().Where(x => ids.Contains(x.ProductVariantId)).ToListAsync(ct);
        var stock = await db.InventoryBalances.AsNoTracking().Where(x => ids.Contains(x.ProductVariantId)).GroupBy(x => x.ProductVariantId)
            .Select(g => new { Id = g.Key, Available = g.Sum(x => x.OnHand - x.Reserved) }).ToDictionaryAsync(x => x.Id, x => x.Available, ct);
        var lines = items.Select(i =>
        {
            var v = variants.FirstOrDefault(x => x.Variant.Id == i.ProductVariantId); var price = SalePriceSelection.Choose(prices, i.ProductVariantId, i.Quantity);
            var available = v is null ? 0 : stock.GetValueOrDefault(i.ProductVariantId);
            var error = v is null ? "Товар больше недоступен. Удалите строку." : i.Quantity > MaximumQuantity || items.Count > MaximumLines ? "Превышен лимит корзины. Исправьте количество или удалите лишние строки."
                : price is null ? "Для этого количества нет цены выбранного сегмента." : available < i.Quantity ? "Недостаточно товара на собственном складе. Уменьшите количество." : null;
            return new CartLine(i.ProductVariantId, v?.Product.Id, v?.Product.Name ?? "Недоступная позиция", v?.Variant.Sku ?? "", v?.Variant.Size, v?.Variant.Color, v?.Variant.SaleUnit ?? "", i.Quantity, available, v is null ? null : price?.Amount, v is null ? null : price?.MinimumQuantity, error);
        }).ToArray();
        return new(cart.Id, cart.Version, segment, lines, lines.Any(x => x.Error is not null) ? null : lines.Sum(x => x.Quantity * x.Price!.Value));
    }
    /// <inheritdoc />
    public async Task<CartView> CartAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var buyer = await Buyer(db, ct);
        return await View(db, await Resolve(db, buyer, false, ct), buyer, ct);
    }
    /// <inheritdoc />
    public Task AddAsync(Guid variantId, int quantity, Guid operationId, CancellationToken ct = default) => Transaction(async db =>
    {
        if (operationId == Guid.Empty || quantity is < 1 or > MaximumQuantity) throw new ArgumentException("Проверьте количество и идентификатор добавления.");
        var buyer = await Buyer(db, ct); var cart = (await Resolve(db, buyer, true, ct))!;
        var prior = await db.CartOperations.SingleOrDefaultAsync(x => x.OperationId == operationId, ct);
        if (prior is not null) { if (prior.CartId != cart.Id || prior.VariantId != variantId || prior.Quantity != quantity) throw new ArgumentException("Ключ добавления уже использован другой командой."); return true; }
        var item = await db.CartItems.SingleOrDefaultAsync(x => x.CartId == cart.Id && x.ProductVariantId == variantId, ct);
        if (item is null) { if (await db.CartItems.CountAsync(x => x.CartId == cart.Id, ct) >= MaximumLines) throw new ArgumentException("Не более 100 строк в корзине."); item = new() { CartId = cart.Id, ProductVariantId = variantId }; db.CartItems.Add(item); }
        item.Quantity += quantity; cart.UpdatedAt = Now;
        if (db.Entry(cart).State != EntityState.Added) db.Entry(cart).Property(x => x.UpdatedAt).IsModified = true;
        await db.SaveChangesAsync(ct);
        var view = await View(db, cart, buyer, ct); var line = view.Lines.Single(x => x.VariantId == variantId);
        if (line.ProductId is null || line.Quantity > MaximumQuantity || line.Available < line.Quantity) throw new ArgumentException(line.Error ?? "Позиция недоступна.");
        db.CartOperations.Add(new() { CartId = cart.Id, OperationId = operationId, VariantId = variantId, Quantity = quantity }); return true;
    }, ct);
    /// <inheritdoc />
    public Task SetAsync(Guid variantId, int quantity, uint version, CancellationToken ct = default) => Transaction(async db =>
    {
        if (quantity is < 0 or > MaximumQuantity) throw new ArgumentException("Количество должно быть от 0 до 10 000.");
        var buyer = await Buyer(db, ct); var cart = await Resolve(db, buyer, false, ct) ?? throw new ArgumentException("Корзина пуста.");
        AdminAccess.Version(cart, version);
        var item = await db.CartItems.SingleOrDefaultAsync(x => x.CartId == cart.Id && x.ProductVariantId == variantId, ct) ?? throw new ArgumentException("Строка отсутствует.");
        if (quantity == 0) db.CartItems.Remove(item); else item.Quantity = quantity;
        cart.UpdatedAt = Now; db.Entry(cart).Property(x => x.UpdatedAt).IsModified = true; return true;
    }, ct);
    /// <inheritdoc />
    public Task MergeAsync(CancellationToken ct = default) => Transaction(async db =>
    {
        var buyer = await customerAccess.RequireAsync(db, ct); var hash = guest.KeyHash;
        if (hash is null) return true;
        var source = await db.Carts.SingleOrDefaultAsync(x => x.GuestKeyHash == hash, ct);
        if (source is null || source.State != CartState.Active) return true;
        var target = (await Resolve(db, buyer, true, ct))!;
        var items = await db.CartItems.Where(x => x.CartId == target.Id).ToListAsync(ct);
        foreach (var old in await db.CartItems.Where(x => x.CartId == source.Id).ToListAsync(ct))
        {
            var item = items.SingleOrDefault(x => x.ProductVariantId == old.ProductVariantId);
            if (item is null) db.CartItems.Add(new() { CartId = target.Id, ProductVariantId = old.ProductVariantId, Quantity = old.Quantity }); else item.Quantity += old.Quantity;
        }
        source.State = CartState.Merged; source.UpdatedAt = target.UpdatedAt = Now;
        if (db.Entry(target).State != EntityState.Added) db.Entry(target).Property(x => x.UpdatedAt).IsModified = true; return true;
    }, ct);
    /// <summary>Снимок условий, сериализуемый только в закрытый preview; версии и текущий остаток намеренно не входят в отпечаток.</summary>
    /// <param name="CartId">Исходная корзина.</param><param name="AddressId">Выбранный адрес.</param><param name="Kind">Правовой тип.</param><param name="View">Условия покупателя.</param>
    private sealed record Conditions(Guid CartId, Guid AddressId, CustomerKind Kind, PreviewView View);
    /// <summary>Собирает подтверждаемые условия в текущей транзакции и проверяет принадлежность адреса.</summary>
    private async Task<Conditions> ConditionsAsync(ApplicationDbContext db, Customer c, Cart cart, Guid addressId, CancellationToken ct)
    {
        var view = await View(db, cart, c, ct);
        if (view.Lines.Count == 0 || view.Total is null) throw new ArgumentException("Корзина пуста или содержит строки, требующие исправления.");
        var address = await db.CustomerAddresss.SingleOrDefaultAsync(x => x.Id == addressId && x.CustomerId == c.Id, ct) ?? throw new ArgumentException("Выберите собственный сохранённый адрес.");
        var user = await db.Users.SingleAsync(x => x.Id == c.ApplicationUserId, ct);
        var org = await db.OrganizationProfiles.SingleOrDefaultAsync(x => x.CustomerId == c.Id, ct);
        if (string.IsNullOrWhiteSpace(user.PhoneNumber)) throw new ArgumentException("Укажите контактный телефон в профиле.");
        // Свободный остаток проверен выше, но его изменение при достаточном количестве не требует нового согласия.
        view = view with { Version = 0, Lines = view.Lines.Select(x => x with { Available = 0 }).ToArray() };
        return new(cart.Id, addressId, c.Kind, new(Guid.Empty, default, view, c.DisplayName, user.Email!, user.PhoneNumber, address.Recipient, address.Address, address.PostalCode, org?.LegalName, org?.Inn, org?.Kpp));
    }
    /// <summary>Хеширует точные серверные условия; не журналирует персональные данные.</summary>
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    /// <summary>Сохраняет preview на 15 минут без изменения корзины или резерва.</summary>
    private CheckoutPreview SavePreview(ApplicationDbContext db, Customer c, Cart cart, Conditions conditions)
    {
        var json = JsonSerializer.Serialize(conditions);
        var p = new CheckoutPreview { CustomerId = c.Id, CartId = cart.Id, CartVersion = cart.Version, AddressId = conditions.AddressId, ConditionsJson = json, Fingerprint = Hash(json), CreatedAt = Now, ExpiresAt = Now.AddMinutes(15) };
        db.CheckoutPreviews.Add(p); return p;
    }
    /// <inheritdoc />
    public Task<PreviewView> PreviewAsync(Guid addressId, CancellationToken ct = default) => Transaction(async db =>
    {
        var c = await customerAccess.RequireAsync(db, ct); var cart = await Resolve(db, c, false, ct) ?? throw new ArgumentException("Корзина пуста.");
        var conditions = await ConditionsAsync(db, c, cart, addressId, ct); var p = SavePreview(db, c, cart, conditions);
        return conditions.View with { Id = p.Id, ExpiresAt = p.ExpiresAt };
    }, ct);
    /// <inheritdoc />
    public async Task<PreviewView> PreviewAsync(Guid previewId, bool saved, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var c = await customerAccess.RequireAsync(db, ct);
        var p = await db.CheckoutPreviews.AsNoTracking().SingleOrDefaultAsync(x => x.Id == previewId && x.CustomerId == c.Id, ct) ?? throw new UnauthorizedAccessException();
        return JsonSerializer.Deserialize<Conditions>(p.ConditionsJson)!.View with { Id = p.Id, ExpiresAt = p.ExpiresAt };
    }
    /// <inheritdoc />
    public async Task<CheckoutResult> SubmitAsync(Guid previewId, Guid operationId, CancellationToken ct = default)
    {
        try { return await SubmitTransactionAsync(previewId, operationId, ct); }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException || ex.InnerException is NpgsqlException)
        {
            // Разрыв соединения мог произойти после COMMIT: сначала ищем результат по тому же ключу.
            // Второй заказ автоматически не создаётся; при недоступной БД форма сохраняет исходный ключ.
            await using var db = await factory.CreateDbContextAsync(ct);
            var customer = await customerAccess.RequireAsync(db, ct);
            var preview = await db.CheckoutPreviews.AsNoTracking().SingleOrDefaultAsync(x => x.Id == previewId && x.CustomerId == customer.Id, ct);
            var saved = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.CheckoutOperationId == operationId && x.CustomerId == customer.Id, ct);
            if (preview is not null && saved is not null && saved.PreviewId == preview.Id && saved.ConditionsFingerprint == preview.Fingerprint)
                return new(saved.Id, null, null);
            throw;
        }
    }
    /// <summary>Атомарно создаёт заказ и резерв; вызов после неизвестного COMMIT допускается только с исходным ключом.</summary>
    private Task<CheckoutResult> SubmitTransactionAsync(Guid previewId, Guid operationId, CancellationToken ct) => Transaction<CheckoutResult>(async db =>
    {
        if (operationId == Guid.Empty) throw new ArgumentException("Нужен ключ оформления.");
        var c = await customerAccess.RequireAsync(db, ct);
        var p = await db.CheckoutPreviews.SingleOrDefaultAsync(x => x.Id == previewId && x.CustomerId == c.Id, ct) ?? throw new UnauthorizedAccessException();
        var prior = await db.Orders.SingleOrDefaultAsync(x => x.CheckoutOperationId == operationId, ct);
        if (prior is not null)
        {
            if (prior.CustomerId != c.Id || prior.PreviewId != p.Id || prior.ConditionsFingerprint != p.Fingerprint) throw new ArgumentException("Ключ оформления уже использован для других условий.");
            return new(prior.Id, null, null);
        }
        var cart = await db.Carts.SingleAsync(x => x.Id == p.CartId && x.CustomerId == c.Id, ct);
        if (cart.State != CartState.Active) throw new ArgumentException("Эта корзина уже оформлена. Откройте историю заказов.");
        var conditions = await ConditionsAsync(db, c, cart, p.AddressId, ct);
        if (p.ExpiresAt <= Now || Hash(JsonSerializer.Serialize(conditions)) != p.Fingerprint)
        { var updated = SavePreview(db, c, cart, conditions); return new(null, updated.Id, "Условия изменились или истёк срок подтверждения. Проверьте состав, цены и адрес и подтвердите заново."); }
        var hours = configuration.GetValue<double?>("Orders:ReservationHours") ?? 24;
        if (hours <= 0 || hours > 168) throw new InvalidOperationException("Настройте срок резерва от 0 до 168 часов.");
        var v = conditions.View;
        var order = new Order { CustomerId = c.Id, CartId = cart.Id, PreviewId = p.Id, CheckoutOperationId = operationId, ConditionsFingerprint = p.Fingerprint,
            Number = "ПЗ-" + Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(), CreatedAt = Now, UpdatedAt = Now,
            Status = CustomerOrderStatus.AwaitingConfirmation, ReserveUntil = Now.AddHours(hours), GoodsTotal = v.Cart.Total!.Value, SalesFormat = v.Cart.Segment,
            ContactName = v.ContactName, ContactEmail = v.Email, ContactPhone = v.Phone, RecipientName = v.Recipient, ShippingAddress = v.Address, PostalCode = v.PostalCode,
            CustomerKind = conditions.Kind, OrganizationName = v.Organization, Inn = v.Inn, Kpp = v.Kpp };
        db.Orders.Add(order);
        // Глобальная блокировка уже взята. Балансы выбираются и блокируются в едином порядке вариантов/складов.
        var ids = v.Cart.Lines.Select(x => x.VariantId).ToArray();
        var balances = await db.InventoryBalances.FromSqlInterpolated($"SELECT *, xmin FROM \"InventoryBalance\" WHERE \"ProductVariantId\" = ANY({ids}) ORDER BY \"ProductVariantId\", \"WarehouseId\" FOR UPDATE").ToListAsync(ct);
        foreach (var line in v.Cart.Lines)
        {
            var variant = await db.ProductVariants.SingleAsync(x => x.Id == line.VariantId, ct); var product = await db.Products.SingleAsync(x => x.Id == variant.ProductId, ct);
            var item = OrderSnapshots.Item(order.Id, product, variant, line.Quantity, line.Price!.Value, 0); item.MinimumQuantity = line.Tier!.Value; db.OrderItems.Add(item);
            var remaining = line.Quantity;
            foreach (var balance in balances.Where(x => x.ProductVariantId == line.VariantId))
            {
                var take = Math.Min(remaining, balance.OnHand - balance.Reserved); if (take <= 0) continue;
                balance.Reserved += take; remaining -= take;
                db.OrderReservations.Add(new() { OrderId = order.Id, OrderItemId = item.Id, ProductVariantId = variant.Id, WarehouseId = balance.WarehouseId, Quantity = take, CreatedAt = Now });
                if (remaining == 0) break;
            }
            if (remaining > 0) throw new ArgumentException("Остаток изменился. Исправьте количество в корзине.");
        }
        Event(db, order, operationId, "created:" + p.Fingerprint, c.ApplicationUserId!, "Заказ отправлен на подтверждение.");
        cart.State = CartState.Converted; cart.UpdatedAt = Now;
        return new(order.Id, null, null);
    }, ct);
    /// <summary>Записывает историю и ровно одно намерение уведомления в текущей транзакции.</summary>
    private void Event(ApplicationDbContext db, Order order, Guid operation, string fingerprint, string actor, string reason)
    {
        var e = new OrderEvent { OrderId = order.Id, OperationId = operation, CommandFingerprint = fingerprint, Status = order.Status, ReserveUntil = order.ReserveUntil, OccurredAt = Now, ActorId = actor, Reason = reason };
        db.OrderEvents.Add(e); db.OrderNotifications.Add(new() { EventId = e.Id, CreatedAt = clock.GetUtcNow(), NextAttemptAt = clock.GetUtcNow() });
    }
}
