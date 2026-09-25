using Projetar.Web.Data;
using Projetar.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Services;

public class NotificacaoService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IPermissaoService permissoes) : INotificacaoService
{
    /// <summary>Admin sempre recebe. Revisor só recebe quando tem escopo sobre o item (ou, na ausência de
    /// item, sobre o princípio em mandamentoId). Só cai no broadcast pra todo Revisor quando nenhum dos
    /// dois é informado — não deveria mais acontecer, já que todo chamador tem pelo menos um dos dois.</summary>
    public async Task NotificarModeradoresAsync(TipoNotificacao tipo, string titulo, string mensagem, Guid? itemId, int? mandamentoId, string? linkUrl, string? usuarioOrigemId)
    {
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        var destinatariosIds = admins.Select(u => u.Id).ToList();

        if (itemId is Guid id)
        {
            var item = await db.Itens.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            if (item is not null)
            {
                destinatariosIds.AddRange(await permissoes.ListarUsuarioIdsComEscopoSobreItemAsync(id, item.MandamentoId));
            }
        }
        else if (mandamentoId is int mId)
        {
            destinatariosIds.AddRange(await permissoes.ListarUsuarioIdsComEscopoSobreMandamentoAsync(mId));
        }
        else
        {
            var revisores = await userManager.GetUsersInRoleAsync("Revisor");
            destinatariosIds.AddRange(revisores.Select(u => u.Id));
        }

        var destinatarios = destinatariosIds
            .Distinct()
            .Where(uid => uid != usuarioOrigemId);

        var agora = DateTimeOffset.UtcNow;
        foreach (var destinoId in destinatarios)
        {
            db.Notificacoes.Add(new Notificacao
            {
                UsuarioDestinoId = destinoId,
                UsuarioOrigemId = usuarioOrigemId,
                Tipo = tipo,
                Titulo = titulo,
                Mensagem = mensagem,
                ItemId = itemId,
                LinkUrl = linkUrl,
                DataCriacao = agora,
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task NotificarUsuarioAsync(string usuarioDestinoId, TipoNotificacao tipo, string titulo, string mensagem, Guid? itemId, string? linkUrl, string? usuarioOrigemId)
    {
        if (usuarioDestinoId == usuarioOrigemId)
        {
            return;
        }

        db.Notificacoes.Add(new Notificacao
        {
            UsuarioDestinoId = usuarioDestinoId,
            UsuarioOrigemId = usuarioOrigemId,
            Tipo = tipo,
            Titulo = titulo,
            Mensagem = mensagem,
            ItemId = itemId,
            LinkUrl = linkUrl,
        });

        await db.SaveChangesAsync();
    }
}
