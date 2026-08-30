namespace Mandamentos.Web.Models;

public class Mandamento
{
    public int Id { get; set; }
    public string Biblical { get; set; } = string.Empty;
    public string Secular { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public int Ordem { get; set; }

    /// <summary>Quando falso, a seção só aparece para administradores — some da página inicial para o público.</summary>
    public bool Visivel { get; set; } = true;

    public List<Item> Itens { get; set; } = [];
}
