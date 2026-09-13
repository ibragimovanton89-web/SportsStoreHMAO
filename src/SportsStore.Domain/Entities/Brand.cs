namespace SportsStore.Domain.Entities;
/// <summary>Бренды собственного каталога магазина.</summary>
public sealed class Brand : Entity
{
    /// <summary>Название бренда собственного каталога.</summary>
    public string Name { get; set; } = "";
}

