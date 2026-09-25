using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projetar.Web.Migrations
{
    /// <inheritdoc />
    public partial class ModeracaoDeTagsBannerEAjusteDeTexto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComentarioDaModeracao",
                table: "ItemRevisoes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorpoAprovado",
                table: "ItemRevisoes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BannerSugestoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    CaminhoImagem = table.Column<string>(type: "text", nullable: false),
                    DataCriacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MotivoRejeicao = table.Column<string>(type: "text", nullable: true),
                    RevisadoPorUsuarioId = table.Column<string>(type: "text", nullable: true),
                    DataModeracao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannerSugestoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BannerSugestoes_AspNetUsers_RevisadoPorUsuarioId",
                        column: x => x.RevisadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BannerSugestoes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BannerSugestoes_Itens_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Itens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagSugestoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    DataCriacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MotivoRejeicao = table.Column<string>(type: "text", nullable: true),
                    RevisadoPorUsuarioId = table.Column<string>(type: "text", nullable: true),
                    DataModeracao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagSugestoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagSugestoes_AspNetUsers_RevisadoPorUsuarioId",
                        column: x => x.RevisadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TagSugestoes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TagSugestoes_Itens_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Itens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BannerSugestoes_ItemId",
                table: "BannerSugestoes",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BannerSugestoes_RevisadoPorUsuarioId",
                table: "BannerSugestoes",
                column: "RevisadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_BannerSugestoes_Status",
                table: "BannerSugestoes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BannerSugestoes_UsuarioId",
                table: "BannerSugestoes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TagSugestoes_ItemId",
                table: "TagSugestoes",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TagSugestoes_RevisadoPorUsuarioId",
                table: "TagSugestoes",
                column: "RevisadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TagSugestoes_Status",
                table: "TagSugestoes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TagSugestoes_UsuarioId",
                table: "TagSugestoes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannerSugestoes");

            migrationBuilder.DropTable(
                name: "TagSugestoes");

            migrationBuilder.DropColumn(
                name: "ComentarioDaModeracao",
                table: "ItemRevisoes");

            migrationBuilder.DropColumn(
                name: "CorpoAprovado",
                table: "ItemRevisoes");
        }
    }
}
