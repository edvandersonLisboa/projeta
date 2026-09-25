namespace Projetar.Web.Models;

public class Mandamento
{
    public int Id { get; set; }

    /// <summary>Resumo curto (subtítulo) mostrado junto do título nos cards — sem referência religiosa.</summary>
    public string Subtitulo { get; set; } = string.Empty;

    public string Secular { get; set; } = string.Empty;

    /// <summary>Parágrafo de abertura do princípio — no máximo 500 caracteres.</summary>
    public string Intro { get; set; } = string.Empty;

    public int Ordem { get; set; }

    /// <summary>Quando falso, a seção só aparece para administradores — some da página inicial para o público.</summary>
    public bool Visivel { get; set; } = true;

    public List<Item> Itens { get; set; } = [];
}
