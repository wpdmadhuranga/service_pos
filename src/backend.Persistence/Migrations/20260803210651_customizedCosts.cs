using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class customizedCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxPrice",
                schema: "service_center",
                table: "Services",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinPrice",
                schema: "service_center",
                table: "Services",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricingType",
                schema: "service_center",
                table: "Services",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Fixed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxPrice",
                schema: "service_center",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "MinPrice",
                schema: "service_center",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "PricingType",
                schema: "service_center",
                table: "Services");
        }
    }
}
