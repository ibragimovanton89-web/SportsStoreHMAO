using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsStore.Infrastructure.Persistence.Migrations
{
    /// <summary>Сохраняет исходную единицу принятого товара отдельно от изменяемого прайса; старые несверенные значения остаются пустыми и блокируют распределение.</summary>
    public partial class PreserveReceivedSupplierUnit : Migration
    {
        /// <summary>Сохраняет исходную единицу принятого товара отдельно от изменяемого прайса; старые несверенные значения остаются пустыми и блокируют распределение.</summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupplierUnit",
                table: "UnallocatedStock",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                comment: "Снимок единицы приёмки; новый прайс его не меняет. Пустое значение требует сверки до распределения.");
        }

        /// <summary>Сохраняет исходную единицу принятого товара отдельно от изменяемого прайса; старые несверенные значения остаются пустыми и блокируют распределение.</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierUnit",
                table: "UnallocatedStock");
        }
    }
}

