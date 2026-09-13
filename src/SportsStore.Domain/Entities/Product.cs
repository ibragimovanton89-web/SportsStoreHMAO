namespace SportsStore.Domain.Entities;
/// <summary>Собственные карточки товаров. Импорт прайса поставщика не создаёт и не публикует карточки автоматически.</summary>
public sealed class Product : Entity
{
    /// <summary>Собственное название карточки товара, независимое от прайса поставщика.</summary>
    public string Name { get; set; } = "";
    /// <summary>Собственное описание товара; импорт его не перезаписывает.</summary>
    public string? Description { get; set; }
    /// <summary>Статус карточки: Draft — черновик, Published — опубликована, Archived — архив.</summary>
    public ProductStatus Status { get; set; }
    /// <summary>Бренд товара; NULL, если ещё не определён.</summary>
    public Guid? BrandId { get; set; }
    /// <summary>Идентификатор категории собственного каталога (Category).</summary>
    public Guid? CategoryId { get; set; }
    /// <summary>Момент создания записи, UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Момент последнего изменения карточки, UTC.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

