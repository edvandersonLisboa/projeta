using Projetar.Web.Data;
using Projetar.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Pages;

/// <summary>Busca simples por título/corpo do item ou pelo texto do princípio — sem índice, é um LIKE no banco.</summary>
public class BuscaModel(ApplicationDbContext db) : PageModel
{
    public string? Q { get; set; }
    public List<Item> Resultados { get; set; } = [];
    public bool EhAdmin { get; set; }

    public async Task OnGetAsync(string? q)
    {
        Q = q?.Trim();
        EhAdmin = User.IsInRole("Admin");

        if (string.IsNullOrWhiteSpace(Q))
        {
            return;
        }

        var termo = $"%{Q}%";

        var query = db.Itens
            .Include(i => i.Mandamento)
            .Include(i => i.CriadoPorUsuario)
            .Where(i => i.Status == ItemStatus.Original || i.Status == ItemStatus.Aprovado)
            .Where(i => EF.Functions.ILike(i.Titulo, termo)
                     || EF.Functions.ILike(i.Corpo, termo)
                     || EF.Functions.ILike(i.Mandamento!.Secular, termo)
                     || (i.Tags != null && EF.Functions.ILike(i.Tags, termo)));

        if (!EhAdmin)
        {
            query = query.Where(i => i.Visivel && i.Mandamento!.Visivel);
        }

        Resultados = await query
            .OrderByDescending(i => i.DataCriacao)
            .AsNoTracking()
            .ToListAsync();
    }
}
