using System.Text.RegularExpressions;

namespace Projetar.Web.Services;

public static partial class RichTextUtils
{
    /// <summary>
    /// O editor Quill sempre grava algo (ex.: "&lt;p&gt;&lt;br&gt;&lt;/p&gt;") mesmo vazio,
    /// então [Required] sozinho não pega campo em branco. Verifica se sobra texto visível.
    /// </summary>
    public static bool EhVazio(string html) => string.IsNullOrWhiteSpace(TagsRegex().Replace(html, ""));

    /// <summary>Texto puro (sem tags) truncado — usado em pré-visualizações curtas, ex: card de item proposto na moderação.</summary>
    public static string Excerto(string html, int maxLen = 220)
    {
        var texto = System.Net.WebUtility.HtmlDecode(TagsRegex().Replace(html, " "));
        texto = EspacosRegex().Replace(texto, " ").Trim();
        return texto.Length <= maxLen ? texto : texto[..maxLen].TrimEnd() + "…";
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagsRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRegex();
}
