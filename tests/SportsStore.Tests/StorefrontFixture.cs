using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Persistence;
namespace SportsStore.Tests;

/// <summary>Синтетические товары только для изолированных тестовых БД; не отражают ассортимент или условия магазина.</summary>
public static class StorefrontFixture
{
    /// <summary>Идентификаторы демонстрационной карточки, вариантов и дерева.</summary>
    /// <param name="Product">Публичная карточка.</param><param name="RedS">Красный S на складе.</param><param name="RedM">Красный M без остатка.</param><param name="BlueM">Синий M без остатка.</param><param name="Root">Родительская категория.</param><param name="Brand">Публичный бренд.</param><param name="Hidden">Скрытая карточка.</param><param name="Image">Первое изображение.</param>
    public sealed record Ids(Guid Product, Guid RedS, Guid RedM, Guid BlueM, Guid Root, Guid Brand, Guid Hidden, Guid Image);
    /// <summary>Создаёт демонстрационную страницу из 28 карточек, скрытые данные и варианты с разными ценами.</summary>
    /// <param name="db">Только изолированный тестовый контекст.</param><param name="images">Необязательный отдельный каталог WebP.</param>
    public static async Task<Ids> SeedAsync(ApplicationDbContext db, string? images = null)
    {
        if (!db.Database.GetDbConnection().Database.Contains("_test_", StringComparison.Ordinal)) throw new InvalidOperationException("Нужна отдельная тестовая БД.");
        var brand = new Brand { Name = "ADIDAS" }; var hiddenBrand = new Brand { Name = "Скрытый бренд" };
        var root = new Category { Name = "Спорт" }; var leaf = new Category { Name = "Футболки", ParentId = root.Id }; var hiddenCategory = new Category { Name = "Скрытая категория" };
        db.AddRange(brand, hiddenBrand, root, leaf, hiddenCategory);
        var product = new Product { Name = "Тренировочная футболка ADIDAS — демонстрационная модель для длительных занятий спортом", Description = "Лёгкая тренировочная футболка. Демонстрационная карточка для проверки витрины.\nОписание выводится обычным текстом.", BrandId = brand.Id, CategoryId = leaf.Id, Status = ProductStatus.Published };
        var hidden = new Product { Name = "Секретный товар", BrandId = hiddenBrand.Id, CategoryId = hiddenCategory.Id, Status = ProductStatus.Draft };
        db.AddRange(product, hidden);
        var redS = new ProductVariant { ProductId = product.Id, Sku = "00001234", ManufacturerCode = "JD8036", SaleUnit = "шт", Size = "S", Color = "Красный" };
        var redM = new ProductVariant { ProductId = product.Id, Sku = "DEMO-RED-M", SaleUnit = "шт", Size = "M", Color = "Красный" };
        var blueM = new ProductVariant { ProductId = product.Id, Sku = "DEMO-BLUE-M", SaleUnit = "шт", Size = "M", Color = "Синий" };
        db.AddRange(redS, redM, blueM);
        foreach (var (variant, price) in new[] { (redS, 1000m), (redM, 1500m), (blueM, 2000m) }) db.SalePrices.Add(new() { ProductVariantId = variant.Id, Amount = price, IsManual = true, Source = "Демонстрационная цена" });
        db.SalePrices.Add(new() { ProductVariantId = redS.Id, Segment = CustomerSegment.Wholesale, Amount = 777, Source = "Не публиковать" });
        var warehouse = new Warehouse { Name = "Тестовый склад" }; db.Add(warehouse); db.InventoryBalances.Add(new() { WarehouseId = warehouse.Id, ProductVariantId = redS.Id, OnHand = 10, Reserved = 3 });
        var picture = new ProductImage { ProductId = product.Id }; picture.Url = $"/media/{picture.Id:N}.webp"; db.Add(picture);
        var second = new ProductImage { ProductId = product.Id, SortOrder = 1 }; second.Url = $"/media/{second.Id:N}.webp"; db.Add(second);
        for (var index = 0; index < 27; index++)
        {
            var p = new Product { Name = $"Мяч тренировочный {index:00} 100%_Sport", BrandId = brand.Id, CategoryId = root.Id, Status = ProductStatus.Published, CreatedAt = DateTime.UnixEpoch.AddDays(index) };
            var v = new ProductVariant { ProductId = p.Id, Sku = $"BALL-{index:00}", SaleUnit = "шт" }; db.AddRange(p, v); db.SalePrices.Add(new() { ProductVariantId = v.Id, Amount = 2500 + index });
            var img = new ProductImage { ProductId = p.Id }; img.Url = $"/media/{img.Id:N}.webp"; db.Add(img);
        }
        var hiddenVariant = new ProductVariant { ProductId = hidden.Id, Sku = "SECRET", SaleUnit = "шт", Size = "Тайный размер", Color = "Тайный цвет" }; db.Add(hiddenVariant); db.SalePrices.Add(new() { ProductVariantId = hiddenVariant.Id, Amount = 1 });
        await db.SaveChangesAsync();
        if (images is not null)
        {
            Directory.CreateDirectory(images);
            foreach (var item in await db.ProductImages.ToArrayAsync())
            {
                using var bitmap = new SKBitmap(800, 900); using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColor.Parse("#f0f3e9"));
                using var paint = new SKPaint { Color = item == picture ? SKColor.Parse("#cb3d40") : SKColor.Parse("#264f85"), IsAntialias = true };
                using var path = new SKPath(); path.MoveTo(280, 180); path.LineTo(160, 245); path.LineTo(90, 405); path.LineTo(215, 455); path.LineTo(260, 355); path.LineTo(250, 725); path.LineTo(550, 725); path.LineTo(540, 355); path.LineTo(585, 455); path.LineTo(710, 405); path.LineTo(640, 245); path.LineTo(520, 180); path.CubicTo(490, 280, 310, 280, 280, 180); path.Close(); if (item.ProductId == product.Id) canvas.DrawPath(path, paint); else { paint.Color = SKColor.Parse("#d4ef73"); canvas.DrawCircle(400, 450, 250, paint); paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 12; paint.Color = SKColor.Parse("#253722"); canvas.DrawCircle(400, 450, 250, paint); canvas.DrawLine(150, 450, 650, 450, paint); canvas.DrawLine(400, 200, 400, 700, paint); }
                using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Webp, 90); await File.WriteAllBytesAsync(Path.Combine(images, $"{item.Id:N}.webp"), data.ToArray());
            }
        }
        return new(product.Id, redS.Id, redM.Id, blueM.Id, root.Id, brand.Id, hidden.Id, picture.Id);
    }
}
