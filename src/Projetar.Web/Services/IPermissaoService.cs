using Projetar.Web.Models;

namespace Projetar.Web.Services;

/// <summary>Centraliza as regras de quem pode moderar o quê. Admin é sempre global (sem linha em
/// EscopoModeracao); Revisor é sempre escopado, por princípio inteiro ou por item específico.</summary>
public interface IPermissaoService
{
    Task<bool> PodeModerarItemAsync(string usuarioId, Guid itemId);
    Task<bool> PodeModerarMandamentoAsync(string usuarioId, int mandamentoId);

    /// <summary>Admin, ou tem pelo menos um escopo de moderação — usado pra liberar o acesso à tela de Moderação.</summary>
    Task<bool> EhModeradorDeAlgoAsync(string usuarioId, bool ehAdmin);

    /// <summary>Ids dos itens que o usuário pode moderar (escopo direto de item + todo item dos princípios
    /// em que ele tem escopo de mandamento). Não chamar para Admin — Admin não tem filtro, vê tudo.</summary>
    Task<List<Guid>> ListarItensModeraveisAsync(string usuarioId);

    Task<List<int>> ListarMandamentosModeraveisAsync(string usuarioId);

    /// <summary>Todos os usuários com escopo (item ou mandamento) que cobre este item — para notificar
    /// só quem realmente modera aquilo, em vez de todo Admin/Revisor do sistema.</summary>
    Task<List<string>> ListarUsuarioIdsComEscopoSobreItemAsync(Guid itemId, int mandamentoId);

    /// <summary>Todos os usuários com escopo sobre um princípio inteiro — direto (Mandamento) ou por
    /// terem escopo de algum item que pertence a ele. Usado quando o evento não tem um item específico.</summary>
    Task<List<string>> ListarUsuarioIdsComEscopoSobreMandamentoAsync(int mandamentoId);

    Task<List<EscopoModeracao>> ListarEscoposDoItemAsync(Guid itemId);
    Task<List<EscopoModeracao>> ListarEscoposDoUsuarioAsync(string usuarioId);
    Task<List<EscopoModeracao>> ListarTodosEscoposAsync();

    Task<(bool Sucesso, string? Erro)> ConcederEscopoItemAsync(string usuarioId, Guid itemId, string concedidoPorId);
    Task<(bool Sucesso, string? Erro)> ConcederEscopoMandamentoAsync(string usuarioId, int mandamentoId, string concedidoPorId);

    /// <summary>Quem já modera um item convida outro usuário — fica Pendente até o convidado aceitar;
    /// não concede nenhum direito de moderação enquanto isso.</summary>
    Task<(bool Sucesso, string? Erro)> ConvidarModeradorItemAsync(string usuarioId, Guid itemId, string convidadoPorId);

    Task<(bool Sucesso, string? Erro)> AceitarConviteModeracaoAsync(Guid escopoId, string usuarioId);
    Task<(bool Sucesso, string? Erro)> RecusarConviteModeracaoAsync(Guid escopoId, string usuarioId);

    /// <summary>Convite Pendente do usuário para este item, se houver — usado pra mostrar o banner de aceitar/recusar.</summary>
    Task<EscopoModeracao?> ObterConvitePendenteItemAsync(string usuarioId, Guid itemId);

    /// <summary>Recusa remover o escopo de item do próprio criador do item — essa moderação nunca pode ser tirada dele.</summary>
    Task<(bool Sucesso, string? Erro)> RevogarEscopoAsync(Guid escopoId);

    /// <summary>Chamado quando um item proposto é aprovado — o criador vira moderador daquele item.</summary>
    Task PromoverCriadorAposAprovacaoAsync(Guid itemId, string criadorUsuarioId, string aprovadoPorUsuarioId);
}
