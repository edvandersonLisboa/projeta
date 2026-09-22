using Projetar.Web.Data;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Pages;

public class IndexModel(
    ApplicationDbContext db,
    IContentRenderer content,
    UserManager<ApplicationUser> userManager) : PageModel
{
    public List<Mandamento> MandamentosList { get; set; } = [];
    public bool PodeParticipar { get; set; }
    public bool EhAdmin { get; set; }
    public Dictionary<Guid, AvaliacaoResumo> AvaliacoesPorItem { get; set; } = [];

    public record AvaliacaoResumo(int Total, double Media);

    public string Excerpt(Item item) => content.ToExcerpt(item.Corpo);

    public async Task OnGetAsync()
    {
        EhAdmin = User.IsInRole("Admin");

        var query = db.Mandamentos.AsQueryable();
        if (!EhAdmin)
        {
            query = query.Where(m => m.Visivel);
        }

        MandamentosList = await query
            .Include(m => m.Itens
                .Where(i => (i.Status == ItemStatus.Original || i.Status == ItemStatus.Aprovado) && (EhAdmin || i.Visivel))
                .OrderBy(i => i.Ordem))
            .OrderBy(m => m.Ordem)
            .AsNoTracking()
            .ToListAsync();

        AvaliacoesPorItem = await db.Votos
            .AsNoTracking()
            .GroupBy(v => v.ItemId)
            .Select(g => new { ItemId = g.Key, Total = g.Count(), Media = g.Average(v => v.Estrelas) })
            .ToDictionaryAsync(g => g.ItemId, g => new AvaliacaoResumo(g.Total, g.Media));

        var usuario = await userManager.GetUserAsync(User);
        PodeParticipar = usuario?.PerfilCompleto ?? false;
    }
}
