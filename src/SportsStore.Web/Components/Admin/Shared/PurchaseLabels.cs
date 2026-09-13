using SportsStore.Domain.Entities;
namespace SportsStore.Web.Components.Admin.Shared;

/// <summary>Единые русские подписи этапов закупки для списка и карточки.</summary>
public static class PurchaseLabels
{
    /// <summary>Возвращает понятное владельцу название состояния.</summary>
    public static string Status(PurchaseStatus status) => status switch
    {
        PurchaseStatus.Draft => "Подбор позиций", PurchaseStatus.Created => "Создан",
        PurchaseStatus.Submitted => "Передан поставщику", PurchaseStatus.InTransit => "В пути",
        PurchaseStatus.PartiallyReceived => "Частично на складе", PurchaseStatus.Received => "Доставлен на склад",
        PurchaseStatus.Cancelled => "Отменён", _ => "Неизвестно"
    };
}
