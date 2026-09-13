using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SportsStore.Infrastructure.Identity;
using SportsStore.Domain.Entities;
namespace SportsStore.Infrastructure.Persistence;
/// <summary>Единая EF-модель бизнеса и Identity. Фабрика создаёт отдельный контекст на операцию; экземпляр не хранится в Blazor-компоненте.</summary>
/// <param name="options">Параметры подключения и модели создаваемых контекстов.</param>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>Закупочный заказ магазина поставщику; не является заказом покупателя.</summary>
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    /// <summary>Выбранная позиция закупки со снимками исходных данных и цен; новый прайс не меняет заказ.</summary>
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    /// <summary>Неизменяемая запись приёмки; повтор той же операции не увеличивает остаток повторно.</summary>
    public DbSet<PurchaseReceiptLine> PurchaseReceiptLines => Set<PurchaseReceiptLine>();
    /// <summary>Фактически полученный товар до сопоставления с карточкой; при создании карточки переносится в InventoryBalance.</summary>
    public DbSet<UnallocatedStock> UnallocatedStocks => Set<UnallocatedStock>();
    /// <summary>Журнал административных изменений, сохраняемый в транзакциях бизнес-операций.</summary>
    public DbSet<AdminAudit> AdminAudits => Set<AdminAudit>();
    /// <summary>Набор EF для запросов и сохранения. Бренды собственного каталога магазина.</summary>
    public DbSet<Brand> Brands => Set<Brand>();
    /// <summary>Набор EF для запросов и сохранения. Иерархия категорий собственного каталога; циклические связи запрещены.</summary>
    public DbSet<Category> Categories => Set<Category>();
    /// <summary>Набор EF для запросов и сохранения. Собственные карточки товаров. Импорт прайса поставщика не создаёт и не публикует карточки автоматически.</summary>
    public DbSet<Product> Products => Set<Product>();
    /// <summary>Набор EF для запросов и сохранения. Продаваемые варианты товаров с уникальным внутренним SKU, единицей продажи, размером и цветом.</summary>
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    /// <summary>Набор EF для запросов и сохранения. Изображения собственных карточек товаров и порядок их отображения.</summary>
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    /// <summary>Набор EF для запросов и сохранения. Поставщики и явно подтверждённые условия закупки у них.</summary>
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    /// <summary>Набор EF для запросов и сохранения. Закупочные тарифы поставщика. Их пороги относятся к закупкам магазина, а не к покупателям магазина.</summary>
    public DbSet<SupplierPriceTier> SupplierPriceTiers => Set<SupplierPriceTier>();
    /// <summary>Набор EF для запросов и сохранения. Закупочные предложения поставщиков; могут храниться без связи с собственным каталогом.</summary>
    public DbSet<SupplierOffer> SupplierOffers => Set<SupplierOffer>();
    /// <summary>Набор EF для запросов и сохранения. Действующие закупочные цены предложений по тарифам поставщика.</summary>
    public DbSet<SupplierOfferPrice> SupplierOfferPrices => Set<SupplierOfferPrice>();
    /// <summary>Набор EF для запросов и сохранения. История закупочных цен с указанием партии импорта, вызвавшей изменение.</summary>
    public DbSet<SupplierPriceHistory> SupplierPriceHistories => Set<SupplierPriceHistory>();
    /// <summary>Набор EF для запросов и сохранения. Партии загрузки прайсов: предварительная проверка, диагностика и отдельное транзакционное применение.</summary>
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    /// <summary>Набор EF для запросов и сохранения. Исходные и разобранные строки прайса, сохранённые для проверки импорта.</summary>
    public DbSet<ImportRow> ImportRows => Set<ImportRow>();
    /// <summary>Набор EF для запросов и сохранения. Покупатели: правовой тип, коммерческий сегмент и независимое подтверждение права на опт.</summary>
    public DbSet<Customer> Customers => Set<Customer>();
    /// <summary>Набор EF для запросов и сохранения. Реквизиты ИП и организаций; содержат защищаемые сведения покупателя.</summary>
    public DbSet<OrganizationProfile> OrganizationProfiles => Set<OrganizationProfile>();
    /// <summary>Набор EF для запросов и сохранения. Адреса и получатели покупателя; персональные данные с ограниченным доступом.</summary>
    public DbSet<CustomerAddress> CustomerAddresss => Set<CustomerAddress>();
    /// <summary>Набор EF для запросов и сохранения. Правила наценки: вариант, затем ближайшая категория по иерархии, затем общее правило; внутри области учитывается количество SKU.</summary>
    public DbSet<MarkupRule> MarkupRules => Set<MarkupRule>();
    /// <summary>Набор EF для запросов и сохранения. Общие настройки собственных цен магазина; единственная запись с фиксированным идентификатором.</summary>
    public DbSet<PricingSettings> PricingSettings => Set<PricingSettings>();
    /// <summary>Набор EF для запросов и сохранения. Опубликованные цены продажи за единицу магазина по сегменту и количественной ступени. Ручные цены защищены от пересчёта.</summary>
    public DbSet<SalePrice> SalePrices => Set<SalePrice>();
    /// <summary>Набор EF для запросов и сохранения. История опубликованных собственных цен с источником изменения.</summary>
    public DbSet<SalePriceHistory> SalePriceHistories => Set<SalePriceHistory>();
    /// <summary>Набор EF для запросов и сохранения. Предложения пересчёта цен; меняют опубликованные цены только после применения, если явно не включён автоматический режим.</summary>
    public DbSet<PriceProposal> PriceProposals => Set<PriceProposal>();
    /// <summary>Набор EF для запросов и сохранения. Собственные склады магазина.</summary>
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    /// <summary>Набор EF для запросов и сохранения. Собственные остатки и резервы по варианту и складу. Импорт остатков поставщика их не изменяет.</summary>
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    /// <summary>Набор EF для запросов и сохранения. Основа заказов со снимками контактов, адреса и реквизитов на момент оформления.</summary>
    public DbSet<Order> Orders => Set<Order>();
    /// <summary>Набор EF для запросов и сохранения. Строки заказа со снимками товара, количества, цены и скидки; изменения каталога не меняют историю заказа.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    /// <summary>Дополняет стандартную модель Identity конфигурациями бизнес-сущностей и русскими комментариями базы данных.</summary>
    /// <param name="builder">Построитель модели Entity Framework Core.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        RussianDatabaseComments.Apply(builder);
    }
}
