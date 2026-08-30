namespace Mandamentos.Web.Models;

/// <summary>Formato de armazenamento de um corpo de texto (Item.Corpo, ItemRevisao.CorpoNovo).</summary>
public enum FormatoTexto
{
    /// <summary>Conteúdo original do seed — convertido com Markdig na renderização.</summary>
    Markdown = 0,

    /// <summary>Conteúdo criado pelo editor rich text (Quill) — já é HTML, só sanitizado na renderização.</summary>
    Html = 1,
}
