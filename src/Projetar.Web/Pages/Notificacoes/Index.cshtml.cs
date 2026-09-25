using Projetar.Web.Data;
using Projetar.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Pages.Notificacoes;

[Authorize]
public class IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
{
    public List<Notificacao> Lista { get; set; } = [];
    public string Filtro { get; set; } = "ativas";
    public int TotalNaoLidas { get; set; }
    public int TotalGeral { get; set; }

    public async Task<IActionResult> OnGetAsync(string? filtro)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Challenge();
        }

        Filtro = filtro is "lidas" or "todas" ? filtro : "ativas";

        var query = db.Notificacoes
            .Where(n => n.UsuarioDestinoId == usuario.Id)
            .Include(n => n.UsuarioOrigem)
            .Include(n => n.Item)
            .AsQueryable();

        query = Filtro switch
        {
            "lidas" => query.Where(n => n.Lida),
            "todas" => query,
            _ => query.Where(n => !n.Lida),
        };

        Lista = await query.OrderByDescending(n => n.DataCriacao).AsNoTracking().ToListAsync();

        TotalNaoLidas = await db.Notificacoes.CountAsync(n => n.UsuarioDestinoId == usuario.Id && !n.Lida);
        TotalGeral = await db.Notificacoes.CountAsync(n => n.UsuarioDestinoId == usuario.Id);

        return Page();
    }

    /// <summary>Aberta a partir do sino — marca como vista e leva direto ao assunto da notificação.</summary>
    public async Task<IActionResult> OnGetAbrirAsync(Guid id)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Challenge();
        }

        var notificacao = await db.Notificacoes.FirstOrDefaultAsync(n => n.Id == id && n.UsuarioDestinoId == usuario.Id);
        if (notificacao is not null && !notificacao.Lida)
        {
            notificacao.Lida = true;
            notificacao.DataLeitura = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        return LocalRedirect(!string.IsNullOrWhiteSpace(notificacao?.LinkUrl) ? notificacao.LinkUrl : "/Notificacoes");
    }

    public async Task<IActionResult> OnPostMarcarLidaAsync(Guid id, string? filtro)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Challenge();
        }

        var notificacao = await db.Notificacoes.FirstOrDefaultAsync(n => n.Id == id && n.UsuarioDestinoId == usuario.Id);
        if (notificacao is not null && !notificacao.Lida)
        {
            notificacao.Lida = true;
            notificacao.DataLeitura = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        return RedirectToPage(new { filtro });
    }

    /// <summary>"Reativar" — volta a notificação pra lista de não lidas.</summary>
    public async Task<IActionResult> OnPostMarcarNaoLidaAsync(Guid id, string? filtro)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Challenge();
        }

        var notificacao = await db.Notificacoes.FirstOrDefaultAsync(n => n.Id == id && n.UsuarioDestinoId == usuario.Id);
        if (notificacao is not null && notificacao.Lida)
        {
            notificacao.Lida = false;
            notificacao.DataLeitura = null;
            await db.SaveChangesAsync();
        }

        return RedirectToPage(new { filtro });
    }

    public async Task<IActionResult> OnPostMarcarTodasLidasAsync(string? filtro)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Challenge();
        }

        var naoLidas = await db.Notificacoes
            .Where(n => n.UsuarioDestinoId == usuario.Id && !n.Lida)
            .ToListAsync();

        var agora = DateTimeOffset.UtcNow;
        foreach (var notificacao in naoLidas)
        {
            notificacao.Lida = true;
            notificacao.DataLeitura = agora;
        }
        await db.SaveChangesAsync();

        return RedirectToPage(new { filtro });
    }
}
