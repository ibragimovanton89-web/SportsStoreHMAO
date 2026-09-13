namespace SportsStore.Domain.Entities;
/// <summary>Закупочные предложения поставщиков; могут храниться без связи с собственным каталогом.</summary>
public sealed class SupplierOffer : Entity
{
    /// <summary>Идентификатор поставщика (Supplier).</summary>
    public Guid SupplierId { get; set; }
    /// <summary>Код позиции поставщика строкой с ведущими нулями; уникален вместе с SupplierId.</summary>
    public string ExternalCode { get; set; } = "";
    /// <summary>Исходное название позиции в прайсе поставщика.</summary>
    public string SourceName { get; set; } = "";
    /// <summary>Исходный раздел прайса; не является автоматически брендом или категорией магазина.</summary>
    public string SourceSection { get; set; } = "";
    /// <summary>Единица, за которую указана закупочная цена поставщика (шт, пар, компл, упак).</summary>
    public string SupplierUnit { get; set; } = "";
    /// <summary>Количество единиц поставщика в коробке; не доказывает обязательную покупку коробкой и не задаёт перевод единиц магазина.</summary>
    public decimal? UnitsPerBox { get; set; }
    /// <summary>Остаток поставщика в его единицах: NULL — неизвестен, 0 — подтверждённый ноль. Не собственный склад магазина.</summary>
    public decimal? Stock { get; set; }
    /// <summary>Дата документа прайса поставщика; не время его загрузки.</summary>
    public DateOnly SourceDate { get; set; }
    /// <summary>Момент последнего появления предложения в применённом прайсе, UTC.</summary>
    public DateTime LastSeenAt { get; set; }
    /// <summary>Результат сопоставления с вариантом магазина; NULL для несопоставленного предложения.</summary>
    public Guid? ProductVariantId { get; set; }
    /// <summary>Количество единиц продажи магазина в одной единице цены поставщика; закупочная цена делится на этот коэффициент только после подтверждения.</summary>
    public decimal? SaleUnitsPerSupplierUnit { get; set; }
    /// <summary>Подтверждён ли коэффициент перевода единиц; без подтверждения автоматический расчёт цены запрещён.</summary>
    public bool ConversionConfirmed { get; set; }
    /// <summary>Подтверждённое минимальное количество заказа у поставщика в единицах поставщика; NULL — неизвестно.</summary>
    public decimal? MinimumOrderQuantity { get; set; }
    /// <summary>Подтверждённая кратность заказа у поставщика в его единицах; NULL — неизвестна.</summary>
    public decimal? OrderMultiple { get; set; }
    /// <summary>JSON с неподтверждёнными характеристиками для ручной проверки; не публикуется автоматически.</summary>
    public string ReviewSuggestionsJson { get; set; } = "{}";
}

