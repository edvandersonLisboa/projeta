using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Projetar.Web.Services;

public partial class RevisaoDiffService(IContentRenderer content) : IRevisaoDiffService
{
    public string RenderRedline(string corpoAntigoHtml, string corpoNovoHtml)
    {
        var textoAntigo = ParaTextoComParagrafos(content.ToSafeHtml(corpoAntigoHtml));
        var textoNovo = ParaTextoComParagrafos(content.ToSafeHtml(corpoNovoHtml));

        var modelo = SideBySideDiffBuilder.Diff(textoAntigo, textoNovo, ignoreWhiteSpace: false, ignoreCase: false);
        var linhasAntigas = modelo.OldText.Lines;
        var linhasNovas = modelo.NewText.Lines;

        var sb = new StringBuilder();
        var totalLinhas = Math.Max(linhasAntigas.Count, linhasNovas.Count);
        for (var i = 0; i < totalLinhas; i++)
        {
            var linhaAntiga = i < linhasAntigas.Count ? linhasAntigas[i] : null;
            var linhaNova = i < linhasNovas.Count ? linhasNovas[i] : null;

            if (linhaNova?.Type == ChangeType.Modified && linhaNova.SubPieces is not null && linhaAntiga?.SubPieces is not null)
            {
                sb.Append("<p>");
                AnexarPalavrasMescladas(linhaAntiga.SubPieces, linhaNova.SubPieces, sb);
                sb.Append("</p>");
                continue;
            }

            if (linhaNova?.Type == ChangeType.Unchanged)
            {
                sb.Append("<p>").Append(WebUtility.HtmlEncode(linhaNova.Text)).Append("</p>");
                continue;
            }

            if (linhaAntiga?.Type == ChangeType.Deleted)
            {
                sb.Append("<p><del class=\"gbr-diff-del\">").Append(WebUtility.HtmlEncode(linhaAntiga.Text)).Append("</del></p>");
            }

            if (linhaNova?.Type == ChangeType.Inserted)
            {
                sb.Append("<p><ins class=\"gbr-diff-add\">").Append(WebUtility.HtmlEncode(linhaNova.Text)).Append("</ins></p>");
            }
        }

        return sb.ToString();
    }

    /// <summary>As duas listas de sub-peças vêm alinhadas por índice (com Imaginary de preenchimento):
    /// palavra igual aparece Unchanged nas duas ao mesmo índice; removida vem Deleted só na antiga;
    /// adicionada vem Inserted só na nova. Percorrendo por índice, dá pra remontar a ordem de leitura original.</summary>
    private static void AnexarPalavrasMescladas(List<DiffPiece> antigas, List<DiffPiece> novas, StringBuilder sb)
    {
        var total = Math.Max(antigas.Count, novas.Count);
        for (var i = 0; i < total; i++)
        {
            var antiga = i < antigas.Count ? antigas[i] : null;
            var nova = i < novas.Count ? novas[i] : null;

            if (nova?.Type == ChangeType.Unchanged)
            {
                sb.Append(WebUtility.HtmlEncode(nova.Text));
                continue;
            }

            if (antiga?.Type == ChangeType.Deleted)
            {
                sb.Append("<del class=\"gbr-diff-del\">").Append(WebUtility.HtmlEncode(antiga.Text)).Append("</del>");
            }

            if (nova?.Type == ChangeType.Inserted)
            {
                sb.Append("<ins class=\"gbr-diff-add\">").Append(WebUtility.HtmlEncode(nova.Text)).Append("</ins>");
            }
        }
    }

    /// <summary>HTML sanitizado vira texto simples, uma linha por parágrafo/item de lista, pra diff por linha+palavra.</summary>
    private static string ParaTextoComParagrafos(string html)
    {
        var comQuebras = QuebraDeBlocoRegex().Replace(html, "\n");
        var semTags = TagRegex().Replace(comQuebras, string.Empty);
        var decodificado = WebUtility.HtmlDecode(semTags);
        var linhas = decodificado
            .Split('\n')
            .Select(l => EspacosRegex().Replace(l, " ").Trim())
            .Where(l => l.Length > 0);
        return string.Join('\n', linhas);
    }

    [GeneratedRegex(@"</p>|</li>|</h[1-6]>|<br\s*/?>")]
    private static partial Regex QuebraDeBlocoRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex EspacosRegex();
}
