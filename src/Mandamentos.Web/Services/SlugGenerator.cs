using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Mandamentos.Web.Services;

public static partial class SlugGenerator
{
    public static string Gerar(string texto, int tamanhoMaximo = 80)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var semAcentos = new StringBuilder();
        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                semAcentos.Append(c);
            }
        }

        var minusculo = semAcentos.ToString().ToLowerInvariant();
        var comHifens = SeparadoresRegex().Replace(minusculo, "-").Trim('-');

        return comHifens.Length > tamanhoMaximo
            ? comHifens[..tamanhoMaximo].Trim('-')
            : comHifens;
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SeparadoresRegex();
}
