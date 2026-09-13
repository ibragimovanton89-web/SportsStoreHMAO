using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Admin;

/// <summary>Удаление только неиспользуемых товаров с сохранением бизнес-истории.</summary>
public sealed partial class AdminCatalog
{
    /// <summary>Атомарно удаляет карточку и её варианты; данные закупок, заказов и цен не удаляются каскадно.</summary>
    /// <param name="id">Карточка собственного каталога.</param>
    /// <param name="version">Ожидаемая версия карточки.</param>
    /// <param name="operationId">Ключ повтора операции из интерфейса.</param>
    /// <param name="ct">Отмена транзакции.</param>
    public async Task DeleteProductAsync(Guid id, uint version, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = await access.RequireAsync(db, true, ct);
        // Общая блокировка с приёмкой не допускает проверки пустого склада одновременно с зачислением товара.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(861100)", ct);
        if (await PriorAsync(db, actor, operationId, ct) is not null) return;
        var product = await EditableAsync(db, id, ct);
        AdminAccess.Version(product, version);
        var variants = await db.ProductVariants.Where(x => x.ProductId == id).ToListAsync(ct);
        var ids = variants.Select(x => x.Id).ToArray();
        var balances = await db.InventoryBalances.Where(x => ids.Contains(x.ProductVariantId)).ToListAsync(ct);
        if (balances.Any(x => x.OnHand != 0 || x.Reserved != 0))
            throw new InvalidOperationException("Товар имеет остаток или резерв на складе. Удаление не выполняет списание. Можно перевести карточку в архив.");
        if (await db.SupplierOffers.AnyAsync(x => x.ProductVariantId != null && ids.Contains(x.ProductVariantId.Value), ct)
            || await db.PurchaseReceiptLines.AnyAsync(x => x.ProductVariantId != null && ids.Contains(x.ProductVariantId.Value), ct)
            || await db.OrderItems.AnyAsync(x => x.ProductVariantId != null && ids.Contains(x.ProductVariantId.Value), ct)
            || await db.SalePriceHistories.AnyAsync(x => ids.Contains(x.ProductVariantId), ct)
            || await db.PriceProposals.AnyAsync(x => ids.Contains(x.ProductVariantId), ct))
            throw new InvalidOperationException("Товар связан с закупками, заказами или историей цен. Чтобы сохранить историю, переведите карточку в архив через «Карточка и цена».");
        db.SalePrices.RemoveRange(await db.SalePrices.Where(x => ids.Contains(x.ProductVariantId)).ToListAsync(ct));
        db.MarkupRules.RemoveRange(await db.MarkupRules.Where(x => x.ProductVariantId != null && ids.Contains(x.ProductVariantId.Value)).ToListAsync(ct));
        db.InventoryBalances.RemoveRange(balances);
        // Удаляются записи изображений. Файлы не удаляются до коммита и не выдаются без записи ProductImage.
        db.ProductImages.RemoveRange(await db.ProductImages.Where(x => x.ProductId == id).ToListAsync(ct));
        db.ProductVariants.RemoveRange(variants);
        db.Products.Remove(product);
        AdminAccess.Audit(db, actor, operationId, "product.delete", "Product", id.ToString(), $"Удалён товар «{product.Name}» и вариантов: {variants.Count}.");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
