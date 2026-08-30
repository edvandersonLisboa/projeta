using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Mandamentos.Web.Models;
using Markdig;

namespace Mandamentos.Web.Services;

public partial class MarkdownRenderer : IMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml() // conteúdo vem de usuários: nunca deixar passar HTML bruto embutido no markdown
        .Build();

    private readonly HtmlSanitizer _sanitizer = new();

    public string ToSafeHtml(string corpo, FormatoTexto formato)
    {
        if (string.IsNullOrWhiteSpace(corpo))
        {
            return string.Empty;
        }

        // Markdown vira HTML via Markdig antes de sanitizar; o editor rich text (Quill) já entrega HTML,
        // então só passa pelo sanitizador.
        var html = formato == FormatoTexto.Markdown ? Markdown.ToHtml(corpo, _pipeline) : corpo;
        return _sanitizer.Sanitize(html);
    }

    public string ToExcerpt(string corpo, FormatoTexto formato, int maxLength = 220)
    {
        if (string.IsNullOrWhiteSpace(corpo))
        {
            return string.Empty;
        }

        var plain = formato == FormatoTexto.Markdown ? ParaTextoPlanoMarkdown(corpo) : ParaTextoPlanoHtml(corpo);

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

    private static string ParaTextoPlanoMarkdown(string markdown)
    {
        var plain = NegritoMarkdownRegex().Replace(markdown, "$1");
        plain = ListaComTracoRegex().Replace(plain, "");
        plain = ListaNumeradaRegex().Replace(plain, "");
        return EspacosRegex().Replace(plain, " ").Trim();
    }

    private string ParaTextoPlanoHtml(string html)
    {
        // Sanitiza antes de tirar as tags: um <script>/<style> teria o CONTEÚDO (não só a tag)
        // vazado pro resumo se a gente só removesse "<[^>]+>" no HTML bruto.
        var seguro = _sanitizer.Sanitize(html);
        var plain = TagsHtmlRegex().Replace(seguro, " ");
        plain = WebUtility.HtmlDecode(plain);
        return EspacosRegex().Replace(plain, " ").Trim();
    }

    [GeneratedRegex(@"\*\*(.*?)\*\*")]
    private static partial Regex NegritoMarkdownRegex();

    [GeneratedRegex(@"^\s*[-*]\s+", RegexOptions.Multiline)]
    private static partial Regex ListaComTracoRegex();

    [GeneratedRegex(@"^\s*\d+\.\s+", RegexOptions.Multiline)]
    private static partial Regex ListaNumeradaRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagsHtmlRegex();
}
