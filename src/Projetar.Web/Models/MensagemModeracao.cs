namespace Projetar.Web.Models;

/// <summary>A que tipo de submissão uma mensagem/entrada de histórico se refere.</summary>
public enum TipoSubmissao
{
    Item = 0,
    EdicaoTexto = 1,
    Referencia = 2,
    Documento = 3,
    Tag = 4,
    Banner = 5,
}

/// <summary>Rótulo e classe visual (reaproveita .pj-type--* da tela de Moderação) de cada tipo de submissão.</summary>
public static class TipoSubmissaoExtensions
{
    public static string Rotulo(this TipoSubmissao tipo) => tipo switch
    {
        TipoSubmissao.Item => "Item novo",
        TipoSubmissao.EdicaoTexto => "Edição de texto",
        TipoSubmissao.Referencia => "Referência",
        TipoSubmissao.Documento => "Documento",
        TipoSubmissao.Tag => "Tag",
        TipoSubmissao.Banner => "Banner",
        _ => "Contribuição",
    };

    public static string ClasseTipo(this TipoSubmissao tipo) => tipo switch
    {
        TipoSubmissao.Item => "pj-type--item",
        TipoSubmissao.EdicaoTexto => "pj-type--edit",
        TipoSubmissao.Referencia => "pj-type--ref",
        TipoSubmissao.Documento => "pj-type--doc",
        TipoSubmissao.Tag => "pj-type--tag",
        _ => "pj-type--banner",
    };
}

/// <summary>Mensagem trocada entre o autor de uma submissão (edição de texto, referência, documento, tag,
/// banner ou item novo) e um moderador — conversa de ida e volta sobre uma proposta, tipicamente depois
/// de uma rejeição. Ligada à entidade original via TipoAlvo+AlvoId em vez de uma FK dedicada por tipo,
/// já que uma mesma mensagem pode se referir a qualquer um dos cinco tipos de submissão.</summary>
public class MensagemModeracao
{
    public Guid Id { get; set; }

    public TipoSubmissao TipoAlvo { get; set; }

    /// <summary>Id da entidade original: ItemRevisao.Id, Referencia.Id, Documento.Id, TagSugestao.Id,
    /// BannerSugestao.Id — ou Item.Id quando TipoAlvo é Item.</summary>
    public Guid AlvoId { get; set; }

    /// <summary>Guardado direto (não derivado de AlvoId) pra listar/filtrar sem precisar resolver o tipo primeiro.</summary>
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public string AutorUsuarioId { get; set; } = string.Empty;
    public ApplicationUser? AutorUsuario { get; set; }

    public string Texto { get; set; } = string.Empty;
    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;
}
