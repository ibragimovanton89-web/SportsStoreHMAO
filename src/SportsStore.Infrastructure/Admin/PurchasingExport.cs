using Microsoft.EntityFrameworkCore;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Экспорт всех выбранных строк закупки независимо от фильтра и страницы интерфейса.</summary>
public sealed partial class Purchasing
{
    /// <summary>Создаёт Excel по согласованному снимку заказа; не меняет статус, склад или состав.</summary>
    /// <param name="id">Идентификатор закупки.</param>
    /// <param name="version">Версия, которую видел сотрудник; защищает от выгрузки незаметно изменённого заказа.</param>
    /// <param name="ct">Отмена чтения и формирования.</param>
    /// <returns>Готовый файл с кодами поставщика, историческими ценами, количеством и итогами.</returns>
    public async Task<PurchaseExcelFile> ExportExcelAsync(Guid id, uint version, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await access.RequireAsync(db, true, ct);
        // Все изменения закупок используют эту же блокировку: заголовок и строки относятся к одной версии.
        await LockAsync(db, ct);
        var order = await db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ArgumentException("Заказ не найден.");
        AdminAccess.Version(order, version);
        if (order.Status is PurchaseStatus.Draft or PurchaseStatus.Cancelled)
            throw new InvalidOperationException("Excel доступен для созданного или переданного заказа. Подборку сначала сохраните как заказ.");
        var supplier = await db.Suppliers.Where(x => x.Id == order.SupplierId).Select(x => x.Name).SingleAsync(ct);
        var lines = await db.PurchaseOrderLines.AsNoTracking().Where(x => x.PurchaseOrderId == id)
            .OrderBy(x => x.ExternalCode).ThenBy(x => x.Id).Take(1001).ToListAsync(ct);
        if (lines.Count is < 1 or > 1000 || lines.Any(x => x.Quantity <= 0 || x.UnitPrice is null or <= 0))
            throw new InvalidOperationException("Для Excel нужен заказ от 1 до 1000 позиций с количеством и известной положительной ценой.");
        await tx.CommitAsync(ct);
        var bytes = PurchaseExcelWriter.Create(order, supplier, lines, ct);
        if (bytes.Length > 8 * 1024 * 1024) throw new InvalidOperationException("Файл заказа превышает допустимые 8 МиБ.");
        return new($"SportsStoreHMAO-order-{order.CreatedAt:yyyyMMdd}-{order.Id:N}.xlsx", bytes);
    }
}
