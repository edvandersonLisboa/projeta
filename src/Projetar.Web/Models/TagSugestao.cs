namespace Projetar.Web.Models;

/// <summary>Sugestão de tag pra um Item — mesma fila de moderação usada em Referencia/Documento/ItemRevisao.</summary>
public class TagSugestao
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public string Nome { get; set; } = string.Empty;
    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;

    public RevisaoStatus Status { get; set; } = RevisaoStatus.Pendente;
    public string? MotivoRejeicao { get; set; }
    public string? RevisadoPorUsuarioId { get; set; }
    public ApplicationUser? RevisadoPorUsuario { get; set; }
    public DateTimeOffset? DataModeracao { get; set; }
}
