using Projetar.Web.Data;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Pages.Secoes;

/// <summary>Admin vê e gerencia tudo. Revisor só vê os princípios/itens sob seu escopo de moderação
/// (<see cref="IPermissaoService"/>) e só pode alterar coisas relacionadas a item — criar/mover/editar
/// princípio inteiro continua exclusivo de Admin.</summary>
[Authorize]
public class GerenciarModel(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IPermissaoService permissoes,
    INotificacaoService notificacoes) : PageModel
{
    public const int IntroMaxLength = 500;

    public List<Mandamento> Todos { get; set; } = [];
    public Mandamento? Atual { get; set; }
    public List<Item> ItensDoAtual { get; set; } = [];

    /// <summary>Lista completa (sem filtro de escopo) — só para calcular a numeração pública real,
    /// que não pode mudar conforme o que um revisor específico enxerga.</summary>
    private List<Mandamento> TodosParaNumeracao { get; set; } = [];

    public bool EhAdmin { get; set; }

    /// <summary>Ids de princípios em que o usuário tem escopo de MANDAMENTO inteiro — só esses aceitam
    /// "criar item"/"mover item para" quando quem está logado é Revisor.</summary>
    public List<int> MandamentosModeraveis { get; set; } = [];

    public string Filtro { get; set; } = "all";
    public string? Busca { get; set; }

    public string? NovoSubtitulo { get; set; }
    public string? NovoSecular { get; set; }
    public string? NovoIntro { get; set; }
    public string? ErroNovoPrincipio { get; set; }
    public bool AbrirModalNovoPrincipio { get; set; }

    public int TotalPrincipiosVisiveis => Todos.Count(m => m.Visivel);
    public int TotalItensPublicados => Todos.Where(m => m.Visivel).Sum(m => m.Itens.Count(i => i.Visivel));
    public int TotalItensOcultos => Todos.Sum(m => m.Itens.Count(i => !i.Visivel));

    public bool PodeCriarItemNoAtual => EhAdmin || (Atual is not null && MandamentosModeraveis.Contains(Atual.Id));

    /// <summary>Número que o público vê — só princípio visível ganha número, e é sequencial só entre os visíveis.
    /// Um princípio oculto não "gasta" um número: se o 3 estiver oculto, o próximo visível continua sendo o 3.
    /// Calculado sempre contra a lista completa, não contra o que um Revisor escopado enxerga.</summary>
    public int? NumeroPublico(Mandamento m)
    {
        if (!m.Visivel)
        {
            return null;
        }

        var n = 0;
        foreach (var x in TodosParaNumeracao)
        {
            if (x.Visivel)
            {
                n++;
            }
            if (x.Id == m.Id)
            {
                return n;
            }
        }
        return null;
    }

    public static string ItensMeta(Mandamento m)
    {
        var off = m.Itens.Count(i => !i.Visivel);
        var s = m.Itens.Count == 1 ? "1 item" : $"{m.Itens.Count} itens";
        return off == 0 ? s : $"{s} · {off} oculto{(off > 1 ? "s" : "")}";
    }

    public async Task<IActionResult> OnGetAsync(int? principioId, string? filtro, string? busca)
    {
        var usuarioId = userManager.GetUserId(User);
        if (usuarioId is null || !await permissoes.EhModeradorDeAlgoAsync(usuarioId, User.IsInRole("Admin")))
        {
            return Forbid();
        }

        await CarregarAsync(principioId, filtro, busca);
        return Page();
    }

    private async Task CarregarAsync(int? principioId, string? filtro, string? busca)
    {
        EhAdmin = User.IsInRole("Admin");

        var todosGlobal = await db.Mandamentos
            .Include(m => m.Itens.Where(i => i.Status == ItemStatus.Original || i.Status == ItemStatus.Aprovado).OrderBy(i => i.Ordem))
            .OrderBy(m => m.Ordem)
            .AsNoTracking()
            .ToListAsync();

        TodosParaNumeracao = todosGlobal;
        Todos = todosGlobal;
        MandamentosModeraveis = todosGlobal.Select(m => m.Id).ToList();

        if (!EhAdmin)
        {
            var usuarioId = userManager.GetUserId(User)!;
            var mandamentosModeraveis = await permissoes.ListarMandamentosModeraveisAsync(usuarioId);
            var itensModeraveis = await permissoes.ListarItensModeraveisAsync(usuarioId);

            MandamentosModeraveis = mandamentosModeraveis;

            Todos = todosGlobal
                .Where(m => mandamentosModeraveis.Contains(m.Id) || m.Itens.Any(i => itensModeraveis.Contains(i.Id)))
                .ToList();

            foreach (var m in Todos)
            {
                m.Itens = m.Itens.Where(i => itensModeraveis.Contains(i.Id)).ToList();
            }
        }

        Atual = Todos.FirstOrDefault(m => m.Id == principioId) ?? Todos.FirstOrDefault();

        Filtro = filtro is "on" or "off" ? filtro : "all";
        Busca = busca;

        if (Atual is not null)
        {
            IEnumerable<Item> itens = Atual.Itens;
            if (Filtro == "on")
            {
                itens = itens.Where(i => i.Visivel);
            }
            else if (Filtro == "off")
            {
                itens = itens.Where(i => !i.Visivel);
            }
            if (!string.IsNullOrWhiteSpace(Busca))
            {
                itens = itens.Where(i => i.Titulo.Contains(Busca.Trim(), StringComparison.OrdinalIgnoreCase));
            }
            ItensDoAtual = itens.ToList();
        }
    }

    private async Task<bool> PodeGerenciarItemAsync(Guid itemId)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }
        var usuarioId = userManager.GetUserId(User);
        return usuarioId is not null && await permissoes.PodeModerarItemAsync(usuarioId, itemId);
    }

    private async Task<bool> PodeGerenciarMandamentoAsync(int mandamentoId)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }
        var usuarioId = userManager.GetUserId(User);
        return usuarioId is not null && await permissoes.PodeModerarMandamentoAsync(usuarioId, mandamentoId);
    }

    private async Task<Guid[]> FiltrarIdsPermitidosAsync(Guid[] ids)
    {
        if (User.IsInRole("Admin"))
        {
            return ids;
        }
        var usuarioId = userManager.GetUserId(User);
        if (usuarioId is null)
        {
            return [];
        }
        var moderaveis = await permissoes.ListarItensModeraveisAsync(usuarioId);
        return ids.Where(moderaveis.Contains).ToArray();
    }

    /// <summary>Cria um princípio novo, além dos originais — nasce oculto do público até o admin publicar. Admin apenas.</summary>
    public async Task<IActionResult> OnPostCriarMandamentoAsync(string subtitulo, string secular, string intro, int? principioId, string? filtro, string? busca)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        NovoSubtitulo = subtitulo;
        NovoSecular = secular;
        NovoIntro = intro;

        if (string.IsNullOrWhiteSpace(subtitulo) || string.IsNullOrWhiteSpace(secular) || string.IsNullOrWhiteSpace(intro))
        {
            ErroNovoPrincipio = "Preencha todos os campos antes de criar o princípio.";
            AbrirModalNovoPrincipio = true;
            await CarregarAsync(principioId, filtro, busca);
            return Page();
        }

        if (intro.Trim().Length > IntroMaxLength)
        {
            ErroNovoPrincipio = $"A introdução pode ter no máximo {IntroMaxLength} caracteres.";
            AbrirModalNovoPrincipio = true;
            await CarregarAsync(principioId, filtro, busca);
            return Page();
        }

        var proximoId = await db.Mandamentos.Select(m => (int?)m.Id).MaxAsync() ?? 0;
        var proximaOrdem = await db.Mandamentos.Select(m => (int?)m.Ordem).MaxAsync() ?? 0;

        var mandamento = new Mandamento
        {
            Id = proximoId + 1,
            Subtitulo = subtitulo.Trim(),
            Secular = secular.Trim(),
            Intro = intro.Trim(),
            Ordem = proximaOrdem + 1,
            Visivel = false,
        };
        db.Mandamentos.Add(mandamento);
        await db.SaveChangesAsync();

        var admin = await userManager.GetUserAsync(User);
        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.SecaoCriada,
            "Novo princípio criado",
            $"{admin?.Nome ?? "Um admin"} criou o princípio {mandamento.Id} ({mandamento.Secular}).",
            null, mandamento.Id, "/Secoes/Gerenciar", admin?.Id);

        TempData["MensagemSucesso"] = $"Princípio {mandamento.Id} criado — ele começa oculto do público até você publicar.";
        return RedirectToPage(new { principioId = mandamento.Id });
    }

    /// <summary>Sobe (-1) ou desce (+1) um princípio na ordem, trocando de posição com o vizinho. Admin apenas.</summary>
    public async Task<IActionResult> OnPostMoverPrincipioAsync(int id, int direcao)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var todos = await db.Mandamentos.OrderBy(m => m.Ordem).ToListAsync();
        var idx = todos.FindIndex(m => m.Id == id);
        var destino = idx + direcao;
        if (idx < 0 || destino < 0 || destino >= todos.Count)
        {
            return RedirectToPage(new { principioId = id });
        }

        (todos[idx].Ordem, todos[destino].Ordem) = (todos[destino].Ordem, todos[idx].Ordem);
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Ordem dos princípios atualizada.";
        return RedirectToPage(new { principioId = id });
    }

    /// <summary>Alterna visibilidade do princípio inteiro. Admin apenas.</summary>
    public async Task<IActionResult> OnPostAlternarVisibilidadeAsync(int id)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var mandamento = await db.Mandamentos.FirstOrDefaultAsync(m => m.Id == id);
        if (mandamento is null)
        {
            return NotFound();
        }

        mandamento.Visivel = !mandamento.Visivel;
        await db.SaveChangesAsync();

        var admin = await userManager.GetUserAsync(User);
        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.SecaoVisibilidadeAlterada,
            "Visibilidade de seção alterada",
            $"{admin?.Nome ?? "Um admin"} {(mandamento.Visivel ? "tornou visível" : "ocultou")} o mandamento {mandamento.Id} ({mandamento.Secular}).",
            null, mandamento.Id, "/Secoes/Gerenciar", admin?.Id);

        TempData["MensagemSucesso"] = mandamento.Visivel
            ? $"Princípio {mandamento.Id} agora está visível ao público."
            : $"Princípio {mandamento.Id} agora está oculto do público.";
        return RedirectToPage(new { principioId = id });
    }

    /// <summary>Edita os textos do princípio (resumo, título, introdução). Admin apenas.</summary>
    public async Task<IActionResult> OnPostEditarAsync(int id, string subtitulo, string secular, string intro)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var mandamento = await db.Mandamentos.FirstOrDefaultAsync(m => m.Id == id);
        if (mandamento is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(subtitulo) || string.IsNullOrWhiteSpace(secular) || string.IsNullOrWhiteSpace(intro))
        {
            TempData["MensagemErro"] = "Preencha todos os campos antes de salvar.";
            return RedirectToPage(new { principioId = id });
        }

        if (intro.Trim().Length > IntroMaxLength)
        {
            TempData["MensagemErro"] = $"A introdução pode ter no máximo {IntroMaxLength} caracteres.";
            return RedirectToPage(new { principioId = id });
        }

        mandamento.Subtitulo = subtitulo.Trim();
        mandamento.Secular = secular.Trim();
        mandamento.Intro = intro.Trim();
        await db.SaveChangesAsync();

        var admin = await userManager.GetUserAsync(User);
        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.SecaoEditada,
            "Seção editada",
            $"{admin?.Nome ?? "Um admin"} editou os textos do mandamento {mandamento.Id} ({mandamento.Secular}).",
            null, mandamento.Id, "/Secoes/Gerenciar", admin?.Id);

        TempData["MensagemSucesso"] = $"Textos do princípio {mandamento.Id} atualizados.";
        return RedirectToPage(new { principioId = id });
    }

    public async Task<IActionResult> OnPostAlternarVisibilidadeItemAsync(Guid id, int principioId)
    {
        var item = await db.Itens.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        if (!await PodeGerenciarItemAsync(id))
        {
            return Forbid();
        }

        item.Visivel = !item.Visivel;
        await db.SaveChangesAsync();

        var admin = await userManager.GetUserAsync(User);
        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.ItemVisibilidadeAlterada,
            "Visibilidade de item alterada",
            $"{admin?.Nome ?? "Um admin"} {(item.Visivel ? "tornou visível" : "ocultou")} o item \"{item.Titulo}\".",
            item.Id, null, "/Secoes/Gerenciar", admin?.Id);

        TempData["MensagemSucesso"] = item.Visivel
            ? $"Item \"{item.Titulo}\" agora está visível ao público."
            : $"Item \"{item.Titulo}\" agora está oculto do público.";
        return RedirectToPage(new { principioId });
    }

    /// <summary>Sobe (-1) ou desce (+1) um item na ordem, dentro do mesmo princípio.</summary>
    public async Task<IActionResult> OnPostMoverItemOrdemAsync(Guid id, int direcao, int principioId)
    {
        var item = await db.Itens.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        if (!await PodeGerenciarItemAsync(id))
        {
            return Forbid();
        }

        var irmaos = await db.Itens
            .Where(i => i.MandamentoId == item.MandamentoId && (i.Status == ItemStatus.Original || i.Status == ItemStatus.Aprovado))
            .OrderBy(i => i.Ordem)
            .ToListAsync();
        var idx = irmaos.FindIndex(i => i.Id == id);
        var destino = idx + direcao;
        if (idx < 0 || destino < 0 || destino >= irmaos.Count)
        {
            return RedirectToPage(new { principioId });
        }

        (irmaos[idx].Ordem, irmaos[destino].Ordem) = (irmaos[destino].Ordem, irmaos[idx].Ordem);
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Ordem dos itens atualizada.";
        return RedirectToPage(new { principioId });
    }

    /// <summary>Cria um item novo só com título, oculto por padrão — pra estruturar rápido e preencher o texto depois em "Editar item".
    /// Exige escopo de MANDAMENTO inteiro (não dá pra "criar" dentro de um escopo de item específico que ainda não existe).</summary>
    public async Task<IActionResult> OnPostCriarItemRapidoAsync(int mandamentoId, string titulo)
    {
        if (!await PodeGerenciarMandamentoAsync(mandamentoId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(titulo))
        {
            TempData["MensagemErro"] = "Dê um título para o item.";
            return RedirectToPage(new { principioId = mandamentoId });
        }

        var mandamento = await db.Mandamentos.FindAsync(mandamentoId);
        if (mandamento is null)
        {
            return NotFound();
        }

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

        var proximaOrdem = await db.Itens.Where(i => i.MandamentoId == mandamentoId).Select(i => (int?)i.Ordem).MaxAsync() ?? 0;
        var admin = await userManager.GetUserAsync(User);

        db.Itens.Add(new Item
        {
            MandamentoId = mandamentoId,
            Slug = slug,
            Titulo = titulo.Trim(),
            Corpo = "<p>Texto a definir.</p>",
            Status = ItemStatus.Original,
            Visivel = false,
            CriadoPorUsuarioId = admin?.Id,
            Ordem = proximaOrdem + 1,
        });
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Item criado como oculto — edite o texto quando quiser.";
        return RedirectToPage(new { principioId = mandamentoId });
    }

    /// <summary>Move um único item, disparado pelo menu "⋮" da própria linha — separado do lote pra não
    /// depender de quais checkboxes estão marcados no restante da lista. Exige poder gerenciar o item de
    /// origem e ter escopo de MANDAMENTO inteiro sobre o destino.</summary>
    public async Task<IActionResult> OnPostMoverItemAsync(Guid id, int principioId, int novoMandamentoId)
    {
        var item = await db.Itens.FirstOrDefaultAsync(i => i.Id == id);
        var destino = await db.Mandamentos.FirstOrDefaultAsync(m => m.Id == novoMandamentoId);
        if (item is null || destino is null)
        {
            return NotFound();
        }

        if (!await PodeGerenciarItemAsync(id) || !await PodeGerenciarMandamentoAsync(novoMandamentoId))
        {
            return Forbid();
        }

        var proximaOrdem = await db.Itens.Where(i => i.MandamentoId == novoMandamentoId).Select(i => (int?)i.Ordem).MaxAsync() ?? 0;
        item.MandamentoId = novoMandamentoId;
        item.Ordem = proximaOrdem + 1;
        item.DataAtualizacao = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var admin = await userManager.GetUserAsync(User);
        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.ItemMovidoDePrincipio,
            "Item movido de princípio",
            $"{admin?.Nome ?? "Um admin"} moveu o item \"{item.Titulo}\" para \"{destino.Secular}\".",
            item.Id, null, $"/Itens/{item.Slug}", admin?.Id);

        TempData["MensagemSucesso"] = $"Item \"{item.Titulo}\" movido para o princípio {destino.Id} — {destino.Secular}.";
        return RedirectToPage(new { principioId = novoMandamentoId });
    }

    public async Task<IActionResult> OnPostMostrarSelecionadosAsync(Guid[] ids, int principioId)
    {
        var idsPermitidos = await FiltrarIdsPermitidosAsync(ids);
        var itens = await db.Itens.Where(i => idsPermitidos.Contains(i.Id)).ToListAsync();
        foreach (var item in itens)
        {
            item.Visivel = true;
        }
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = itens.Count == 1 ? "1 item visível ao público." : $"{itens.Count} itens visíveis ao público.";
        return RedirectToPage(new { principioId });
    }

    public async Task<IActionResult> OnPostOcultarSelecionadosAsync(Guid[] ids, int principioId)
    {
        var idsPermitidos = await FiltrarIdsPermitidosAsync(ids);
        var itens = await db.Itens.Where(i => idsPermitidos.Contains(i.Id)).ToListAsync();
        foreach (var item in itens)
        {
            item.Visivel = false;
        }
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = itens.Count == 1 ? "1 item oculto do público." : $"{itens.Count} itens ocultos do público.";
        return RedirectToPage(new { principioId });
    }

    /// <summary>Move um ou mais itens (seleção em lote ou um só via menu da linha) pra outro princípio.
    /// Exige escopo de MANDAMENTO inteiro sobre o destino; cada item movido também passa pelo filtro de
    /// itens que o usuário pode gerenciar.</summary>
    public async Task<IActionResult> OnPostMoverSelecionadosAsync(Guid[] ids, int principioId, int novoMandamentoId)
    {
        var destino = await db.Mandamentos.FirstOrDefaultAsync(m => m.Id == novoMandamentoId);
        if (destino is null || ids.Length == 0)
        {
            TempData["MensagemErro"] = "Escolha um princípio de destino válido.";
            return RedirectToPage(new { principioId });
        }

        if (!await PodeGerenciarMandamentoAsync(novoMandamentoId))
        {
            return Forbid();
        }

        var idsPermitidos = await FiltrarIdsPermitidosAsync(ids);
        var itens = await db.Itens.Include(i => i.Mandamento).Where(i => idsPermitidos.Contains(i.Id)).ToListAsync();
        if (itens.Count == 0)
        {
            TempData["MensagemErro"] = "Nenhum dos itens selecionados pode ser movido por você.";
            return RedirectToPage(new { principioId });
        }

        var proximaOrdem = await db.Itens.Where(i => i.MandamentoId == novoMandamentoId).Select(i => (int?)i.Ordem).MaxAsync() ?? 0;

        foreach (var item in itens)
        {
            proximaOrdem++;
            item.MandamentoId = novoMandamentoId;
            item.Ordem = proximaOrdem;
            item.DataAtualizacao = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync();

        var admin = await userManager.GetUserAsync(User);
        if (itens.Count == 1)
        {
            var item = itens[0];
            await notificacoes.NotificarModeradoresAsync(
                TipoNotificacao.ItemMovidoDePrincipio,
                "Item movido de princípio",
                $"{admin?.Nome ?? "Um admin"} moveu o item \"{item.Titulo}\" para \"{destino.Secular}\".",
                item.Id, null, $"/Itens/{item.Slug}", admin?.Id);
        }

        TempData["MensagemSucesso"] = itens.Count == 1
            ? $"Item movido para o princípio {destino.Id} — {destino.Secular}."
            : $"{itens.Count} itens movidos para o princípio {destino.Id} — {destino.Secular}.";
        return RedirectToPage(new { principioId = novoMandamentoId });
    }
}
