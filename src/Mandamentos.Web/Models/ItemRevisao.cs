namespace Mandamentos.Web.Models;

public enum RevisaoStatus
{
    Pendente = 0,
    Aprovada = 1,
    Rejeitada = 2,
}

/// <summary>Histórico de edições colaborativas de um Item, tipo wiki.</summary>
public class ItemRevisao
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public string AutorUsuarioId { get; set; } = string.Empty;
    public ApplicationUser? AutorUsuario { get; set; }

    public string CorpoAnterior { get; set; } = string.Empty;

    /// <summary>Formato de CorpoAnterior no momento da edição (o Item pode ter sido reformatado depois).</summary>
    public FormatoTexto FormatoCorpoAnterior { get; set; } = FormatoTexto.Markdown;

    /// <summary>CorpoNovo sempre vem do editor rich text (Quill), então é sempre HTML.</summary>
    public string CorpoNovo { get; set; } = string.Empty;

    public string? ComentarioDaMudanca { get; set; }
    public DateTimeOffset DataRevisao { get; set; } = DateTimeOffset.UtcNow;

    public RevisaoStatus Status { get; set; } = RevisaoStatus.Pendente;
    public string? MotivoRejeicao { get; set; }
    public string? RevisadoPorUsuarioId { get; set; }
    public ApplicationUser? RevisadoPorUsuario { get; set; }
    public DateTimeOffset? DataModeracao { get; set; }
}
