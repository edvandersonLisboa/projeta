using System.ComponentModel.DataAnnotations;
using Mandamentos.Web.Data;
using Mandamentos.Web.Models;
using Mandamentos.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Mandamentos.Web.Pages.Itens;

[Authorize]
public class EditarModel(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IMarkdownRenderer markdown) : PageModel
{
    public Item ItemAtual { get; set; } = null!;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "O texto não pode ficar vazio.")]
        public string CorpoNovo { get; set; } = string.Empty;

        [Display(Name = "O que mudou e por quê (opcional)")]
        public string? ComentarioDaMudanca { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var item = await db.Itens.Include(i => i.Mandamento).FirstOrDefaultAsync(i => i.Slug == slug);
        if (item is null)
        {
            return NotFound();
        }

        ItemAtual = item;
        // Texto antigo em Markdown vira HTML pra abrir no editor rich text; a partir daqui a edição já sai em HTML.
        Input.CorpoNovo = item.FormatoCorpo == FormatoTexto.Html
            ? item.Corpo
            : markdown.ToSafeHtml(item.Corpo, item.FormatoCorpo);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var item = await db.Itens.Include(i => i.Mandamento).FirstOrDefaultAsync(i => i.Slug == slug);
        if (item is null)
        {
            return NotFound();
        }

        ItemAtual = item;

        if (RichTextUtils.EhVazio(Input.CorpoNovo))
        {
            ModelState.AddModelError("Input.CorpoNovo", "O texto não pode ficar vazio.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        db.ItemRevisoes.Add(new ItemRevisao
        {
            ItemId = item.Id,
            AutorUsuarioId = usuario.Id,
            CorpoAnterior = item.Corpo,
            FormatoCorpoAnterior = item.FormatoCorpo,
            CorpoNovo = Input.CorpoNovo,
            ComentarioDaMudanca = string.IsNullOrWhiteSpace(Input.ComentarioDaMudanca) ? null : Input.ComentarioDaMudanca,
            Status = RevisaoStatus.Pendente,
        });
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Sua edição foi enviada e está aguardando revisão.";
        return RedirectToPage("./Index", new { slug });
    }
}
