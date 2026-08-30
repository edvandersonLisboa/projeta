namespace Mandamentos.Web.Models;

public enum ReferenciaTipo
{
    Artigo = 0,
    Video = 1,
    LeiOuNorma = 2,
    Outro = 3,
}

/// <summary>Link externo (artigo, vídeo, lei) usado para fortalecer o argumento de um Item.</summary>
public class Referencia
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ReferenciaTipo Tipo { get; set; } = ReferenciaTipo.Outro;
    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Mesma fila de moderação usada em ItemRevisao — só aparece publicamente quando Aprovada.</summary>
    public RevisaoStatus Status { get; set; } = RevisaoStatus.Pendente;
    public string? MotivoRejeicao { get; set; }
    public string? RevisadoPorUsuarioId { get; set; }
    public ApplicationUser? RevisadoPorUsuario { get; set; }
    public DateTimeOffset? DataModeracao { get; set; }
}
