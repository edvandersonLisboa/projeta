namespace Mandamentos.Web.Models;

public class Comentario
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public Guid? ComentarioPaiId { get; set; }
    public Comentario? ComentarioPai { get; set; }
    public List<Comentario> Respostas { get; set; } = [];

    public string Texto { get; set; } = string.Empty;
    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DataEdicao { get; set; }
    public bool Removido { get; set; }
}
