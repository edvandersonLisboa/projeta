using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace Projetar.Web.Services;

public partial class ContentRenderer : IContentRenderer
{
    private readonly HtmlSanitizer _sanitizer = new();

    public string ToSafeHtml(string corpoHtml)
    {
        if (string.IsNullOrWhiteSpace(corpoHtml))
        {
            return string.Empty;
        }

        return _sanitizer.Sanitize(corpoHtml);
    }

    public string ToExcerpt(string corpoHtml, int maxLength = 220)
    {
        if (string.IsNullOrWhiteSpace(corpoHtml))
        {
            return string.Empty;
        }

        var plain = ParaTextoPlano(corpoHtml);

        if (plain.Length <= maxLength)
        {
            return plain;
        }

        var cut = plain[..maxLength];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > 0)
        {
            cut = cut[..lastSpace];
        }

        return cut.TrimEnd('.', ',', ';', ':') + "…";
    }

    private string ParaTextoPlano(string html)
    {
        // Sanitiza antes de tirar as tags: um <script>/<style> teria o CONTEÚDO (não só a tag)
        // vazado pro resumo se a gente só removesse "<[^>]+>" no HTML bruto.
        var seguro = _sanitizer.Sanitize(html);
        var plain = TagsHtmlRegex().Replace(seguro, " ");
        plain = WebUtility.HtmlDecode(plain);
        return EspacosRegex().Replace(plain, " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagsHtmlRegex();
}
