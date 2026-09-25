using System.Text.RegularExpressions;

namespace Projetar.Web.Services;

/// <summary>Ajuda a montar meta description a partir de corpo rich-text — usado nas tags
/// Open Graph/Twitter Card e no sitemap.</summary>
public static partial class SeoUtils
{
    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagHtml();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRepetidos();

    /// <summary>Remove tags HTML e corta no tamanho pedido, sem quebrar palavra no meio — pronto
    /// pra usar em meta description (~155-160 caracteres é o ideal pro Google não truncar feio).</summary>
    public static string ResumoTexto(string html, int maxLength = 155)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var semTags = System.Net.WebUtility.HtmlDecode(TagHtml().Replace(html, " "));
        var texto = EspacosRepetidos().Replace(semTags, " ").Trim();
        if (texto.Length <= maxLength)
        {
            return texto;
        }

        var cortado = texto[..maxLength];
        var ultimoEspaco = cortado.LastIndexOf(' ');
        if (ultimoEspaco > 0)
        {
            cortado = cortado[..ultimoEspaco];
        }
        return cortado.TrimEnd('.', ',', ';', ':', '-') + "…";
    }
}
