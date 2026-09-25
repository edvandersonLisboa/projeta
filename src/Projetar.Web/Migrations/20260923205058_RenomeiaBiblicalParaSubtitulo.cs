using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projetar.Web.Migrations
{
    /// <inheritdoc />
    public partial class RenomeiaBiblicalParaSubtitulo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Biblical",
                table: "Mandamentos",
                newName: "Subtitulo");

            // Os 10 princípios originais já estavam seedados com a frase bíblica antiga — troca pelo
            // resumo curto e laico correspondente, o mesmo texto que passou a valer no JSON de seed.
            migrationBuilder.Sql("UPDATE \"Mandamentos\" SET \"Subtitulo\" = 'Lealdade ao país acima de partido, lobby e financiador' WHERE \"Id\" = 1;");
            migrationBuilder.Sql("UPDATE \"Mandamentos\" SET \"Subtitulo\" = 'Mandato é função revogável, não figura de culto' WHERE \"Id\" = 2;");
            migrationBuilder.Sql("UPDATE \"Mandamentos\" SET \"Subtitulo\" = 'A fé é do cidadão; o Estado é de todos' WHERE \"Id\" = 3;");
            migrationBuilder.Sql("UPDATE \"Mandamentos\" SET \"Subtitulo\" = 'A pessoa é o fim da política, nunca o meio' WHERE \"Id\" = 4;");
            migrationBuilder.Sql("UPDATE \"Mandamentos\" SET \"Subtitulo\" = 'Decidir hoje respondendo por quem vem depois' WHERE \"Id\" = 5;");
            migrationBuilder.Sql("UPDATE \"Mandamentos\" SET \"Subtitulo\" = 'Segurança pública como política de Estado, não como bandeira' WHERE \"Id\" = 6;");
            migrationBuilder.Sql("UPDATE \"Mandamentos\" SET \"Subtitulo\" = 'O programa registrado obriga quem foi eleito' WHERE \"Id\" = 7;");
            migrationBuilder.Sql("UPDATE \"Mandamentos\" SET \"Subtitulo\" = 'Dinheiro público é intocável' WHERE \"Id\" = 8;");
            migrationBuilder.Sql("UPDATE \"Mandamentos\" SET \"Subtitulo\" = 'Desinformação é ataque à democracia' WHERE \"Id\" = 9;");
            migrationBuilder.Sql("UPDATE \"Mandamentos\" SET \"Subtitulo\" = 'Desigualdade extrema é falha de política pública, não destino' WHERE \"Id\" = 10;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Subtitulo",
                table: "Mandamentos",
                newName: "Biblical");
        }
    }
}
