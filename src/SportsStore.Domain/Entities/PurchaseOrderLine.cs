namespace SportsStore.Domain.Entities;
/// <summary>Выбранная позиция закупки со снимками исходных данных и цен; новый прайс не меняет заказ.</summary>
public sealed class PurchaseOrderLine : Entity
{
    /// <summary>Закупочный заказ.</summary>
    public Guid PurchaseOrderId { get; set; }
    /// <summary>Предложение поставщика, выбранное вручную.</summary>
    public Guid SupplierOfferId { get; set; }
    /// <summary>Код поставщика с ведущими нулями.</summary>
    public string ExternalCode { get; set; } = "";
    /// <summary>Название позиции на момент выбора.</summary>
    public string Name { get; set; } = "";
    /// <summary>Единица закупки: плюс один означает одну такую единицу, а не коробку.</summary>
    public string SupplierUnit { get; set; } = "";
    /// <summary>Справочное количество в коробке; не задаёт минимальный заказ.</summary>
    public decimal? UnitsPerBox { get; set; }
    /// <summary>Снимок цены мелкого опта за единицу поставщика; null не равен нулю.</summary>
    public decimal? SmallPrice { get; set; }
    /// <summary>Снимок цены опта за единицу поставщика.</summary>
    public decimal? WholesalePrice { get; set; }
    /// <summary>Снимок цены крупного опта за единицу поставщика.</summary>
    public decimal? LargePrice { get; set; }
    /// <summary>Зафиксированная цена выбранного тарифа за единицу поставщика.</summary>
    public decimal? UnitPrice { get; set; }
    /// <summary>Заказанное количество единиц поставщика, от 1 до 1000000.</summary>
    public int Quantity { get; set; }
    /// <summary>Суммарно принятое количество; увеличивается только командой приёмки.</summary>
    public int ReceivedQuantity { get; set; }
}
