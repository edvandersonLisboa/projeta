namespace Projetar.Web.Models;

/// <summary>Avaliação por estrela (1-5) de um usuário para um Item. Um voto por usuário por item.</summary>
public class Voto
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public int Estrelas { get; set; }
    public string? Observacao { get; set; }
    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DataAtualizacao { get; set; } = DateTimeOffset.UtcNow;
}
