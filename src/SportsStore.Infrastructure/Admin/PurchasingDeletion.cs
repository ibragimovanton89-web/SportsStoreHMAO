using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Удаление ошибочных закупок с защитой фактического склада и сохранением аудита.</summary>
public sealed partial class Purchasing
{
    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, uint version, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, true, ct);
        // Та же блокировка, что у приёмки: удаление и поступление не могут пройти одновременно.
        await LockAsync(db, ct);
        if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) return;
        var order = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Заказ уже удалён или не найден. Обновите список.");
        AdminAccess.Version(order, version);
        if (order.Status is not (PurchaseStatus.Draft or PurchaseStatus.Created or PurchaseStatus.Cancelled))
            throw new InvalidOperationException("Переданный заказ сначала отмените. Заказ с приёмкой удалять нельзя.");
        var lines = await db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == id).ToListAsync(ct);
        var lineIds = lines.Select(x => x.Id).ToArray();
        if (lines.Any(x => x.ReceivedQuantity > 0) || await db.PurchaseReceiptLines.AnyAsync(x => lineIds.Contains(x.PurchaseOrderLineId), ct))
            throw new InvalidOperationException("По заказу уже принят товар. Удаление запрещено для сохранения истории склада.");
        var total = lines.Sum(x => x.Quantity * (x.UnitPrice ?? 0));
        AdminAccess.Audit(db, actor, operationId, "purchase.delete", "PurchaseOrder", id.ToString(),
            $"Удалён заказ {order.Number}; статус {order.Status}; строк {lines.Count}; сумма {total:0.00} RUB. Приёмок не было.");
        db.PurchaseOrderLines.RemoveRange(lines);
        db.PurchaseOrders.Remove(order);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
