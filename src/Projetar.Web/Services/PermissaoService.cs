using Projetar.Web.Data;
using Projetar.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Services;

public class PermissaoService(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : IPermissaoService
{
    private const string RevisorRole = "Revisor";

    public async Task<bool> PodeModerarItemAsync(string usuarioId, Guid itemId)
    {
        var item = await db.Itens.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null)
        {
            return false;
        }

        return await db.EscoposModeracao.AnyAsync(e => e.UsuarioId == usuarioId &&
            e.Status == StatusEscopoModeracao.Aceito &&
            ((e.TipoEscopo == TipoEscopoModeracao.Item && e.ItemId == itemId) ||
             (e.TipoEscopo == TipoEscopoModeracao.Mandamento && e.MandamentoId == item.MandamentoId)));
    }

    public async Task<bool> PodeModerarMandamentoAsync(string usuarioId, int mandamentoId) =>
        await db.EscoposModeracao.AnyAsync(e => e.UsuarioId == usuarioId &&
            e.Status == StatusEscopoModeracao.Aceito &&
            e.TipoEscopo == TipoEscopoModeracao.Mandamento && e.MandamentoId == mandamentoId);

    public async Task<bool> EhModeradorDeAlgoAsync(string usuarioId, bool ehAdmin)
    {
        if (ehAdmin)
        {
            return true;
        }

        return await db.EscoposModeracao.AnyAsync(e => e.UsuarioId == usuarioId && e.Status == StatusEscopoModeracao.Aceito);
    }

    public async Task<List<Guid>> ListarItensModeraveisAsync(string usuarioId)
    {
        var escopos = await db.EscoposModeracao
            .Where(e => e.UsuarioId == usuarioId && e.Status == StatusEscopoModeracao.Aceito)
            .AsNoTracking()
            .ToListAsync();

        var itensDiretos = escopos.Where(e => e.TipoEscopo == TipoEscopoModeracao.Item && e.ItemId.HasValue)
            .Select(e => e.ItemId!.Value)
            .ToList();

        var mandamentoIds = escopos.Where(e => e.TipoEscopo == TipoEscopoModeracao.Mandamento && e.MandamentoId.HasValue)
            .Select(e => e.MandamentoId!.Value)
            .ToList();

        if (mandamentoIds.Count > 0)
        {
            var itensDoMandamento = await db.Itens
                .Where(i => mandamentoIds.Contains(i.MandamentoId))
                .Select(i => i.Id)
                .ToListAsync();
            itensDiretos.AddRange(itensDoMandamento);
        }

        return itensDiretos.Distinct().ToList();
    }

    public async Task<List<int>> ListarMandamentosModeraveisAsync(string usuarioId) =>
        await db.EscoposModeracao
            .Where(e => e.UsuarioId == usuarioId && e.Status == StatusEscopoModeracao.Aceito &&
                        e.TipoEscopo == TipoEscopoModeracao.Mandamento && e.MandamentoId.HasValue)
            .Select(e => e.MandamentoId!.Value)
            .Distinct()
            .ToListAsync();

    public async Task<List<string>> ListarUsuarioIdsComEscopoSobreItemAsync(Guid itemId, int mandamentoId) =>
        await db.EscoposModeracao
            .Where(e => (e.TipoEscopo == TipoEscopoModeracao.Item && e.ItemId == itemId) ||
                        (e.TipoEscopo == TipoEscopoModeracao.Mandamento && e.MandamentoId == mandamentoId))
            .Select(e => e.UsuarioId)
            .Distinct()
            .ToListAsync();

    public async Task<List<string>> ListarUsuarioIdsComEscopoSobreMandamentoAsync(int mandamentoId)
    {
        var diretos = await db.EscoposModeracao
            .Where(e => e.TipoEscopo == TipoEscopoModeracao.Mandamento && e.MandamentoId == mandamentoId)
            .Select(e => e.UsuarioId)
            .ToListAsync();

        var itensDoMandamento = await db.Itens
            .Where(i => i.MandamentoId == mandamentoId)
            .Select(i => i.Id)
            .ToListAsync();

        var porItem = await db.EscoposModeracao
            .Where(e => e.TipoEscopo == TipoEscopoModeracao.Item && e.ItemId.HasValue && itensDoMandamento.Contains(e.ItemId.Value))
            .Select(e => e.UsuarioId)
            .ToListAsync();

        return diretos.Concat(porItem).Distinct().ToList();
    }

    public async Task<List<EscopoModeracao>> ListarEscoposDoItemAsync(Guid itemId)
    {
        var item = await db.Itens.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null)
        {
            return [];
        }

        return await db.EscoposModeracao
            .Where(e => (e.TipoEscopo == TipoEscopoModeracao.Item && e.ItemId == itemId) ||
                        (e.TipoEscopo == TipoEscopoModeracao.Mandamento && e.MandamentoId == item.MandamentoId))
            .Include(e => e.Usuario)
            .Include(e => e.Mandamento)
            .OrderBy(e => e.DataCriacao)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<EscopoModeracao>> ListarEscoposDoUsuarioAsync(string usuarioId) =>
        await db.EscoposModeracao
            .Where(e => e.UsuarioId == usuarioId)
            .Include(e => e.Mandamento)
            .Include(e => e.Item)
            .OrderBy(e => e.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<EscopoModeracao>> ListarTodosEscoposAsync() =>
        await db.EscoposModeracao
            .Include(e => e.Usuario)
            .Include(e => e.Mandamento)
            .Include(e => e.Item)
            .Include(e => e.AdicionadoPorUsuario)
            .OrderByDescending(e => e.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

    public async Task<(bool Sucesso, string? Erro)> ConcederEscopoItemAsync(string usuarioId, Guid itemId, string concedidoPorId)
    {
        var jaTem = await db.EscoposModeracao.AnyAsync(e =>
            e.UsuarioId == usuarioId && e.TipoEscopo == TipoEscopoModeracao.Item && e.ItemId == itemId);
        if (jaTem)
        {
            return (false, "Esse usuário já modera este item.");
        }

        db.EscoposModeracao.Add(new EscopoModeracao
        {
            UsuarioId = usuarioId,
            TipoEscopo = TipoEscopoModeracao.Item,
            ItemId = itemId,
            AdicionadoPorUsuarioId = concedidoPorId,
            Status = StatusEscopoModeracao.Aceito,
        });
        await db.SaveChangesAsync();
        await GarantirPapelRevisorAsync(usuarioId);

        return (true, null);
    }

    public async Task<(bool Sucesso, string? Erro)> ConcederEscopoMandamentoAsync(string usuarioId, int mandamentoId, string concedidoPorId)
    {
        var jaTem = await db.EscoposModeracao.AnyAsync(e =>
            e.UsuarioId == usuarioId && e.TipoEscopo == TipoEscopoModeracao.Mandamento && e.MandamentoId == mandamentoId);
        if (jaTem)
        {
            return (false, "Esse usuário já modera este princípio.");
        }

        db.EscoposModeracao.Add(new EscopoModeracao
        {
            UsuarioId = usuarioId,
            TipoEscopo = TipoEscopoModeracao.Mandamento,
            MandamentoId = mandamentoId,
            AdicionadoPorUsuarioId = concedidoPorId,
            Status = StatusEscopoModeracao.Aceito,
        });
        await db.SaveChangesAsync();
        await GarantirPapelRevisorAsync(usuarioId);

        return (true, null);
    }

    public async Task<(bool Sucesso, string? Erro)> ConvidarModeradorItemAsync(string usuarioId, Guid itemId, string convidadoPorId)
    {
        var jaTem = await db.EscoposModeracao.AnyAsync(e =>
            e.UsuarioId == usuarioId && e.TipoEscopo == TipoEscopoModeracao.Item && e.ItemId == itemId);
        if (jaTem)
        {
            return (false, "Esse usuário já modera este item (ou já foi convidado e ainda não respondeu).");
        }

        db.EscoposModeracao.Add(new EscopoModeracao
        {
            UsuarioId = usuarioId,
            TipoEscopo = TipoEscopoModeracao.Item,
            ItemId = itemId,
            AdicionadoPorUsuarioId = convidadoPorId,
            Status = StatusEscopoModeracao.Pendente,
        });
        await db.SaveChangesAsync();
        // Sem GarantirPapelRevisorAsync aqui — convite pendente ainda não dá nenhum direito de moderação.

        return (true, null);
    }

    public async Task<(bool Sucesso, string? Erro)> AceitarConviteModeracaoAsync(Guid escopoId, string usuarioId)
    {
        var escopo = await db.EscoposModeracao.FirstOrDefaultAsync(e => e.Id == escopoId);
        if (escopo is null || escopo.UsuarioId != usuarioId || escopo.Status != StatusEscopoModeracao.Pendente)
        {
            return (false, "Convite não encontrado.");
        }

        escopo.Status = StatusEscopoModeracao.Aceito;
        await db.SaveChangesAsync();
        await GarantirPapelRevisorAsync(usuarioId);

        return (true, null);
    }

    public async Task<(bool Sucesso, string? Erro)> RecusarConviteModeracaoAsync(Guid escopoId, string usuarioId)
    {
        var escopo = await db.EscoposModeracao.FirstOrDefaultAsync(e => e.Id == escopoId);
        if (escopo is null || escopo.UsuarioId != usuarioId || escopo.Status != StatusEscopoModeracao.Pendente)
        {
            return (false, "Convite não encontrado.");
        }

        db.EscoposModeracao.Remove(escopo);
        await db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<EscopoModeracao?> ObterConvitePendenteItemAsync(string usuarioId, Guid itemId) =>
        await db.EscoposModeracao.Include(e => e.AdicionadoPorUsuario).AsNoTracking().FirstOrDefaultAsync(e =>
            e.UsuarioId == usuarioId && e.ItemId == itemId &&
            e.TipoEscopo == TipoEscopoModeracao.Item && e.Status == StatusEscopoModeracao.Pendente);

    public async Task<(bool Sucesso, string? Erro)> RevogarEscopoAsync(Guid escopoId)
    {
        var escopo = await db.EscoposModeracao.FirstOrDefaultAsync(e => e.Id == escopoId);
        if (escopo is null)
        {
            return (false, "Escopo não encontrado.");
        }

        if (escopo.TipoEscopo == TipoEscopoModeracao.Item && escopo.Status == StatusEscopoModeracao.Aceito)
        {
            var item = await db.Itens.AsNoTracking().FirstOrDefaultAsync(i => i.Id == escopo.ItemId);
            if (item is not null && item.CriadoPorUsuarioId == escopo.UsuarioId)
            {
                return (false, "O criador do item não pode ser removido como moderador dele.");
            }
        }

        var usuarioId = escopo.UsuarioId;
        db.EscoposModeracao.Remove(escopo);
        await db.SaveChangesAsync();
        await RemoverPapelRevisorSeSemEscopoAsync(usuarioId);

        return (true, null);
    }

    public async Task PromoverCriadorAposAprovacaoAsync(Guid itemId, string criadorUsuarioId, string aprovadoPorUsuarioId)
    {
        var jaTem = await db.EscoposModeracao.AnyAsync(e =>
            e.UsuarioId == criadorUsuarioId && e.TipoEscopo == TipoEscopoModeracao.Item && e.ItemId == itemId);
        if (jaTem)
        {
            return;
        }

        db.EscoposModeracao.Add(new EscopoModeracao
        {
            UsuarioId = criadorUsuarioId,
            TipoEscopo = TipoEscopoModeracao.Item,
            ItemId = itemId,
            AdicionadoPorUsuarioId = aprovadoPorUsuarioId,
            Status = StatusEscopoModeracao.Aceito,
        });
        await db.SaveChangesAsync();
        await GarantirPapelRevisorAsync(criadorUsuarioId);
    }

    /// <summary>Mantém o Identity Role "Revisor" sincronizado com a existência de pelo menos um escopo —
    /// permite que checagens rápidas como a do header (User.IsInRole) continuem funcionando sem consulta extra.</summary>
    private async Task GarantirPapelRevisorAsync(string usuarioId)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId);
        if (usuario is not null && !await userManager.IsInRoleAsync(usuario, RevisorRole))
        {
            await userManager.AddToRoleAsync(usuario, RevisorRole);
        }
    }

    private async Task RemoverPapelRevisorSeSemEscopoAsync(string usuarioId)
    {
        var aindaTemEscopo = await db.EscoposModeracao.AnyAsync(e => e.UsuarioId == usuarioId);
        if (aindaTemEscopo)
        {
            return;
        }

        var usuario = await userManager.FindByIdAsync(usuarioId);
        if (usuario is not null && await userManager.IsInRoleAsync(usuario, RevisorRole))
        {
            await userManager.RemoveFromRoleAsync(usuario, RevisorRole);
        }
    }
}
