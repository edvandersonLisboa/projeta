namespace Projetar.Web.Data.Seed;

public class MandamentoSeedDto
{
    public int Id { get; set; }
    public string Biblical { get; set; } = string.Empty;
    public string Secular { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public List<ItemSeedDto> Itens { get; set; } = [];
}

public class ItemSeedDto
{
    public string Slug { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Corpo { get; set; } = string.Empty;
    public int Ordem { get; set; }
}
