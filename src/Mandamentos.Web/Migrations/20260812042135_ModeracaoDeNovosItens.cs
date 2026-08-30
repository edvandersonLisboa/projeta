using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mandamentos.Web.Migrations
{
    /// <inheritdoc />
    public partial class ModeracaoDeNovosItens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataModeracao",
                table: "Itens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRejeicao",
                table: "Itens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisadoPorUsuarioId",
                table: "Itens",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Itens_RevisadoPorUsuarioId",
                table: "Itens",
                column: "RevisadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Itens_Status",
                table: "Itens",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Itens_AspNetUsers_RevisadoPorUsuarioId",
                table: "Itens",
                column: "RevisadoPorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Itens_AspNetUsers_RevisadoPorUsuarioId",
                table: "Itens");

            migrationBuilder.DropIndex(
                name: "IX_Itens_RevisadoPorUsuarioId",
                table: "Itens");

            migrationBuilder.DropIndex(
                name: "IX_Itens_Status",
                table: "Itens");

            migrationBuilder.DropColumn(
                name: "DataModeracao",
                table: "Itens");

            migrationBuilder.DropColumn(
                name: "MotivoRejeicao",
                table: "Itens");

            migrationBuilder.DropColumn(
                name: "RevisadoPorUsuarioId",
                table: "Itens");
        }
    }
}
