using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsStore.Infrastructure.Persistence.Migrations
{
    /// <summary>Добавляет атомарный административный аудит и сохранённое сравнение закупочных цен; прежние данные не удаляются.</summary>
    public partial class AdministrativeAuditAndImportSnapshots : Migration
    {
        /// <summary>Создаёт журнал и заполняет отсутствовавшие снимки прежних партий пустым JSON-массивом.</summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BeforePricesJson",
                table: "ImportRow",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "[]",
                comment: "Снимок закупочных цен до предварительной проверки в порядке тарифов партии; JSON для просмотра изменений после применения.");

            migrationBuilder.CreateTable(
                name: "AdminAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Уникальный идентификатор записи журнала."),
                    ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false, comment: "Идентификатор сотрудника или доверенного локального оператора."),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Момент успешного изменения, UTC."),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Стабильный код административного действия."),
                    ObjectType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Тип изменённого объекта."),
                    ObjectId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false, comment: "Идентификатор объекта без его персонального содержимого."),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Безопасное описание изменения без секретов, адресов и реквизитов."),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Основание решения; не предназначено для персональных сведений."),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор отправки команды для аудита и защиты от повторов."),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAudit", x => x.Id);
                },
                comment: "Журнал успешных административных изменений; сохраняется атомарно с операцией, без секретов и реквизитов.");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAudit_ActorId_OperationId",
                table: "AdminAudit",
                columns: new[] { "ActorId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminAudit_OccurredAt",
                table: "AdminAudit",
                column: "OccurredAt");
        }

        /// <summary>Удаляет только добавленные этим этапом объекты при явном откате.</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAudit");

            migrationBuilder.DropColumn(
                name: "BeforePricesJson",
                table: "ImportRow");
        }
    }
}
