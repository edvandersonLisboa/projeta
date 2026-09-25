using Projetar.Web.Models;

namespace Projetar.Web.Services;

/// <summary>Uma linha unificada de "Meu Histórico" / "Conversas" — pode representar um Item novo, uma
/// edição de texto, uma referência, um documento, uma tag ou um banner, todos com o mesmo formato de exibição.</summary>
public class SubmissaoResumo
{
    public TipoSubmissao Tipo { get; set; }

    /// <summary>Id da entidade original (ItemRevisao.Id, Referencia.Id, etc. — ou Item.Id quando Tipo é Item).</summary>
    public Guid AlvoId { get; set; }

    public Guid ItemId { get; set; }
    public string ItemTitulo { get; set; } = string.Empty;
    public string ItemSlug { get; set; } = string.Empty;

    public string TipoRotulo { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;

    public string StatusRotulo { get; set; } = string.Empty;
    /// <summary>"pendente", "aprovado" ou "rejeitado" — mesma nomenclatura usada nas notificações.</summary>
    public string Situacao { get; set; } = string.Empty;

    public DateTimeOffset DataCriacao { get; set; }
    public DateTimeOffset? DataModeracao { get; set; }
    public string? MotivoRejeicao { get; set; }

    /// <summary>Link pra corrigir e reenviar (edição de texto/banner) ou pra ir até a seção certa do item
    /// (referência/documento/tag) — null quando não existe um fluxo de reenvio direto (item novo rejeitado).</summary>
    public string? LinkAcao { get; set; }

    public string AutorUsuarioId { get; set; } = string.Empty;
    public string? RevisorUsuarioId { get; set; }

    public int QuantidadeMensagens { get; set; }
    public DateTimeOffset? UltimaMensagemEm { get; set; }
}

/// <summary>Junta os seis tipos de submissão (item, edição de texto, referência, documento, tag, banner)
/// numa visão só, pra alimentar "Meu Histórico" (por autor) e "Conversas" na Moderação (por mensagem).</summary>
public interface ISubmissaoModeracaoService
{
    Task<List<SubmissaoResumo>> ListarPorAutorAsync(string usuarioId);

    /// <summary>Toda movimentação (de qualquer autor) que aconteceu nos itens informados — usado pelo
    /// Dashboard pra dar ao criador de um item a visão geral de tudo que aconteceu nele.</summary>
    Task<List<SubmissaoResumo>> ListarPorItensAsync(IEnumerable<Guid> itemIds);

    /// <summary>Toda movimentação da plataforma, sem filtro — visão global só pra Admin no Dashboard.</summary>
    Task<List<SubmissaoResumo>> ListarTodasAsync();

    /// <summary>Toda submissão (de qualquer autor) que já tem pelo menos uma mensagem trocada, mais recente primeiro.</summary>
    Task<List<SubmissaoResumo>> ListarComConversaAsync();

    Task<SubmissaoResumo?> ObterAsync(TipoSubmissao tipo, Guid alvoId);
}
