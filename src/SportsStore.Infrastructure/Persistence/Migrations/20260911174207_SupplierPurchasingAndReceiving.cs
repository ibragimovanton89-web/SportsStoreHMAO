using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsStore.Infrastructure.Persistence.Migrations
{
    /// <summary>Добавляет закупочные заказы, частичную приёмку, нераспределённый склад и ручные пороги поставщика без изменения ассортимента.</summary>
    public partial class SupplierPurchasingAndReceiving : Migration
    {
        /// <summary>Создаёт новые объекты и комментарии; существующие предложения, цены, пользователи и остатки сохраняются.</summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ManualMinimumAmount",
                table: "SupplierPriceTier",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "Ручной порог закупки владельца; null использует исходный порог прайса. Импорт это значение не меняет.");

            migrationBuilder.CreateTable(
                name: "PurchaseOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Уникальный идентификатор записи."),
                    Number = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Номер закупки для сотрудника."),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Поставщик закупки."),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Склад назначения приёмки."),
                    SupplierPriceTierId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Выбранный закупочный тариф."),
                    TierCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Снимок кода тарифа: small, wholesale или large."),
                    TierName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Снимок названия выбранного тарифа."),
                    MinimumAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Снимок порога выбранного тарифа в рублях; null означает неизвестный порог."),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, comment: "Валюта закупки, RUB."),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Идентификатор сотрудника, собравшего заказ."),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Этап закупки; поступление оформляется отдельной транзакционной командой."),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Примечание оператора без секретов."),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Момент начала сборки закупки, UTC."),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Момент последнего изменения, UTC."),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrder", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrder_Minimum", "\"MinimumAmount\" IS NULL OR \"MinimumAmount\" >= 0");
                    table.CheckConstraint("CK_PurchaseOrder_Status", "\"Status\" IN ('Draft', 'Created', 'Submitted', 'InTransit', 'PartiallyReceived', 'Received', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_SupplierPriceTier_SupplierPriceTierId",
                        column: x => x.SupplierPriceTierId,
                        principalTable: "SupplierPriceTier",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_Supplier_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Supplier",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Закупочный заказ магазина поставщику; не является заказом покупателя.");

            migrationBuilder.CreateTable(
                name: "UnallocatedStock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Уникальный идентификатор записи."),
                    SupplierOfferId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Полученная позиция поставщика."),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Склад хранения."),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "Количество в единицах поставщика, ещё не зачисленное варианту; перенос обнуляет этот остаток."),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnallocatedStock", x => x.Id);
                    table.CheckConstraint("CK_UnallocatedStock_Quantity", "\"Quantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_UnallocatedStock_SupplierOffer_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalTable: "SupplierOffer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnallocatedStock_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Фактически полученный товар до сопоставления с карточкой; при создании карточки переносится в InventoryBalance.");

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Уникальный идентификатор записи."),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Закупочный заказ."),
                    SupplierOfferId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Предложение поставщика, выбранное вручную."),
                    ExternalCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Код поставщика с ведущими нулями."),
                    Name = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false, comment: "Название позиции на момент выбора."),
                    SupplierUnit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Единица закупки: плюс один означает одну такую единицу, а не коробку."),
                    UnitsPerBox = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Справочное количество в коробке; не задаёт минимальный заказ."),
                    SmallPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Снимок цены мелкого опта за единицу поставщика; null не равен нулю."),
                    WholesalePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Снимок цены опта за единицу поставщика."),
                    LargePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Снимок цены крупного опта за единицу поставщика."),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Зафиксированная цена выбранного тарифа за единицу поставщика."),
                    Quantity = table.Column<int>(type: "integer", nullable: false, comment: "Заказанное количество единиц поставщика, от 1 до 1000000."),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false, comment: "Суммарно принятое количество; увеличивается только командой приёмки."),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLine", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrderLine_Prices", "(\"UnitPrice\" IS NULL OR \"UnitPrice\" > 0) AND (\"SmallPrice\" IS NULL OR \"SmallPrice\" > 0) AND (\"WholesalePrice\" IS NULL OR \"WholesalePrice\" > 0) AND (\"LargePrice\" IS NULL OR \"LargePrice\" > 0)");
                    table.CheckConstraint("CK_PurchaseOrderLine_Quantities", "\"Quantity\" BETWEEN 1 AND 1000000 AND \"ReceivedQuantity\" BETWEEN 0 AND \"Quantity\"");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLine_PurchaseOrder_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLine_SupplierOffer_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalTable: "SupplierOffer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Выбранная позиция закупки со снимками исходных данных и цен; новый прайс не меняет заказ.");

            migrationBuilder.CreateTable(
                name: "PurchaseReceiptLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Уникальный идентификатор записи."),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Строка закупки, по которой принято количество."),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Склад фактического поступления."),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор приёмки; уникален вместе со строкой закупки."),
                    Quantity = table.Column<int>(type: "integer", nullable: false, comment: "Принято в этой операции, в единицах поставщика."),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Вариант, получивший остаток; null означает приёмку до создания карточки."),
                    Conversion = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Зафиксированный перевод единиц при зачислении варианту; null для нераспределённого остатка."),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Момент приёмки, UTC."),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceiptLine", x => x.Id);
                    table.CheckConstraint("CK_PurchaseReceiptLine_Quantity", "\"Quantity\" > 0 AND (\"Conversion\" IS NULL OR \"Conversion\" > 0)");
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptLine_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptLine_PurchaseOrderLine_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalTable: "PurchaseOrderLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptLine_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Неизменяемая запись приёмки; повтор той же операции не увеличивает остаток повторно.");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupplierPriceTier_ManualMinimum",
                table: "SupplierPriceTier",
                sql: "\"ManualMinimumAmount\" IS NULL OR \"ManualMinimumAmount\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_CreatedBy_SupplierId",
                table: "PurchaseOrder",
                columns: new[] { "CreatedBy", "SupplierId" },
                unique: true,
                filter: "\"Status\" = 'Draft'");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_Number",
                table: "PurchaseOrder",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_Status_CreatedAt",
                table: "PurchaseOrder",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_SupplierId",
                table: "PurchaseOrder",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_SupplierPriceTierId",
                table: "PurchaseOrder",
                column: "SupplierPriceTierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_WarehouseId",
                table: "PurchaseOrder",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLine_PurchaseOrderId_SupplierOfferId",
                table: "PurchaseOrderLine",
                columns: new[] { "PurchaseOrderId", "SupplierOfferId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLine_SupplierOfferId",
                table: "PurchaseOrderLine",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptLine_OperationId_PurchaseOrderLineId",
                table: "PurchaseReceiptLine",
                columns: new[] { "OperationId", "PurchaseOrderLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptLine_ProductVariantId",
                table: "PurchaseReceiptLine",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptLine_PurchaseOrderLineId",
                table: "PurchaseReceiptLine",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptLine_WarehouseId",
                table: "PurchaseReceiptLine",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_UnallocatedStock_SupplierOfferId_WarehouseId",
                table: "UnallocatedStock",
                columns: new[] { "SupplierOfferId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnallocatedStock_WarehouseId",
                table: "UnallocatedStock",
                column: "WarehouseId");
            // xmin — системная колонка PostgreSQL: комментарий задаётся SQL, а не созданием собственной колонки версии.
            foreach (var table in new[] { "PurchaseOrder", "PurchaseOrderLine", "PurchaseReceiptLine", "UnallocatedStock" })
                migrationBuilder.Sql($"COMMENT ON COLUMN \"{table}\".xmin IS 'Версия строки PostgreSQL для защиты от конкурентного изменения.';");
        }

        /// <summary>Откатывает только закупочное расширение; удаляет его историю, поэтому требует отдельного решения оператора.</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseReceiptLine");

            migrationBuilder.DropTable(
                name: "UnallocatedStock");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLine");

            migrationBuilder.DropTable(
                name: "PurchaseOrder");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SupplierPriceTier_ManualMinimum",
                table: "SupplierPriceTier");

            migrationBuilder.DropColumn(
                name: "ManualMinimumAmount",
                table: "SupplierPriceTier");
        }
    }
}
