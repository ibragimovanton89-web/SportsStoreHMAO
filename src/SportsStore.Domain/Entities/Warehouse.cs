namespace SportsStore.Domain.Entities;
/// <summary>Собственные склады магазина.</summary>
public sealed class Warehouse : Entity
{
    /// <summary>Название собственного склада магазина.</summary>
    public string Name { get; set; } = "";
}

