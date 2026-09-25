using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projetar.Web.Migrations
{
    /// <inheritdoc />
    public partial class ConversaDeModeracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MensagensModeracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoAlvo = table.Column<int>(type: "integer", nullable: false),
                    AlvoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AutorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    DataCriacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagensModeracao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagensModeracao_AspNetUsers_AutorUsuarioId",
                        column: x => x.AutorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MensagensModeracao_Itens_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Itens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensModeracao_AutorUsuarioId",
                table: "MensagensModeracao",
                column: "AutorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MensagensModeracao_ItemId",
                table: "MensagensModeracao",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MensagensModeracao_TipoAlvo_AlvoId",
                table: "MensagensModeracao",
                columns: new[] { "TipoAlvo", "AlvoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MensagensModeracao");
        }
    }
}
