using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuintable.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddStatementAccountType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountType",
                table: "CardStatements",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "CardStatements");
        }
    }
}
