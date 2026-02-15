using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuintable.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxPayments_UserId_PeriodYear_PeriodMonth",
                table: "TaxPayments");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "TaxPayments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "TaxableExpenses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Incomes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Expenses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CreditCards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_TenantId_PeriodYear_PeriodMonth",
                table: "TaxPayments",
                columns: new[] { "TenantId", "PeriodYear", "PeriodMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_UserId",
                table: "TaxPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxableExpenses_TenantId",
                table: "TaxableExpenses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_TenantId",
                table: "Incomes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_TenantId",
                table: "Expenses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCards_TenantId",
                table: "CreditCards",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditCards_Tenants_TenantId",
                table: "CreditCards",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Tenants_TenantId",
                table: "Expenses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Incomes_Tenants_TenantId",
                table: "Incomes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxableExpenses_Tenants_TenantId",
                table: "TaxableExpenses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxPayments_Tenants_TenantId",
                table: "TaxPayments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditCards_Tenants_TenantId",
                table: "CreditCards");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Tenants_TenantId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Incomes_Tenants_TenantId",
                table: "Incomes");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxableExpenses_Tenants_TenantId",
                table: "TaxableExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxPayments_Tenants_TenantId",
                table: "TaxPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TaxPayments_TenantId_PeriodYear_PeriodMonth",
                table: "TaxPayments");

            migrationBuilder.DropIndex(
                name: "IX_TaxPayments_UserId",
                table: "TaxPayments");

            migrationBuilder.DropIndex(
                name: "IX_TaxableExpenses_TenantId",
                table: "TaxableExpenses");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_TenantId",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_TenantId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_CreditCards_TenantId",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TaxPayments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TaxableExpenses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CreditCards");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_UserId_PeriodYear_PeriodMonth",
                table: "TaxPayments",
                columns: new[] { "UserId", "PeriodYear", "PeriodMonth" },
                unique: true);
        }
    }
}
