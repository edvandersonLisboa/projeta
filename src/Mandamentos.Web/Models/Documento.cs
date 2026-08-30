namespace Mandamentos.Web.Models;

/// <summary>Documento anexado a um Item (ex: proposta formal, estudo, PDF de apoio).</summary>
public class Documento
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public string NomeOriginal { get; set; } = string.Empty;
    public string CaminhoArmazenado { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public DateTimeOffset DataUpload { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Mesma fila de moderação usada em ItemRevisao — só aparece publicamente quando Aprovado.</summary>
    public RevisaoStatus Status { get; set; } = RevisaoStatus.Pendente;
    public string? MotivoRejeicao { get; set; }
    public string? RevisadoPorUsuarioId { get; set; }
    public ApplicationUser? RevisadoPorUsuario { get; set; }
    public DateTimeOffset? DataModeracao { get; set; }
}
