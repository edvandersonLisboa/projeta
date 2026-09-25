namespace Projetar.Web.Models;

public enum TipoNotificacao
{
    NovoItemProposto = 0,
    ItemAprovado = 1,
    ItemRejeitado = 2,
    EdicaoTextoProposta = 3,
    EdicaoTextoAprovada = 4,
    EdicaoTextoRejeitada = 5,
    ReferenciaProposta = 6,
    ReferenciaAprovada = 7,
    ReferenciaRejeitada = 8,
    DocumentoProposto = 9,
    DocumentoAprovado = 10,
    DocumentoRejeitado = 11,
    TagProposta = 12,
    TagAprovada = 13,
    TagRejeitada = 14,
    BannerProposto = 15,
    BannerAprovado = 16,
    BannerRejeitado = 17,
    ComentarioNovo = 18,
    ComentarioRespondido = 19,
    ComentarioRemovido = 20,
    AvaliacaoNova = 21,
    ApoioRecebido = 22,
    BannerRemovido = 23,
    TagRemovida = 24,
    SecaoEditada = 25,
    SecaoVisibilidadeAlterada = 26,
    ItemVisibilidadeAlterada = 27,
    MensagemModeracao = 28,
    SecaoCriada = 29,
    ItemMovidoDePrincipio = 30,
    ConviteModeracaoItem = 31,
}

/// <summary>Rótulo curto e classe visual de cada tipo, usados nos cards de notificação.</summary>
public static class TipoNotificacaoExtensions
{
    public static string Rotulo(this TipoNotificacao tipo) => tipo switch
    {
        TipoNotificacao.NovoItemProposto => "Novo item",
        TipoNotificacao.ItemAprovado => "Item aprovado",
        TipoNotificacao.ItemRejeitado => "Item rejeitado",
        TipoNotificacao.EdicaoTextoProposta => "Edição de texto",
        TipoNotificacao.EdicaoTextoAprovada => "Edição aprovada",
        TipoNotificacao.EdicaoTextoRejeitada => "Edição rejeitada",
        TipoNotificacao.ReferenciaProposta => "Referência",
        TipoNotificacao.ReferenciaAprovada => "Referência aprovada",
        TipoNotificacao.ReferenciaRejeitada => "Referência rejeitada",
        TipoNotificacao.DocumentoProposto => "Documento",
        TipoNotificacao.DocumentoAprovado => "Documento aprovado",
        TipoNotificacao.DocumentoRejeitado => "Documento rejeitado",
        TipoNotificacao.TagProposta => "Tag sugerida",
        TipoNotificacao.TagAprovada => "Tag aprovada",
        TipoNotificacao.TagRejeitada => "Tag rejeitada",
        TipoNotificacao.BannerProposto => "Banner proposto",
        TipoNotificacao.BannerAprovado => "Banner aprovado",
        TipoNotificacao.BannerRejeitado => "Banner rejeitado",
        TipoNotificacao.ComentarioNovo => "Novo comentário",
        TipoNotificacao.ComentarioRespondido => "Resposta a comentário",
        TipoNotificacao.ComentarioRemovido => "Comentário removido",
        TipoNotificacao.AvaliacaoNova => "Nova avaliação",
        TipoNotificacao.ApoioRecebido => "Apoio a proposta",
        TipoNotificacao.BannerRemovido => "Banner removido",
        TipoNotificacao.TagRemovida => "Tag removida",
        TipoNotificacao.SecaoEditada => "Seção editada",
        TipoNotificacao.SecaoVisibilidadeAlterada => "Visibilidade de seção alterada",
        TipoNotificacao.ItemVisibilidadeAlterada => "Visibilidade de item alterada",
        TipoNotificacao.MensagemModeracao => "Nova mensagem",
        TipoNotificacao.SecaoCriada => "Novo princípio",
        TipoNotificacao.ItemMovidoDePrincipio => "Item movido de princípio",
        TipoNotificacao.ConviteModeracaoItem => "Convite para moderar",
        _ => "Notificação",
    };

    public static string ClasseIcone(this TipoNotificacao tipo) => $"pj-notif__icone--{tipo.Situacao()}";

    /// <summary>"aprovado", "rejeitado", "pendente" ou "info" — usado tanto no sino (pj-notif__icone--*) quanto na lista (gbr-notif-card__ponto--*).</summary>
    public static string Situacao(this TipoNotificacao tipo) => tipo switch
    {
        TipoNotificacao.ItemAprovado or TipoNotificacao.EdicaoTextoAprovada or TipoNotificacao.ReferenciaAprovada
            or TipoNotificacao.DocumentoAprovado or TipoNotificacao.TagAprovada or TipoNotificacao.BannerAprovado => "aprovado",
        TipoNotificacao.ItemRejeitado or TipoNotificacao.EdicaoTextoRejeitada or TipoNotificacao.ReferenciaRejeitada
            or TipoNotificacao.DocumentoRejeitado or TipoNotificacao.TagRejeitada or TipoNotificacao.BannerRejeitado => "rejeitado",
        TipoNotificacao.NovoItemProposto or TipoNotificacao.EdicaoTextoProposta or TipoNotificacao.ReferenciaProposta
            or TipoNotificacao.DocumentoProposto or TipoNotificacao.TagProposta or TipoNotificacao.BannerProposto
            or TipoNotificacao.ConviteModeracaoItem => "pendente",
        _ => "info",
    };
}

/// <summary>Notificação de um evento de moderação/edição — criada tanto para os moderadores (algo novo pra avaliar)
/// quanto para o autor de uma proposta (quando ela é aprovada ou rejeitada).</summary>
public class Notificacao
{
    public Guid Id { get; set; }

    public string UsuarioDestinoId { get; set; } = string.Empty;
    public ApplicationUser? UsuarioDestino { get; set; }

    /// <summary>Quem praticou a ação que gerou a notificação — null quando a origem é o sistema.</summary>
    public string? UsuarioOrigemId { get; set; }
    public ApplicationUser? UsuarioOrigem { get; set; }

    public TipoNotificacao Tipo { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>Item relacionado ao assunto da notificação, quando existir.</summary>
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    /// <summary>Para onde a notificação leva ao ser clicada (ex: página do item, ou a própria tela de moderação).</summary>
    public string? LinkUrl { get; set; }

    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;

    public bool Lida { get; set; }
    public DateTimeOffset? DataLeitura { get; set; }
}
