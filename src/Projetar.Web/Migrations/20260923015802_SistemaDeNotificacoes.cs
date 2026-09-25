using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projetar.Web.Migrations
{
    /// <inheritdoc />
    public partial class SistemaDeNotificacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioDestinoId = table.Column<string>(type: "text", nullable: false),
                    UsuarioOrigemId = table.Column<string>(type: "text", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Mensagem = table.Column<string>(type: "text", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkUrl = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Lida = table.Column<bool>(type: "boolean", nullable: false),
                    DataLeitura = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_AspNetUsers_UsuarioDestinoId",
                        column: x => x.UsuarioDestinoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notificacoes_AspNetUsers_UsuarioOrigemId",
                        column: x => x.UsuarioOrigemId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Itens_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Itens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_ItemId",
                table: "Notificacoes",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UsuarioDestinoId_Lida",
                table: "Notificacoes",
                columns: new[] { "UsuarioDestinoId", "Lida" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UsuarioOrigemId",
                table: "Notificacoes",
                column: "UsuarioOrigemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificacoes");
        }
    }
}
