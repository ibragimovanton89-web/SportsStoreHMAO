using SportsStore.Domain.Entities;
namespace SportsStore.Application.Orders;
/// <summary>Создаёт независимые снимки строк заказа, чтобы последующие изменения каталога не меняли историю.</summary>
public static class OrderSnapshots
{
    /// <summary>Проверяет количество, скидку и принадлежность варианта товару, затем копирует данные в снимок строки заказа.</summary>
    /// <param name="orderId">Идентификатор заказа для создаваемой строки.</param>
    /// <param name="product">Карточка товара, данные которой копируются в снимок.</param>
    /// <param name="variant">Вариант этой карточки с SKU и единицей продажи.</param>
    /// <param name="quantity">Количество одного SKU в единицах продажи магазина.</param>
    /// <param name="actualUnitPrice">Фиксируемая цена единицы до вычета отдельной скидки.</param>
    /// <param name="unitDiscount">Скидка на одну единицу в валюте заказа; не больше цены единицы.</param>
    public static OrderItem Item(Guid orderId, Product product, ProductVariant variant, decimal quantity, decimal actualUnitPrice, decimal unitDiscount)
    {
        if (variant.ProductId != product.Id || quantity <= 0 || actualUnitPrice < 0 || unitDiscount < 0 || unitDiscount > actualUnitPrice)
            throw new ArgumentException("Invalid order snapshot.");
        return new OrderItem { OrderId = orderId, ProductVariantId = variant.Id, Sku = variant.Sku,
            Name = product.Name, Size = variant.Size, Color = variant.Color, SaleUnit = variant.SaleUnit, Quantity = quantity,
            UnitPrice = actualUnitPrice, UnitDiscount = unitDiscount };
    }
}
