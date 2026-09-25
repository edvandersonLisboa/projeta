using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projetar.Web.Migrations
{
    /// <inheritdoc />
    public partial class EscopoDeModeracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EscoposModeracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    TipoEscopo = table.Column<int>(type: "integer", nullable: false),
                    MandamentoId = table.Column<int>(type: "integer", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdicionadoPorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    DataCriacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscoposModeracao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscoposModeracao_AspNetUsers_AdicionadoPorUsuarioId",
                        column: x => x.AdicionadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscoposModeracao_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscoposModeracao_Itens_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Itens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscoposModeracao_Mandamentos_MandamentoId",
                        column: x => x.MandamentoId,
                        principalTable: "Mandamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscoposModeracao_AdicionadoPorUsuarioId",
                table: "EscoposModeracao",
                column: "AdicionadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EscoposModeracao_ItemId",
                table: "EscoposModeracao",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EscoposModeracao_MandamentoId",
                table: "EscoposModeracao",
                column: "MandamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_EscoposModeracao_UsuarioId_TipoEscopo_MandamentoId_ItemId",
                table: "EscoposModeracao",
                columns: new[] { "UsuarioId", "TipoEscopo", "MandamentoId", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EscoposModeracao");
        }
    }
}
