using System.IO.Compression;
using System.Xml.Linq;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using SportsStore.Domain.Entities;
using Xunit;

namespace SportsStore.Tests;

/// <summary>Проверяет книгу Excel реальным читателем, снимки заказа и серверные ограничения экспорта в PostgreSQL.</summary>
public sealed partial class AdminTests
{
    /// <summary>Выгружает все страницы, сохраняет ведущие нули и исторические цены; поставщицкий текст не становится формулой.</summary>
    [Fact]
    public async Task PurchaseExcelIncludesAllRowsWithSafeTextAndHistoricalPrices()
    {
        var (supplier, offer) = await PurchaseFixture();
        var id = await ReadyPurchase(supplier, offer);
        await using (var db = await factory.CreateDbContextAsync())
        {
            (await db.PurchaseOrders.SingleAsync()).Note = "ВНУТРЕННЕЕ ПРИМЕЧАНИЕ НЕ ЭКСПОРТИРОВАТЬ";
            var source = await db.SupplierOffers.SingleAsync(); source.SourceName = "Новое название прайса";
            foreach (var price in await db.SupplierOfferPrices.ToListAsync()) price.Amount = 999;
            // Выборка длиннее страницы интерфейса, включая строку, похожую на формулу Excel.
            for (var n = 0; n < 26; n++)
            {
                var extra = new SupplierOffer { SupplierId = supplier, ExternalCode = $"9000{n:0000}", SourceName = "DEMO", SupplierUnit = "шт", SourceDate = new DateOnly(2026, 7, 28) };
                db.SupplierOffers.Add(extra);
                db.PurchaseOrderLines.Add(new PurchaseOrderLine { PurchaseOrderId = id, SupplierOfferId = extra.Id, ExternalCode = extra.ExternalCode,
                    Name = n == 0 ? "=HYPERLINK(\"https://example.invalid\",\"текст\")" : "DEMO позиция", SupplierUnit = "шт", Quantity = 1,
                    UnitPrice = 100, SmallPrice = 100, WholesalePrice = 90, LargePrice = null });
            }
            await db.SaveChangesAsync();
        }
        var service = Purchases(); var order = await service.OrderAsync(id);
        var file = await service.ExportExcelAsync(id, order.Version);
        Assert.EndsWith(".xlsx", file.FileName);
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using (var input = new MemoryStream(file.Content))
        using (var reader = ExcelReaderFactory.CreateReader(input))
        {
            Assert.Equal("Заказ", reader.Name);
            var rows = new List<object?[]>();
            while (reader.Read()) { var row = new object?[reader.FieldCount]; for (var i = 0; i < row.Length; i++) row[i] = reader.GetValue(i); rows.Add(row); }
            Assert.Equal("0000042", rows[8][1]); Assert.Equal("DEMO воланы, 6 в упаковке", rows[8][2]);
            Assert.Equal(100d, rows[8][9]); Assert.Equal(300d, rows[8][10]);
            Assert.Equal(27, rows.Skip(8).Count(x => x[1] is string));
            Assert.Equal((double)order.Total, rows[^1][10]);
            Assert.Null(rows[9][7]);
            Assert.StartsWith("=HYPERLINK", Assert.IsType<string>(rows[9][2]));
            Assert.False(reader.NextResult());
        }
        using var archive = new ZipArchive(new MemoryStream(file.Content), ZipArchiveMode.Read);
        using var sheetStream = archive.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var sheet = XDocument.Load(sheetStream); XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var cell = sheet.Descendants(ns + "c").Single(x => (string?)x.Attribute("r") == "C10");
        Assert.Equal("inlineStr", (string?)cell.Attribute("t")); Assert.Null(cell.Element(ns + "f"));
        Assert.DoesNotContain("ВНУТРЕННЕЕ ПРИМЕЧАНИЕ", sheet.ToString());
        Assert.DoesNotContain(archive.Entries, x => x.FullName.Contains("externalLinks") || x.FullName.Contains("vbaProject"));
        var repeat = await service.ExportExcelAsync(id, order.Version);
        Assert.Equal(file.FileName, repeat.FileName);
        Assert.Equal(order.Version, (await service.OrderAsync(id)).Version);
        Assert.Equal(PurchaseStatus.Created, (await service.OrderAsync(id)).Status);
    }

    /// <summary>Экспорт закрыт для Manager, устаревшей версии, подборки и отменённого заказа.</summary>
    [Fact]
    public async Task PurchaseExcelEnforcesAccessVersionAndStatus()
    {
        var (supplier, offer) = await PurchaseFixture(); var service = Purchases();
        var id = await service.StartAsync(supplier, Guid.NewGuid());
        var draft = await service.OrderAsync(id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportExcelAsync(id, draft.Version));
        id = await ReadyPurchase(supplier, offer);
        var createdOrder = await service.OrderAsync(id);
        await service.ConfigureAsync(id, createdOrder.Version, createdOrder.TierId, createdOrder.WarehouseId, null, Guid.NewGuid());
        var version = (await service.OrderAsync(id)).Version;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.ExportExcelAsync(id, 0));
        await Role("Manager");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ExportExcelAsync(id, version));
        await Role("Admin");
        await PurchaseStatusTo(id, PurchaseStatus.Submitted);
        Assert.NotEmpty((await service.ExportExcelAsync(id, (await service.OrderAsync(id)).Version)).Content);
        await PurchaseStatusTo(id, PurchaseStatus.Cancelled);
        var cancelled = await service.OrderAsync(id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportExcelAsync(id, cancelled.Version));
    }
}
