using Projetar.Web.Data;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Pages;

/// <summary>Painel de movimentações — o que cada um vê depende de quem é:
/// Admin vê tudo na plataforma; quem criou pelo menos um item vê tudo que aconteceu
/// nos itens que criou (de qualquer autor); todo o resto vê só as próprias contribuições.</summary>
[Authorize]
public class DashboardModel(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ISubmissaoModeracaoService submissoes) : PageModel
{
    public record Contagem(string Rotulo, int Valor, string Cor);

    public bool EhAdmin { get; set; }
    public bool EhCriador { get; set; }
    public string Escopo { get; set; } = "minhas";
    public string NomeUsuario { get; set; } = string.Empty;

    public int Total { get; set; }
    public int TotalAprovadas { get; set; }
    public int TotalPendentes { get; set; }
    public int TotalRejeitadas { get; set; }
    public double TaxaAprovacao => Total == 0 ? 0 : (double)TotalAprovadas / Total;

    public List<Contagem> PorTipo { get; set; } = [];
    public List<Contagem> PorSituacao { get; set; } = [];
    public List<Contagem> PorItem { get; set; } = [];

    /// <summary>Últimos 14 dias, quantidade de movimentações por dia — usado no gráfico de linha.</summary>
    public List<(string Rotulo, int Valor)> LinhaDoTempo { get; set; } = [];

    public List<SubmissaoResumo> UltimasMovimentacoes { get; set; } = [];

    // Só preenchido quando EhAdmin.
    public int TotalUsuarios { get; set; }
    public int TotalPrincipios { get; set; }
    public int TotalItens { get; set; }
    public List<Contagem> UsuariosPorPerfil { get; set; } = [];
    public List<Contagem> UsuariosPorEstado { get; set; } = [];

    private static readonly string[] Cores = ["#17305A", "#127A36", "#8A6410", "#5B3F86", "#B4322A", "#1E6FA8", "#EE8F14"];

    public async Task<IActionResult> OnGetAsync()
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return RedirectToPage("/Conta/Entrar");
        }

        NomeUsuario = usuario.Nome;
        EhAdmin = User.IsInRole("Admin");

        var itensCriados = await db.Itens
            .Where(i => i.CriadoPorUsuarioId == usuario.Id)
            .Select(i => i.Id)
            .ToListAsync();
        EhCriador = itensCriados.Count > 0;

        List<SubmissaoResumo> movimentacoes;
        if (EhAdmin)
        {
            Escopo = "admin";
            movimentacoes = await submissoes.ListarTodasAsync();
            await CarregarNumerosGlobaisAsync();
        }
        else if (EhCriador)
        {
            Escopo = "criador";
            movimentacoes = await submissoes.ListarPorItensAsync(itensCriados);
        }
        else
        {
            Escopo = "minhas";
            movimentacoes = await submissoes.ListarPorAutorAsync(usuario.Id);
        }

        MontarNumeros(movimentacoes);
        return Page();
    }

    private async Task CarregarNumerosGlobaisAsync()
    {
        TotalUsuarios = await db.Users.CountAsync();
        TotalPrincipios = await db.Principios.CountAsync();
        TotalItens = await db.Itens.CountAsync();

        var admins = await userManager.GetUsersInRoleAsync("Admin");
        var revisores = await userManager.GetUsersInRoleAsync("Revisor");
        var adminIds = admins.Select(a => a.Id).ToHashSet();
        var revisorIds = revisores.Select(r => r.Id).ToHashSet();
        var comuns = TotalUsuarios - adminIds.Count - revisorIds.Except(adminIds).Count();

        UsuariosPorPerfil =
        [
            new Contagem("Admin", adminIds.Count, Cores[0]),
            new Contagem("Revisor", revisorIds.Except(adminIds).Count(), Cores[1]),
            new Contagem("Participante", Math.Max(0, comuns), Cores[2]),
        ];

        var brutos = await db.Users
            .Where(u => u.Estado != null && u.Estado != "")
            .GroupBy(u => u.Estado)
            .Select(g => new { Estado = g.Key!, Total = g.Count() })
            .OrderByDescending(g => g.Total)
            .Take(8)
            .ToListAsync();
        UsuariosPorEstado = brutos
            .Select((g, i) => new Contagem(g.Estado, g.Total, Cores[i % Cores.Length]))
            .ToList();
    }

    private void MontarNumeros(List<SubmissaoResumo> movimentacoes)
    {
        Total = movimentacoes.Count;
        TotalAprovadas = movimentacoes.Count(m => m.Situacao == "aprovado");
        TotalPendentes = movimentacoes.Count(m => m.Situacao == "pendente");
        TotalRejeitadas = movimentacoes.Count(m => m.Situacao == "rejeitado");

        PorTipo = movimentacoes
            .GroupBy(m => m.TipoRotulo)
            .Select((g, i) => new Contagem(g.Key, g.Count(), Cores[i % Cores.Length]))
            .OrderByDescending(c => c.Valor)
            .ToList();

        PorSituacao =
        [
            new Contagem("Aprovadas", TotalAprovadas, "#1E9E48"),
            new Contagem("Pendentes", TotalPendentes, "#F5A81B"),
            new Contagem("Rejeitadas", TotalRejeitadas, "#B4322A"),
        ];

        PorItem = movimentacoes
            .GroupBy(m => m.ItemTitulo)
            .Select(g => new Contagem(g.Key, g.Count(), "#17305A"))
            .OrderByDescending(c => c.Valor)
            .Take(8)
            .ToList();

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var porDia = movimentacoes
            .GroupBy(m => DateOnly.FromDateTime(m.DataCriacao.UtcDateTime))
            .ToDictionary(g => g.Key, g => g.Count());
        LinhaDoTempo = Enumerable.Range(0, 14)
            .Select(i => hoje.AddDays(-13 + i))
            .Select(dia => (dia.ToString("dd/MM"), porDia.GetValueOrDefault(dia)))
            .ToList();

        UltimasMovimentacoes = movimentacoes.Take(10).ToList();
    }
}
