namespace Mandamentos.Web.Models;

public class Item
{
    public Guid Id { get; set; }

    public int MandamentoId { get; set; }
    public Mandamento? Mandamento { get; set; }

    public string Slug { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Corpo { get; set; } = string.Empty;
    public FormatoTexto FormatoCorpo { get; set; } = FormatoTexto.Markdown;
    public ItemStatus Status { get; set; } = ItemStatus.Original;

    /// <summary>Quando falso, o item só aparece para administradores — some da página inicial e do link direto para o público.</summary>
    public bool Visivel { get; set; } = true;

    /// <summary>Null = conteúdo original vindo do seed, não criado por um usuário.</summary>
    public string? CriadoPorUsuarioId { get; set; }
    public ApplicationUser? CriadoPorUsuario { get; set; }

    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DataAtualizacao { get; set; } = DateTimeOffset.UtcNow;
    public int Ordem { get; set; }

    /// <summary>Preenchidos quando um item proposto pela comunidade (Status inicial PropostaComunidade) é avaliado.</summary>
    public string? MotivoRejeicao { get; set; }
    public string? RevisadoPorUsuarioId { get; set; }
    public ApplicationUser? RevisadoPorUsuario { get; set; }
    public DateTimeOffset? DataModeracao { get; set; }

    public List<ItemRevisao> Revisoes { get; set; } = [];
    public List<Comentario> Comentarios { get; set; } = [];
    public List<Voto> Votos { get; set; } = [];
    public List<Documento> Documentos { get; set; } = [];
    public List<Referencia> Referencias { get; set; } = [];
}
