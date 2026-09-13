using SportsStore.Domain.Entities;
namespace SportsStore.Application.Admin;

/// <summary>Закупочный тариф и редактируемый порог; исходное значение прайса хранится отдельно.</summary>
/// <param name="Id">Тариф.</param><param name="Version">Ожидаемая версия.</param><param name="Code">Код тарифа.</param><param name="Name">Подпись.</param>
/// <param name="SourceMinimum">Порог прайса, если известен.</param><param name="ManualMinimum">Ручной порог; null возвращает значение прайса.</param>
public sealed record PurchaseTier(Guid Id, uint Version, string Code, string Name, decimal? SourceMinimum, decimal? ManualMinimum)
{
    /// <summary>Действующий порог для нового снимка заказа, в рублях.</summary>
    public decimal? Minimum => ManualMinimum ?? SourceMinimum;
}
/// <summary>Сводка закупки для списка и нижней панели сборки.</summary>
/// <param name="Id">Заказ.</param><param name="Version">Версия формы.</param><param name="Number">Номер.</param><param name="SupplierId">Поставщик.</param>
/// <param name="Supplier">Название поставщика.</param><param name="WarehouseId">Склад.</param><param name="TierId">Выбранный тариф.</param><param name="TierName">Его снимок названия.</param>
/// <param name="Minimum">Порог закупки.</param><param name="Status">Статус.</param><param name="Total">Сумма позиций с известной ценой.</param><param name="Units">Количество единиц поставщика.</param>
/// <param name="MissingPrices">Строки без цены выбранного уровня; блокируют создание.</param><param name="CreatedAt">UTC-время.</param><param name="Note">Примечание.</param>
public sealed record PurchaseSummary(Guid Id, uint Version, string Number, Guid SupplierId, string Supplier, Guid WarehouseId, Guid TierId,
    string TierName, decimal? Minimum, PurchaseStatus Status, decimal Total, int Units, int MissingPrices, DateTime CreatedAt, string? Note)
{
    /// <summary>Недостающая сумма; неизвестный порог не заменяется нулём.</summary>
    public decimal? Remaining => Minimum is decimal minimum ? Math.Max(0, minimum - Total) : null;
}
/// <summary>Строка подбора из текущего прайса либо снимка уже выбранной позиции.</summary>
/// <param name="OfferId">Позиция поставщика.</param><param name="LineId">Строка заказа, если выбрана.</param><param name="Code">Код с нулями.</param><param name="Name">Название.</param>
/// <param name="Unit">Единица закупки.</param><param name="Box">Количество в коробке.</param><param name="Small">Мелкий опт.</param><param name="Wholesale">Опт.</param><param name="Large">Крупный опт.</param>
/// <param name="Price">Выбранная цена.</param><param name="Quantity">Заказано.</param><param name="Received">Принято.</param><param name="ProductId">Связанная карточка.</param>
/// <param name="SupplierStock">Последний известный остаток поставщика; null означает неизвестный остаток.</param>
/// <param name="Brand">Бренд из текста поставщика; не является брендом собственной карточки.</param>
/// <param name="Category">Группа поставщика для закупочного фильтра.</param>
public sealed record PurchasePosition(Guid OfferId, Guid? LineId, string Code, string Name, string Unit, decimal? Box, decimal? Small,
    decimal? Wholesale, decimal? Large, decimal? Price, int Quantity, int Received, Guid? ProductId, decimal? SupplierStock = null, string Brand = "", string Category = "")
{
    /// <summary>Осталось доступно для выбора в этом заказе; исходный прайс не изменяется.</summary>
    public decimal? RemainingStock => SupplierStock is decimal stock ? Math.Max(0, stock - Quantity) : null;
}
/// <summary>Количество отдельной частичной приёмки.</summary>
/// <param name="LineId">Строка заказа.</param><param name="Quantity">Дополнительно принять, в единицах поставщика.</param>
public sealed record ReceivePosition(Guid LineId, int Quantity);
/// <summary>Фактически полученный товар без карточки.</summary>
/// <param name="OfferId">Позиция.</param><param name="Code">Код.</param><param name="Name">Название.</param><param name="Unit">Единица поставщика.</param>
/// <param name="Quantity">Остаток до переноса в вариант.</param><param name="Warehouse">Название склада.</param>
public sealed record UnallocatedPosition(Guid OfferId, string Code, string Name, string Unit, decimal Quantity, string Warehouse);
/// <summary>Вариант на собственном складе либо в выбранной закупке.</summary>
/// <param name="ProductId">Карточка.</param><param name="VariantId">Вариант.</param><param name="Name">Название.</param><param name="Sku">SKU.</param>
/// <param name="Unit">Единица магазина.</param><param name="Available">Доступный остаток.</param><param name="Incoming">Ожидается в подтверждённых закупках.</param>
/// <param name="Status">Публикация карточки независимо от наличия; пустой VariantId означает черновик без варианта.</param>
/// <param name="ProductVersion">Версия карточки для защиты удаления из устаревшего списка.</param>
public sealed record OwnedPosition(Guid ProductId, Guid VariantId, string Name, string Sku, string Unit, decimal Available, decimal Incoming, ProductStatus Status = ProductStatus.Draft, uint ProductVersion = 0);
/// <summary>Цена выбранной исторической закупки для настройки продажи.</summary>
/// <param name="LineId">Источник закупочной цены.</param><param name="Order">Номер закупки.</param><param name="Tier">Закупочный уровень.</param><param name="UnitCost">Цена за единицу продажи.</param>
public sealed record PurchaseCost(Guid LineId, string Order, string Tier, decimal UnitCost);
/// <summary>Текущая цена выбранного варианта и доступные закупочные основания.</summary>
/// <param name="VariantId">Вариант.</param><param name="Version">Версия текущей SalePrice; ноль, если её ещё нет.</param><param name="Amount">Текущая розничная цена.</param>
/// <param name="Costs">Последние 50 закупочных оснований.</param>
public sealed record PurchaseRetail(Guid VariantId, uint Version, decimal? Amount, IReadOnlyList<PurchaseCost> Costs);

/// <summary>Серверный сценарий «прайс → моя закупка → склад → карточка», с аудитом и проверкой актуальных прав.</summary>
public interface IPurchasing
{
    /// <summary>Формирует Excel всех выбранных строк по снимкам заказа; требуется Admin, данные не изменяются.</summary>
    /// <param name="id">Созданный или переданный заказ.</param>
    /// <param name="version">Ожидаемая версия заказа.</param>
    /// <param name="ct">Отмена формирования.</param>
    /// <returns>Файл xlsx для самостоятельной отправки поставщику.</returns>
    Task<PurchaseExcelFile> ExportExcelAsync(Guid id, uint version, CancellationToken ct = default);
    /// <summary>Удаляет подборку, созданный или отменённый заказ без приёмок; карточки, цены и склад сохраняются. Требуется Admin.</summary>
    /// <param name="id">Удаляемая закупка.</param><param name="version">Версия, показанная сотруднику.</param>
    /// <param name="operationId">Идентификатор подтверждённой команды для безопасного повтора.</param><param name="ct">Отмена операции.</param>
    /// <returns>Завершение атомарного удаления состава и заказа с записью аудита.</returns>
    Task DeleteAsync(Guid id, uint version, Guid operationId, CancellationToken ct = default);
    /// <summary>Список закупок с серверной пагинацией.</summary>
    Task<AdminPage<PurchaseSummary>> OrdersAsync(AdminQuery query, CancellationToken ct = default);
    /// <summary>Начинает или возвращает сборку текущего сотрудника; не отправляет заказ поставщику.</summary>
    Task<Guid> StartAsync(Guid supplierId, Guid operationId, CancellationToken ct = default);
    /// <summary>Читает суммы, статус и версию закупки.</summary>
    Task<PurchaseSummary> OrderAsync(Guid id, CancellationToken ct = default);
    /// <summary>Читает страницу прайса с выбранными количествами; selectedOnly показывает только заказанное.</summary>
    Task<AdminPage<PurchasePosition>> PositionsAsync(Guid id, AdminQuery query, bool selectedOnly, CancellationToken ct = default, string? brand = null, string? category = null);
    /// <summary>Возвращает значения фильтров по всем предложениям поставщика данного заказа.</summary>
    Task<PurchaseFacets> FacetsAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>Меняет счётчик на заданное неотрицательное количество с ожидаемой версией заказа.</summary>
    Task SetQuantityAsync(Guid id, uint version, Guid offerId, int quantity, Guid operationId, CancellationToken ct = default);
    /// <summary>Меняет выбранный тариф, склад и примечание только до передачи поставщику.</summary>
    Task ConfigureAsync(Guid id, uint version, Guid tierId, Guid warehouseId, string? note, Guid operationId, CancellationToken ct = default);
    /// <summary>Меняет этап закупки; приёмка выполняется отдельным методом.</summary>
    Task ChangeStatusAsync(Guid id, uint version, PurchaseStatus status, Guid operationId, CancellationToken ct = default);
    /// <summary>Принимает перечисленные количества; null означает весь ещё не принятый остаток заказа.</summary>
    Task ReceiveAsync(Guid id, uint version, IReadOnlyList<ReceivePosition>? positions, Guid operationId, CancellationToken ct = default);
    /// <summary>Возвращает три закупочных уровня и настройки порогов.</summary>
    Task<IReadOnlyList<PurchaseTier>> TiersAsync(Guid supplierId, CancellationToken ct = default);
    /// <summary>Сохраняет ручные пороги Admin, не меняя действующие заказы и данные прайса.</summary>
    Task SaveTiersAsync(Guid supplierId, IReadOnlyList<PurchaseTier> tiers, Guid operationId, CancellationToken ct = default);
    /// <summary>Список собственных складов.</summary>
    Task<IReadOnlyList<LookupItem>> WarehousesAsync(CancellationToken ct = default);
    /// <summary>Страница принятого товара, для которого ещё не создана карточка.</summary>
    Task<AdminPage<UnallocatedPosition>> UnallocatedAsync(AdminQuery query, CancellationToken ct = default);
    /// <summary>Страница существующих вариантов с собственным остатком или входящей закупкой.</summary>
    Task<AdminPage<OwnedPosition>> OwnedAsync(AdminQuery query, CancellationToken ct = default);
    /// <summary>Создаёт черновик только для выбранной закупленной позиции и атомарно переносит её принятый остаток.</summary>
    /// <param name="offerId">Выбранная позиция поставщика.</param>
    /// <param name="name">Собственное название новой карточки.</param>
    /// <param name="brandId">Существующий бренд; имеет приоритет над введённым названием.</param>
    /// <param name="categoryId">Существующая категория; имеет приоритет над введённым названием.</param>
    /// <param name="operationId">Идентификатор идемпотентной команды.</param>
    /// <param name="ct">Отмена транзакции.</param>
    /// <param name="brandName">Название нового или существующего бренда; сохраняется только вместе с новой карточкой.</param>
    /// <param name="categoryName">Название категории; новая категория создаётся без родителя.</param>
    /// <returns>Идентификатор созданной либо ранее связанной карточки.</returns>
    Task<Guid> CreateCardAsync(Guid offerId, string name, Guid? brandId, Guid? categoryId, Guid operationId, CancellationToken ct = default, string? brandName = null, string? categoryName = null);
    /// <summary>Читает текущую розничную цену и исторические закупки выбранного варианта.</summary>
    Task<PurchaseRetail> RetailAsync(Guid variantId, CancellationToken ct = default);
    /// <summary>Читает базовую цену розницы или опта для карточки товара и закупочные снимки.</summary>
    Task<PurchaseRetail> SalePriceAsync(Guid variantId, CustomerSegment segment, CancellationToken ct = default, decimal minimumQuantity = 1);
    /// <summary>Читает базовый оптовый порог и существующие количественные ступени для редактирования в карточке.</summary>
    Task<IReadOnlyList<decimal>> WholesaleQuantitiesAsync(Guid variantId, CancellationToken ct = default);

    /// <summary>Сохраняет независимую цену выбранного формата, проверяя Admin, версию, закупку и единицы.</summary>
    Task SetSalePriceAsync(Guid variantId, CustomerSegment segment, uint priceVersion, Guid? costLineId, decimal? fixedPrice, decimal? markupPercent, Guid operationId, CancellationToken ct = default, decimal minimumQuantity = 1);

    /// <summary>Устанавливает явную цену либо рассчитывает её от выбранной закупки; оба режима применяются только командой Admin.</summary>
    Task SetRetailAsync(Guid variantId, uint priceVersion, Guid? costLineId, decimal? fixedPrice, decimal? markupPercent, Guid operationId, CancellationToken ct = default);
}

/// <summary>Доступные признаки для фильтрации закупочного прайса.</summary>
/// <param name="Brands">Отдельные бренды и неопределённое значение.</param>
/// <param name="Categories">Товарные группы поставщика и неопределённое значение.</param>
public sealed record PurchaseFacets(IReadOnlyList<string> Brands, IReadOnlyList<string> Categories);
