using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projetar.Web.Migrations
{
    /// <inheritdoc />
    public partial class ApoioDeRevisoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemRevisaoApoios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemRevisaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    DataCriacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRevisaoApoios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemRevisaoApoios_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemRevisaoApoios_ItemRevisoes_ItemRevisaoId",
                        column: x => x.ItemRevisaoId,
                        principalTable: "ItemRevisoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemRevisaoApoios_ItemRevisaoId_UsuarioId",
                table: "ItemRevisaoApoios",
                columns: new[] { "ItemRevisaoId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemRevisaoApoios_UsuarioId",
                table: "ItemRevisaoApoios",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemRevisaoApoios");
        }
    }
}
