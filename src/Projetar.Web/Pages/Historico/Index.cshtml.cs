using Projetar.Web.Data;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Pages.Historico;

[Authorize]
public class IndexModel(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ISubmissaoModeracaoService submissoes,
    INotificacaoService notificacoes) : PageModel
{
    public static readonly (string Key, string Label)[] Filtros =
    [
        ("all", "Todas"), ("pend", "Em análise"), ("ok", "Aprovadas"), ("rej", "Rejeitadas"),
    ];

    /// <summary>Uma linha da lista: a submissão + as mensagens + se a última mensagem foi do
    /// moderador (ou seja, "nova" pro autor, já que ele ainda não respondeu de volta).</summary>
    public record HistoricoLinha(SubmissaoResumo Submissao, List<MensagemModeracao> Mensagens, bool HasUnread);

    public List<HistoricoLinha> Lista { get; set; } = [];
    public string Filtro { get; set; } = "rej";
    public string? Selecionado { get; set; }
    public string MeuUsuarioId { get; set; } = string.Empty;
    public Dictionary<string, int> ContagemPorFiltro { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string? filtro, string? sel)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Challenge();
        }

        MeuUsuarioId = usuario.Id;
        Filtro = Filtros.Any(f => f.Key == filtro) ? filtro! : "rej";
        Selecionado = sel;

        var todas = await submissoes.ListarPorAutorAsync(usuario.Id);

        var alvoIds = todas.Select(s => s.AlvoId).ToList();
        var mensagens = alvoIds.Count == 0
            ? []
            : await db.MensagensModeracao
                .Where(m => alvoIds.Contains(m.AlvoId))
                .Include(m => m.AutorUsuario)
                .OrderBy(m => m.DataCriacao)
                .AsNoTracking()
                .ToListAsync();
        var porAlvo = mensagens.GroupBy(m => m.AlvoId).ToDictionary(g => g.Key, g => g.ToList());

        var linhas = todas.Select(s =>
        {
            var msgs = porAlvo.TryGetValue(s.AlvoId, out var l) ? l : [];
            var naoLida = msgs.Count > 0 && msgs[^1].AutorUsuarioId != usuario.Id;
            return new HistoricoLinha(s, msgs, naoLida);
        }).ToList();

        ContagemPorFiltro = new Dictionary<string, int>
        {
            ["all"] = linhas.Count,
            ["pend"] = linhas.Count(l => l.Submissao.Situacao == "pendente"),
            ["ok"] = linhas.Count(l => l.Submissao.Situacao == "aprovado"),
            ["rej"] = linhas.Count(l => l.Submissao.Situacao == "rejeitado"),
        };

        Lista = Filtro switch
        {
            "pend" => linhas.Where(l => l.Submissao.Situacao == "pendente").ToList(),
            "ok" => linhas.Where(l => l.Submissao.Situacao == "aprovado").ToList(),
            "rej" => linhas.Where(l => l.Submissao.Situacao == "rejeitado").ToList(),
            _ => linhas,
        };

        return Page();
    }

    /// <summary>O autor responde sobre a própria submissão — vira uma mensagem pro moderador que decidiu
    /// (ou, se ainda está pendente, pra todos os moderadores).</summary>
    public async Task<IActionResult> OnPostResponderAsync(TipoSubmissao tipo, Guid alvoId, string texto, string? filtro)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(texto))
        {
            TempData["MensagemErro"] = "Escreva uma mensagem antes de enviar.";
            return RedirectToPage(new { filtro });
        }

        var submissao = await submissoes.ObterAsync(tipo, alvoId);
        if (submissao is null || submissao.AutorUsuarioId != usuario.Id)
        {
            return Forbid();
        }

        db.MensagensModeracao.Add(new MensagemModeracao
        {
            TipoAlvo = tipo,
            AlvoId = alvoId,
            ItemId = submissao.ItemId,
            AutorUsuarioId = usuario.Id,
            Texto = texto.Trim(),
        });
        await db.SaveChangesAsync();

        var mensagem = $"{usuario.Nome} respondeu sobre \"{submissao.ItemTitulo}\" ({submissao.TipoRotulo}).";
        if (!string.IsNullOrWhiteSpace(submissao.RevisorUsuarioId))
        {
            await notificacoes.NotificarUsuarioAsync(
                submissao.RevisorUsuarioId, TipoNotificacao.MensagemModeracao,
                "Nova mensagem sobre uma submissão", mensagem,
                submissao.ItemId, "/Moderacao/Index", usuario.Id);
        }
        else
        {
            await notificacoes.NotificarModeradoresAsync(
                TipoNotificacao.MensagemModeracao,
                "Nova mensagem sobre uma submissão", mensagem,
                submissao.ItemId, null, "/Moderacao/Index", usuario.Id);
        }

        TempData["MensagemSucesso"] = "Mensagem enviada.";
        return RedirectToPage(new { filtro, sel = $"{tipo}-{alvoId}" });
    }
}
