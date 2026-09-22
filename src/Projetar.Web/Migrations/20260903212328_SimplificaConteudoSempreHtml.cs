using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projetar.Web.Migrations
{
    /// <inheritdoc />
    public partial class SimplificaConteudoSempreHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormatoCorpo",
                table: "Itens");

            migrationBuilder.DropColumn(
                name: "FormatoCorpoAnterior",
                table: "ItemRevisoes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormatoCorpo",
                table: "Itens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FormatoCorpoAnterior",
                table: "ItemRevisoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
