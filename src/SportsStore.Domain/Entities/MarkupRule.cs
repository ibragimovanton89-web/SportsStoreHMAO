namespace SportsStore.Domain.Entities;
/// <summary>Правила наценки: вариант, затем ближайшая категория по иерархии, затем общее правило; внутри области учитывается количество SKU.</summary>
public sealed class MarkupRule : Entity
{
    /// <summary>Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.</summary>
    public CustomerSegment Segment { get; set; }
    /// <summary>Категория действия правила, включая потомков; ближайший предок имеет приоритет. NULL для правила варианта или общего правила.</summary>
    public Guid? CategoryId { get; set; }
    /// <summary>Вариант для индивидуального правила с наивысшим приоритетом; NULL для категорийного или общего правила.</summary>
    public Guid? ProductVariantId { get; set; }
    /// <summary>Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.</summary>
    public decimal MinimumQuantity { get; set; } = 1;
    /// <summary>Наценка в процентах к закупочной стоимости, не маржа: стоимость × (1 + процент / 100).</summary>
    public decimal MarkupPercent { get; set; }
}

