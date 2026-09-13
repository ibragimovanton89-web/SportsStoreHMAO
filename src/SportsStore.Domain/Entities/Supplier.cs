namespace SportsStore.Domain.Entities;
/// <summary>Поставщики и явно подтверждённые условия закупки у них.</summary>
public sealed class Supplier : Entity
{
    /// <summary>Название поставщика.</summary>
    public string Name { get; set; } = "";
    /// <summary>Уникальный внутренний код поставщика.</summary>
    public string Code { get; set; } = "";
    /// <summary>Ревизия данных поставщика для защиты от применения устаревшего предварительного импорта.</summary>
    public long Revision { get; set; }
    /// <summary>Дата последнего применённого прайса; NULL до первого применения.</summary>
    public DateOnly? LatestSourceDate { get; set; }
    /// <summary>Явно выбранный закупочный тариф; минимальная цена автоматически не выбирается.</summary>
    public Guid? SelectedPriceTierId { get; set; }
    /// <summary>Подтверждено ли право магазина закупать по выбранному тарифу.</summary>
    public bool PriceTierConfirmed { get; set; }
}

