using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceCustomerVehicleCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId_VehicleId_CreatedAt",
                schema: "service_center",
                table: "Invoices",
                columns: new[] { "CustomerId", "VehicleId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_CustomerId_VehicleId_CreatedAt",
                schema: "service_center",
                table: "Invoices");
        }
    }
}
