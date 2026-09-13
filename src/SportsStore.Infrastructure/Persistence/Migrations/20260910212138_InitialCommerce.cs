// Миграция EF Core: история изменения схемы. Комментарии поясняют назначение; операции выполняются штатным EF CLI.
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SportsStore.Infrastructure.Persistence.Migrations
{
        /// <summary>Версионированное изменение схемы PostgreSQL, применяемое через EF Core.</summary>
        public partial class InitialCommerce : Migration
    {
                /// <summary>Применяет изменения этой миграции в прямом направлении.</summary>
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Brand",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brand", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                    table.CheckConstraint("CK_Category_Valid", "\"ParentId\" IS NULL OR \"ParentId\" <> \"Id\"");
                    table.ForeignKey(
                        name: "FK_Category_Category_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PricingSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MinimumWholesaleOrder = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    AutoApplyProposals = table.Column<bool>(type: "boolean", nullable: false),
                    TaxTreatmentNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingSettings", x => x.Id);
                    table.CheckConstraint("CK_PricingSettings_Valid", "\"Id\" = '00000000-0000-0000-0000-000000000001'::uuid AND (\"MinimumWholesaleOrder\" IS NULL OR \"MinimumWholesaleOrder\" >= 0)");
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Segment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WholesaleStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customer_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_Brand_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brand",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Recipient = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAddress_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Number = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SalesFormat = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ShippingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OrganizationName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Inn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Kpp = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Order_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Inn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Kpp = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationProfile", x => x.Id);
                    table.CheckConstraint("CK_OrganizationProfile_Valid", "\"Inn\" ~ '^([0-9]{10}|[0-9]{12})$' AND (\"Kpp\" IS NULL OR \"Kpp\" ~ '^[0-9]{9}$')");
                    table.ForeignKey(
                        name: "FK_OrganizationProfile_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductImage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImage", x => x.Id);
                    table.CheckConstraint("CK_ProductImage_Valid", "\"SortOrder\" >= 0");
                    table.ForeignKey(
                        name: "FK_ProductImage_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParserVersion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Conditions = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    TiersJson = table.Column<string>(type: "jsonb", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpectedSupplierRevision = table.Column<long>(type: "bigint", nullable: false),
                    NewCount = table.Column<int>(type: "integer", nullable: false),
                    ChangedCount = table.Column<int>(type: "integer", nullable: false),
                    UnchangedCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportRow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    ParsedJson = table.Column<string>(type: "jsonb", nullable: true),
                    Diagnostics = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    MatchResult = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportRow_ImportBatch_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBalance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    OnHand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Reserved = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBalance", x => x.Id);
                    table.CheckConstraint("CK_InventoryBalance_Valid", "\"OnHand\" >= 0 AND \"Reserved\" >= 0 AND \"Reserved\" <= \"OnHand\"");
                    table.ForeignKey(
                        name: "FK_InventoryBalance_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarkupRule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Segment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    MinimumQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MarkupPercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkupRule", x => x.Id);
                    table.CheckConstraint("CK_MarkupRule_Valid", "NOT (\"CategoryId\" IS NOT NULL AND \"ProductVariantId\" IS NOT NULL) AND \"MarkupPercent\" >= 0 AND \"MinimumQuantity\" >= 1 AND (\"Segment\" <> 'Retail' OR \"MinimumQuantity\" = 1)");
                    table.ForeignKey(
                        name: "FK_MarkupRule_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Sku = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SaleUnit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitDiscount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.Id);
                    table.CheckConstraint("CK_OrderItem_Valid", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"UnitDiscount\" >= 0 AND \"UnitDiscount\" <= \"UnitPrice\"");
                    table.ForeignKey(
                        name: "FK_OrderItem_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceProposal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Segment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MinimumQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceProposal", x => x.Id);
                    table.CheckConstraint("CK_PriceProposal_Valid", "\"Amount\" > 0 AND \"MinimumQuantity\" >= 1");
                    table.ForeignKey(
                        name: "FK_PriceProposal_ImportBatch_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingSupplierOfferId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ManufacturerCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Barcode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Size = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SaleUnit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariant_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalePrice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Segment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MinimumQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsManual = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalePrice", x => x.Id);
                    table.CheckConstraint("CK_SalePrice_Valid", "\"Amount\" > 0 AND \"MinimumQuantity\" >= 1 AND (\"Segment\" <> 'Retail' OR \"MinimumQuantity\" = 1)");
                    table.ForeignKey(
                        name: "FK_SalePrice_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalePriceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Segment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MinimumQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OldAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NewAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalePriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalePriceHistory_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Supplier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    LatestSourceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SelectedPriceTierId = table.Column<Guid>(type: "uuid", nullable: true),
                    PriceTierConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierOffer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceName = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SourceSection = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SupplierUnit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UnitsPerBox = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Stock = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    SourceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SaleUnitsPerSupplierUnit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ConversionConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumOrderQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    OrderMultiple = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ReviewSuggestionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierOffer", x => x.Id);
                    table.CheckConstraint("CK_SupplierOffer_Valid", "(\"Stock\" IS NULL OR \"Stock\" >= 0) AND (\"UnitsPerBox\" IS NULL OR \"UnitsPerBox\" > 0) AND (\"SaleUnitsPerSupplierUnit\" IS NULL OR \"SaleUnitsPerSupplierUnit\" > 0) AND (NOT \"ConversionConfirmed\" OR (\"SaleUnitsPerSupplierUnit\" IS NOT NULL AND \"ProductVariantId\" IS NOT NULL)) AND (\"MinimumOrderQuantity\" IS NULL OR \"MinimumOrderQuantity\" > 0) AND (\"OrderMultiple\" IS NULL OR \"OrderMultiple\" > 0)");
                    table.ForeignKey(
                        name: "FK_SupplierOffer_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierOffer_Supplier_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Supplier",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPriceTier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SourceConditions = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPriceTier", x => x.Id);
                    table.CheckConstraint("CK_SupplierPriceTier_Valid", "\"MinimumAmount\" IS NULL OR \"MinimumAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_SupplierPriceTier_Supplier_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Supplier",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierOfferPrice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPriceTierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierOfferPrice", x => x.Id);
                    table.CheckConstraint("CK_SupplierOfferPrice_Valid", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_SupplierOfferPrice_ImportBatch_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierOfferPrice_SupplierOffer_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalTable: "SupplierOffer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierOfferPrice_SupplierPriceTier_SupplierPriceTierId",
                        column: x => x.SupplierPriceTierId,
                        principalTable: "SupplierPriceTier",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPriceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierOfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPriceTierId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    NewAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPriceHistory", x => x.Id);
                    table.CheckConstraint("CK_SupplierPriceHistory_Valid", "\"NewAmount\" > 0 AND (\"OldAmount\" IS NULL OR \"OldAmount\" > 0)");
                    table.ForeignKey(
                        name: "FK_SupplierPriceHistory_ImportBatch_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPriceHistory_SupplierOffer_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalTable: "SupplierOffer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPriceHistory_SupplierPriceTier_SupplierPriceTierId",
                        column: x => x.SupplierPriceTierId,
                        principalTable: "SupplierPriceTier",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brand_Name",
                table: "Brand",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Category_ParentId",
                table: "Category",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_ApplicationUserId",
                table: "Customer",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddress_CustomerId",
                table: "CustomerAddress",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatch_SupplierId_Sha256_ParserVersion",
                table: "ImportBatch",
                columns: new[] { "SupplierId", "Sha256", "ParserVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportRow_ImportBatchId_RowNumber",
                table: "ImportRow",
                columns: new[] { "ImportBatchId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalance_ProductVariantId_WarehouseId",
                table: "InventoryBalance",
                columns: new[] { "ProductVariantId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalance_WarehouseId",
                table: "InventoryBalance",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_MarkupRule_CategoryId",
                table: "MarkupRule",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MarkupRule_ProductVariantId",
                table: "MarkupRule",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_MarkupRule_Segment_CategoryId_ProductVariantId_MinimumQuant~",
                table: "MarkupRule",
                columns: new[] { "Segment", "CategoryId", "ProductVariantId", "MinimumQuantity" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_Order_CustomerId",
                table: "Order",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Number",
                table: "Order",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItem",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_ProductVariantId",
                table: "OrderItem",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationProfile_CustomerId",
                table: "OrganizationProfile",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceProposal_ImportBatchId",
                table: "PriceProposal",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceProposal_ProductVariantId",
                table: "PriceProposal",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceProposal_SupplierOfferId_Segment_MinimumQuantity_Finge~",
                table: "PriceProposal",
                columns: new[] { "SupplierOfferId", "Segment", "MinimumQuantity", "Fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_BrandId",
                table: "Product",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_CategoryId",
                table: "Product",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Status_CategoryId",
                table: "Product",
                columns: new[] { "Status", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImage_ProductId_SortOrder",
                table: "ProductImage",
                columns: new[] { "ProductId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_PricingSupplierOfferId",
                table: "ProductVariant",
                column: "PricingSupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_ProductId",
                table: "ProductVariant",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_Sku",
                table: "ProductVariant",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalePrice_ProductVariantId_Segment_MinimumQuantity",
                table: "SalePrice",
                columns: new[] { "ProductVariantId", "Segment", "MinimumQuantity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalePriceHistory_ProductVariantId",
                table: "SalePriceHistory",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Code",
                table: "Supplier",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_SelectedPriceTierId",
                table: "Supplier",
                column: "SelectedPriceTierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffer_LastSeenAt",
                table: "SupplierOffer",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffer_ProductVariantId",
                table: "SupplierOffer",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffer_SupplierId_ExternalCode",
                table: "SupplierOffer",
                columns: new[] { "SupplierId", "ExternalCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOfferPrice_ImportBatchId",
                table: "SupplierOfferPrice",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOfferPrice_SupplierOfferId_SupplierPriceTierId",
                table: "SupplierOfferPrice",
                columns: new[] { "SupplierOfferId", "SupplierPriceTierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOfferPrice_SupplierPriceTierId",
                table: "SupplierOfferPrice",
                column: "SupplierPriceTierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceHistory_ImportBatchId_SupplierOfferId_Supplier~",
                table: "SupplierPriceHistory",
                columns: new[] { "ImportBatchId", "SupplierOfferId", "SupplierPriceTierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceHistory_SupplierOfferId",
                table: "SupplierPriceHistory",
                column: "SupplierOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceHistory_SupplierPriceTierId",
                table: "SupplierPriceHistory",
                column: "SupplierPriceTierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceTier_SupplierId_Code",
                table: "SupplierPriceTier",
                columns: new[] { "SupplierId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_Name",
                table: "Warehouse",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportBatch_Supplier_SupplierId",
                table: "ImportBatch",
                column: "SupplierId",
                principalTable: "Supplier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryBalance_ProductVariant_ProductVariantId",
                table: "InventoryBalance",
                column: "ProductVariantId",
                principalTable: "ProductVariant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MarkupRule_ProductVariant_ProductVariantId",
                table: "MarkupRule",
                column: "ProductVariantId",
                principalTable: "ProductVariant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_ProductVariant_ProductVariantId",
                table: "OrderItem",
                column: "ProductVariantId",
                principalTable: "ProductVariant",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceProposal_ProductVariant_ProductVariantId",
                table: "PriceProposal",
                column: "ProductVariantId",
                principalTable: "ProductVariant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceProposal_SupplierOffer_SupplierOfferId",
                table: "PriceProposal",
                column: "SupplierOfferId",
                principalTable: "SupplierOffer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariant_SupplierOffer_PricingSupplierOfferId",
                table: "ProductVariant",
                column: "PricingSupplierOfferId",
                principalTable: "SupplierOffer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_SupplierPriceTier_SelectedPriceTierId",
                table: "Supplier",
                column: "SelectedPriceTierId",
                principalTable: "SupplierPriceTier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

                /// <summary>Отменяет изменения этой миграции при явном откате версии БД.</summary>
                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierOffer_Supplier_SupplierId",
                table: "SupplierOffer");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPriceTier_Supplier_SupplierId",
                table: "SupplierPriceTier");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierOffer_ProductVariant_ProductVariantId",
                table: "SupplierOffer");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CustomerAddress");

            migrationBuilder.DropTable(
                name: "ImportRow");

            migrationBuilder.DropTable(
                name: "InventoryBalance");

            migrationBuilder.DropTable(
                name: "MarkupRule");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "OrganizationProfile");

            migrationBuilder.DropTable(
                name: "PriceProposal");

            migrationBuilder.DropTable(
                name: "PricingSettings");

            migrationBuilder.DropTable(
                name: "ProductImage");

            migrationBuilder.DropTable(
                name: "SalePrice");

            migrationBuilder.DropTable(
                name: "SalePriceHistory");

            migrationBuilder.DropTable(
                name: "SupplierOfferPrice");

            migrationBuilder.DropTable(
                name: "SupplierPriceHistory");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Warehouse");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "ImportBatch");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Supplier");

            migrationBuilder.DropTable(
                name: "SupplierPriceTier");

            migrationBuilder.DropTable(
                name: "ProductVariant");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "SupplierOffer");

            migrationBuilder.DropTable(
                name: "Brand");

            migrationBuilder.DropTable(
                name: "Category");
        }
    }
}
