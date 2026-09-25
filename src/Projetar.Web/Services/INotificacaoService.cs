using Projetar.Web.Models;

namespace Projetar.Web.Services;

/// <summary>Centraliza a criação de notificações: pra moderadores (Admin/Revisor) quando algo novo entra
/// na fila de aprovação, e pro autor de uma proposta quando ela é aprovada ou rejeitada.</summary>
public interface INotificacaoService
{
    /// <summary>Notifica quem modera o assunto: Admin sempre recebe; Revisor só recebe se tiver escopo sobre
    /// o item (ou, na ausência de item, sobre o princípio informado em principioId). Sem item nem princípio,
    /// cai no comportamento antigo de notificar todo Revisor — evite deixar os dois nulos quando der.</summary>
    Task NotificarModeradoresAsync(TipoNotificacao tipo, string titulo, string mensagem, Guid? itemId, int? principioId, string? linkUrl, string? usuarioOrigemId);

    /// <summary>Notifica um usuário específico — não faz nada se ele for o próprio autor da ação.</summary>
    Task NotificarUsuarioAsync(string usuarioDestinoId, TipoNotificacao tipo, string titulo, string mensagem, Guid? itemId, string? linkUrl, string? usuarioOrigemId);
}
