namespace Projetar.Web.Models;

/// <summary>Coassinatura de um participante numa revisão pendente de outro autor — em vez de propor uma edição concorrente.</summary>
public class ItemRevisaoApoio
{
    public Guid Id { get; set; }

    public Guid ItemRevisaoId { get; set; }
    public ItemRevisao? ItemRevisao { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;
}
