using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using SportsStore.Application.Admin;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Проверяет фотографии настоящим декодером и сохраняет перекодированный WebP вне исходников.</summary>
/// <param name="factory">Фабрика контекстов.</param><param name="access">Проверка прав.</param><param name="configuration">Внешнее хранилище.</param>
public sealed class AdminImages(IDbContextFactory<ApplicationDbContext> factory, AdminAccess access, IConfiguration configuration) : IAdminImages
{
    /// <summary>Предел входной фотографии: 5 МиБ.</summary>
    public const int MaxBytes = 5 * 1024 * 1024;
    /// <summary>Не более 16 мегапикселей и 8000 пикселей по каждой стороне до декодирования.</summary>
    public const long MaxPixels = 16_000_000;
    /// <summary>Возвращает снимок метаданных без загрузки файлов.</summary>
    public async Task<IReadOnlyList<ImageData>> ListAsync(Guid productId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await access.RequireAsync(db, false, ct);
        return await db.ProductImages.AsNoTracking().Where(x => x.ProductId == productId).OrderBy(x => x.SortOrder).Take(20)
            .Select(x => new ImageData(x.Id, x.Version, x.Url, x.SortOrder)).ToListAsync(ct);
    }
    /// <summary>Читает ограниченный поток, проверяет формат и размеры, декодирует и перекодирует без исходных метаданных.</summary>
    public static async Task<byte[]> SanitizeAsync(Stream input, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream(); var bytes = new byte[81920]; int read;
        while ((read = await input.ReadAsync(bytes, ct)) > 0)
        {
            if (buffer.Length + read > MaxBytes) throw new InvalidDataException("Фотография превышает 5 МиБ.");
            buffer.Write(bytes, 0, read);
        }
        buffer.Position = 0;
        using var managed = new SKManagedStream(buffer); using var codec = SKCodec.Create(managed);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new InvalidDataException("Допускаются только настоящие JPEG, PNG и WebP.");
        var info = codec.Info;
        if (info.Width < 1 || info.Height < 1 || info.Width > 8000 || info.Height > 8000 || (long)info.Width * info.Height > MaxPixels || codec.FrameCount > 1)
            throw new InvalidDataException("Допускается статичное изображение до 8000 пикселей по стороне и до 16 мегапикселей.");
        using var bitmap = new SKBitmap(new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success) throw new InvalidDataException("Файл изображения повреждён.");
        ct.ThrowIfCancellationRequested(); using var image = SKImage.FromBitmap(bitmap); using var encoded = image.Encode(SKEncodedImageFormat.Webp, 90);
        return encoded.ToArray();
    }
    /// <summary>Проверяет права на карточку при каждой операции изображения.</summary>
    private async Task<Product> EditableAsync(ApplicationDbContext db, Guid productId, CancellationToken ct)
    {
        var product = await db.Products.SingleAsync(x => x.Id == productId, ct);
        await access.RequireAsync(db, product.Status != ProductStatus.Draft, ct); return product;
    }
    /// <summary>Записывает файл до фиксации метаданных; при отказе БД файл удаляется.</summary>
    public async Task UploadAsync(Guid productId, uint productVersion, Stream content, Guid operationId, CancellationToken ct = default)
    {
        await using (var check = await factory.CreateDbContextAsync(ct)) await EditableAsync(check, productId, ct);
        var data = await SanitizeAsync(content, ct); var image = new ProductImage { ProductId = productId };
        var folder = Path.Combine(AdminImport.StorageRoot(configuration), "images"); Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, image.Id.ToString("N") + ".webp"); bool committed = false;
        try
        {
            await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var actor = await access.RequireAsync(db, false, ct); if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) return;
            var product = await EditableAsync(db, productId, ct); AdminAccess.Version(product, productVersion);
            if (await db.ProductImages.CountAsync(x => x.ProductId == productId, ct) >= 20) throw new ArgumentException("Допускается до 20 фотографий карточки.");
            image.SortOrder = (await db.ProductImages.Where(x => x.ProductId == productId).MaxAsync(x => (int?)x.SortOrder, ct) ?? -1) + 1;
            image.Url = "/media/" + image.Id.ToString("N") + ".webp";
            await File.WriteAllBytesAsync(path, data, ct); product.UpdatedAt = DateTime.UtcNow; db.ProductImages.Add(image);
            AdminAccess.Audit(db, actor, operationId, "image.upload", "ProductImage", image.Id.ToString(), "Загружено проверенное и перекодированное изображение.");
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); committed = true;
        }
        finally { if (!committed && File.Exists(path)) File.Delete(path); }
    }
    /// <summary>Удаляет метаданные и файл; последнюю фотографию опубликованной карточки удалить нельзя.</summary>
    public async Task DeleteAsync(Guid id, uint version, Guid operationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var actor = await access.RequireAsync(db, false, ct); if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) return;
        var image = await db.ProductImages.SingleAsync(x => x.Id == id, ct); AdminAccess.Version(image, version); var product = await EditableAsync(db, image.ProductId, ct);
        if (product.Status == ProductStatus.Published && await db.ProductImages.CountAsync(x => x.ProductId == product.Id, ct) <= 1)
            throw new InvalidOperationException("Сначала загрузите другую фотографию или верните карточку в черновик.");
        product.UpdatedAt = DateTime.UtcNow; db.ProductImages.Remove(image);
        AdminAccess.Audit(db, actor, operationId, "image.delete", "ProductImage", id.ToString(), "Удалено изображение карточки.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        var path = Path.Combine(AdminImport.StorageRoot(configuration), "images", id.ToString("N") + ".webp");
        if (File.Exists(path)) File.Delete(path);
    }
    /// <summary>Меняет соседние позиции в транзакции с временным порядком для соблюдения уникального индекса.</summary>
    public async Task MoveAsync(Guid id, uint version, int direction, Guid operationId, CancellationToken ct = default)
    {
        if (direction is not (-1 or 1)) throw new ArgumentException("Неверное направление.");
        await using var db = await factory.CreateDbContextAsync(ct); await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var actor = await access.RequireAsync(db, false, ct); if (await AdminCatalog.PriorAsync(db, actor, operationId, ct) is not null) return;
        var image = await db.ProductImages.SingleAsync(x => x.Id == id, ct); AdminAccess.Version(image, version); var product = await EditableAsync(db, image.ProductId, ct);
        var q = db.ProductImages.Where(x => x.ProductId == product.Id && (direction < 0 ? x.SortOrder < image.SortOrder : x.SortOrder > image.SortOrder));
        var other = direction < 0 ? await q.OrderByDescending(x => x.SortOrder).FirstOrDefaultAsync(ct) : await q.OrderBy(x => x.SortOrder).FirstOrDefaultAsync(ct);
        if (other is null) return;
        var old = image.SortOrder; var next = other.SortOrder;
        image.SortOrder = int.MaxValue; await db.SaveChangesAsync(ct); other.SortOrder = old; await db.SaveChangesAsync(ct); image.SortOrder = next;
        product.UpdatedAt = DateTime.UtcNow;
        AdminAccess.Audit(db, actor, operationId, "image.move", "ProductImage", id.ToString(), "Изменён порядок фотографий.");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
