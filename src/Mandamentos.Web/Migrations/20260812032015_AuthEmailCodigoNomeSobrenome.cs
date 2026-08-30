using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mandamentos.Web.Migrations
{
    /// <inheritdoc />
    public partial class AuthEmailCodigoNomeSobrenome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "NomeCompleto",
                table: "AspNetUsers",
                newName: "Sobrenome");

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EmailConfirmationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    CodigoHash = table.Column<string>(type: "text", nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Tentativas = table.Column<int>(type: "integer", nullable: false),
                    Usado = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfirmationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailConfirmationCodes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfirmationCodes_UsuarioId_Usado",
                table: "EmailConfirmationCodes",
                columns: new[] { "UsuarioId", "Usado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailConfirmationCodes");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Sobrenome",
                table: "AspNetUsers",
                newName: "NomeCompleto");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");
        }
    }
}
