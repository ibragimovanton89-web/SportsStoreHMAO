using SportsStore.Domain.Entities;

namespace SportsStore.Application.Admin;

/// <summary>Именованные разрешения административных операций; роли проверяются сервером перед исполнением.</summary>
public static class AdminPolicies
{
    /// <summary>Чтение административных данных и работа с черновиками: Admin или Manager после MFA.</summary>
    public const string Staff = "Staff";
    /// <summary>Изменения цен, публикация, сотрудники и коммерческие решения: только Admin после MFA.</summary>
    public const string Administrator = "Administrator";
}

/// <summary>Получает личность только из доверенного контекста хоста. Реализация локального CLI регистрируется исключительно в CLI.</summary>
public interface IAdminIdentity
{
    /// <summary>Возвращает снимок предъявленной сессии; актуальная учётная запись и роли дополнительно проверяются в БД.</summary>
    Task<AdminSession> GetAsync(CancellationToken ct = default);
}

/// <summary>Доверенный снимок сессии, недоступный для привязки из формы.</summary>
/// <param name="UserId">Идентификатор Identity или локального оператора.</param>
/// <param name="SecurityStamp">Метка безопасности предъявленной cookie.</param>
/// <param name="Mfa">Пройдена ли двухфакторная проверка в этой сессии.</param>
/// <param name="Local">Признак доверенного процесса CLI; Web никогда не создаёт такую сессию.</param>
public sealed record AdminSession(string UserId, string SecurityStamp, bool Mfa, bool Local = false);

/// <summary>Ограниченный запрос списка; значения страницы и сортировки проверяются сервером.</summary>
/// <param name="Search">Поиск по названию, SKU или коду.</param>
/// <param name="Page">Номер страницы, начиная с 1.</param>
/// <param name="Sort">Разрешённый ключ сортировки.</param>
/// <param name="Filter">Разрешённый фильтр раздела.</param>
public sealed record AdminQuery(string Search = "", int Page = 1, string Sort = "name", string Filter = "");

/// <summary>Страница проекции, не содержащая EF-сущностей.</summary>
/// <typeparam name="T">Тип строки интерфейса.</typeparam>
/// <param name="Items">Не более 25 строк.</param>
/// <param name="Total">Общее число результатов фильтра.</param>
/// <param name="Page">Текущая страница.</param>
public sealed record AdminPage<T>(IReadOnlyList<T> Items, int Total, int Page);

/// <summary>Реальные счётчики административного обзора.</summary>
/// <param name="Drafts">Черновики.</param><param name="Published">Опубликованные карточки.</param>
/// <param name="Unmapped">Несопоставленные предложения.</param><param name="InvalidBatches">Ошибочные партии.</param>
/// <param name="PendingPrices">Ожидающие предложения цен.</param>
public sealed record AdminOverview(int Drafts, int Published, int Unmapped, int InvalidBatches, int PendingPrices);

/// <summary>Краткая запись справочника для выбора в форме.</summary>
/// <param name="Id">Идентификатор.</param><param name="Name">Отображаемое имя.</param>
/// <param name="Version">Ожидаемая версия для редактирования.</param><param name="ParentId">Родитель категории.</param>
public sealed record LookupItem(Guid Id, string Name, uint Version, Guid? ParentId = null);

/// <summary>Данные карточки для формы и списка без отслеживания EF.</summary>
/// <param name="Id">Карточка.</param><param name="Version">Версия, которую видел сотрудник.</param>
/// <param name="Name">Собственное название.</param><param name="Description">Описание.</param>
/// <param name="BrandId">Бренд.</param><param name="CategoryId">Категория.</param><param name="Status">Текущий статус.</param>
public sealed record ProductData(Guid Id, uint Version, string Name, string? Description, Guid? BrandId, Guid? CategoryId, ProductStatus Status)
{
    /// <summary>Введённое название бренда; используется при отсутствии BrandId и сохраняется вместе с карточкой.</summary>
    public string? BrandName { get; init; }
    /// <summary>Введённое название категории; новая запись создаётся в корне справочника при отсутствии CategoryId.</summary>
    public string? CategoryName { get; init; }
}

/// <summary>Редактируемые данные варианта; источник цены меняется отдельной операцией.</summary>
/// <param name="Id">Вариант; пустой GUID создаёт новый.</param><param name="Version">Версия формы.</param>
/// <param name="ProductId">Родительская карточка.</param><param name="Sku">Уникальный внутренний SKU.</param>
/// <param name="Unit">Единица продажи.</param><param name="Size">Размер.</param><param name="Color">Цвет.</param>
/// <param name="ManufacturerCode">Артикул производителя.</param><param name="Barcode">Штрихкод.</param>
public sealed record VariantData(Guid Id, uint Version, Guid ProductId, string Sku, string Unit, string? Size, string? Color, string? ManufacturerCode, string? Barcode);

/// <summary>Предложение поставщика для таблицы и сопоставления.</summary>
/// <param name="Id">Предложение.</param><param name="Version">Версия формы.</param><param name="Code">Код с ведущими нулями.</param>
/// <param name="Name">Исходное название.</param><param name="Section">Исходный раздел.</param><param name="Unit">Единица поставщика.</param>
/// <param name="Box">Количество в коробке, не минимум заказа.</param><param name="Stock">Остаток; null — неизвестно.</param>
/// <param name="Date">Дата документа.</param><param name="Small">Мелкий опт.</param><param name="Wholesale">Опт.</param><param name="Large">Крупный опт.</param>
/// <param name="VariantId">Связанный вариант.</param><param name="Factor">Перевод единиц.</param><param name="Confirmed">Подтверждение перевода.</param>
/// <param name="PricingSource">Выбрано ли предложение источником цены.</param>
public sealed record OfferData(Guid Id, uint Version, string Code, string Name, string Section, string Unit, decimal? Box, decimal? Stock, DateOnly Date,
    decimal? Small, decimal? Wholesale, decimal? Large, Guid? VariantId, decimal? Factor, bool Confirmed, bool PricingSource);

/// <summary>Контракт управления каталогом; все методы проверяют текущую серверную личность.</summary>
public interface IAdminCatalog
{
    /// <summary>Предлагает признаки прайса без записи в базу; неоднозначные значения возвращает как null.</summary>
    /// <param name="id">Идентификатор предложения либо карточки.</param>
    /// <param name="product">True ищет предложения вариантов карточки; false читает одно предложение.</param>
    /// <param name="ct">Отмена чтения.</param>
    Task<(string? Brand, string? Category)> SuggestLookupsAsync(Guid id, bool product, CancellationToken ct = default);
    /// <summary>Возвращает счётчики обзора.</summary>
    Task<AdminOverview> OverviewAsync(CancellationToken ct = default);
    /// <summary>Находит страницу собственных товаров.</summary>
    Task<AdminPage<ProductData>> ProductsAsync(AdminQuery query, CancellationToken ct = default);
    /// <summary>Читает карточку для редактирования.</summary>
    Task<ProductData> ProductAsync(Guid id, CancellationToken ct = default);
    /// <summary>Читает ограниченный список вариантов одной карточки.</summary>
    Task<IReadOnlyList<VariantData>> VariantsAsync(Guid productId, CancellationToken ct = default);
    /// <summary>Читает один сопоставленный вариант по серверному идентификатору.</summary>
    Task<VariantData> VariantAsync(Guid id, CancellationToken ct = default);
    /// <summary>Возвращает страницу предложений с закупочными ценами.</summary>
    Task<AdminPage<OfferData>> OffersAsync(AdminQuery query, CancellationToken ct = default);
    /// <summary>Читает одно предложение.</summary>
    Task<OfferData> OfferAsync(Guid id, CancellationToken ct = default);
    /// <summary>Ищет бренды или категории для ограниченного списка выбора.</summary>
    Task<AdminPage<LookupItem>> LookupsAsync(bool categories, AdminQuery query, CancellationToken ct = default);
    /// <summary>Читает выбранное название независимо от страницы результатов поиска; null означает удалённую запись.</summary>
    /// <param name="categories">True для категории, false для бренда.</param>
    /// <param name="id">Идентификатор выбранного справочника.</param>
    /// <param name="ct">Отмена чтения при закрытии формы.</param>
    Task<LookupItem?> LookupAsync(bool categories, Guid id, CancellationToken ct = default);
    /// <summary>Сохраняет бренд или категорию с проверкой версии и родителя.</summary>
    Task<Guid> SaveLookupAsync(bool categories, LookupItem data, Guid operationId, CancellationToken ct = default);
    /// <summary>Удаляет только неиспользуемую запись справочника.</summary>
    Task DeleteLookupAsync(bool categories, Guid id, uint version, Guid operationId, CancellationToken ct = default);
    /// <summary>Создаёт черновик либо редактирует разрешённую карточку; Status входа не применяется.</summary>
    Task<Guid> SaveProductAsync(ProductData data, Guid operationId, CancellationToken ct = default);
    /// <summary>Сохраняет вариант с серверной валидацией SKU и единицы.</summary>
    Task<Guid> SaveVariantAsync(VariantData data, Guid operationId, CancellationToken ct = default);
    /// <summary>Удаляет неиспользуемую карточку со всеми вариантами; история, связи закупок и ненулевые остатки запрещают удаление.</summary>
    /// <param name="id">Карточка для полного удаления.</param>
    /// <param name="version">Версия карточки из списка склада.</param>
    /// <param name="operationId">Идемпотентный идентификатор удаления.</param>
    /// <param name="ct">Отмена транзакции.</param>
    Task DeleteProductAsync(Guid id, uint version, Guid operationId, CancellationToken ct = default);
    /// <summary>Предлагает свободный внутренний SKU для нового варианта; запись появится только после сохранения формы.</summary>
    /// <param name="productId">Карточка, доступная сотруднику для редактирования.</param>
    /// <param name="ct">Отмена проверки уникальности.</param>
    /// <returns>Код SS- и 16 шестнадцатеричных символов; уникальность окончательно защищает индекс БД при сохранении.</returns>
    Task<string> GenerateSkuAsync(Guid productId, CancellationToken ct = default);
    /// <summary>Возвращает конкретные препятствия публикации.</summary>
    Task<IReadOnlyList<string>> PublicationProblemsAsync(Guid productId, CancellationToken ct = default);
    /// <summary>Публикует проверенную карточку либо архивирует её; разрешено только Admin.</summary>
    Task SetStatusAsync(Guid id, uint version, ProductStatus status, Guid operationId, CancellationToken ct = default);
}
