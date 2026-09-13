namespace SportsStore.Domain.Entities;
/// <summary>Продаваемые варианты товаров с уникальным внутренним SKU, единицей продажи, размером и цветом.</summary>
public sealed class ProductVariant : Entity
{
    /// <summary>Явно выбранное предложение поставщика для расчёта цены; NULL блокирует автоматический выбор закупочной цены.</summary>
    public Guid? PricingSupplierOfferId { get; set; }
    /// <summary>Идентификатор собственной карточки товара (Product).</summary>
    public Guid ProductId { get; set; }
    /// <summary>Уникальный внутренний код продаваемого варианта магазина; отличается от кода поставщика.</summary>
    public string Sku { get; set; } = "";
    /// <summary>Подтверждённый артикул производителя, если известен.</summary>
    public string? ManufacturerCode { get; set; }
    /// <summary>Штрихкод варианта строкой с сохранением ведущих нулей, если известен.</summary>
    public string? Barcode { get; set; }
    /// <summary>Размер продаваемого варианта, если применим.</summary>
    public string? Size { get; set; }
    /// <summary>Цвет продаваемого варианта, если применим.</summary>
    public string? Color { get; set; }
    /// <summary>Единица продажи магазина; не обязательно совпадает с единицей цены поставщика.</summary>
    public string SaleUnit { get; set; } = "";
}
