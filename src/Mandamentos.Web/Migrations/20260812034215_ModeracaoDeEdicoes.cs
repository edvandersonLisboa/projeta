using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mandamentos.Web.Migrations
{
    /// <inheritdoc />
    public partial class ModeracaoDeEdicoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataModeracao",
                table: "ItemRevisoes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRejeicao",
                table: "ItemRevisoes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisadoPorUsuarioId",
                table: "ItemRevisoes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ItemRevisoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ItemRevisoes_RevisadoPorUsuarioId",
                table: "ItemRevisoes",
                column: "RevisadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRevisoes_Status",
                table: "ItemRevisoes",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemRevisoes_AspNetUsers_RevisadoPorUsuarioId",
                table: "ItemRevisoes",
                column: "RevisadoPorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemRevisoes_AspNetUsers_RevisadoPorUsuarioId",
                table: "ItemRevisoes");

            migrationBuilder.DropIndex(
                name: "IX_ItemRevisoes_RevisadoPorUsuarioId",
                table: "ItemRevisoes");

            migrationBuilder.DropIndex(
                name: "IX_ItemRevisoes_Status",
                table: "ItemRevisoes");

            migrationBuilder.DropColumn(
                name: "DataModeracao",
                table: "ItemRevisoes");

            migrationBuilder.DropColumn(
                name: "MotivoRejeicao",
                table: "ItemRevisoes");

            migrationBuilder.DropColumn(
                name: "RevisadoPorUsuarioId",
                table: "ItemRevisoes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ItemRevisoes");
        }
    }
}
