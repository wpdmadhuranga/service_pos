using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addednewtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_InventoryItems_InventoryItemId",
                schema: "inventory",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_InvoiceItems_ReferenceInvoiceItemId",
                schema: "inventory",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Users_CreatedBy",
                schema: "inventory",
                table: "InventoryTransactions");

            migrationBuilder.RenameColumn(
                name: "ReferenceInvoiceItemId",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "InvoiceId");

            migrationBuilder.RenameColumn(
                name: "Note",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "InventoryItemId",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransactions_ReferenceInvoiceItemId",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "IX_InventoryTransactions_InvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransactions_InventoryItemId",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "IX_InventoryTransactions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransactions_CreatedBy",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "IX_InventoryTransactions_ProductId");

            migrationBuilder.AddColumn<string>(
                name: "BrandSnapshot",
                schema: "service_center",
                table: "InvoiceItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                schema: "service_center",
                table: "InvoiceItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                schema: "inventory",
                table: "InventoryTransactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.CreateTable(
                name: "InventoryTransaction",
                schema: "service_center",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ReferenceInvoiceItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransaction_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CompatibleVehicleType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CostPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "service_center",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ProductId",
                schema: "service_center",
                table: "InvoiceItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_InventoryItemId",
                schema: "service_center",
                table: "InventoryTransaction",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                schema: "inventory",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ServiceId",
                schema: "inventory",
                table: "Products",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ServiceId_Brand_Name",
                schema: "inventory",
                table: "Products",
                columns: new[] { "ServiceId", "Brand", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Invoices_InvoiceId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "InvoiceId",
                principalSchema: "service_center",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "ProductId",
                principalSchema: "inventory",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Users_UserId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "UserId",
                principalSchema: "service_center",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Products_ProductId",
                schema: "service_center",
                table: "InvoiceItems",
                column: "ProductId",
                principalSchema: "inventory",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Invoices_InvoiceId",
                schema: "inventory",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                schema: "inventory",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Users_UserId",
                schema: "inventory",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Products_ProductId",
                schema: "service_center",
                table: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "InventoryTransaction",
                schema: "service_center");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_ProductId",
                schema: "service_center",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "BrandSnapshot",
                schema: "service_center",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ProductId",
                schema: "service_center",
                table: "InvoiceItems");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "InventoryItemId");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "Notes",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "InvoiceId",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "ReferenceInvoiceItemId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransactions_UserId",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "IX_InventoryTransactions_InventoryItemId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransactions_ProductId",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "IX_InventoryTransactions_CreatedBy");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryTransactions_InvoiceId",
                schema: "inventory",
                table: "InventoryTransactions",
                newName: "IX_InventoryTransactions_ReferenceInvoiceItemId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                schema: "inventory",
                table: "InventoryTransactions",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_InventoryItems_InventoryItemId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "InventoryItemId",
                principalSchema: "inventory",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_InvoiceItems_ReferenceInvoiceItemId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "ReferenceInvoiceItemId",
                principalSchema: "service_center",
                principalTable: "InvoiceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Users_CreatedBy",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "CreatedBy",
                principalSchema: "service_center",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
