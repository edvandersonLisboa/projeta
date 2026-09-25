namespace Projetar.Web.Models;

/// <summary>Catálogo de tags sugeridas pelo projeto — oferecido como ponto de partida no modal de
/// tags de um item, somado às tags que já foram usadas organicamente em outros itens.</summary>
public class TagCatalogo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
}
