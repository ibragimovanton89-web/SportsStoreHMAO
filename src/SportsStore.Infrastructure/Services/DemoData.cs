using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Identity;
using SportsStore.Infrastructure.Persistence;
using SportsStore.Infrastructure.Pricing;
namespace SportsStore.Infrastructure.Services;
/// <summary>Явно учебный набор черновиков и вымышленных покупателей; не задаёт реальные коммерческие условия магазина.</summary>
public static class DemoData
{
    /// <summary>Постоянный идентификатор учебного варианта для идемпотентного создания DEMO.</summary>
    public static readonly Guid VariantId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
    /// <summary>Идемпотентно создаёт вымышленные данные DEMO в транзакции; не публикует товары, не задаёт пароли и общие условия опта.</summary>
    /// <param name="db">Контекст текущей операции; его жизненным циклом управляет вызывающий код.</param>
    public static async Task<object> SeedAsync(ApplicationDbContext db)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        if (await db.ProductVariants.AnyAsync(x => x.Id == VariantId)) return new { VariantId, Existing = true };
        var category = new Category { Name = "DEMO — учебные товары" };
        var product = new Product { Name = "DEMO — учебный мяч", CategoryId = category.Id, Status = ProductStatus.Draft };
        var variant = new ProductVariant { Id = VariantId, ProductId = product.Id, Sku = "DEMO-BALL-001", SaleUnit = "шт" };
        var supplier = new Supplier { Code = "demo", Name = "DEMO — вымышленный поставщик" };
        var tier = new SupplierPriceTier { SupplierId = supplier.Id, Code = "demo", Name = "DEMO", MinimumAmount = 0, SourceConditions = "Demonstration only." };
        var batch = new ImportBatch { SupplierId = supplier.Id, FileName = "DEMO", Sha256 = new string('D',64), ParserVersion = "demo/1",
            SourceDate = new DateOnly(2026,1,1), Status = ImportStatus.Applied, AppliedAt = DateTime.UtcNow };
        var offer = new SupplierOffer { SupplierId = supplier.Id, ExternalCode = "DEMO-001", SourceName = "DEMO",
            SupplierUnit = "шт", SourceDate = batch.SourceDate, LastSeenAt = DateTime.UtcNow,
            ProductVariantId = variant.Id, SaleUnitsPerSupplierUnit = 1, ConversionConfirmed = true };
        db.AddRange(category, product, variant, supplier, tier, batch, offer);
        await db.SaveChangesAsync();
        supplier.SelectedPriceTierId = tier.Id; supplier.PriceTierConfirmed = true;
        variant.PricingSupplierOfferId = offer.Id;
        db.SupplierOfferPrices.Add(new() { SupplierOfferId = offer.Id, SupplierPriceTierId = tier.Id, Amount = 100, ImportBatchId = batch.Id });
        db.MarkupRules.AddRange(
            new() { ProductVariantId = variant.Id, Segment = CustomerSegment.Retail, MarkupPercent = 30 },
            new() { ProductVariantId = variant.Id, Segment = CustomerSegment.Wholesale, MarkupPercent = 20 },
            new() { ProductVariantId = variant.Id, Segment = CustomerSegment.Wholesale, MinimumQuantity = 10, MarkupPercent = 10 });
        // Реальный минимум оптового заказа остаётся незаданным: DEMO не определяет общие коммерческие настройки.
        foreach (var state in new[] { WholesaleStatus.Pending, WholesaleStatus.Approved })
        {
            var user = new ApplicationUser { Id = "demo-" + state, UserName = "demo-" + state, NormalizedUserName = "DEMO-" + state.ToString().ToUpperInvariant() };
            db.Users.Add(user);
            db.Customers.Add(new() { ApplicationUserId = user.Id, DisplayName = "DEMO " + state, Kind = CustomerKind.Organization,
                Segment = CustomerSegment.Wholesale, WholesaleStatus = state });
        }
        db.Customers.Add(new() { DisplayName = "DEMO — организация в рознице", Kind = CustomerKind.Organization, Segment = CustomerSegment.Retail });
        db.Warehouses.Add(new() { Name = "DEMO — склад" });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new { VariantId, OfferId = offer.Id, Note = "DEMO only. Draft product; no published prices; store wholesale minimum remains unset." };
    }
}

