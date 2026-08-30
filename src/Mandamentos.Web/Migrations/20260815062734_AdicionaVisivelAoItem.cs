using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mandamentos.Web.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaVisivelAoItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Visivel",
                table: "Itens",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Visivel",
                table: "Itens");
        }
    }
}
