using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projetar.Web.Migrations
{
    /// <inheritdoc />
    public partial class TagsDoItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Itens",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Itens");
        }
    }
}
