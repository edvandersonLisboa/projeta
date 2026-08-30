using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mandamentos.Web.Migrations
{
    /// <inheritdoc />
    public partial class ModeracaoReferenciasEDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataModeracao",
                table: "Referencias",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRejeicao",
                table: "Referencias",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisadoPorUsuarioId",
                table: "Referencias",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Referencias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataModeracao",
                table: "Documentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRejeicao",
                table: "Documentos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisadoPorUsuarioId",
                table: "Documentos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Documentos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Referencias_RevisadoPorUsuarioId",
                table: "Referencias",
                column: "RevisadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Referencias_Status",
                table: "Referencias",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_RevisadoPorUsuarioId",
                table: "Documentos",
                column: "RevisadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_Status",
                table: "Documentos",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Documentos_AspNetUsers_RevisadoPorUsuarioId",
                table: "Documentos",
                column: "RevisadoPorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Referencias_AspNetUsers_RevisadoPorUsuarioId",
                table: "Referencias",
                column: "RevisadoPorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documentos_AspNetUsers_RevisadoPorUsuarioId",
                table: "Documentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Referencias_AspNetUsers_RevisadoPorUsuarioId",
                table: "Referencias");

            migrationBuilder.DropIndex(
                name: "IX_Referencias_RevisadoPorUsuarioId",
                table: "Referencias");

            migrationBuilder.DropIndex(
                name: "IX_Referencias_Status",
                table: "Referencias");

            migrationBuilder.DropIndex(
                name: "IX_Documentos_RevisadoPorUsuarioId",
                table: "Documentos");

            migrationBuilder.DropIndex(
                name: "IX_Documentos_Status",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "DataModeracao",
                table: "Referencias");

            migrationBuilder.DropColumn(
                name: "MotivoRejeicao",
                table: "Referencias");

            migrationBuilder.DropColumn(
                name: "RevisadoPorUsuarioId",
                table: "Referencias");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Referencias");

            migrationBuilder.DropColumn(
                name: "DataModeracao",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "MotivoRejeicao",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "RevisadoPorUsuarioId",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Documentos");
        }
    }
}
