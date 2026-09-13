namespace SportsStore.Domain.Entities;
/// <summary>Общие настройки собственных цен магазина; единственная запись с фиксированным идентификатором.</summary>
public sealed class PricingSettings : Entity
{
    /// <summary>Трёхбуквенный код валюты, например RUB (российский рубль).</summary>
    public string Currency { get; set; } = "RUB";
    /// <summary>Собственная минимальная сумма оптового заказа магазина в указанной валюте; NULL — не настроена.</summary>
    public decimal? MinimumWholesaleOrder { get; set; }
    /// <summary>Явное разрешение автоматически применять предложения пересчёта; по умолчанию выключено. Ручные цены защищены.</summary>
    public bool AutoApplyProposals { get; set; }
    /// <summary>Настраиваемое описание налогового режима и трактовки цен; NULL до подтверждения владельцем.</summary>
    public string? TaxTreatmentNote { get; set; }
}

