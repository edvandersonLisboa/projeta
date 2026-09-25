namespace Projetar.Web.Models;

public enum TipoEscopoModeracao
{
    /// <summary>Modera qualquer item do princípio, presente ou futuro.</summary>
    Principio = 0,

    /// <summary>Modera só aquele item específico.</summary>
    Item = 1,
}

public enum StatusEscopoModeracao
{
    /// <summary>Concedido diretamente (por Admin) ou pelo criador ao aprovar o próprio item — já vale.</summary>
    Aceito = 0,

    /// <summary>Convite enviado por quem já modera o item — só passa a valer quando o convidado aceitar.</summary>
    Pendente = 1,
}

/// <summary>Concessão de permissão de moderação (Revisor) a um usuário, escopada a um princípio inteiro
/// ou a um item específico. Admin não precisa de linha aqui — tem acesso global sempre.</summary>
public class EscopoModeracao
{
    public Guid Id { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public TipoEscopoModeracao TipoEscopo { get; set; }

    public int? PrincipioId { get; set; }
    public Principio? Principio { get; set; }

    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    public string AdicionadoPorUsuarioId { get; set; } = string.Empty;
    public ApplicationUser? AdicionadoPorUsuario { get; set; }

    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;

    public StatusEscopoModeracao Status { get; set; } = StatusEscopoModeracao.Aceito;
}
