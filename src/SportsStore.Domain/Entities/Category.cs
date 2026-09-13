namespace SportsStore.Domain.Entities;
/// <summary>Иерархия категорий собственного каталога; циклические связи запрещены.</summary>
public sealed class Category : Entity
{
    /// <summary>Название категории магазина.</summary>
    public string Name { get; set; } = "";
    /// <summary>Родительская категория; NULL для корня. Циклы запрещены.</summary>
    public Guid? ParentId { get; set; }
}

