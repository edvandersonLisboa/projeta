namespace Projetar.Web.Services;

/// <summary>Sanitização e resumo de conteúdo — todo Item/ItemRevisao guarda HTML vindo do editor rich text (Quill).</summary>
public interface IContentRenderer
{
    string ToSafeHtml(string corpoHtml);

    string ToExcerpt(string corpoHtml, int maxLength = 220);
}
