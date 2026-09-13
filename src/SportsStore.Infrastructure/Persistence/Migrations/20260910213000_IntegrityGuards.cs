// Миграция EF Core: история изменения схемы. Комментарии поясняют назначение; операции выполняются штатным EF CLI.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace SportsStore.Infrastructure.Persistence.Migrations;
/// <summary>Добавляет защиту иерархии категорий, согласованности цен поставщика и минимальные роли Identity без пользователей и паролей.</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260910213000_IntegrityGuards")]
public sealed class IntegrityGuards : Migration
{
    /// <summary>Применяет изменения этой миграции в прямом направлении.</summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE FUNCTION sportsstore_category_no_cycle() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                PERFORM pg_advisory_xact_lock(861001);
                IF NEW."ParentId" IS NOT NULL AND EXISTS (
                    WITH RECURSIVE ancestors AS (
                        SELECT "Id", "ParentId" FROM "Category" WHERE "Id" = NEW."ParentId"
                        UNION
                        SELECT c."Id", c."ParentId" FROM "Category" c JOIN ancestors a ON c."Id" = a."ParentId"
                    ) SELECT 1 FROM ancestors WHERE "Id" = NEW."Id"
                ) THEN RAISE EXCEPTION 'Category cycle' USING ERRCODE = '23514'; END IF;
                RETURN NEW;
            END $$;
            CREATE TRIGGER category_no_cycle BEFORE INSERT OR UPDATE OF "ParentId" ON "Category"
                FOR EACH ROW EXECUTE FUNCTION sportsstore_category_no_cycle();

            CREATE FUNCTION sportsstore_price_supplier_check() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                IF (SELECT "SupplierId" FROM "SupplierOffer" WHERE "Id" = NEW."SupplierOfferId")
                    IS DISTINCT FROM (SELECT "SupplierId" FROM "SupplierPriceTier" WHERE "Id" = NEW."SupplierPriceTierId")
                THEN RAISE EXCEPTION 'Offer and tier must belong to the same supplier' USING ERRCODE = '23514'; END IF;
                RETURN NEW;
            END $$;
            CREATE TRIGGER price_supplier_check BEFORE INSERT OR UPDATE ON "SupplierOfferPrice"
                FOR EACH ROW EXECUTE FUNCTION sportsstore_price_supplier_check();
            ALTER TABLE "ProductVariant" ADD CONSTRAINT "CK_ProductVariant_Sku" CHECK (length(trim("Sku")) > 0 AND length(trim("SaleUnit")) > 0);
            ALTER TABLE "SupplierOffer" ADD CONSTRAINT "CK_SupplierOffer_Code" CHECK (length(trim("ExternalCode")) > 0);
            INSERT INTO "AspNetRoles" ("Id","Name","NormalizedName","ConcurrencyStamp") VALUES
                ('role-admin','Admin','ADMIN','role-admin-v1'),
                ('role-manager','Manager','MANAGER','role-manager-v1'),
                ('role-customer','Customer','CUSTOMER','role-customer-v1');
            """);
    }
    /// <summary>Отменяет изменения этой миграции при явном откате версии БД.</summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AspNetRoles" WHERE "Id" IN ('role-admin','role-manager','role-customer');
            ALTER TABLE "SupplierOffer" DROP CONSTRAINT "CK_SupplierOffer_Code";
            ALTER TABLE "ProductVariant" DROP CONSTRAINT "CK_ProductVariant_Sku";
            DROP TRIGGER price_supplier_check ON "SupplierOfferPrice";
            DROP FUNCTION sportsstore_price_supplier_check();
            DROP TRIGGER category_no_cycle ON "Category";
            DROP FUNCTION sportsstore_category_no_cycle();
            """);
    }
}
