namespace SportsStore.Application.Admin;

/// <summary>Сформированный в памяти заказ поставщику; не содержит розничных цен, внутренних примечаний или персональных данных.</summary>
/// <param name="FileName">Безопасное имя файла с расширением xlsx.</param>
/// <param name="Content">Содержимое книги Excel, ограниченное сервером восемью МиБ.</param>
public sealed record PurchaseExcelFile(string FileName, byte[] Content);
