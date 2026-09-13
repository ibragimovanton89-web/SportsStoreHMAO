// Миграция EF Core: история изменения схемы. Комментарии поясняют назначение; операции выполняются штатным EF CLI.
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SportsStore.Infrastructure.Persistence.Migrations
{
        /// <summary>Версионированное изменение схемы PostgreSQL, применяемое через EF Core.</summary>
        public partial class RussianDatabaseComments : Migration
    {
                /// <summary>Применяет изменения этой миграции в прямом направлении.</summary>
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                COMMENT ON TABLE "__EFMigrationsHistory" IS 'Служебная история применённых миграций Entity Framework Core; вручную не изменять.';
                COMMENT ON COLUMN "__EFMigrationsHistory"."MigrationId" IS 'Идентификатор применённой миграции: временная метка и название.';
                COMMENT ON COLUMN "__EFMigrationsHistory"."ProductVersion" IS 'Версия Entity Framework Core, записавшая миграцию.';
                """);
            migrationBuilder.AlterTable(
                name: "Warehouse",
                comment: "Собственные склады магазина.");

            migrationBuilder.AlterTable(
                name: "SupplierPriceTier",
                comment: "Закупочные тарифы поставщика. Их пороги относятся к закупкам магазина, а не к покупателям магазина.");

            migrationBuilder.AlterTable(
                name: "SupplierPriceHistory",
                comment: "История закупочных цен с указанием партии импорта, вызвавшей изменение.");

            migrationBuilder.AlterTable(
                name: "SupplierOfferPrice",
                comment: "Действующие закупочные цены предложений по тарифам поставщика.");

            migrationBuilder.AlterTable(
                name: "SupplierOffer",
                comment: "Закупочные предложения поставщиков; могут храниться без связи с собственным каталогом.");

            migrationBuilder.AlterTable(
                name: "Supplier",
                comment: "Поставщики и явно подтверждённые условия закупки у них.");

            migrationBuilder.AlterTable(
                name: "SalePriceHistory",
                comment: "История опубликованных собственных цен с источником изменения.");

            migrationBuilder.AlterTable(
                name: "SalePrice",
                comment: "Опубликованные цены продажи за единицу магазина по сегменту и количественной ступени. Ручные цены защищены от пересчёта.");

            migrationBuilder.AlterTable(
                name: "ProductVariant",
                comment: "Продаваемые варианты товаров с уникальным внутренним SKU, единицей продажи, размером и цветом.");

            migrationBuilder.AlterTable(
                name: "ProductImage",
                comment: "Изображения собственных карточек товаров и порядок их отображения.");

            migrationBuilder.AlterTable(
                name: "Product",
                comment: "Собственные карточки товаров. Импорт прайса поставщика не создаёт и не публикует карточки автоматически.");

            migrationBuilder.AlterTable(
                name: "PricingSettings",
                comment: "Общие настройки собственных цен магазина; единственная запись с фиксированным идентификатором.");

            migrationBuilder.AlterTable(
                name: "PriceProposal",
                comment: "Предложения пересчёта цен; меняют опубликованные цены только после применения, если явно не включён автоматический режим.");

            migrationBuilder.AlterTable(
                name: "OrganizationProfile",
                comment: "Реквизиты ИП и организаций; содержат защищаемые сведения покупателя.");

            migrationBuilder.AlterTable(
                name: "OrderItem",
                comment: "Строки заказа со снимками товара, количества, цены и скидки; изменения каталога не меняют историю заказа.");

            migrationBuilder.AlterTable(
                name: "Order",
                comment: "Основа заказов со снимками контактов, адреса и реквизитов на момент оформления.");

            migrationBuilder.AlterTable(
                name: "MarkupRule",
                comment: "Правила наценки: вариант, затем ближайшая категория по иерархии, затем общее правило; внутри области учитывается количество SKU.");

            migrationBuilder.AlterTable(
                name: "InventoryBalance",
                comment: "Собственные остатки и резервы по варианту и складу. Импорт остатков поставщика их не изменяет.");

            migrationBuilder.AlterTable(
                name: "ImportRow",
                comment: "Исходные и разобранные строки прайса, сохранённые для проверки импорта.");

            migrationBuilder.AlterTable(
                name: "ImportBatch",
                comment: "Партии загрузки прайсов: предварительная проверка, диагностика и отдельное транзакционное применение.");

            migrationBuilder.AlterTable(
                name: "CustomerAddress",
                comment: "Адреса и получатели покупателя; персональные данные с ограниченным доступом.");

            migrationBuilder.AlterTable(
                name: "Customer",
                comment: "Покупатели: правовой тип, коммерческий сегмент и независимое подтверждение права на опт.");

            migrationBuilder.AlterTable(
                name: "Category",
                comment: "Иерархия категорий собственного каталога; циклические связи запрещены.");

            migrationBuilder.AlterTable(
                name: "Brand",
                comment: "Бренды собственного каталога магазина.");

            migrationBuilder.AlterTable(
                name: "AspNetUserTokens",
                comment: "Служебные токены Identity; значения являются конфиденциальными.");

            migrationBuilder.AlterTable(
                name: "AspNetUsers",
                comment: "Учётные записи ASP.NET Core Identity. Пароли хранятся только в виде хешей.");

            migrationBuilder.AlterTable(
                name: "AspNetUserRoles",
                comment: "Назначения ролей доступа пользователям.");

            migrationBuilder.AlterTable(
                name: "AspNetUserLogins",
                comment: "Связи пользователей с внешними провайдерами входа.");

            migrationBuilder.AlterTable(
                name: "AspNetUserClaims",
                comment: "Утверждения Identity об отдельных пользователях для проверки доступа.");

            migrationBuilder.AlterTable(
                name: "AspNetRoles",
                comment: "Роли доступа Identity, включая Admin, Manager и Customer; не определяют коммерческий сегмент покупателя.");

            migrationBuilder.AlterTable(
                name: "AspNetRoleClaims",
                comment: "Утверждения Identity, предоставляемые через роли доступа.");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Warehouse",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Warehouse",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Название собственного склада магазина.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Warehouse",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SupplierPriceTier",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "SupplierPriceTier",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор поставщика (Supplier).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "SourceConditions",
                table: "SupplierPriceTier",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: false,
                comment: "Исходное описание условий тарифа из прайса; неподтверждённые формулы скидок не исполняются.",
                oldClrType: typeof(string),
                oldType: "character varying(16000)",
                oldMaxLength: 16000);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SupplierPriceTier",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Название закупочного тарифа поставщика.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumAmount",
                table: "SupplierPriceTier",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                comment: "Минимальная сумма закупки у поставщика для тарифа; NULL, если неизвестна. Не является минимумом заказа покупателя магазина.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SupplierPriceTier",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                comment: "Трёхбуквенный код валюты, например RUB (российский рубль).",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SupplierPriceTier",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Код закупочного тарифа в пределах поставщика.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SupplierPriceTier",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SupplierPriceHistory",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierPriceTierId",
                table: "SupplierPriceHistory",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор закупочного тарифа поставщика (SupplierPriceTier).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierOfferId",
                table: "SupplierPriceHistory",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор закупочного предложения поставщика (SupplierOffer).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "OldAmount",
                table: "SupplierPriceHistory",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                comment: "Предыдущая закупочная цена за единицу поставщика; NULL при первом появлении цены.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "NewAmount",
                table: "SupplierPriceHistory",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "Новая закупочная цена за единицу поставщика в валюте тарифа.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "SupplierPriceHistory",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор партии импорта прайса (ImportBatch).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "SupplierPriceHistory",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Момент изменения цены, UTC.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SupplierPriceHistory",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SupplierOfferPrice",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierPriceTierId",
                table: "SupplierOfferPrice",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор закупочного тарифа поставщика (SupplierPriceTier).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierOfferId",
                table: "SupplierOfferPrice",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор закупочного предложения поставщика (SupplierOffer).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "SupplierOfferPrice",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор партии импорта прайса (ImportBatch).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SupplierOfferPrice",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "Закупочная цена за одну единицу поставщика в валюте закупочного тарифа.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SupplierOfferPrice",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SupplierOffer",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitsPerBox",
                table: "SupplierOffer",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                comment: "Количество единиц поставщика в коробке; не доказывает обязательную покупку коробкой и не задаёт перевод единиц магазина.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplierUnit",
                table: "SupplierOffer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Единица, за которую указана закупочная цена поставщика (шт, пар, компл, упак).",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "SupplierOffer",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор поставщика (Supplier).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "Stock",
                table: "SupplierOffer",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                comment: "Остаток поставщика в его единицах: NULL — неизвестен, 0 — подтверждённый ноль. Не собственный склад магазина.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceSection",
                table: "SupplierOffer",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                comment: "Исходный раздел прайса; не является автоматически брендом или категорией магазина.",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "SourceName",
                table: "SupplierOffer",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                comment: "Исходное название позиции в прайсе поставщика.",
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "SourceDate",
                table: "SupplierOffer",
                type: "date",
                nullable: false,
                comment: "Дата документа прайса поставщика; не время его загрузки.",
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<decimal>(
                name: "SaleUnitsPerSupplierUnit",
                table: "SupplierOffer",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                comment: "Количество единиц продажи магазина в одной единице цены поставщика; закупочная цена делится на этот коэффициент только после подтверждения.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReviewSuggestionsJson",
                table: "SupplierOffer",
                type: "jsonb",
                nullable: false,
                comment: "JSON с неподтверждёнными характеристиками для ручной проверки; не публикуется автоматически.",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "SupplierOffer",
                type: "uuid",
                nullable: true,
                comment: "Результат сопоставления с вариантом магазина; NULL для несопоставленного предложения.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OrderMultiple",
                table: "SupplierOffer",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                comment: "Подтверждённая кратность заказа у поставщика в его единицах; NULL — неизвестна.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumOrderQuantity",
                table: "SupplierOffer",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                comment: "Подтверждённое минимальное количество заказа у поставщика в единицах поставщика; NULL — неизвестно.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSeenAt",
                table: "SupplierOffer",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Момент последнего появления предложения в применённом прайсе, UTC.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalCode",
                table: "SupplierOffer",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "Код позиции поставщика строкой с ведущими нулями; уникален вместе с SupplierId.",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<bool>(
                name: "ConversionConfirmed",
                table: "SupplierOffer",
                type: "boolean",
                nullable: false,
                comment: "Подтверждён ли коэффициент перевода единиц; без подтверждения автоматический расчёт цены запрещён.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SupplierOffer",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Supplier",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SelectedPriceTierId",
                table: "Supplier",
                type: "uuid",
                nullable: true,
                comment: "Явно выбранный закупочный тариф; минимальная цена автоматически не выбирается.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Revision",
                table: "Supplier",
                type: "bigint",
                nullable: false,
                comment: "Ревизия данных поставщика для защиты от применения устаревшего предварительного импорта.",
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<bool>(
                name: "PriceTierConfirmed",
                table: "Supplier",
                type: "boolean",
                nullable: false,
                comment: "Подтверждено ли право магазина закупать по выбранному тарифу.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Supplier",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Название поставщика.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "LatestSourceDate",
                table: "Supplier",
                type: "date",
                nullable: true,
                comment: "Дата последнего применённого прайса; NULL до первого применения.",
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Supplier",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Уникальный внутренний код поставщика.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Supplier",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SalePriceHistory",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "SalePriceHistory",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                comment: "Источник изменения цены: описание ручной установки или данные расчёта для аудита.",
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "Segment",
                table: "SalePriceHistory",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "SalePriceHistory",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор продаваемого варианта собственного каталога (ProductVariant).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "OldAmount",
                table: "SalePriceHistory",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "Предыдущая цена за единицу продажи магазина; NULL при первой публикации.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "NewAmount",
                table: "SalePriceHistory",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "Новая опубликованная цена за единицу продажи магазина.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumQuantity",
                table: "SalePriceHistory",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SalePriceHistory",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                comment: "Трёхбуквенный код валюты, например RUB (российский рубль).",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "SalePriceHistory",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Момент изменения цены, UTC.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SalePriceHistory",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SalePrice",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "SalePrice",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                comment: "Источник изменения цены: описание ручной установки или данные расчёта для аудита.",
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "Segment",
                table: "SalePrice",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PublishedAt",
                table: "SalePrice",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Момент публикации действующей цены, UTC.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "SalePrice",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор продаваемого варианта собственного каталога (ProductVariant).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumQuantity",
                table: "SalePrice",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<bool>(
                name: "IsManual",
                table: "SalePrice",
                type: "boolean",
                nullable: false,
                comment: "Цена установлена вручную и защищена от автоматического пересчёта при импорте.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SalePrice",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                comment: "Трёхбуквенный код валюты, например RUB (российский рубль).",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SalePrice",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "Опубликованная цена за единицу продажи магазина в указанной валюте.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SalePrice",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "ProductVariant",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Уникальный внутренний код продаваемого варианта магазина; отличается от кода поставщика.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Размер продаваемого варианта, если применим.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SaleUnit",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Единица продажи магазина; не обязательно совпадает с единицей цены поставщика.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductVariant",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор собственной карточки товара (Product).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "PricingSupplierOfferId",
                table: "ProductVariant",
                type: "uuid",
                nullable: true,
                comment: "Явно выбранное предложение поставщика для расчёта цены; NULL блокирует автоматический выбор закупочной цены.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ManufacturerCode",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Подтверждённый артикул производителя, если известен.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Цвет продаваемого варианта, если применим.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Штрихкод варианта строкой с сохранением ведущих нулей, если известен.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ProductVariant",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "ProductImage",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "ProductImage",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Адрес изображения товара.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "ProductImage",
                type: "integer",
                nullable: false,
                comment: "Порядок изображения при отображении; меньшие значения идут первыми.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductImage",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор собственной карточки товара (Product).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ProductImage",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Product",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Product",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Момент последнего изменения карточки, UTC.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Product",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Статус карточки: Draft — черновик, Published — опубликована, Archived — архив.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Product",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Собственное название карточки товара, независимое от прайса поставщика.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Product",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true,
                comment: "Собственное описание товара; импорт его не перезаписывает.",
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Product",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Момент создания записи, UTC.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Product",
                type: "uuid",
                nullable: true,
                comment: "Идентификатор категории собственного каталога (Category).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BrandId",
                table: "Product",
                type: "uuid",
                nullable: true,
                comment: "Бренд товара; NULL, если ещё не определён.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Product",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "PricingSettings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxTreatmentNote",
                table: "PricingSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Настраиваемое описание налогового режима и трактовки цен; NULL до подтверждения владельцем.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumWholesaleOrder",
                table: "PricingSettings",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                comment: "Собственная минимальная сумма оптового заказа магазина в указанной валюте; NULL — не настроена.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PricingSettings",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                comment: "Трёхбуквенный код валюты, например RUB (российский рубль).",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<bool>(
                name: "AutoApplyProposals",
                table: "PricingSettings",
                type: "boolean",
                nullable: false,
                comment: "Явное разрешение автоматически применять предложения пересчёта; по умолчанию выключено. Ручные цены защищены.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PricingSettings",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "PriceProposal",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierOfferId",
                table: "PriceProposal",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор закупочного предложения поставщика (SupplierOffer).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PriceProposal",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Статус пересчёта: Pending — ожидает применения, Applied — применён, Superseded — заменён или устарел.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "PriceProposal",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                comment: "Источник изменения цены: описание ручной установки или данные расчёта для аудита.",
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "Segment",
                table: "PriceProposal",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "PriceProposal",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор продаваемого варианта собственного каталога (ProductVariant).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumQuantity",
                table: "PriceProposal",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "PriceProposal",
                type: "uuid",
                nullable: true,
                comment: "Идентификатор партии импорта прайса (ImportBatch).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Fingerprint",
                table: "PriceProposal",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Отпечаток исходных параметров расчёта для обнаружения устаревшего предложения пересчёта.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PriceProposal",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                comment: "Трёхбуквенный код валюты, например RUB (российский рубль).",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PriceProposal",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Момент создания записи, UTC.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PriceProposal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "Предлагаемая цена за единицу продажи магазина после расчёта наценки и округления.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PriceProposal",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "OrganizationProfile",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "LegalName",
                table: "OrganizationProfile",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Официальное наименование организации или ИП.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Kpp",
                table: "OrganizationProfile",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "КПП организации; NULL, если неприменим или не указан.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Inn",
                table: "OrganizationProfile",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "ИНН физлица-предпринимателя или организации; хранится строкой.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "OrganizationProfile",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор покупателя (Customer).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "OrganizationProfile",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "OrderItem",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderItem",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "Зафиксированная цена за единицу до вычета UnitDiscount, в валюте заказа.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitDiscount",
                table: "OrderItem",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "Зафиксированная скидка на одну единицу в валюте заказа; итог строки = Quantity × (UnitPrice − UnitDiscount).",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "OrderItem",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Снимок внутреннего SKU на момент заказа.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "SaleUnit",
                table: "OrderItem",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Снимок единицы продажи на момент заказа.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "OrderItem",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "Заказанное количество в зафиксированных единицах продажи.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "OrderItem",
                type: "uuid",
                nullable: true,
                comment: "Необязательная ссылка на вариант каталога; история строки сохраняется независимо от изменений каталога.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "OrderItem",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор заказа, которому принадлежит строка (Order).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OrderItem",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Снимок названия товара на момент заказа.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "OrderItem",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Order",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShippingAddress",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Снимок адреса доставки на момент заказа; изменения адресов покупателя его не меняют.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "SalesFormat",
                table: "Order",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Фактически применённый формат продажи: Retail — розница, Wholesale — опт.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationName",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Снимок наименования организации или ИП на момент заказа; NULL, если неприменимо.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Уникальный номер заказа магазина.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Kpp",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Снимок КПП на момент заказа; NULL, если неприменимо.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Inn",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Снимок ИНН на момент заказа; NULL, если неприменимо.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "Order",
                type: "uuid",
                nullable: true,
                comment: "Необязательная ссылка на покупателя; исполнение и история используют снимки данных в заказе.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Order",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                comment: "Трёхбуквенный код валюты, например RUB (российский рубль).",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Order",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Момент создания записи, UTC.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "ContactPhone",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Снимок контактного телефона на момент заказа; персональные данные.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ContactName",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Снимок имени контактного лица на момент заказа; персональные данные.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Снимок контактного адреса электронной почты на момент заказа; персональные данные.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Order",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "MarkupRule",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "Segment",
                table: "MarkupRule",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "MarkupRule",
                type: "uuid",
                nullable: true,
                comment: "Вариант для индивидуального правила с наивысшим приоритетом; NULL для категорийного или общего правила.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumQuantity",
                table: "MarkupRule",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "MarkupPercent",
                table: "MarkupRule",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "Наценка в процентах к закупочной стоимости, не маржа: стоимость × (1 + процент / 100).",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "MarkupRule",
                type: "uuid",
                nullable: true,
                comment: "Категория действия правила, включая потомков; ближайший предок имеет приоритет. NULL для правила варианта или общего правила.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "MarkupRule",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "InventoryBalance",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                table: "InventoryBalance",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор собственного склада (Warehouse).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "Reserved",
                table: "InventoryBalance",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "Зарезервированное количество в единицах продажи; от 0 до OnHand. Доступно OnHand − Reserved.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "InventoryBalance",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор продаваемого варианта собственного каталога (ProductVariant).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "OnHand",
                table: "InventoryBalance",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "Физическое количество на собственном складе в единицах продажи; неотрицательное.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "InventoryBalance",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "ImportRow",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<int>(
                name: "RowNumber",
                table: "ImportRow",
                type: "integer",
                nullable: false,
                comment: "Номер строки в исходном листе Excel, начиная с 1.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "RawJson",
                table: "ImportRow",
                type: "jsonb",
                nullable: false,
                comment: "Исходные значения ячеек в JSON для аудита; содержимое не исполняется.",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "ParsedJson",
                table: "ImportRow",
                type: "jsonb",
                nullable: true,
                comment: "Нормализованные данные предложения в JSON; NULL для строк без распознанной позиции.",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MatchResult",
                table: "ImportRow",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Результат сопоставления строки с действующими предложениями при предварительной проверке.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "ImportRow",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Тип строки: Empty — пустая, Header — заголовок, Section — раздел, Product — позиция, Error — ошибочная.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "ImportRow",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор партии импорта прайса (ImportBatch).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Diagnostics",
                table: "ImportRow",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                comment: "Ошибки и замечания при разборе и проверке строки.",
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ImportRow",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "ImportBatch",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UploadedAt",
                table: "ImportBatch",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Момент загрузки файла, UTC.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "UnchangedCount",
                table: "ImportBatch",
                type: "integer",
                nullable: false,
                comment: "Количество неизменившихся предложений по результату предварительной проверки.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "TiersJson",
                table: "ImportBatch",
                type: "jsonb",
                nullable: false,
                comment: "Снимок распознанных закупочных тарифов в JSON до применения партии.",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "ImportBatch",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор поставщика (Supplier).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ImportBatch",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Статус партии: Preview — предварительная проверка, Invalid — есть ошибки, Applied — применена.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "SourceDate",
                table: "ImportBatch",
                type: "date",
                nullable: false,
                comment: "Дата документа прайса поставщика; не время его загрузки.",
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "Sha256",
                table: "ImportBatch",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "Контрольная сумма SHA-256 содержимого файла для обнаружения повторной загрузки.",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "ParserVersion",
                table: "ImportBatch",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Версия парсера, разобравшего файл.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<int>(
                name: "NewCount",
                table: "ImportBatch",
                type: "integer",
                nullable: false,
                comment: "Количество новых предложений по результату предварительной проверки.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "ImportBatch",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Имя загруженного файла поставщика.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<long>(
                name: "ExpectedSupplierRevision",
                table: "ImportBatch",
                type: "bigint",
                nullable: false,
                comment: "Ревизия поставщика на момент предварительной проверки; несовпадение при применении означает устаревшую партию.",
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "ErrorCount",
                table: "ImportBatch",
                type: "integer",
                nullable: false,
                comment: "Количество ошибок, обнаруженных при предварительной проверке.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "ImportBatch",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                comment: "Трёхбуквенный код валюты, например RUB (российский рубль).",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Conditions",
                table: "ImportBatch",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: false,
                comment: "Служебные условия закупок из шапки исходного прайса.",
                oldClrType: typeof(string),
                oldType: "character varying(16000)",
                oldMaxLength: 16000);

            migrationBuilder.AlterColumn<int>(
                name: "ChangedCount",
                table: "ImportBatch",
                type: "integer",
                nullable: false,
                comment: "Количество изменившихся предложений по результату предварительной проверки.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AppliedAt",
                table: "ImportBatch",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Момент применения партии, UTC; NULL до применения.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ImportBatch",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "CustomerAddress",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "Recipient",
                table: "CustomerAddress",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Имя получателя по адресу; персональные данные.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "CustomerAddress",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Почтовый индекс строкой; NULL, если не указан.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "CustomerAddress",
                type: "uuid",
                nullable: false,
                comment: "Идентификатор покупателя (Customer).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "CustomerAddress",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Адрес доставки покупателя; персональные данные.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "CustomerAddress",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Customer",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "WholesaleStatus",
                table: "Customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Подтверждение опта: NotRequested — не запрошен, Pending — проверяется, Approved — одобрен, Rejected — отклонён. Право проверяется сервером.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Segment",
                table: "Customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "Правовой тип: Individual — физлицо, SoleProprietor — ИП, Organization — организация.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Отображаемое имя покупателя; может содержать персональные данные.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "Customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Необязательная уникальная ссылка на учётную запись Identity; собственных паролей у покупателя нет.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Customer",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Category",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "Category",
                type: "uuid",
                nullable: true,
                comment: "Родительская категория; NULL для корня. Циклы запрещены.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Category",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Название категории магазина.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Category",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Brand",
                type: "xid",
                rowVersion: true,
                nullable: false,
                comment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.",
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Brand",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Название бренда собственного каталога.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Brand",
                type: "uuid",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "AspNetUserTokens",
                type: "text",
                nullable: true,
                comment: "Конфиденциальное значение служебного токена Identity; не выводить в журналы.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                comment: "Имя служебного токена Identity.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                comment: "Имя провайдера входа или провайдера служебного токена Identity.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                comment: "Идентификатор учётной записи пользователя (AspNetUsers).",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "Имя пользователя для входа.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "TwoFactorEnabled",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                comment: "Включена ли двухфакторная аутентификация.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "SecurityStamp",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                comment: "Метка безопасности Identity для отзыва ранее выданных сеансов при изменении учётных данных.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                comment: "Подтверждён ли телефон пользователя.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                comment: "Телефон пользователя; персональные данные.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                comment: "Хеш пароля в формате ASP.NET Core Identity; не открытый пароль. Конфиденциальное значение.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedUserName",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "Нормализованное имя пользователя для поиска и проверки уникальности.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "Нормализованный адрес электронной почты для поиска.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Момент окончания блокировки входа; NULL, если срок не задан.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "LockoutEnabled",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                comment: "Разрешена ли блокировка при неудачных попытках входа.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "EmailConfirmed",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                comment: "Подтверждён ли адрес электронной почты.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "Адрес электронной почты пользователя; персональные данные.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                comment: "Метка Identity для обнаружения конкурентного изменения записи.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AccessFailedCount",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                comment: "Число учитываемых Identity неудачных попыток входа.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "RoleId",
                table: "AspNetUserRoles",
                type: "text",
                nullable: false,
                comment: "Идентификатор роли доступа (AspNetRoles).",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserRoles",
                type: "text",
                nullable: false,
                comment: "Идентификатор учётной записи пользователя (AspNetUsers).",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                comment: "Идентификатор учётной записи пользователя (AspNetUsers).",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderDisplayName",
                table: "AspNetUserLogins",
                type: "text",
                nullable: true,
                comment: "Отображаемое название внешнего провайдера входа.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                comment: "Идентификатор пользователя у внешнего провайдера входа.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                comment: "Имя провайдера входа или провайдера служебного токена Identity.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserClaims",
                type: "text",
                nullable: false,
                comment: "Идентификатор учётной записи пользователя (AspNetUsers).",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimValue",
                table: "AspNetUserClaims",
                type: "text",
                nullable: true,
                comment: "Значение утверждения Identity (claim).",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClaimType",
                table: "AspNetUserClaims",
                type: "text",
                nullable: true,
                comment: "Тип утверждения Identity (claim).",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "AspNetUserClaims",
                type: "integer",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "AspNetRoles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "Нормализованное название роли для поиска и проверки уникальности.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetRoles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "Название роли доступа, например Admin, Manager или Customer.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "AspNetRoles",
                type: "text",
                nullable: true,
                comment: "Метка Identity для обнаружения конкурентного изменения записи.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "AspNetRoles",
                type: "text",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "RoleId",
                table: "AspNetRoleClaims",
                type: "text",
                nullable: false,
                comment: "Идентификатор роли доступа (AspNetRoles).",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimValue",
                table: "AspNetRoleClaims",
                type: "text",
                nullable: true,
                comment: "Значение утверждения Identity (claim).",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClaimType",
                table: "AspNetRoleClaims",
                type: "text",
                nullable: true,
                comment: "Тип утверждения Identity (claim).",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "AspNetRoleClaims",
                type: "integer",
                nullable: false,
                comment: "Уникальный идентификатор записи (первичный ключ).",
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }

                /// <summary>Отменяет изменения этой миграции при явном откате версии БД.</summary>
                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                COMMENT ON TABLE "__EFMigrationsHistory" IS NULL;
                COMMENT ON COLUMN "__EFMigrationsHistory"."MigrationId" IS NULL;
                COMMENT ON COLUMN "__EFMigrationsHistory"."ProductVersion" IS NULL;
                """);
            migrationBuilder.AlterTable(
                name: "Warehouse",
                oldComment: "Собственные склады магазина.");

            migrationBuilder.AlterTable(
                name: "SupplierPriceTier",
                oldComment: "Закупочные тарифы поставщика. Их пороги относятся к закупкам магазина, а не к покупателям магазина.");

            migrationBuilder.AlterTable(
                name: "SupplierPriceHistory",
                oldComment: "История закупочных цен с указанием партии импорта, вызвавшей изменение.");

            migrationBuilder.AlterTable(
                name: "SupplierOfferPrice",
                oldComment: "Действующие закупочные цены предложений по тарифам поставщика.");

            migrationBuilder.AlterTable(
                name: "SupplierOffer",
                oldComment: "Закупочные предложения поставщиков; могут храниться без связи с собственным каталогом.");

            migrationBuilder.AlterTable(
                name: "Supplier",
                oldComment: "Поставщики и явно подтверждённые условия закупки у них.");

            migrationBuilder.AlterTable(
                name: "SalePriceHistory",
                oldComment: "История опубликованных собственных цен с источником изменения.");

            migrationBuilder.AlterTable(
                name: "SalePrice",
                oldComment: "Опубликованные цены продажи за единицу магазина по сегменту и количественной ступени. Ручные цены защищены от пересчёта.");

            migrationBuilder.AlterTable(
                name: "ProductVariant",
                oldComment: "Продаваемые варианты товаров с уникальным внутренним SKU, единицей продажи, размером и цветом.");

            migrationBuilder.AlterTable(
                name: "ProductImage",
                oldComment: "Изображения собственных карточек товаров и порядок их отображения.");

            migrationBuilder.AlterTable(
                name: "Product",
                oldComment: "Собственные карточки товаров. Импорт прайса поставщика не создаёт и не публикует карточки автоматически.");

            migrationBuilder.AlterTable(
                name: "PricingSettings",
                oldComment: "Общие настройки собственных цен магазина; единственная запись с фиксированным идентификатором.");

            migrationBuilder.AlterTable(
                name: "PriceProposal",
                oldComment: "Предложения пересчёта цен; меняют опубликованные цены только после применения, если явно не включён автоматический режим.");

            migrationBuilder.AlterTable(
                name: "OrganizationProfile",
                oldComment: "Реквизиты ИП и организаций; содержат защищаемые сведения покупателя.");

            migrationBuilder.AlterTable(
                name: "OrderItem",
                oldComment: "Строки заказа со снимками товара, количества, цены и скидки; изменения каталога не меняют историю заказа.");

            migrationBuilder.AlterTable(
                name: "Order",
                oldComment: "Основа заказов со снимками контактов, адреса и реквизитов на момент оформления.");

            migrationBuilder.AlterTable(
                name: "MarkupRule",
                oldComment: "Правила наценки: вариант, затем ближайшая категория по иерархии, затем общее правило; внутри области учитывается количество SKU.");

            migrationBuilder.AlterTable(
                name: "InventoryBalance",
                oldComment: "Собственные остатки и резервы по варианту и складу. Импорт остатков поставщика их не изменяет.");

            migrationBuilder.AlterTable(
                name: "ImportRow",
                oldComment: "Исходные и разобранные строки прайса, сохранённые для проверки импорта.");

            migrationBuilder.AlterTable(
                name: "ImportBatch",
                oldComment: "Партии загрузки прайсов: предварительная проверка, диагностика и отдельное транзакционное применение.");

            migrationBuilder.AlterTable(
                name: "CustomerAddress",
                oldComment: "Адреса и получатели покупателя; персональные данные с ограниченным доступом.");

            migrationBuilder.AlterTable(
                name: "Customer",
                oldComment: "Покупатели: правовой тип, коммерческий сегмент и независимое подтверждение права на опт.");

            migrationBuilder.AlterTable(
                name: "Category",
                oldComment: "Иерархия категорий собственного каталога; циклические связи запрещены.");

            migrationBuilder.AlterTable(
                name: "Brand",
                oldComment: "Бренды собственного каталога магазина.");

            migrationBuilder.AlterTable(
                name: "AspNetUserTokens",
                oldComment: "Служебные токены Identity; значения являются конфиденциальными.");

            migrationBuilder.AlterTable(
                name: "AspNetUsers",
                oldComment: "Учётные записи ASP.NET Core Identity. Пароли хранятся только в виде хешей.");

            migrationBuilder.AlterTable(
                name: "AspNetUserRoles",
                oldComment: "Назначения ролей доступа пользователям.");

            migrationBuilder.AlterTable(
                name: "AspNetUserLogins",
                oldComment: "Связи пользователей с внешними провайдерами входа.");

            migrationBuilder.AlterTable(
                name: "AspNetUserClaims",
                oldComment: "Утверждения Identity об отдельных пользователях для проверки доступа.");

            migrationBuilder.AlterTable(
                name: "AspNetRoles",
                oldComment: "Роли доступа Identity, включая Admin, Manager и Customer; не определяют коммерческий сегмент покупателя.");

            migrationBuilder.AlterTable(
                name: "AspNetRoleClaims",
                oldComment: "Утверждения Identity, предоставляемые через роли доступа.");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Warehouse",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Warehouse",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Название собственного склада магазина.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Warehouse",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SupplierPriceTier",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "SupplierPriceTier",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор поставщика (Supplier).");

            migrationBuilder.AlterColumn<string>(
                name: "SourceConditions",
                table: "SupplierPriceTier",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16000)",
                oldMaxLength: 16000,
                oldComment: "Исходное описание условий тарифа из прайса; неподтверждённые формулы скидок не исполняются.");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SupplierPriceTier",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Название закупочного тарифа поставщика.");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumAmount",
                table: "SupplierPriceTier",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true,
                oldComment: "Минимальная сумма закупки у поставщика для тарифа; NULL, если неизвестна. Не является минимумом заказа покупателя магазина.");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SupplierPriceTier",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldComment: "Трёхбуквенный код валюты, например RUB (российский рубль).");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SupplierPriceTier",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Код закупочного тарифа в пределах поставщика.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SupplierPriceTier",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SupplierPriceHistory",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierPriceTierId",
                table: "SupplierPriceHistory",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор закупочного тарифа поставщика (SupplierPriceTier).");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierOfferId",
                table: "SupplierPriceHistory",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор закупочного предложения поставщика (SupplierOffer).");

            migrationBuilder.AlterColumn<decimal>(
                name: "OldAmount",
                table: "SupplierPriceHistory",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true,
                oldComment: "Предыдущая закупочная цена за единицу поставщика; NULL при первом появлении цены.");

            migrationBuilder.AlterColumn<decimal>(
                name: "NewAmount",
                table: "SupplierPriceHistory",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "Новая закупочная цена за единицу поставщика в валюте тарифа.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "SupplierPriceHistory",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор партии импорта прайса (ImportBatch).");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "SupplierPriceHistory",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Момент изменения цены, UTC.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SupplierPriceHistory",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SupplierOfferPrice",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierPriceTierId",
                table: "SupplierOfferPrice",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор закупочного тарифа поставщика (SupplierPriceTier).");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierOfferId",
                table: "SupplierOfferPrice",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор закупочного предложения поставщика (SupplierOffer).");

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "SupplierOfferPrice",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор партии импорта прайса (ImportBatch).");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SupplierOfferPrice",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "Закупочная цена за одну единицу поставщика в валюте закупочного тарифа.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SupplierOfferPrice",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SupplierOffer",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitsPerBox",
                table: "SupplierOffer",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true,
                oldComment: "Количество единиц поставщика в коробке; не доказывает обязательную покупку коробкой и не задаёт перевод единиц магазина.");

            migrationBuilder.AlterColumn<string>(
                name: "SupplierUnit",
                table: "SupplierOffer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Единица, за которую указана закупочная цена поставщика (шт, пар, компл, упак).");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "SupplierOffer",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор поставщика (Supplier).");

            migrationBuilder.AlterColumn<decimal>(
                name: "Stock",
                table: "SupplierOffer",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true,
                oldComment: "Остаток поставщика в его единицах: NULL — неизвестен, 0 — подтверждённый ноль. Не собственный склад магазина.");

            migrationBuilder.AlterColumn<string>(
                name: "SourceSection",
                table: "SupplierOffer",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldComment: "Исходный раздел прайса; не является автоматически брендом или категорией магазина.");

            migrationBuilder.AlterColumn<string>(
                name: "SourceName",
                table: "SupplierOffer",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldComment: "Исходное название позиции в прайсе поставщика.");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "SourceDate",
                table: "SupplierOffer",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldComment: "Дата документа прайса поставщика; не время его загрузки.");

            migrationBuilder.AlterColumn<decimal>(
                name: "SaleUnitsPerSupplierUnit",
                table: "SupplierOffer",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true,
                oldComment: "Количество единиц продажи магазина в одной единице цены поставщика; закупочная цена делится на этот коэффициент только после подтверждения.");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewSuggestionsJson",
                table: "SupplierOffer",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldComment: "JSON с неподтверждёнными характеристиками для ручной проверки; не публикуется автоматически.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "SupplierOffer",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Результат сопоставления с вариантом магазина; NULL для несопоставленного предложения.");

            migrationBuilder.AlterColumn<decimal>(
                name: "OrderMultiple",
                table: "SupplierOffer",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true,
                oldComment: "Подтверждённая кратность заказа у поставщика в его единицах; NULL — неизвестна.");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumOrderQuantity",
                table: "SupplierOffer",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true,
                oldComment: "Подтверждённое минимальное количество заказа у поставщика в единицах поставщика; NULL — неизвестно.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSeenAt",
                table: "SupplierOffer",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Момент последнего появления предложения в применённом прайсе, UTC.");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalCode",
                table: "SupplierOffer",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "Код позиции поставщика строкой с ведущими нулями; уникален вместе с SupplierId.");

            migrationBuilder.AlterColumn<bool>(
                name: "ConversionConfirmed",
                table: "SupplierOffer",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Подтверждён ли коэффициент перевода единиц; без подтверждения автоматический расчёт цены запрещён.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SupplierOffer",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Supplier",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<Guid>(
                name: "SelectedPriceTierId",
                table: "Supplier",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Явно выбранный закупочный тариф; минимальная цена автоматически не выбирается.");

            migrationBuilder.AlterColumn<long>(
                name: "Revision",
                table: "Supplier",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldComment: "Ревизия данных поставщика для защиты от применения устаревшего предварительного импорта.");

            migrationBuilder.AlterColumn<bool>(
                name: "PriceTierConfirmed",
                table: "Supplier",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Подтверждено ли право магазина закупать по выбранному тарифу.");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Supplier",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Название поставщика.");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "LatestSourceDate",
                table: "Supplier",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true,
                oldComment: "Дата последнего применённого прайса; NULL до первого применения.");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Supplier",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Уникальный внутренний код поставщика.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Supplier",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SalePriceHistory",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "SalePriceHistory",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldComment: "Источник изменения цены: описание ручной установки или данные расчёта для аудита.");

            migrationBuilder.AlterColumn<string>(
                name: "Segment",
                table: "SalePriceHistory",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "SalePriceHistory",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор продаваемого варианта собственного каталога (ProductVariant).");

            migrationBuilder.AlterColumn<decimal>(
                name: "OldAmount",
                table: "SalePriceHistory",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true,
                oldComment: "Предыдущая цена за единицу продажи магазина; NULL при первой публикации.");

            migrationBuilder.AlterColumn<decimal>(
                name: "NewAmount",
                table: "SalePriceHistory",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "Новая опубликованная цена за единицу продажи магазина.");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumQuantity",
                table: "SalePriceHistory",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SalePriceHistory",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldComment: "Трёхбуквенный код валюты, например RUB (российский рубль).");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "SalePriceHistory",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Момент изменения цены, UTC.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SalePriceHistory",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "SalePrice",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "SalePrice",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldComment: "Источник изменения цены: описание ручной установки или данные расчёта для аудита.");

            migrationBuilder.AlterColumn<string>(
                name: "Segment",
                table: "SalePrice",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PublishedAt",
                table: "SalePrice",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Момент публикации действующей цены, UTC.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "SalePrice",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор продаваемого варианта собственного каталога (ProductVariant).");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumQuantity",
                table: "SalePrice",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.");

            migrationBuilder.AlterColumn<bool>(
                name: "IsManual",
                table: "SalePrice",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Цена установлена вручную и защищена от автоматического пересчёта при импорте.");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SalePrice",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldComment: "Трёхбуквенный код валюты, например RUB (российский рубль).");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SalePrice",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "Опубликованная цена за единицу продажи магазина в указанной валюте.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SalePrice",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "ProductVariant",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Уникальный внутренний код продаваемого варианта магазина; отличается от кода поставщика.");

            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Размер продаваемого варианта, если применим.");

            migrationBuilder.AlterColumn<string>(
                name: "SaleUnit",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Единица продажи магазина; не обязательно совпадает с единицей цены поставщика.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductVariant",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор собственной карточки товара (Product).");

            migrationBuilder.AlterColumn<Guid>(
                name: "PricingSupplierOfferId",
                table: "ProductVariant",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Явно выбранное предложение поставщика для расчёта цены; NULL блокирует автоматический выбор закупочной цены.");

            migrationBuilder.AlterColumn<string>(
                name: "ManufacturerCode",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Подтверждённый артикул производителя, если известен.");

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Цвет продаваемого варианта, если применим.");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ProductVariant",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Штрихкод варианта строкой с сохранением ведущих нулей, если известен.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ProductVariant",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "ProductImage",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "ProductImage",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Адрес изображения товара.");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "ProductImage",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Порядок изображения при отображении; меньшие значения идут первыми.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductImage",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор собственной карточки товара (Product).");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ProductImage",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Product",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Product",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Момент последнего изменения карточки, UTC.");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Product",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Статус карточки: Draft — черновик, Published — опубликована, Archived — архив.");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Product",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Собственное название карточки товара, независимое от прайса поставщика.");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Product",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000,
                oldNullable: true,
                oldComment: "Собственное описание товара; импорт его не перезаписывает.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Product",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Момент создания записи, UTC.");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Product",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Идентификатор категории собственного каталога (Category).");

            migrationBuilder.AlterColumn<Guid>(
                name: "BrandId",
                table: "Product",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Бренд товара; NULL, если ещё не определён.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Product",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "PricingSettings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "TaxTreatmentNote",
                table: "PricingSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Настраиваемое описание налогового режима и трактовки цен; NULL до подтверждения владельцем.");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumWholesaleOrder",
                table: "PricingSettings",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true,
                oldComment: "Собственная минимальная сумма оптового заказа магазина в указанной валюте; NULL — не настроена.");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PricingSettings",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldComment: "Трёхбуквенный код валюты, например RUB (российский рубль).");

            migrationBuilder.AlterColumn<bool>(
                name: "AutoApplyProposals",
                table: "PricingSettings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Явное разрешение автоматически применять предложения пересчёта; по умолчанию выключено. Ручные цены защищены.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PricingSettings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "PriceProposal",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierOfferId",
                table: "PriceProposal",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор закупочного предложения поставщика (SupplierOffer).");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PriceProposal",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Статус пересчёта: Pending — ожидает применения, Applied — применён, Superseded — заменён или устарел.");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "PriceProposal",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldComment: "Источник изменения цены: описание ручной установки или данные расчёта для аудита.");

            migrationBuilder.AlterColumn<string>(
                name: "Segment",
                table: "PriceProposal",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "PriceProposal",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор продаваемого варианта собственного каталога (ProductVariant).");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumQuantity",
                table: "PriceProposal",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "PriceProposal",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Идентификатор партии импорта прайса (ImportBatch).");

            migrationBuilder.AlterColumn<string>(
                name: "Fingerprint",
                table: "PriceProposal",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Отпечаток исходных параметров расчёта для обнаружения устаревшего предложения пересчёта.");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PriceProposal",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldComment: "Трёхбуквенный код валюты, например RUB (российский рубль).");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PriceProposal",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Момент создания записи, UTC.");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PriceProposal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "Предлагаемая цена за единицу продажи магазина после расчёта наценки и округления.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PriceProposal",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "OrganizationProfile",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "LegalName",
                table: "OrganizationProfile",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Официальное наименование организации или ИП.");

            migrationBuilder.AlterColumn<string>(
                name: "Kpp",
                table: "OrganizationProfile",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "КПП организации; NULL, если неприменим или не указан.");

            migrationBuilder.AlterColumn<string>(
                name: "Inn",
                table: "OrganizationProfile",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "ИНН физлица-предпринимателя или организации; хранится строкой.");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "OrganizationProfile",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор покупателя (Customer).");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "OrganizationProfile",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "OrderItem",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderItem",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "Зафиксированная цена за единицу до вычета UnitDiscount, в валюте заказа.");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitDiscount",
                table: "OrderItem",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "Зафиксированная скидка на одну единицу в валюте заказа; итог строки = Quantity × (UnitPrice − UnitDiscount).");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "OrderItem",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Снимок внутреннего SKU на момент заказа.");

            migrationBuilder.AlterColumn<string>(
                name: "SaleUnit",
                table: "OrderItem",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Снимок единицы продажи на момент заказа.");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "OrderItem",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "Заказанное количество в зафиксированных единицах продажи.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "OrderItem",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Необязательная ссылка на вариант каталога; история строки сохраняется независимо от изменений каталога.");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "OrderItem",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор заказа, которому принадлежит строка (Order).");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OrderItem",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Снимок названия товара на момент заказа.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "OrderItem",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Order",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "ShippingAddress",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Снимок адреса доставки на момент заказа; изменения адресов покупателя его не меняют.");

            migrationBuilder.AlterColumn<string>(
                name: "SalesFormat",
                table: "Order",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Фактически применённый формат продажи: Retail — розница, Wholesale — опт.");

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationName",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Снимок наименования организации или ИП на момент заказа; NULL, если неприменимо.");

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Уникальный номер заказа магазина.");

            migrationBuilder.AlterColumn<string>(
                name: "Kpp",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Снимок КПП на момент заказа; NULL, если неприменимо.");

            migrationBuilder.AlterColumn<string>(
                name: "Inn",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Снимок ИНН на момент заказа; NULL, если неприменимо.");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "Order",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Необязательная ссылка на покупателя; исполнение и история используют снимки данных в заказе.");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Order",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldComment: "Трёхбуквенный код валюты, например RUB (российский рубль).");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Order",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Момент создания записи, UTC.");

            migrationBuilder.AlterColumn<string>(
                name: "ContactPhone",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Снимок контактного телефона на момент заказа; персональные данные.");

            migrationBuilder.AlterColumn<string>(
                name: "ContactName",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Снимок имени контактного лица на момент заказа; персональные данные.");

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Снимок контактного адреса электронной почты на момент заказа; персональные данные.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Order",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "MarkupRule",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "Segment",
                table: "MarkupRule",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "MarkupRule",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Вариант для индивидуального правила с наивысшим приоритетом; NULL для категорийного или общего правила.");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumQuantity",
                table: "MarkupRule",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "Нижняя включительная граница ступени: количество одного SKU в единицах продажи магазина.");

            migrationBuilder.AlterColumn<decimal>(
                name: "MarkupPercent",
                table: "MarkupRule",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "Наценка в процентах к закупочной стоимости, не маржа: стоимость × (1 + процент / 100).");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "MarkupRule",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Категория действия правила, включая потомков; ближайший предок имеет приоритет. NULL для правила варианта или общего правила.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "MarkupRule",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "InventoryBalance",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                table: "InventoryBalance",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор собственного склада (Warehouse).");

            migrationBuilder.AlterColumn<decimal>(
                name: "Reserved",
                table: "InventoryBalance",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "Зарезервированное количество в единицах продажи; от 0 до OnHand. Доступно OnHand − Reserved.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductVariantId",
                table: "InventoryBalance",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор продаваемого варианта собственного каталога (ProductVariant).");

            migrationBuilder.AlterColumn<decimal>(
                name: "OnHand",
                table: "InventoryBalance",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "Физическое количество на собственном складе в единицах продажи; неотрицательное.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "InventoryBalance",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "ImportRow",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<int>(
                name: "RowNumber",
                table: "ImportRow",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Номер строки в исходном листе Excel, начиная с 1.");

            migrationBuilder.AlterColumn<string>(
                name: "RawJson",
                table: "ImportRow",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldComment: "Исходные значения ячеек в JSON для аудита; содержимое не исполняется.");

            migrationBuilder.AlterColumn<string>(
                name: "ParsedJson",
                table: "ImportRow",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "Нормализованные данные предложения в JSON; NULL для строк без распознанной позиции.");

            migrationBuilder.AlterColumn<string>(
                name: "MatchResult",
                table: "ImportRow",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Результат сопоставления строки с действующими предложениями при предварительной проверке.");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "ImportRow",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Тип строки: Empty — пустая, Header — заголовок, Section — раздел, Product — позиция, Error — ошибочная.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "ImportRow",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор партии импорта прайса (ImportBatch).");

            migrationBuilder.AlterColumn<string>(
                name: "Diagnostics",
                table: "ImportRow",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldComment: "Ошибки и замечания при разборе и проверке строки.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ImportRow",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "ImportBatch",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UploadedAt",
                table: "ImportBatch",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "Момент загрузки файла, UTC.");

            migrationBuilder.AlterColumn<int>(
                name: "UnchangedCount",
                table: "ImportBatch",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Количество неизменившихся предложений по результату предварительной проверки.");

            migrationBuilder.AlterColumn<string>(
                name: "TiersJson",
                table: "ImportBatch",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldComment: "Снимок распознанных закупочных тарифов в JSON до применения партии.");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "ImportBatch",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор поставщика (Supplier).");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ImportBatch",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Статус партии: Preview — предварительная проверка, Invalid — есть ошибки, Applied — применена.");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "SourceDate",
                table: "ImportBatch",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldComment: "Дата документа прайса поставщика; не время его загрузки.");

            migrationBuilder.AlterColumn<string>(
                name: "Sha256",
                table: "ImportBatch",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "Контрольная сумма SHA-256 содержимого файла для обнаружения повторной загрузки.");

            migrationBuilder.AlterColumn<string>(
                name: "ParserVersion",
                table: "ImportBatch",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Версия парсера, разобравшего файл.");

            migrationBuilder.AlterColumn<int>(
                name: "NewCount",
                table: "ImportBatch",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Количество новых предложений по результату предварительной проверки.");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "ImportBatch",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Имя загруженного файла поставщика.");

            migrationBuilder.AlterColumn<long>(
                name: "ExpectedSupplierRevision",
                table: "ImportBatch",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldComment: "Ревизия поставщика на момент предварительной проверки; несовпадение при применении означает устаревшую партию.");

            migrationBuilder.AlterColumn<int>(
                name: "ErrorCount",
                table: "ImportBatch",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Количество ошибок, обнаруженных при предварительной проверке.");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "ImportBatch",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldComment: "Трёхбуквенный код валюты, например RUB (российский рубль).");

            migrationBuilder.AlterColumn<string>(
                name: "Conditions",
                table: "ImportBatch",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16000)",
                oldMaxLength: 16000,
                oldComment: "Служебные условия закупок из шапки исходного прайса.");

            migrationBuilder.AlterColumn<int>(
                name: "ChangedCount",
                table: "ImportBatch",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Количество изменившихся предложений по результату предварительной проверки.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AppliedAt",
                table: "ImportBatch",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Момент применения партии, UTC; NULL до применения.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ImportBatch",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "CustomerAddress",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "Recipient",
                table: "CustomerAddress",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Имя получателя по адресу; персональные данные.");

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "CustomerAddress",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Почтовый индекс строкой; NULL, если не указан.");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "CustomerAddress",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Идентификатор покупателя (Customer).");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "CustomerAddress",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Адрес доставки покупателя; персональные данные.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "CustomerAddress",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Customer",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "WholesaleStatus",
                table: "Customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Подтверждение опта: NotRequested — не запрошен, Pending — проверяется, Approved — одобрен, Rejected — отклонён. Право проверяется сервером.");

            migrationBuilder.AlterColumn<string>(
                name: "Segment",
                table: "Customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Коммерческий сегмент цены или покупателя: Retail — розница, Wholesale — опт. Сам по себе не подтверждает право на опт.");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Правовой тип: Individual — физлицо, SoleProprietor — ИП, Organization — организация.");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Отображаемое имя покупателя; может содержать персональные данные.");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "Customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Необязательная уникальная ссылка на учётную запись Identity; собственных паролей у покупателя нет.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Customer",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Category",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "Category",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Родительская категория; NULL для корня. Циклы запрещены.");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Category",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Название категории магазина.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Category",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Brand",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true,
                oldComment: "Системное поле PostgreSQL xmin: версия строки для защиты от конкурентного изменения; не дата и не время.");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Brand",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Название бренда собственного каталога.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Brand",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "AspNetUserTokens",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Конфиденциальное значение служебного токена Identity; не выводить в журналы.");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Имя служебного токена Identity.");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Имя провайдера входа или провайдера служебного токена Identity.");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Идентификатор учётной записи пользователя (AspNetUsers).");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "Имя пользователя для входа.");

            migrationBuilder.AlterColumn<bool>(
                name: "TwoFactorEnabled",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Включена ли двухфакторная аутентификация.");

            migrationBuilder.AlterColumn<string>(
                name: "SecurityStamp",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Метка безопасности Identity для отзыва ранее выданных сеансов при изменении учётных данных.");

            migrationBuilder.AlterColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Подтверждён ли телефон пользователя.");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Телефон пользователя; персональные данные.");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Хеш пароля в формате ASP.NET Core Identity; не открытый пароль. Конфиденциальное значение.");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedUserName",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "Нормализованное имя пользователя для поиска и проверки уникальности.");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "Нормализованный адрес электронной почты для поиска.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Момент окончания блокировки входа; NULL, если срок не задан.");

            migrationBuilder.AlterColumn<bool>(
                name: "LockoutEnabled",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Разрешена ли блокировка при неудачных попытках входа.");

            migrationBuilder.AlterColumn<bool>(
                name: "EmailConfirmed",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Подтверждён ли адрес электронной почты.");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "Адрес электронной почты пользователя; персональные данные.");

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Метка Identity для обнаружения конкурентного изменения записи.");

            migrationBuilder.AlterColumn<int>(
                name: "AccessFailedCount",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Число учитываемых Identity неудачных попыток входа.");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<string>(
                name: "RoleId",
                table: "AspNetUserRoles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Идентификатор роли доступа (AspNetRoles).");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserRoles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Идентификатор учётной записи пользователя (AspNetUsers).");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Идентификатор учётной записи пользователя (AspNetUsers).");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderDisplayName",
                table: "AspNetUserLogins",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Отображаемое название внешнего провайдера входа.");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Идентификатор пользователя у внешнего провайдера входа.");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Имя провайдера входа или провайдера служебного токена Identity.");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AspNetUserClaims",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Идентификатор учётной записи пользователя (AspNetUsers).");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimValue",
                table: "AspNetUserClaims",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Значение утверждения Identity (claim).");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimType",
                table: "AspNetUserClaims",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Тип утверждения Identity (claim).");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "AspNetUserClaims",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Уникальный идентификатор записи (первичный ключ).")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "AspNetRoles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "Нормализованное название роли для поиска и проверки уникальности.");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetRoles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "Название роли доступа, например Admin, Manager или Customer.");

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "AspNetRoles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Метка Identity для обнаружения конкурентного изменения записи.");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "AspNetRoles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Уникальный идентификатор записи (первичный ключ).");

            migrationBuilder.AlterColumn<string>(
                name: "RoleId",
                table: "AspNetRoleClaims",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Идентификатор роли доступа (AspNetRoles).");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimValue",
                table: "AspNetRoleClaims",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Значение утверждения Identity (claim).");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimType",
                table: "AspNetRoleClaims",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Тип утверждения Identity (claim).");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "AspNetRoleClaims",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Уникальный идентификатор записи (первичный ключ).")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
