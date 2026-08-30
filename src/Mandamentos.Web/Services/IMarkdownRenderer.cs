using Mandamentos.Web.Models;

namespace Mandamentos.Web.Services;

public interface IMarkdownRenderer
{
    /// <summary>Renderiza o corpo (Markdown ou HTML do editor rich text) para HTML sanitizado.</summary>
    string ToSafeHtml(string corpo, FormatoTexto formato);

    /// <summary>Resumo em texto puro, truncado, para uso em listagens/cards.</summary>
    string ToExcerpt(string corpo, FormatoTexto formato, int maxLength = 220);
}
