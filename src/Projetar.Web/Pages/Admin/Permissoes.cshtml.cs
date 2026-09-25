using Projetar.Web.Data;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Pages.Admin;

/// <summary>Painel de administração: só Admin entra. Gerencia quem é Admin e quem modera o quê
/// (Revisor escopado por princípio ou por item) — "liberar e excluir permissões" do sistema.</summary>
[Authorize(Roles = "Admin")]
public class PermissoesModel(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IPermissaoService permissoes) : PageModel
{
    public List<ApplicationUser> Usuarios { get; set; } = [];
    public HashSet<string> AdminIds { get; set; } = [];
    public HashSet<string> RevisorIds { get; set; } = [];
    public List<EscopoModeracao> TodosEscopos { get; set; } = [];
    public List<Principio> TodosPrincipios { get; set; } = [];
    public List<Item> TodosItens { get; set; } = [];

    public int TotalUsuarios { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalRevisores { get; set; }

    public async Task OnGetAsync()
    {
        Usuarios = await db.Users.OrderBy(u => u.Nome).AsNoTracking().ToListAsync();
        TotalUsuarios = Usuarios.Count;

        var admins = await userManager.GetUsersInRoleAsync("Admin");
        AdminIds = admins.Select(u => u.Id).ToHashSet();
        TotalAdmins = AdminIds.Count;

        var revisores = await userManager.GetUsersInRoleAsync("Revisor");
        RevisorIds = revisores.Select(u => u.Id).ToHashSet();
        TotalRevisores = RevisorIds.Count;

        TodosEscopos = await permissoes.ListarTodosEscoposAsync();

        TodosPrincipios = await db.Principios.OrderBy(m => m.Ordem).AsNoTracking().ToListAsync();
        TodosItens = await db.Itens
            .Where(i => i.Status == ItemStatus.Original || i.Status == ItemStatus.Aprovado)
            .Include(i => i.Principio)
            .OrderBy(i => i.Principio!.Ordem).ThenBy(i => i.Ordem)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostTornarAdminAsync(string email)
    {
        var alvo = await userManager.FindByEmailAsync(email?.Trim() ?? string.Empty);
        if (alvo is null)
        {
            TempData["MensagemErro"] = "Não existe usuário cadastrado com esse e-mail.";
            return RedirectToPage();
        }

        if (!await userManager.IsInRoleAsync(alvo, "Admin"))
        {
            await userManager.AddToRoleAsync(alvo, "Admin");
        }

        TempData["MensagemSucesso"] = $"{alvo.Nome} agora é Admin.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevogarAdminAsync(string usuarioId)
    {
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        if (admins.Count <= 1)
        {
            TempData["MensagemErro"] = "Precisa existir pelo menos um Admin — não dá pra remover o último.";
            return RedirectToPage();
        }

        var alvo = await userManager.FindByIdAsync(usuarioId);
        if (alvo is null)
        {
            return RedirectToPage();
        }

        await userManager.RemoveFromRoleAsync(alvo, "Admin");
        TempData["MensagemSucesso"] = $"{alvo.Nome} não é mais Admin.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostConcederEscopoAsync(string email, TipoEscopoModeracao tipoEscopo, int? principioId, Guid? itemId)
    {
        var atual = await userManager.GetUserAsync(User);
        var alvo = await userManager.FindByEmailAsync(email?.Trim() ?? string.Empty);
        if (atual is null || alvo is null)
        {
            TempData["MensagemErro"] = "Não existe usuário cadastrado com esse e-mail.";
            return RedirectToPage();
        }

        (bool Sucesso, string? Erro) resultado = tipoEscopo switch
        {
            TipoEscopoModeracao.Principio when principioId is int mId =>
                await permissoes.ConcederEscopoPrincipioAsync(alvo.Id, mId, atual.Id),
            TipoEscopoModeracao.Item when itemId is Guid iId =>
                await permissoes.ConcederEscopoItemAsync(alvo.Id, iId, atual.Id),
            _ => (false, "Escolha o princípio ou o item."),
        };

        TempData[resultado.Sucesso ? "MensagemSucesso" : "MensagemErro"] =
            resultado.Sucesso ? $"{alvo.Nome} agora modera." : resultado.Erro;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevogarEscopoAsync(Guid escopoId)
    {
        var (sucesso, erro) = await permissoes.RevogarEscopoAsync(escopoId);
        TempData[sucesso ? "MensagemSucesso" : "MensagemErro"] = sucesso ? "Escopo de moderação revogado." : erro;
        return RedirectToPage();
    }
}
