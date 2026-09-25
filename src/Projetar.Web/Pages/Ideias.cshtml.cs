using Projetar.Web.Data;
using Projetar.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Pages;

/// <summary>Lista, em ordem cronológica, todos os itens públicos de todos os princípios — a "vitrine" de ideias do site.</summary>
public class IdeiasModel(ApplicationDbContext db) : PageModel
{
    public List<Item> Itens { get; set; } = [];
    public bool EhAdmin { get; set; }

    public async Task OnGetAsync()
    {
        EhAdmin = User.IsInRole("Admin");

        var query = db.Itens
            .Include(i => i.Principio)
            .Include(i => i.CriadoPorUsuario)
            .Where(i => i.Status == ItemStatus.Original || i.Status == ItemStatus.Aprovado)
            .AsQueryable();

        if (!EhAdmin)
        {
            query = query.Where(i => i.Visivel && i.Principio!.Visivel);
        }

        Itens = await query
            .OrderByDescending(i => i.DataCriacao)
            .AsNoTracking()
            .ToListAsync();
    }
}
