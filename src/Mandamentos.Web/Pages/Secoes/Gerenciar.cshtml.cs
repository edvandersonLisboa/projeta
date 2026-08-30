using Mandamentos.Web.Data;
using Mandamentos.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Mandamentos.Web.Pages.Secoes;

[Authorize(Roles = "Admin")]
public class GerenciarModel(ApplicationDbContext db) : PageModel
{
    public List<Mandamento> MandamentosList { get; set; } = [];

    public async Task OnGetAsync()
    {
        MandamentosList = await db.Mandamentos
            .Include(m => m.Itens
                .Where(i => i.Status == ItemStatus.Original || i.Status == ItemStatus.Aprovado)
                .OrderBy(i => i.Ordem))
            .OrderBy(m => m.Ordem)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAlternarVisibilidadeAsync(int id)
    {
        var mandamento = await db.Mandamentos.FirstOrDefaultAsync(m => m.Id == id);
        if (mandamento is null)
        {
            return NotFound();
        }

        mandamento.Visivel = !mandamento.Visivel;
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = mandamento.Visivel
            ? $"Mandamento {mandamento.Id} agora está visível ao público."
            : $"Mandamento {mandamento.Id} agora está oculto do público.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAlternarVisibilidadeItemAsync(Guid id)
    {
        var item = await db.Itens.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.Visivel = !item.Visivel;
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = item.Visivel
            ? $"Item \"{item.Titulo}\" agora está visível ao público."
            : $"Item \"{item.Titulo}\" agora está oculto do público.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditarAsync(int id, string biblical, string secular, string intro)
    {
        var mandamento = await db.Mandamentos.FirstOrDefaultAsync(m => m.Id == id);
        if (mandamento is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(biblical) || string.IsNullOrWhiteSpace(secular) || string.IsNullOrWhiteSpace(intro))
        {
            TempData["MensagemErro"] = "Preencha todos os campos antes de salvar.";
            return RedirectToPage();
        }

        mandamento.Biblical = biblical.Trim();
        mandamento.Secular = secular.Trim();
        mandamento.Intro = intro.Trim();
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = $"Textos do mandamento {mandamento.Id} atualizados.";
        return RedirectToPage();
    }
}
