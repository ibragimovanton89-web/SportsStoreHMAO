namespace SportsStore.Domain.Entities;
/// <summary>Строки заказа со снимками товара, количества, цены и скидки; изменения каталога не меняют историю заказа.</summary>
public sealed class OrderItem : Entity
{
    /// <summary>Идентификатор заказа, которому принадлежит строка (Order).</summary>
    public Guid OrderId { get; set; }
    /// <summary>Необязательная ссылка на вариант каталога; история строки сохраняется независимо от изменений каталога.</summary>
    public Guid? ProductVariantId { get; set; }
    /// <summary>Снимок внутреннего SKU на момент заказа.</summary>
    public string Sku { get; set; } = "";
    /// <summary>Снимок названия товара на момент заказа.</summary>
    public string Name { get; set; } = "";
    /// <summary>Снимок единицы продажи на момент заказа.</summary>
    public string SaleUnit { get; set; } = "";
    /// <summary>Заказанное количество в зафиксированных единицах продажи.</summary>
    public decimal Quantity { get; set; }
    /// <summary>Зафиксированная цена за единицу до вычета UnitDiscount, в валюте заказа.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>Зафиксированная скидка на одну единицу в валюте заказа; итог строки = Quantity × (UnitPrice − UnitDiscount).</summary>
    public decimal UnitDiscount { get; set; }
    /// <summary>Снимок размера; null, если неприменим.</summary>
    public string? Size { get; set; } = null;
    /// <summary>Снимок цвета; null, если неприменим.</summary>
    public string? Color { get; set; } = null;
    /// <summary>Применённая количественная ступень одного SKU.</summary>
    public decimal MinimumQuantity { get; set; } = 1;
}

