using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuintable.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxableExpenseIvaAndValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "IvaMXN",
                table: "TaxableExpenses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationComment",
                table: "TaxableExpenses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValidationStatus",
                table: "TaxableExpenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IvaMXN",
                table: "TaxableExpenses");

            migrationBuilder.DropColumn(
                name: "ValidationComment",
                table: "TaxableExpenses");

            migrationBuilder.DropColumn(
                name: "ValidationStatus",
                table: "TaxableExpenses");
        }
    }
}
