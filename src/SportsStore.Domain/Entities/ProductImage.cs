namespace SportsStore.Domain.Entities;
/// <summary>Изображения собственных карточек товаров и порядок их отображения.</summary>
public sealed class ProductImage : Entity
{
    /// <summary>Идентификатор собственной карточки товара (Product).</summary>
    public Guid ProductId { get; set; }
    /// <summary>Адрес изображения товара.</summary>
    public string Url { get; set; } = "";
    /// <summary>Порядок изображения при отображении; меньшие значения идут первыми.</summary>
    public int SortOrder { get; set; }
}

