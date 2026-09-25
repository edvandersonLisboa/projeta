using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projetar.Web.Migrations
{
    /// <inheritdoc />
    public partial class RenomeiaMandamentoParaPrincipio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EscoposModeracao_Mandamentos_MandamentoId",
                table: "EscoposModeracao");

            migrationBuilder.DropForeignKey(
                name: "FK_Itens_Mandamentos_MandamentoId",
                table: "Itens");

            migrationBuilder.RenameTable(
                name: "Mandamentos",
                newName: "Principios");

            migrationBuilder.Sql("ALTER TABLE \"Principios\" RENAME CONSTRAINT \"PK_Mandamentos\" TO \"PK_Principios\";");

            migrationBuilder.RenameColumn(
                name: "MandamentoId",
                table: "Itens",
                newName: "PrincipioId");

            migrationBuilder.RenameIndex(
                name: "IX_Itens_MandamentoId",
                table: "Itens",
                newName: "IX_Itens_PrincipioId");

            migrationBuilder.RenameColumn(
                name: "MandamentoId",
                table: "EscoposModeracao",
                newName: "PrincipioId");

            migrationBuilder.RenameIndex(
                name: "IX_EscoposModeracao_UsuarioId_TipoEscopo_MandamentoId_ItemId",
                table: "EscoposModeracao",
                newName: "IX_EscoposModeracao_UsuarioId_TipoEscopo_PrincipioId_ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_EscoposModeracao_MandamentoId",
                table: "EscoposModeracao",
                newName: "IX_EscoposModeracao_PrincipioId");

            migrationBuilder.RenameIndex(
                name: "IX_Mandamentos_Ordem",
                table: "Principios",
                newName: "IX_Principios_Ordem");

            migrationBuilder.AddForeignKey(
                name: "FK_EscoposModeracao_Principios_PrincipioId",
                table: "EscoposModeracao",
                column: "PrincipioId",
                principalTable: "Principios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Itens_Principios_PrincipioId",
                table: "Itens",
                column: "PrincipioId",
                principalTable: "Principios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EscoposModeracao_Principios_PrincipioId",
                table: "EscoposModeracao");

            migrationBuilder.DropForeignKey(
                name: "FK_Itens_Principios_PrincipioId",
                table: "Itens");

            migrationBuilder.RenameTable(
                name: "Principios",
                newName: "Mandamentos");

            migrationBuilder.Sql("ALTER TABLE \"Mandamentos\" RENAME CONSTRAINT \"PK_Principios\" TO \"PK_Mandamentos\";");

            migrationBuilder.RenameColumn(
                name: "PrincipioId",
                table: "Itens",
                newName: "MandamentoId");

            migrationBuilder.RenameIndex(
                name: "IX_Itens_PrincipioId",
                table: "Itens",
                newName: "IX_Itens_MandamentoId");

            migrationBuilder.RenameColumn(
                name: "PrincipioId",
                table: "EscoposModeracao",
                newName: "MandamentoId");

            migrationBuilder.RenameIndex(
                name: "IX_EscoposModeracao_UsuarioId_TipoEscopo_PrincipioId_ItemId",
                table: "EscoposModeracao",
                newName: "IX_EscoposModeracao_UsuarioId_TipoEscopo_MandamentoId_ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_EscoposModeracao_PrincipioId",
                table: "EscoposModeracao",
                newName: "IX_EscoposModeracao_MandamentoId");

            migrationBuilder.RenameIndex(
                name: "IX_Principios_Ordem",
                table: "Mandamentos",
                newName: "IX_Mandamentos_Ordem");

            migrationBuilder.AddForeignKey(
                name: "FK_EscoposModeracao_Mandamentos_MandamentoId",
                table: "EscoposModeracao",
                column: "MandamentoId",
                principalTable: "Mandamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Itens_Mandamentos_MandamentoId",
                table: "Itens",
                column: "MandamentoId",
                principalTable: "Mandamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
