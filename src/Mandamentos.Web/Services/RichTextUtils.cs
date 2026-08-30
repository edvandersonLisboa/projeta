using System.Text.RegularExpressions;

namespace Mandamentos.Web.Services;

public static partial class RichTextUtils
{
    /// <summary>
    /// O editor Quill sempre grava algo (ex.: "&lt;p&gt;&lt;br&gt;&lt;/p&gt;") mesmo vazio,
    /// então [Required] sozinho não pega campo em branco. Verifica se sobra texto visível.
    /// </summary>
    public static bool EhVazio(string html) => string.IsNullOrWhiteSpace(TagsRegex().Replace(html, ""));

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagsRegex();
}
