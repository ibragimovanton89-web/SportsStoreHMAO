using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using SportsStore.Domain.Entities;

namespace SportsStore.Infrastructure.Admin;

/// <summary>Создаёт ограниченную книгу SpreadsheetML средствами .NET: без Excel COM, макросов и внешних ссылок.</summary>
internal static class PurchaseExcelWriter
{
    /// <summary>Пространство имён листов и стилей Open XML.</summary>
    private static readonly XNamespace Sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    /// <summary>Пространство имён связей частей книги.</summary>
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    /// <summary>Формат чисел внутри XML, независимый от локали сервера.</summary>
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>Создаёт одну вкладку с шапкой заказа, таблицей и формулами с вычисленными значениями.</summary>
    /// <param name="order">Согласованный снимок закупки; внутреннее примечание не экспортируется.</param>
    /// <param name="supplier">Название поставщика.</param>
    /// <param name="lines">Все выбранные строки, до 1000; количества и закупочные цены уже проверены сервисом.</param>
    /// <param name="ct">Отмена между строками.</param>
    /// <returns>ZIP-пакет xlsx в памяти; файлов на сервере не создаёт.</returns>
    internal static byte[] Create(PurchaseOrder order, string supplier, IReadOnlyList<PurchaseOrderLine> lines, CancellationToken ct)
    {
        var last = 8 + lines.Count;
        var totalRow = last + 1;
        var total = lines.Sum(x => x.Quantity * x.UnitPrice!.Value);
        var rows = new XElement(Sheet + "sheetData",
            Row(2, 28, Text("A2", "SportsStoreHMAO — заказ поставщику", 1)),
            Row(3, 25, Text("A3", $"Поставщик: {supplier}")),
            Row(4, 25, Text("A4", $"Заказ {order.Number}. Дата создания (UTC): {order.CreatedAt:dd.MM.yyyy}")),
            Row(5, 25, Text("A5", $"Выбранный опт: {order.TierName}. Валюта: {order.Currency}")),
            Row(6, 25, Text("A6", $"Позиций: {lines.Count}. Итоговая сумма, {order.Currency}:", 7), Formula("K6", $"K{totalRow}", total, 6)),
            Row(7, 25, Text("A7", "Заказано в единицах поставщика. Количество в коробке — справочная информация.")));
        string[] headings = ["№", "Код поставщика", "Наименование товаров", "Ед.", "Кол. в кор.", "Мелкий опт", "Опт", "Крупный опт", "Заказано", "Цена выбранного опта", "Сумма"];
        rows.Add(Row(8, 42, headings.Select((name, i) => Text($"{(char)('A' + i)}8", name, 2)).ToArray()));
        for (var index = 0; index < lines.Count; index++)
        {
            ct.ThrowIfCancellationRequested();
            var line = lines[index]; var row = index + 9;
            // Названия и коды всегда текстовые, даже если начинаются с =, +, - или @.
            // Единственные формулы создаёт приложение из числовых координат ячеек.
            rows.Add(Row(row, Math.Clamp(18 * Math.Ceiling(line.Name.Length / 55d), 42, 409),
                Number($"A{row}", index + 1, 5), Text($"B{row}", line.ExternalCode, 8), Text($"C{row}", line.Name, 3),
                Text($"D{row}", line.SupplierUnit, 3), Number($"E{row}", line.UnitsPerBox, 5),
                Number($"F{row}", line.SmallPrice), Number($"G{row}", line.WholesalePrice), Number($"H{row}", line.LargePrice),
                Number($"I{row}", line.Quantity, 5), Number($"J{row}", line.UnitPrice),
                Formula($"K{row}", $"I{row}*J{row}", line.Quantity * line.UnitPrice!.Value, 4)));
        }
        rows.Add(Row(totalRow, 28, Text($"A{totalRow}", $"ИТОГО, {order.Currency}", 7), Formula($"K{totalRow}", $"SUM(K9:K{last})", total, 6)));
        double[] widths = [6, 17, 65, 9, 12, 16, 16, 16, 12, 19, 20];
        var worksheet = new XElement(Sheet + "worksheet",
            new XElement(Sheet + "sheetPr", new XElement(Sheet + "pageSetUpPr", new XAttribute("fitToPage", 1))),
            new XElement(Sheet + "dimension", new XAttribute("ref", $"A1:K{totalRow}")),
            new XElement(Sheet + "sheetViews", new XElement(Sheet + "sheetView", new XAttribute("workbookViewId", 0), new XAttribute("showGridLines", 0),
                new XElement(Sheet + "pane", new XAttribute("ySplit", 8), new XAttribute("topLeftCell", "A9"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
            new XElement(Sheet + "sheetFormatPr", new XAttribute("defaultRowHeight", 18)),
            new XElement(Sheet + "cols", widths.Select((width, i) => new XElement(Sheet + "col", new XAttribute("min", i + 1), new XAttribute("max", i + 1), new XAttribute("width", width), new XAttribute("customWidth", 1)))),
            rows,
            new XElement(Sheet + "autoFilter", new XAttribute("ref", $"A8:K{last}")),
            new XElement(Sheet + "mergeCells", new XAttribute("count", 7), new[] { "A2:K2", "A3:K3", "A4:K4", "A5:K5", "A6:J6", "A7:K7", $"A{totalRow}:J{totalRow}" }.Select(reference => new XElement(Sheet + "mergeCell", new XAttribute("ref", reference)))),
            new XElement(Sheet + "printOptions", new XAttribute("horizontalCentered", 1)),
            new XElement(Sheet + "pageMargins", new XAttribute("left", .25), new XAttribute("right", .25), new XAttribute("top", .4), new XAttribute("bottom", .4), new XAttribute("header", .2), new XAttribute("footer", .2)),
            new XElement(Sheet + "pageSetup", new XAttribute("paperSize", 9), new XAttribute("orientation", "landscape"), new XAttribute("fitToWidth", 1), new XAttribute("fitToHeight", 0)));
        var workbook = new XElement(Sheet + "workbook", new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Sheet + "bookViews", new XElement(Sheet + "workbookView")),
            new XElement(Sheet + "sheets", new XElement(Sheet + "sheet", new XAttribute("name", "Заказ"), new XAttribute("sheetId", 1), new XAttribute(Relationships + "id", "rId1"))),
            new XElement(Sheet + "definedNames",
                new XElement(Sheet + "definedName", new XAttribute("name", "_xlnm.Print_Titles"), new XAttribute("localSheetId", 0), "'Заказ'!$8:$8"),
                new XElement(Sheet + "definedName", new XAttribute("name", "_xlnm.Print_Area"), new XAttribute("localSheetId", 0), $"'Заказ'!$A$1:$K${totalRow}")),
            new XElement(Sheet + "calcPr", new XAttribute("calcId", 191029), new XAttribute("fullCalcOnLoad", 1)));
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Write(archive, "[Content_Types].xml", XElement.Parse(ContentTypes));
            Write(archive, "_rels/.rels", XElement.Parse(PackageRelationships));
            Write(archive, "xl/_rels/workbook.xml.rels", XElement.Parse(WorkbookRelationships));
            Write(archive, "xl/workbook.xml", workbook);
            Write(archive, "xl/worksheets/sheet1.xml", worksheet);
            Write(archive, "xl/styles.xml", XElement.Parse(Styles));
        }
        return output.ToArray();
    }

    /// <summary>Создаёт строку с явной высотой для длинных названий.</summary>
    /// <param name="index">Номер строки Excel, начиная с единицы.</param><param name="height">Высота в пунктах.</param><param name="cells">Ячейки строки.</param>
    private static XElement Row(int index, double height, params XElement[] cells) => new(Sheet + "row", new XAttribute("r", index), new XAttribute("ht", height), new XAttribute("customHeight", 1), cells);
    /// <summary>Сохраняет код и произвольный текст как inlineStr, сохраняя ведущие нули и исключая формулы поставщика.</summary>
    /// <param name="address">Адрес ячейки, сформированный приложением.</param><param name="value">Буквальное содержимое без исполнения.</param><param name="style">Индекс стиля книги.</param>
    private static XElement Text(string address, string value, int style = 0) => new(Sheet + "c", new XAttribute("r", address), new XAttribute("s", style), new XAttribute("t", "inlineStr"),
        new XElement(Sheet + "is", new XElement(Sheet + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), new string(value.Where(XmlConvert.IsXmlChar).ToArray()))));
    /// <summary>Пишет число инвариантно; null оставляет ячейку пустой, а не нулевой.</summary>
    /// <param name="address">Адрес ячейки.</param><param name="value">Число либо неизвестное значение.</param><param name="style">Индекс числового стиля.</param>
    private static XElement Number(string address, decimal? value, int style = 4) => new(Sheet + "c", new XAttribute("r", address), new XAttribute("s", style),
        value is null ? null : new XElement(Sheet + "v", value.Value.ToString(Invariant)));
    /// <summary>Пишет только формулу приложения и заранее вычисленный decimal-результат для предварительного просмотра.</summary>
    /// <param name="address">Адрес результата.</param><param name="formula">Формула приложения без начального знака равенства.</param><param name="result">Предварительно вычисленное значение.</param><param name="style">Индекс стиля.</param>
    private static XElement Formula(string address, string formula, decimal result, int style) => new(Sheet + "c", new XAttribute("r", address), new XAttribute("s", style),
        new XElement(Sheet + "f", formula), new XElement(Sheet + "v", result.ToString(Invariant)));
    /// <summary>Записывает одну XML-часть в пакет сжатия; имена частей заданы приложением.</summary>
    /// <param name="archive">Пакет, которым владеет вызывающий метод.</param><param name="name">Фиксированный путь части внутри ZIP.</param><param name="element">Корневой XML-элемент.</param>
    private static void Write(ZipArchive archive, string name, XElement element)
    {
        using var stream = archive.CreateEntry(name, CompressionLevel.Fastest).Open();
        new XDocument(new XDeclaration("1.0", "utf-8", "yes"), element).Save(stream);
    }

    /// <summary>Обязательные типы частей минимальной книги xlsx.</summary>
    private const string ContentTypes = """
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;
    /// <summary>Связь корня ZIP-пакета с книгой.</summary>
    private const string PackageRelationships = """
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
        """;
    /// <summary>Локальные связи книги с листом и стилями; внешних источников нет.</summary>
    private const string WorkbookRelationships = """
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
        """;
    /// <summary>Компактные стили: тёмная шапка, перенос названий, денежные числа и выделенные итоги.</summary>
    private const string Styles = """
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <numFmts count="2"><numFmt numFmtId="164" formatCode="#,##0.00##"/><numFmt numFmtId="165" formatCode="0.####"/></numFmts>
          <fonts count="4">
            <font><sz val="11"/><color rgb="FF172B40"/><name val="Calibri"/></font>
            <font><b/><sz val="18"/><color rgb="FF172B40"/><name val="Calibri"/></font>
            <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
            <font><b/><sz val="11"/><color rgb="FF172B40"/><name val="Calibri"/></font>
          </fonts>
          <fills count="4"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF172B40"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFDDF2EF"/><bgColor indexed="64"/></patternFill></fill></fills>
          <borders count="2"><border><left/><right/><top/><bottom/><diagonal/></border><border><left/><right/><top/><bottom style="hair"><color rgb="FFD7DFE8"/></bottom><diagonal/></border></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="9">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="2" fillId="2" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center" horizontal="center" wrapText="1"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
            <xf numFmtId="164" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment vertical="center" horizontal="right"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment vertical="center" horizontal="center"/></xf>
            <xf numFmtId="164" fontId="3" fillId="3" borderId="0" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment vertical="center" horizontal="right"/></xf>
            <xf numFmtId="0" fontId="3" fillId="3" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="49" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment vertical="center" horizontal="left"/></xf>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;
}
