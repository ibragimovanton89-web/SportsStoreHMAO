namespace SportsStore.Application.Storefront;

/// <summary>Публичная карточка без закупочных цен, служебных полей и персональных данных.</summary>
/// <param name="Id">Идентификатор опубликованной карточки.</param><param name="Name">Название магазина.</param><param name="Description">Описание.</param>
/// <param name="Brand">Бренд.</param><param name="Category">Категория.</param><param name="Images">Адреса опубликованных изображений.</param><param name="Variants">Доступные варианты с розничной ценой.</param>
public sealed record StoreProduct(Guid Id, string Name, string? Description, string? Brand, string? Category, IReadOnlyList<string> Images, IReadOnlyList<StoreVariant> Variants);
/// <summary>Публичный вариант; ожидаемое поступление не считается доступным остатком.</summary>
/// <param name="Sku">Код магазина.</param><param name="Unit">Единица продажи.</param><param name="Size">Размер.</param><param name="Color">Цвет.</param>
/// <param name="Price">Розничная цена RUB.</param><param name="Available">Физически доступно.</param><param name="Incoming">Ожидается по подтверждённым закупкам.</param>
public sealed record StoreVariant(string Sku, string Unit, string? Size, string? Color, decimal Price, decimal Available, decimal Incoming);
/// <summary>Ограниченная страница опубликованного каталога.</summary>
/// <param name="Items">До 24 карточек.</param><param name="Total">Количество совпадений.</param><param name="Page">Номер страницы.</param>
public sealed record StorePage(IReadOnlyList<StoreProduct> Items, int Total, int Page);
/// <summary>Чтение витрины без доступа к административным DTO и изменениям базы.</summary>
public interface IStorefront
{
    /// <summary>Ищет только опубликованные карточки с розничной ценой.</summary>
    Task<StorePage> ListAsync(string? search, int page, CancellationToken ct = default);
    /// <summary>Возвращает публичную карточку; черновик и архив дают null.</summary>
    Task<StoreProduct?> ProductAsync(Guid id, CancellationToken ct = default);
}
