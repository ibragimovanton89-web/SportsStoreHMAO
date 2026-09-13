namespace SportsStore.Domain.Entities;
/// <summary>Адреса и получатели покупателя; персональные данные с ограниченным доступом.</summary>
public sealed class CustomerAddress : Entity
{
    /// <summary>Идентификатор покупателя (Customer).</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Имя получателя по адресу; персональные данные.</summary>
    public string Recipient { get; set; } = "";
    /// <summary>Адрес доставки покупателя; персональные данные.</summary>
    public string Address { get; set; } = "";
    /// <summary>Почтовый индекс строкой; NULL, если не указан.</summary>
    public string? PostalCode { get; set; }
}

