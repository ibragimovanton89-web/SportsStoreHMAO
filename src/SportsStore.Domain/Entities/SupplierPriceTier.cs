namespace SportsStore.Domain.Entities;
/// <summary>Закупочные тарифы поставщика. Их пороги относятся к закупкам магазина, а не к покупателям магазина.</summary>
public sealed class SupplierPriceTier : Entity
{
    /// <summary>Минимальная сумма, вручную установленная владельцем; null использует порог прайса. Импорт это поле не меняет.</summary>
    public decimal? ManualMinimumAmount { get; set; }
    /// <summary>Идентификатор поставщика (Supplier).</summary>
    public Guid SupplierId { get; set; }
    /// <summary>Код закупочного тарифа в пределах поставщика.</summary>
    public string Code { get; set; } = "";
    /// <summary>Название закупочного тарифа поставщика.</summary>
    public string Name { get; set; } = "";
    /// <summary>Минимальная сумма закупки у поставщика для тарифа; NULL, если неизвестна. Не является минимумом заказа покупателя магазина.</summary>
    public decimal? MinimumAmount { get; set; }
    /// <summary>Трёхбуквенный код валюты, например RUB (российский рубль).</summary>
    public string Currency { get; set; } = "RUB";
    /// <summary>Исходное описание условий тарифа из прайса; неподтверждённые формулы скидок не исполняются.</summary>
    public string SourceConditions { get; set; } = "";
}

