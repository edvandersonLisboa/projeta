using System.ComponentModel.DataAnnotations;
using Mandamentos.Web.Data;
using Mandamentos.Web.Models;
using Mandamentos.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Mandamentos.Web.Pages.PropostaItem;

[Authorize]
public class ProporModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
{
    public Mandamento MandamentoAtual { get; set; } = null!;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Dê um título para o item.")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escreva o texto da lei proposta.")]
        public string Corpo { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int mandamentoId)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var mandamento = await db.Mandamentos.FindAsync(mandamentoId);
        if (mandamento is null)
        {
            return NotFound();
        }

        MandamentoAtual = mandamento;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int mandamentoId)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var mandamento = await db.Mandamentos.FindAsync(mandamentoId);
        if (mandamento is null)
        {
            return NotFound();
        }

        MandamentoAtual = mandamento;

        if (RichTextUtils.EhVazio(Input.Corpo))
        {
            ModelState.AddModelError("Input.Corpo", "Escreva o texto da lei proposta.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var slug = await GerarSlugUnicoAsync(Input.Titulo);
        var proximaOrdem = await db.Itens
            .Where(i => i.MandamentoId == mandamentoId)
            .Select(i => (int?)i.Ordem)
            .MaxAsync() ?? 0;

        var item = new Item
        {
            MandamentoId = mandamentoId,
            Slug = slug,
            Titulo = Input.Titulo.Trim(),
            Corpo = Input.Corpo,
            FormatoCorpo = FormatoTexto.Html,
            Status = ItemStatus.PropostaComunidade,
            CriadoPorUsuarioId = usuario.Id,
            Ordem = proximaOrdem + 1,
        };
        db.Itens.Add(item);
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Item proposto! Ele fica visível só pra você até um revisor aprovar.";
        return RedirectToPage("/Itens/Index", new { slug });
    }

    private async Task<string> GerarSlugUnicoAsync(string titulo)
    {
        var baseSlug = SlugGenerator.Gerar(titulo);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "item";
        }

        var slug = baseSlug;
        var sufixo = 2;
        while (await db.Itens.AnyAsync(i => i.Slug == slug))
        {
            slug = $"{baseSlug}-{sufixo}";
            sufixo++;
        }

        return slug;
    }
}
