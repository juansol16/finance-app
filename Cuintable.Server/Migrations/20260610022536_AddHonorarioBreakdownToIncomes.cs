using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuintable.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddHonorarioBreakdownToIncomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HonorarioMXN",
                table: "Incomes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IsrWithheldMXN",
                table: "Incomes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IvaMXN",
                table: "Incomes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IvaWithheldMXN",
                table: "Incomes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalMXN",
                table: "Incomes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TakeHomePayUSD",
                table: "Incomes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // Backfill existing USD incomes. AmountMXN holds the net amount deposited;
            // the formulas and rounding mirror HonorarioCalculator exactly
            // (Postgres round(numeric, 2) = half away from zero = MidpointRounding.AwayFromZero).
            migrationBuilder.Sql("""
                UPDATE "Incomes" SET
                    "HonorarioMXN"   = calc.h,
                    "IvaMXN"         = round(calc.h * 0.16, 2),
                    "SubtotalMXN"    = calc.h + round(calc.h * 0.16, 2),
                    "IsrWithheldMXN" = round(calc.h * 0.0125, 2),
                    "IvaWithheldMXN" = round(calc.h * 0.10666, 2),
                    "TakeHomePayUSD" = round((calc.h + round(calc.h * 0.16, 2)) / calc.rate, 2)
                FROM (
                    SELECT "Id" AS id,
                           "ExchangeRate" AS rate,
                           round("AmountMXN" / 1.04084, 2) AS h
                    FROM "Incomes"
                    WHERE "ExchangeRate" IS NOT NULL AND "ExchangeRate" > 0
                ) AS calc
                WHERE "Incomes"."Id" = calc.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HonorarioMXN",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "IsrWithheldMXN",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "IvaMXN",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "IvaWithheldMXN",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "SubtotalMXN",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "TakeHomePayUSD",
                table: "Incomes");
        }
    }
}
