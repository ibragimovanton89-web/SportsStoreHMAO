// Миграция EF Core: история изменения схемы. Комментарии поясняют назначение; операции выполняются штатным EF CLI.
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsStore.Infrastructure.Persistence.Migrations
{
        /// <summary>Версионированное изменение схемы PostgreSQL, применяемое через EF Core.</summary>
        public partial class EnumAndCurrencyConstraints : Migration
    {
                /// <summary>Применяет изменения этой миграции в прямом направлении.</summary>
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SalePriceHistory",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SalePrice",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PricingSettings",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PriceProposal",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Order",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "ImportBatch",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SalePriceHistory_Segment",
                table: "SalePriceHistory",
                sql: "\"Segment\" IN ('Retail', 'Wholesale')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SalePrice_Segment",
                table: "SalePrice",
                sql: "\"Segment\" IN ('Retail', 'Wholesale')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_Status",
                table: "Product",
                sql: "\"Status\" IN ('Draft', 'Published', 'Archived')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PriceProposal_Segment",
                table: "PriceProposal",
                sql: "\"Segment\" IN ('Retail', 'Wholesale')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PriceProposal_Status",
                table: "PriceProposal",
                sql: "\"Status\" IN ('Pending', 'Applied', 'Superseded')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_SalesFormat",
                table: "Order",
                sql: "\"SalesFormat\" IN ('Retail', 'Wholesale')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MarkupRule_Segment",
                table: "MarkupRule",
                sql: "\"Segment\" IN ('Retail', 'Wholesale')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ImportRow_Kind",
                table: "ImportRow",
                sql: "\"Kind\" IN ('Empty', 'Header', 'Section', 'Product', 'Error')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ImportBatch_Status",
                table: "ImportBatch",
                sql: "\"Status\" IN ('Preview', 'Invalid', 'Applied')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customer_Kind",
                table: "Customer",
                sql: "\"Kind\" IN ('Individual', 'SoleProprietor', 'Organization')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customer_Segment",
                table: "Customer",
                sql: "\"Segment\" IN ('Retail', 'Wholesale')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customer_WholesaleStatus",
                table: "Customer",
                sql: "\"WholesaleStatus\" IN ('NotRequested', 'Pending', 'Approved', 'Rejected')");
        }

                /// <summary>Отменяет изменения этой миграции при явном откате версии БД.</summary>
                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SalePriceHistory_Segment",
                table: "SalePriceHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SalePrice_Segment",
                table: "SalePrice");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_Status",
                table: "Product");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PriceProposal_Segment",
                table: "PriceProposal");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PriceProposal_Status",
                table: "PriceProposal");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_SalesFormat",
                table: "Order");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MarkupRule_Segment",
                table: "MarkupRule");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ImportRow_Kind",
                table: "ImportRow");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ImportBatch_Status",
                table: "ImportBatch");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customer_Kind",
                table: "Customer");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customer_Segment",
                table: "Customer");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customer_WholesaleStatus",
                table: "Customer");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SalePriceHistory",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SalePrice",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PricingSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PriceProposal",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "ImportBatch",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);
        }
    }
}
