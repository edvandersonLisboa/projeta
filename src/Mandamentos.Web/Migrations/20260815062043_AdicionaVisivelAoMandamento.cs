using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mandamentos.Web.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaVisivelAoMandamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Visivel",
                table: "Mandamentos",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Visivel",
                table: "Mandamentos");
        }
    }
}
