namespace SportsStore.Domain.Entities;
/// <summary>Реквизиты ИП и организаций; содержат защищаемые сведения покупателя.</summary>
public sealed class OrganizationProfile : Entity
{
    /// <summary>Идентификатор покупателя (Customer).</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Официальное наименование организации или ИП.</summary>
    public string LegalName { get; set; } = "";
    /// <summary>ИНН физлица-предпринимателя или организации; хранится строкой.</summary>
    public string Inn { get; set; } = "";
    /// <summary>КПП организации; NULL, если неприменим или не указан.</summary>
    public string? Kpp { get; set; }
}

