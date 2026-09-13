using SportsStore.Application.Admin;
using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Commerce;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Admin;
using SportsStore.Infrastructure.Persistence;
namespace SportsStore.Infrastructure.Commerce;
/// <summary>Чтение снимков, переходы и освобождение только принадлежащих заказу резервов.</summary>
public sealed partial class CommerceService
{
    /// <inheritdoc />
    public async Task<OrderPage> OrdersAsync(bool staff, string? search, CustomerOrderStatus? status, int page, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var query = db.Orders.AsNoTracking();
        if (staff) await admin.RequireAsync(db, false, ct); else { var c = await customerAccess.RequireAsync(db, ct); query = query.Where(x => x.CustomerId == c.Id); }
        if (status is not null && Enum.IsDefined(status.Value)) query = query.Where(x => x.Status == status);
        search = (search ?? "").Trim(); if (search.Length > 100) search = search[..100];
        if (search.Length > 0) { var pattern = "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%"; query = query.Where(x => EF.Functions.ILike(x.Number, pattern) || EF.Functions.ILike(x.ContactName, pattern)); }
        var count = await query.CountAsync(ct); page = Math.Clamp(page, 1, Math.Max(1, (count + 24) / 25));
        return new(page, count, await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25)
            .Select(x => new OrderListRow(x.Id, x.Number, x.CreatedAt, x.ContactName, x.Status, x.SalesFormat, x.GoodsTotal, x.ReserveUntil)).ToListAsync(ct));
    }
    /// <inheritdoc />
    public async Task<CustomerOrderView> OrderAsync(Guid id, bool staff = false, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var query = db.Orders.AsNoTracking().Where(x => x.Id == id);
        if (staff) await admin.RequireAsync(db, false, ct); else { var c = await customerAccess.RequireAsync(db, ct); query = query.Where(x => x.CustomerId == c.Id); }
        var o = await query.SingleOrDefaultAsync(ct) ?? throw new UnauthorizedAccessException("Заказ недоступен.");
        var lines = await db.OrderItems.AsNoTracking().Where(x => x.OrderId == id).OrderBy(x => x.Sku)
            .Select(x => new OrderLineView(x.Sku, x.Name, x.Size, x.Color, x.SaleUnit, x.Quantity, x.UnitPrice, x.UnitDiscount, x.MinimumQuantity)).ToListAsync(ct);
        var history = await db.OrderEvents.AsNoTracking().Where(x => x.OrderId == id).OrderBy(x => x.OccurredAt).ThenBy(x => x.Id)
            .Select(x => new OrderHistoryView(x.OccurredAt, x.Status, x.ReserveUntil, x.Reason)).ToListAsync(ct);
        List<ReservationView> reservations = [];
        if (staff) reservations = await (from r in db.OrderReservations.AsNoTracking() join w in db.Warehouses on r.WarehouseId equals w.Id
            join i in db.OrderItems on r.OrderItemId equals i.Id where r.OrderId == id orderby i.Sku, w.Name select new ReservationView(i.Sku, w.Name, r.Quantity, r.Active)).ToListAsync(ct);
        return new(o.Id, o.Version, o.Number, o.CreatedAt, o.Status, o.ReserveUntil, o.SalesFormat, o.GoodsTotal, o.ContactName, o.ContactEmail, o.ContactPhone,
            o.RecipientName, o.ShippingAddress, o.PostalCode, o.OrganizationName, o.Inn, o.Kpp, lines, history, reservations);
    }
    /// <inheritdoc />
    public Task ChangeOrderAsync(Guid id, uint version, string action, string reason, DateTime? reserveUntil, Guid operationId, bool staff, CancellationToken ct = default) => Transaction(async db =>
    {
        if (operationId == Guid.Empty || action is not ("confirm" or "cancel" or "extend")) throw new ArgumentException("Неизвестная команда заказа.");
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new UnauthorizedAccessException();
        AdminSession? employee = null; string actor;
        if (staff) { employee = await admin.RequireAsync(db, false, ct); actor = employee.UserId; reason = AdminAccess.Text(reason, "Причина", 500); }
        else { var c = await customerAccess.RequireAsync(db, ct); if (order.CustomerId != c.Id || action != "cancel") throw new UnauthorizedAccessException(); actor = c.ApplicationUserId!; reason = "Отменён покупателем."; }
        var fingerprint = Hash(System.Text.Json.JsonSerializer.Serialize(new { id, action, reason, reserveUntil }));
        var prior = await db.OrderEvents.SingleOrDefaultAsync(x => x.OperationId == operationId, ct);
        if (prior is not null) { if (prior.OrderId != id || prior.CommandFingerprint != fingerprint) throw new ArgumentException("Ключ команды уже использован."); return true; }
        AdminAccess.Version(order, version);
        if (order.Status is not (CustomerOrderStatus.AwaitingConfirmation or CustomerOrderStatus.Confirmed)) throw new ArgumentException("Заказ уже закрыт или является историческим.");
        if (order.ReserveUntil <= Now) throw new ArgumentException("Срок резерва уже истёк. Обновите заказ после обработки истечения.");
        if (!staff && order.Status != CustomerOrderStatus.AwaitingConfirmation) throw new ArgumentException("Подтверждённый заказ отменяет сотрудник.");
        switch (action)
        {
            case "confirm":
                if (order.Status != CustomerOrderStatus.AwaitingConfirmation) throw new ArgumentException("Заказ уже подтверждён.");
                order.Status = CustomerOrderStatus.Confirmed; break;
            case "cancel": order.Status = CustomerOrderStatus.Cancelled; await Release(db, order.Id, Now, ct); break;
            case "extend":
                if (reserveUntil is null || reserveUntil.Value.Kind != DateTimeKind.Utc || reserveUntil <= order.ReserveUntil || reserveUntil > Now.AddDays(7))
                    throw new ArgumentException("Новый срок должен продлевать резерв и быть в пределах семи суток от текущего времени.");
                order.ReserveUntil = reserveUntil; break;
        }
        order.UpdatedAt = Now; Event(db, order, operationId, fingerprint, actor, reason);
        if (employee is not null) AdminAccess.Audit(db, employee, operationId, "customer-order." + action, "Order", id.ToString(), "Изменён статус или срок покупательского заказа.", reason);
        return true;
    }, ct);
    /// <summary>Освобождает только активные распределения этого заказа; прежние и чужие резервы не затрагиваются.</summary>
    private static async Task Release(ApplicationDbContext db, Guid id, DateTime now, CancellationToken ct)
    {
        var reservations = await db.OrderReservations.Where(x => x.OrderId == id && x.Active).OrderBy(x => x.ProductVariantId).ThenBy(x => x.WarehouseId).ToListAsync(ct);
        foreach (var r in reservations)
        {
            var b = await db.InventoryBalances.SingleAsync(x => x.ProductVariantId == r.ProductVariantId && x.WarehouseId == r.WarehouseId, ct);
            if (b.Reserved < r.Quantity) throw new InvalidOperationException("Несогласованность распределения резерва.");
            b.Reserved -= r.Quantity; r.Active = false; r.ReleasedAt = now;
        }
    }
    /// <summary>Обрабатывает один просроченный заказ; повтор и параллельные обработчики не освобождают резерв дважды.</summary>
    public Task<bool> ExpireOneAsync(CancellationToken ct = default) => Transaction(async db =>
    {
        var now = Now;
        var order = await db.Orders.Where(x => (x.Status == CustomerOrderStatus.AwaitingConfirmation || x.Status == CustomerOrderStatus.Confirmed) && x.ReserveUntil <= now)
            .OrderBy(x => x.ReserveUntil).ThenBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (order is null) return false;
        await Release(db, order.Id, now, ct); order.Status = CustomerOrderStatus.Expired; order.UpdatedAt = now;
        Event(db, order, Guid.NewGuid(), "expired", "reservation-worker", "Срок резерва истёк."); return true;
    }, ct);
}
