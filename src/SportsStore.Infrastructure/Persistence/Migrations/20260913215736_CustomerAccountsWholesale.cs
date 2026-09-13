// Миграция четвёртого этапа: заявки, история решений и очередь писем; существующие покупатели не изменяются.
﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerAccountsWholesale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WholesaleApplication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор заявки."),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Покупатель — владелец заявки."),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Pending: проверка; Approved: одобрено; Rejected: отказ; Withdrawn: отзыв заявки; Revoked: отзыв опта."),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Снимок правового типа покупателя."),
                    LegalName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Снимок официального наименования; null для физлица."),
                    Inn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Снимок ИНН; null для физлица."),
                    Kpp = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Снимок КПП; null, если неприменим."),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Текст обращения покупателя."),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Время подачи, UTC."),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Время последнего решения, UTC; null до рассмотрения."),
                    ReviewedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Служебный идентификатор автора решения."),
                    PublicReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Доступная покупателю причина результата."),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Уникальная команда подачи для защиты от повтора."),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false, comment: "Версия xmin для защиты от устаревшего решения.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WholesaleApplication", x => x.Id);
                    table.CheckConstraint("CK_WholesaleApplication_Kind", "\"Kind\" IN ('Individual', 'SoleProprietor', 'Organization')");
                    table.CheckConstraint("CK_WholesaleApplication_Status", "\"Status\" IN ('Pending', 'Approved', 'Rejected', 'Withdrawn', 'Revoked')");
                    table.ForeignKey(
                        name: "FK_WholesaleApplication_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Заявки покупателей на опт с неизменяемыми снимками реквизитов.");

            migrationBuilder.CreateTable(
                name: "WholesaleDecision",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор решения."),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Покупатель, чьи условия изменились."),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Заявка-основание; null для ручного решения без заявки."),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Результат рассмотрения или отзыва права на опт."),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Время решения, UTC."),
                    ActorId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Доверенный автор решения; не раскрывается покупателю."),
                    PublicReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Объяснение, доступное покупателю."),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Уникальный идентификатор команды изменения."),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false, comment: "Системная версия xmin.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WholesaleDecision", x => x.Id);
                    table.CheckConstraint("CK_WholesaleDecision_Outcome", "\"Outcome\" IN ('Pending', 'Approved', 'Rejected', 'Withdrawn', 'Revoked')");
                    table.ForeignKey(
                        name: "FK_WholesaleDecision_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WholesaleDecision_WholesaleApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "WholesaleApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Неизменяемая история решений по опту, включая ручные решения без заявки.");

            migrationBuilder.CreateTable(
                name: "NotificationOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор уведомления."),
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Решение, вызвавшее единственное логическое уведомление."),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Время постановки в очередь, UTC."),
                    Attempts = table.Column<int>(type: "integer", nullable: false, comment: "Количество начатых попыток доставки."),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Время следующей допустимой попытки, UTC."),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Окончание аренды обработчиком, UTC; null без аренды."),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Подтверждённая отправка, UTC; null до отправки."),
                    LastErrorCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Безопасный код ошибки без персональных данных и SMTP-ответа."),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false, comment: "Версия xmin для защиты аренды обработчика.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationOutbox", x => x.Id);
                    table.CheckConstraint("CK_NotificationOutbox_Attempts", "\"Attempts\" >= 0");
                    table.ForeignKey(
                        name: "FK_NotificationOutbox_WholesaleDecision_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "WholesaleDecision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Очередь уведомлений по опту без токенов и полных реквизитов.");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutbox_DecisionId",
                table: "NotificationOutbox",
                column: "DecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutbox_SentAt_NextAttemptAt",
                table: "NotificationOutbox",
                columns: new[] { "SentAt", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WholesaleApplication_CustomerId",
                table: "WholesaleApplication",
                column: "CustomerId",
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_WholesaleApplication_CustomerId_SubmittedAt",
                table: "WholesaleApplication",
                columns: new[] { "CustomerId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WholesaleApplication_OperationId",
                table: "WholesaleApplication",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WholesaleDecision_ApplicationId",
                table: "WholesaleDecision",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_WholesaleDecision_CustomerId_OccurredAt",
                table: "WholesaleDecision",
                columns: new[] { "CustomerId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WholesaleDecision_OperationId",
                table: "WholesaleDecision",
                column: "OperationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationOutbox");

            migrationBuilder.DropTable(
                name: "WholesaleDecision");

            migrationBuilder.DropTable(
                name: "WholesaleApplication");
        }
    }
}
