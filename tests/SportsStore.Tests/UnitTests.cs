using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SportsStore.Application.Import;
using SportsStore.Application.Orders;
using SportsStore.Application.Pricing;
using SportsStore.Domain.Entities;
using SportsStore.Infrastructure.Import;
using Xunit;
namespace SportsStore.Tests;
/// <summary>Модульные проверки парсера, правил цен и снимков заказа без базы данных.</summary>
public sealed class UnitTests
{
    /// <summary>Проверяет ведущие нули, составной раздел, единицы, неизвестный остаток и три тарифа.</summary>
    [Fact]
    public void ParserPreservesCodesSectionsUnitsUnknownAndThreeTiers()
    {
        var doc = Fixtures.Document();
        var row = Assert.Single(doc.Rows, x => x.Kind == ImportRowKind.Product).Offer!;
        Assert.Equal("00024387", row.ExternalCode); Assert.Null(row.Stock);
        Assert.Equal("BABOLAT, NEVA, TECNIFIBRE", row.Section);
        Assert.Equal("упак", row.Unit); Assert.Equal(12m, row.UnitsPerBox);
        Assert.Equal(new decimal?[] { 4633,4455,4325 }, row.Prices);
        Assert.Equal(new decimal?[] {150000,450000,2000000},doc.Tiers.Select(x => x.MinimumAmount));
    }
    /// <summary>Проверяет сохранение каждой поддерживаемой единицы поставщика и явного нулевого остатка.</summary>
    /// <param name="unit">Единица поставщика, проверяемая тестом.</param>
    [Theory]
    [InlineData("шт")][InlineData("пар")][InlineData("компл")][InlineData("упак")]
    public void AllSupplierUnitsAndKnownZeroArePreserved(string unit)
    {
        var doc = Fixtures.Document(unit:unit,stock:"0");
        var row = Assert.Single(doc.Rows, x => x.Kind == ImportRowKind.Product).Offer!;
        Assert.Equal(unit,row.Unit); Assert.Equal(0m,row.Stock);
    }
    /// <summary>Проверяет, что пустая цена не становится нулём, а неверные числа дают диагностику.</summary>
    [Fact]
    public void MissingPriceIsNotZeroAndInvalidRowHasDiagnostic()
    {
        var missing = Fixtures.Document(price:"");
        Assert.Null(Assert.Single(missing.Rows,x=>x.Kind==ImportRowKind.Product).Offer!.Prices[0]);
        var invalid = Fixtures.Document(extra:["", "00099999", "Bad", "шт", "1", "-1", "abc", "20", "-3"]);
        Assert.Single(invalid.Rows,x=>x.Kind==ImportRowKind.Error);
        Assert.Contains("invalid number",invalid.Rows.Single(x=>x.Kind==ImportRowKind.Error).Diagnostics);
    }
    /// <summary>Проверяет отклонение конфликтующих кодов и исключение идентичных повторов.</summary>
    [Fact]
    public void ConflictingDuplicatesAreRejectedAndIdenticalIgnored()
    {
        var conflict = Fixtures.Document(extra:["","00024387","Other","шт","1","50","40","30","0"]);
        Assert.Contains(conflict.Rows,x=>x.Kind==ImportRowKind.Error && x.Diagnostics.Contains("duplicate"));
        var same = Fixtures.Document(extra:Fixtures.Offer());
        Assert.Single(same.Rows,x=>x.Kind==ImportRowKind.Product);
    }
    /// <summary>Проверяет отказ при неверном содержимом XLS и превышении размера файла.</summary>
    [Fact]
    public void ContentSignatureAndSizeLimitsAreEnforced()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path,"not an Excel workbook");
            Assert.Throws<InvalidDataException>(()=>new BallMarketParser().Parse(path));
            using(var stream = File.OpenWrite(path)) stream.SetLength(BallMarketParser.MaxBytes+1);
            Assert.Throws<InvalidDataException>(()=>new BallMarketParser().Parse(path));
        }
        finally { File.Delete(path); }
    }
    /// <summary>Проверяет наценку, перевод единиц, округление половин от нуля и запрет неподтверждённого расчёта.</summary>
    [Fact]
    public void MarkupUsesDecimalAndAwayFromZero()
    {
        Assert.Equal(125m,PriceCalculator.Calculate(100m,1,true,25)); // Наценка 25% к закупочной цене, а не маржа 25%.
        Assert.Equal(12.35m,PriceCalculator.Calculate(12.345m,1,true,0));
        Assert.Equal(60m,PriceCalculator.Calculate(100m,2,true,20));
        Assert.Throws<InvalidOperationException>(()=>PriceCalculator.Calculate(100m,null,false,20));
        Assert.Throws<InvalidOperationException>(()=>PriceCalculator.Calculate(100m,1,false,20));
    }
    /// <summary>Проверяет приоритеты области наценки и включительные границы количественных ступеней.</summary>
    [Fact]
    public void VariantThenNearestCategoryThenDefaultAndQuantityBoundaries()
    {
        var variant=Guid.NewGuid(); var child=Guid.NewGuid(); var parent=Guid.NewGuid();
        MarkupRule[] rules=[
            new(){MarkupPercent=1,Segment=CustomerSegment.Wholesale},
            new(){CategoryId=parent,MarkupPercent=2,Segment=CustomerSegment.Wholesale},
            new(){CategoryId=child,MarkupPercent=3,Segment=CustomerSegment.Wholesale},
            new(){ProductVariantId=variant,MarkupPercent=4,Segment=CustomerSegment.Wholesale},
            new(){ProductVariantId=variant,MinimumQuantity=10,MarkupPercent=5,Segment=CustomerSegment.Wholesale}];
        Assert.Equal(4m,PriceCalculator.SelectRule(rules,variant,[child,parent],CustomerSegment.Wholesale,9)!.MarkupPercent);
        Assert.Equal(5m,PriceCalculator.SelectRule(rules,variant,[child,parent],CustomerSegment.Wholesale,10)!.MarkupPercent);
        Assert.Equal(3m,PriceCalculator.SelectRule(rules,Guid.NewGuid(),[child,parent],CustomerSegment.Wholesale,1)!.MarkupPercent);
        Assert.Equal(2m,PriceCalculator.SelectRule(rules,Guid.NewGuid(),[parent],CustomerSegment.Wholesale,1)!.MarkupPercent);
        Assert.Equal(1m,PriceCalculator.SelectRule(rules,Guid.NewGuid(),[],CustomerSegment.Wholesale,1)!.MarkupPercent);
    }
    /// <summary>Проверяет, что юридический тип не даёт права на опт без одобренного статуса.</summary>
    /// <param name="state">Проверяемый статус подтверждения опта.</param>
    /// <param name="expected">Ожидаемый доступный коммерческий сегмент.</param>
    [Theory]
    [InlineData(WholesaleStatus.Pending,CustomerSegment.Retail)]
    [InlineData(WholesaleStatus.Approved,CustomerSegment.Wholesale)]
    [InlineData(WholesaleStatus.Rejected,CustomerSegment.Retail)]
    public void CustomerApprovalIsIndependentOfLegalKind(WholesaleStatus state,CustomerSegment expected)
    {
        Assert.Equal(expected,PriceCalculator.AvailableSegment(new(){Kind=CustomerKind.Organization,Segment=CustomerSegment.Wholesale,WholesaleStatus=state}));
        Assert.Equal(CustomerSegment.Retail,PriceCalculator.AvailableSegment(new(){Kind=CustomerKind.Organization,Segment=CustomerSegment.Retail,WholesaleStatus=WholesaleStatus.Approved}));
    }
    /// <summary>Проверяет неизменность снимка строки заказа после изменения названия, SKU и единицы каталога.</summary>
    [Fact]
    public void OrderItemIsASnapshot()
    {
        var product=new Product{Name="Original"}; var variant=new ProductVariant{ProductId=product.Id,Sku="SKU",SaleUnit="шт"};
        var item=OrderSnapshots.Item(Guid.NewGuid(),product,variant,2,120,5);
        product.Name="Changed"; variant.Sku="Changed"; variant.SaleUnit="упак";
        Assert.Equal("Original",item.Name); Assert.Equal("SKU",item.Sku); Assert.Equal("шт",item.SaleUnit);
        Assert.Equal(120m,item.UnitPrice); Assert.Equal(5m,item.UnitDiscount);
    }
}
/// <summary>Создаёт минимальные синтетические прайсы для воспроизводимых тестов.</summary>
internal static class Fixtures
{
    /// <summary>Формирует синтетическую строку прайса с ведущими нулями и тремя закупочными ценами.</summary>
    /// <param name="price">Текст цены для тестовой строки; допускает неверные значения для отрицательных сценариев.</param>
    /// <param name="stock">Текст остатка тестовой строки; пробел проверяет неизвестное значение.</param>
    /// <param name="unit">Единица поставщика, проверяемая тестом.</param>
    public static string[] Offer(string price="4633",string stock=" ",string unit="упак") =>
        ["","00024387","Мяч ADIDAS JD8036",unit,"12",price,"4455","4325",stock];
    /// <summary>Собирает синтетический прайс, вычисляет его хеш и разбирает реальным парсером для тестов.</summary>
    /// <param name="price">Текст цены для тестовой строки; допускает неверные значения для отрицательных сценариев.</param>
    /// <param name="stock">Текст остатка тестовой строки; пробел проверяет неизвестное значение.</param>
    /// <param name="unit">Единица поставщика, проверяемая тестом.</param>
    /// <param name="date">Текст даты синтетического прайса.</param>
    /// <param name="extra">Дополнительная строка для проверки дубликатов или ошибок; null без дополнительной строки.</param>
    public static ParsedPriceList Document(string price="4633",string stock=" ",string unit="упак",string date="28.07.26",string[]? extra=null)
    {
        List<string[]> rows=[
            ["","", "На "+date],
            ["","","Цены в рублях. Мелкий опт - от 150 000 руб., Опт - закупка от 450 000 руб."],
            ["","","Крупный опт - закупка от 2 000 000 руб."],
            ["","","Наименование товаров","Ед.","Кол. в кор.","Мелкий Опт","Опт","Крупный Опт","Остаток на складе"],
            ["","","BABOLAT, NEVA, TECNIFIBRE"], Offer(price,stock,unit)];
        if(extra is not null)rows.Add(extra);
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rows))));
        return new BallMarketParser().ParseRows("test.xls",hash,rows);
    }
}
