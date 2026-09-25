using System.ComponentModel.DataAnnotations;
using Projetar.Web.Data;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Pages.Moderacao;

[Authorize]
public class IndexModel(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IContentRenderer content,
    IRevisaoDiffService diff,
    INotificacaoService notificacoes,
    ISubmissaoModeracaoService submissoes,
    IPermissaoService permissoes,
    IWebHostEnvironment env) : PageModel
{
    public bool EhAdmin { get; set; }
    private static readonly string[] ExtensoesDocumentoPermitidas = [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt"];
    private const long TamanhoMaximoDocumentoBytes = 10 * 1024 * 1024;

    public string Render(string corpo) => content.ToSafeHtml(corpo);
    public string Redline(string corpoAntigo, string corpoNovo) => diff.RenderRedline(corpoAntigo, corpoNovo);
    public List<ItemRevisao> Pendentes { get; set; } = [];
    public List<Referencia> ReferenciasPendentes { get; set; } = [];
    public List<Documento> DocumentosPendentes { get; set; } = [];
    public List<Item> ItensPropostos { get; set; } = [];
    public List<TagSugestao> TagsPendentes { get; set; } = [];
    public List<BannerSugestao> BannersPendentes { get; set; } = [];

    /// <summary>Submissões (de qualquer autor, qualquer status) que já têm pelo menos uma mensagem trocada.</summary>
    public List<SubmissaoResumo> Conversas { get; set; } = [];
    public Dictionary<Guid, List<MensagemModeracao>> MensagensPorAlvo { get; set; } = [];

    /// <summary>Uma linha da caixa de entrada "Conversas": a submissão + as mensagens + se está esperando
    /// resposta do moderador (a última mensagem foi do autor) — calculado, não persistido.</summary>
    public record ConversaLinha(SubmissaoResumo Submissao, List<MensagemModeracao> Mensagens, string AutorNome, bool AguardandoResposta);
    public List<ConversaLinha> ConversasLinhas { get; set; } = [];

    public static readonly (string Key, string Label)[] Tabs =
    [
        ("all", "Todas as pendências"), ("edit", "Edições de texto"), ("item", "Novos itens"),
        ("ref", "Referências"), ("doc", "Documentos"), ("tag", "Tags"), ("banner", "Banners"), ("threads", "Conversas"),
    ];

    public static readonly (string Key, string Label)[] ThreadFiltros =
    [
        ("waiting", "Aguardando"), ("all", "Todas"), ("resolved", "Respondidas"),
    ];

    public static readonly string[] MotivosRapidos =
    [
        "Falta argumentação e fontes", "Duplica um item existente", "Fora do tema do princípio", "Linguagem inadequada",
    ];

    public string Tab { get; set; } = "all";
    public string ThreadFiltro { get; set; } = "waiting";
    public string? ThreadBusca { get; set; }
    public string? ThreadSelecionado { get; set; }

    public int TotalPendentes => Pendentes.Count + ReferenciasPendentes.Count + DocumentosPendentes.Count
        + ItensPropostos.Count + TagsPendentes.Count + BannersPendentes.Count;

    public int AguardandoRespostaCount => ConversasLinhas.Count(c => c.AguardandoResposta);

    public string OldestLabel
    {
        get
        {
            DateTimeOffset? maisAntigo = null;
            void Considera(DateTimeOffset d) { if (maisAntigo is null || d < maisAntigo) maisAntigo = d; }
            foreach (var r in Pendentes) Considera(r.DataRevisao);
            foreach (var r in ReferenciasPendentes) Considera(r.DataCriacao);
            foreach (var d in DocumentosPendentes) Considera(d.DataUpload);
            foreach (var i in ItensPropostos) Considera(i.DataCriacao);
            foreach (var t in TagsPendentes) Considera(t.DataCriacao);
            foreach (var b in BannersPendentes) Considera(b.DataCriacao);
            return maisAntigo is null ? "—" : TempoRelativo.Formatar(maisAntigo.Value).Replace("há ", "");
        }
    }

    public int CountFor(string key) => key switch
    {
        "all" => TotalPendentes,
        "edit" => Pendentes.Count,
        "item" => ItensPropostos.Count,
        "ref" => ReferenciasPendentes.Count,
        "doc" => DocumentosPendentes.Count,
        "tag" => TagsPendentes.Count,
        "banner" => BannersPendentes.Count,
        "threads" => AguardandoRespostaCount,
        _ => 0,
    };

    /// <summary>"pendente"/"aprovado"/"rejeitado" (SubmissaoResumo.Situacao) → "pend"/"ok"/"rej" (classe .pj-status--*).</summary>
    public static string PjStatusKey(string situacao) => situacao switch
    {
        "aprovado" => "ok",
        "rejeitado" => "rej",
        _ => "pend",
    };

    public static string TabHint(string key) => key switch
    {
        "all" => "Mais antigos primeiro",
        "edit" => "Compare o texto antes de aprovar",
        "item" => "Aprovado, o item aparece na listagem do princípio",
        "ref" => "Confira se o link abre e é fonte confiável",
        "doc" => "Arquivos anexados a itens",
        "tag" => "Tags sugeridas por participantes",
        "banner" => "Imagens propostas para itens",
        _ => "Mensagens trocadas com autores de submissões",
    };

    /// <summary>Todos os itens visíveis publicamente — pra escolher o alvo ao criar referência/documento direto na moderação.</summary>
    public List<Item> TodosItens { get; set; } = [];

    /// <summary>Todos os mandamentos — pra escolher onde entra um item proposto direto na moderação.</summary>
    public List<Mandamento> TodosMandamentos { get; set; } = [];

    [BindProperty]
    public AprovarInput Aprovar { get; set; } = new();

    [BindProperty]
    public RejeitarInput Rejeitar { get; set; } = new();

    [BindProperty]
    public RejeitarInput RejeitarReferencia { get; set; } = new();

    [BindProperty]
    public RejeitarInput RejeitarDocumento { get; set; } = new();

    [BindProperty]
    public RejeitarInput RejeitarItem { get; set; } = new();

    [BindProperty]
    public NovaReferenciaInput NovaReferencia { get; set; } = new();

    [BindProperty]
    public NovoDocumentoInput NovoDocumento { get; set; } = new();

    [BindProperty]
    public NovoItemInput NovoItemProposto { get; set; } = new();

    [BindProperty]
    public RejeitarInput RejeitarTag { get; set; } = new();

    [BindProperty]
    public RejeitarInput RejeitarBanner { get; set; } = new();

    public class RejeitarInput
    {
        public Guid AlvoId { get; set; }

        [Required(ErrorMessage = "Informe o motivo da rejeição.")]
        public string Motivo { get; set; } = string.Empty;
    }

    public class AprovarInput
    {
        public Guid AlvoId { get; set; }

        /// <summary>Vazio = aprovar exatamente como foi proposto. Preenchido = o moderador ajustou o texto.</summary>
        public string? CorpoAjustado { get; set; }

        public string? ComentarioDaModeracao { get; set; }
    }

    public class NovaReferenciaInput
    {
        [Required(ErrorMessage = "Escolha o item.")]
        public Guid ItemId { get; set; }

        [Required(ErrorMessage = "Dê um título para a referência.")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a URL.")]
        [Url(ErrorMessage = "URL inválida.")]
        public string Url { get; set; } = string.Empty;

        public ReferenciaTipo Tipo { get; set; } = ReferenciaTipo.Outro;
    }

    public class NovoDocumentoInput
    {
        [Required(ErrorMessage = "Escolha o item.")]
        public Guid ItemId { get; set; }
    }

    public class NovoItemInput
    {
        [Required(ErrorMessage = "Escolha o princípio.")]
        public int MandamentoId { get; set; }

        [Required(ErrorMessage = "Dê um título para o item.")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escreva o texto da lei proposta.")]
        public string Corpo { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string? tab, string? thread, string? threadFiltro, string? threadBusca)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Challenge();
        }

        EhAdmin = User.IsInRole("Admin");
        if (!await permissoes.EhModeradorDeAlgoAsync(usuario.Id, EhAdmin))
        {
            return Forbid();
        }

        Tab = Tabs.Any(t => t.Key == tab) ? tab! : "all";
        ThreadFiltro = ThreadFiltros.Any(f => f.Key == threadFiltro) ? threadFiltro! : "waiting";
        ThreadBusca = threadBusca;
        ThreadSelecionado = thread;

        // Admin vê tudo, sem filtro. Revisor só vê o que está no escopo dele (item direto ou princípio inteiro).
        List<Guid>? itensModeraveis = null;
        List<int>? mandamentosModeraveis = null;
        if (!EhAdmin)
        {
            itensModeraveis = await permissoes.ListarItensModeraveisAsync(usuario.Id);
            mandamentosModeraveis = await permissoes.ListarMandamentosModeraveisAsync(usuario.Id);
        }

        var pendentesQuery = db.ItemRevisoes.Where(r => r.Status == RevisaoStatus.Pendente);
        if (!EhAdmin) pendentesQuery = pendentesQuery.Where(r => itensModeraveis!.Contains(r.ItemId));
        Pendentes = await pendentesQuery
            .Include(r => r.Item).ThenInclude(i => i!.Mandamento)
            .Include(r => r.AutorUsuario)
            .Include(r => r.Apoios)
            .OrderBy(r => r.DataRevisao)
            .AsNoTracking()
            .ToListAsync();

        var referenciasQuery = db.Referencias.Where(r => r.Status == RevisaoStatus.Pendente);
        if (!EhAdmin) referenciasQuery = referenciasQuery.Where(r => itensModeraveis!.Contains(r.ItemId));
        ReferenciasPendentes = await referenciasQuery
            .Include(r => r.Item).ThenInclude(i => i!.Mandamento)
            .Include(r => r.Usuario)
            .OrderBy(r => r.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

        var documentosQuery = db.Documentos.Where(d => d.Status == RevisaoStatus.Pendente);
        if (!EhAdmin) documentosQuery = documentosQuery.Where(d => itensModeraveis!.Contains(d.ItemId));
        DocumentosPendentes = await documentosQuery
            .Include(d => d.Item).ThenInclude(i => i!.Mandamento)
            .Include(d => d.Usuario)
            .OrderBy(d => d.DataUpload)
            .AsNoTracking()
            .ToListAsync();

        // Item novo ainda não existe como alvo de escopo próprio — só entra pelo escopo de princípio.
        var itensPropostosQuery = db.Itens.Where(i => i.Status == ItemStatus.PropostaComunidade);
        if (!EhAdmin) itensPropostosQuery = itensPropostosQuery.Where(i => mandamentosModeraveis!.Contains(i.MandamentoId));
        ItensPropostos = await itensPropostosQuery
            .Include(i => i.Mandamento)
            .Include(i => i.CriadoPorUsuario)
            .OrderBy(i => i.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

        var tagsQuery = db.TagSugestoes.Where(t => t.Status == RevisaoStatus.Pendente);
        if (!EhAdmin) tagsQuery = tagsQuery.Where(t => itensModeraveis!.Contains(t.ItemId));
        TagsPendentes = await tagsQuery
            .Include(t => t.Item).ThenInclude(i => i!.Mandamento)
            .Include(t => t.Usuario)
            .OrderBy(t => t.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

        var bannersQuery = db.BannerSugestoes.Where(b => b.Status == RevisaoStatus.Pendente);
        if (!EhAdmin) bannersQuery = bannersQuery.Where(b => itensModeraveis!.Contains(b.ItemId));
        BannersPendentes = await bannersQuery
            .Include(b => b.Item).ThenInclude(i => i!.Mandamento)
            .Include(b => b.Usuario)
            .OrderBy(b => b.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

        var todosItensQuery = db.Itens.Where(i => i.Status == ItemStatus.Original || i.Status == ItemStatus.Aprovado);
        if (!EhAdmin) todosItensQuery = todosItensQuery.Where(i => mandamentosModeraveis!.Contains(i.MandamentoId) || itensModeraveis!.Contains(i.Id));
        TodosItens = await todosItensQuery
            .Include(i => i.Mandamento)
            .OrderBy(i => i.Mandamento!.Ordem).ThenBy(i => i.Ordem)
            .AsNoTracking()
            .ToListAsync();

        TodosMandamentos = EhAdmin
            ? await db.Mandamentos.OrderBy(m => m.Ordem).AsNoTracking().ToListAsync()
            : await db.Mandamentos.Where(m => mandamentosModeraveis!.Contains(m.Id)).OrderBy(m => m.Ordem).AsNoTracking().ToListAsync();

        Conversas = await submissoes.ListarComConversaAsync();
        if (!EhAdmin)
        {
            Conversas = Conversas.Where(c => itensModeraveis!.Contains(c.ItemId)).ToList();
        }
        if (Conversas.Count > 0)
        {
            var alvoIds = Conversas.Select(c => c.AlvoId).ToList();
            var mensagens = await db.MensagensModeracao
                .Where(m => alvoIds.Contains(m.AlvoId))
                .Include(m => m.AutorUsuario)
                .OrderBy(m => m.DataCriacao)
                .AsNoTracking()
                .ToListAsync();
            MensagensPorAlvo = mensagens.GroupBy(m => m.AlvoId).ToDictionary(g => g.Key, g => g.ToList());

            var autorIds = Conversas.Select(c => c.AutorUsuarioId).Distinct().ToList();
            var autores = await db.Users
                .Where(u => autorIds.Contains(u.Id))
                .AsNoTracking()
                .ToDictionaryAsync(u => u.Id, u => u.Nome);

            ConversasLinhas = Conversas.Select(c =>
            {
                var msgs = MensagensPorAlvo.TryGetValue(c.AlvoId, out var l) ? l : [];
                var aguardando = msgs.Count > 0 && msgs[^1].AutorUsuarioId == c.AutorUsuarioId;
                var autorNome = autores.TryGetValue(c.AutorUsuarioId, out var nome) ? nome : "Usuário removido";
                return new ConversaLinha(c, msgs, autorNome, aguardando);
            }).ToList();
        }

        return Page();
    }

    /// <summary>Um moderador responde numa conversa já existente — vira uma mensagem pro autor da submissão.</summary>
    public async Task<IActionResult> OnPostResponderModeracaoAsync(TipoSubmissao tipo, Guid alvoId, string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            TempData["MensagemErro"] = "Escreva uma mensagem antes de enviar.";
            return RedirectToPage();
        }

        var moderador = await userManager.GetUserAsync(User);
        if (moderador is null)
        {
            return Challenge();
        }

        var submissao = await submissoes.ObterAsync(tipo, alvoId);
        if (submissao is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(moderador.Id, submissao.ItemId))
        {
            return Forbid();
        }

        db.MensagensModeracao.Add(new MensagemModeracao
        {
            TipoAlvo = tipo,
            AlvoId = alvoId,
            ItemId = submissao.ItemId,
            AutorUsuarioId = moderador.Id,
            Texto = texto.Trim(),
        });
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(submissao.AutorUsuarioId))
        {
            await notificacoes.NotificarUsuarioAsync(
                submissao.AutorUsuarioId, TipoNotificacao.MensagemModeracao,
                "Nova mensagem sobre sua submissão",
                $"{moderador.Nome} respondeu sobre \"{submissao.ItemTitulo}\" ({submissao.TipoRotulo}).",
                submissao.ItemId, "/Historico/Index", moderador.Id);
        }

        TempData["MensagemSucesso"] = "Mensagem enviada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAprovarAsync()
    {
        var revisao = await db.ItemRevisoes.Include(r => r.Item).FirstOrDefaultAsync(r => r.Id == Aprovar.AlvoId);
        if (revisao is null || revisao.Status != RevisaoStatus.Pendente || revisao.Item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, revisao.ItemId))
        {
            return Forbid();
        }

        var corpoAjustado = Aprovar.CorpoAjustado?.Trim();
        var houveAjuste = !string.IsNullOrWhiteSpace(corpoAjustado) && corpoAjustado != revisao.CorpoNovo.Trim();

        if (houveAjuste && string.IsNullOrWhiteSpace(Aprovar.ComentarioDaModeracao))
        {
            TempData["MensagemErro"] = "Descreva o que foi ajustado antes de aprovar com alterações no texto.";
            return RedirectToPage();
        }

        var moderador = await userManager.GetUserAsync(User);
        var corpoFinal = houveAjuste ? corpoAjustado! : revisao.CorpoNovo;

        if (houveAjuste)
        {
            revisao.CorpoAprovado = corpoFinal;
            revisao.ComentarioDaModeracao = Aprovar.ComentarioDaModeracao!.Trim();
        }

        revisao.Item.Corpo = corpoFinal;
        revisao.Item.DataAtualizacao = DateTimeOffset.UtcNow;
        revisao.Status = RevisaoStatus.Aprovada;
        revisao.RevisadoPorUsuarioId = moderador?.Id;
        revisao.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await notificacoes.NotificarUsuarioAsync(
            revisao.AutorUsuarioId,
            TipoNotificacao.EdicaoTextoAprovada,
            "Sua edição foi aprovada",
            $"Sua edição em \"{revisao.Item.Titulo}\" foi aprovada e publicada.",
            revisao.ItemId, $"/Itens/{revisao.Item.Slug}", moderador?.Id);

        TempData["MensagemSucesso"] = houveAjuste ? "Edição aprovada com ajustes e publicada." : "Edição aprovada e publicada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejeitarAsync()
    {
        if (string.IsNullOrWhiteSpace(Rejeitar.Motivo))
        {
            TempData["MensagemErro"] = "Informe o motivo da rejeição.";
            return RedirectToPage();
        }

        var revisao = await db.ItemRevisoes.Include(r => r.Item).FirstOrDefaultAsync(r => r.Id == Rejeitar.AlvoId);
        if (revisao is null || revisao.Status != RevisaoStatus.Pendente || revisao.Item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, revisao.ItemId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);

        revisao.Status = RevisaoStatus.Rejeitada;
        revisao.MotivoRejeicao = Rejeitar.Motivo.Trim();
        revisao.RevisadoPorUsuarioId = moderador?.Id;
        revisao.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await notificacoes.NotificarUsuarioAsync(
            revisao.AutorUsuarioId,
            TipoNotificacao.EdicaoTextoRejeitada,
            "Sua edição foi rejeitada",
            $"Sua edição em \"{revisao.Item.Titulo}\" foi rejeitada: {revisao.MotivoRejeicao}",
            revisao.ItemId, $"/Itens/{revisao.Item.Slug}", moderador?.Id);

        TempData["MensagemSucesso"] = "Edição rejeitada. O autor verá o motivo.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAprovarReferenciaAsync(Guid id)
    {
        var referencia = await db.Referencias.Include(r => r.Item).FirstOrDefaultAsync(r => r.Id == id);
        if (referencia is null || referencia.Status != RevisaoStatus.Pendente || referencia.Item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, referencia.ItemId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);
        referencia.Status = RevisaoStatus.Aprovada;
        referencia.RevisadoPorUsuarioId = moderador?.Id;
        referencia.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await notificacoes.NotificarUsuarioAsync(
            referencia.UsuarioId,
            TipoNotificacao.ReferenciaAprovada,
            "Sua referência foi aprovada",
            $"Sua referência \"{referencia.Titulo}\" em \"{referencia.Item.Titulo}\" foi aprovada e publicada.",
            referencia.ItemId, $"/Itens/{referencia.Item.Slug}", moderador?.Id);

        TempData["MensagemSucesso"] = "Referência aprovada e publicada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejeitarReferenciaAsync()
    {
        if (string.IsNullOrWhiteSpace(RejeitarReferencia.Motivo))
        {
            TempData["MensagemErro"] = "Informe o motivo da rejeição.";
            return RedirectToPage();
        }

        var referencia = await db.Referencias.Include(r => r.Item).FirstOrDefaultAsync(r => r.Id == RejeitarReferencia.AlvoId);
        if (referencia is null || referencia.Status != RevisaoStatus.Pendente || referencia.Item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, referencia.ItemId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);
        referencia.Status = RevisaoStatus.Rejeitada;
        referencia.MotivoRejeicao = RejeitarReferencia.Motivo.Trim();
        referencia.RevisadoPorUsuarioId = moderador?.Id;
        referencia.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await notificacoes.NotificarUsuarioAsync(
            referencia.UsuarioId,
            TipoNotificacao.ReferenciaRejeitada,
            "Sua referência foi rejeitada",
            $"Sua referência \"{referencia.Titulo}\" em \"{referencia.Item.Titulo}\" foi rejeitada: {referencia.MotivoRejeicao}",
            referencia.ItemId, $"/Itens/{referencia.Item.Slug}", moderador?.Id);

        TempData["MensagemSucesso"] = "Referência rejeitada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAprovarDocumentoAsync(Guid id)
    {
        var documento = await db.Documentos.Include(d => d.Item).FirstOrDefaultAsync(d => d.Id == id);
        if (documento is null || documento.Status != RevisaoStatus.Pendente || documento.Item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, documento.ItemId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);
        documento.Status = RevisaoStatus.Aprovada;
        documento.RevisadoPorUsuarioId = moderador?.Id;
        documento.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await notificacoes.NotificarUsuarioAsync(
            documento.UsuarioId,
            TipoNotificacao.DocumentoAprovado,
            "Seu documento foi aprovado",
            $"Seu documento \"{documento.NomeOriginal}\" em \"{documento.Item.Titulo}\" foi aprovado e publicado.",
            documento.ItemId, $"/Itens/{documento.Item.Slug}", moderador?.Id);

        TempData["MensagemSucesso"] = "Documento aprovado e publicado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejeitarDocumentoAsync()
    {
        if (string.IsNullOrWhiteSpace(RejeitarDocumento.Motivo))
        {
            TempData["MensagemErro"] = "Informe o motivo da rejeição.";
            return RedirectToPage();
        }

        var documento = await db.Documentos.Include(d => d.Item).FirstOrDefaultAsync(d => d.Id == RejeitarDocumento.AlvoId);
        if (documento is null || documento.Status != RevisaoStatus.Pendente || documento.Item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, documento.ItemId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);
        documento.Status = RevisaoStatus.Rejeitada;
        documento.MotivoRejeicao = RejeitarDocumento.Motivo.Trim();
        documento.RevisadoPorUsuarioId = moderador?.Id;
        documento.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await notificacoes.NotificarUsuarioAsync(
            documento.UsuarioId,
            TipoNotificacao.DocumentoRejeitado,
            "Seu documento foi rejeitado",
            $"Seu documento \"{documento.NomeOriginal}\" em \"{documento.Item.Titulo}\" foi rejeitado: {documento.MotivoRejeicao}",
            documento.ItemId, $"/Itens/{documento.Item.Slug}", moderador?.Id);

        TempData["MensagemSucesso"] = "Documento rejeitado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAprovarItemAsync(Guid id)
    {
        var item = await db.Itens.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null || item.Status != ItemStatus.PropostaComunidade)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarMandamentoAsync(userManager.GetUserId(User)!, item.MandamentoId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);
        item.Status = ItemStatus.Aprovado;
        item.RevisadoPorUsuarioId = moderador?.Id;
        item.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        if (item.CriadoPorUsuarioId is not null)
        {
            await permissoes.PromoverCriadorAposAprovacaoAsync(item.Id, item.CriadoPorUsuarioId, moderador!.Id);

            await notificacoes.NotificarUsuarioAsync(
                item.CriadoPorUsuarioId,
                TipoNotificacao.ItemAprovado,
                "Seu item foi aprovado",
                $"Seu item \"{item.Titulo}\" foi aprovado e publicado. Você agora modera esse item.",
                item.Id, $"/Itens/{item.Slug}", moderador?.Id);
        }

        TempData["MensagemSucesso"] = "Item aprovado e publicado no mandamento.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejeitarItemAsync()
    {
        if (string.IsNullOrWhiteSpace(RejeitarItem.Motivo))
        {
            TempData["MensagemErro"] = "Informe o motivo da rejeição.";
            return RedirectToPage();
        }

        var item = await db.Itens.FirstOrDefaultAsync(i => i.Id == RejeitarItem.AlvoId);
        if (item is null || item.Status != ItemStatus.PropostaComunidade)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarMandamentoAsync(userManager.GetUserId(User)!, item.MandamentoId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);
        item.Status = ItemStatus.Arquivado;
        item.MotivoRejeicao = RejeitarItem.Motivo.Trim();
        item.RevisadoPorUsuarioId = moderador?.Id;
        item.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        if (item.CriadoPorUsuarioId is not null)
        {
            await notificacoes.NotificarUsuarioAsync(
                item.CriadoPorUsuarioId,
                TipoNotificacao.ItemRejeitado,
                "Seu item foi rejeitado",
                $"Seu item \"{item.Titulo}\" foi rejeitado: {item.MotivoRejeicao}",
                item.Id, null, moderador?.Id);
        }

        TempData["MensagemSucesso"] = "Item rejeitado. O autor verá o motivo.";
        return RedirectToPage();
    }

    /// <summary>Criada direto na moderação, mas ainda entra pendente — passa pela mesma fila de aprovação que uma referência de qualquer participante.</summary>
    public async Task<IActionResult> OnPostCriarReferenciaAsync()
    {
        if (NovaReferencia.ItemId == Guid.Empty || string.IsNullOrWhiteSpace(NovaReferencia.Titulo) || string.IsNullOrWhiteSpace(NovaReferencia.Url))
        {
            TempData["MensagemErro"] = "Preencha item, título e URL da referência.";
            return RedirectToPage();
        }

        var item = await db.Itens.FirstOrDefaultAsync(i => i.Id == NovaReferencia.ItemId);
        if (item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, item.Id))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);
        db.Referencias.Add(new Referencia
        {
            ItemId = item.Id,
            UsuarioId = moderador!.Id,
            Titulo = NovaReferencia.Titulo.Trim(),
            Url = NovaReferencia.Url.Trim(),
            Tipo = NovaReferencia.Tipo,
            Status = RevisaoStatus.Pendente,
        });
        await db.SaveChangesAsync();

        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.ReferenciaProposta,
            "Nova referência para aprovar",
            $"{moderador.Nome} criou a referência \"{NovaReferencia.Titulo.Trim()}\" para \"{item.Titulo}\".",
            item.Id, null, "/Moderacao/Index", moderador.Id);

        TempData["MensagemSucesso"] = "Referência criada — aguardando aprovação, igual às da comunidade.";
        return RedirectToPage();
    }

    /// <summary>Criado direto na moderação, mas ainda entra pendente — passa pela mesma fila de aprovação que um documento de qualquer participante.</summary>
    public async Task<IActionResult> OnPostCriarDocumentoAsync(IFormFile? arquivo)
    {
        if (NovoDocumento.ItemId == Guid.Empty)
        {
            TempData["MensagemErro"] = "Escolha o item.";
            return RedirectToPage();
        }

        if (arquivo is null || arquivo.Length == 0)
        {
            TempData["MensagemErro"] = "Escolha um arquivo.";
            return RedirectToPage();
        }

        if (arquivo.Length > TamanhoMaximoDocumentoBytes)
        {
            TempData["MensagemErro"] = "Arquivo maior que 10 MB.";
            return RedirectToPage();
        }

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!ExtensoesDocumentoPermitidas.Contains(extensao))
        {
            TempData["MensagemErro"] = "Tipo de arquivo não permitido. Use PDF, Word, imagem (JPG/PNG) ou TXT.";
            return RedirectToPage();
        }

        var item = await db.Itens.FirstOrDefaultAsync(i => i.Id == NovoDocumento.ItemId);
        if (item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, item.Id))
        {
            return Forbid();
        }

        var pastaUploads = Path.Combine(env.ContentRootPath, "App_Data", "uploads");
        Directory.CreateDirectory(pastaUploads);

        var nomeArmazenado = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = Path.Combine(pastaUploads, nomeArmazenado);
        await using (var destino = System.IO.File.Create(caminhoCompleto))
        {
            await arquivo.CopyToAsync(destino);
        }

        var moderador = await userManager.GetUserAsync(User);
        db.Documentos.Add(new Documento
        {
            ItemId = item.Id,
            UsuarioId = moderador!.Id,
            NomeOriginal = arquivo.FileName,
            CaminhoArmazenado = nomeArmazenado,
            ContentType = arquivo.ContentType,
            TamanhoBytes = arquivo.Length,
            Status = RevisaoStatus.Pendente,
        });
        await db.SaveChangesAsync();

        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.DocumentoProposto,
            "Novo documento para aprovar",
            $"{moderador.Nome} enviou o documento \"{arquivo.FileName}\" para \"{item.Titulo}\".",
            item.Id, null, "/Moderacao/Index", moderador.Id);

        TempData["MensagemSucesso"] = "Documento enviado — aguardando aprovação, igual aos da comunidade.";
        return RedirectToPage();
    }

    /// <summary>Proposto direto na moderação, mas ainda entra pendente — passa pela mesma fila de aprovação que um item de qualquer participante.</summary>
    public async Task<IActionResult> OnPostCriarItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NovoItemProposto.Titulo) || RichTextUtils.EhVazio(NovoItemProposto.Corpo))
        {
            TempData["MensagemErro"] = "Preencha o princípio, o título e o texto do item.";
            return RedirectToPage();
        }

        var mandamento = await db.Mandamentos.FindAsync(NovoItemProposto.MandamentoId);
        if (mandamento is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarMandamentoAsync(userManager.GetUserId(User)!, mandamento.Id))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);

        var baseSlug = SlugGenerator.Gerar(NovoItemProposto.Titulo);
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

        var proximaOrdem = await db.Itens
            .Where(i => i.MandamentoId == mandamento.Id)
            .Select(i => (int?)i.Ordem)
            .MaxAsync() ?? 0;

        var novoItem = new Item
        {
            MandamentoId = mandamento.Id,
            Slug = slug,
            Titulo = NovoItemProposto.Titulo.Trim(),
            Corpo = NovoItemProposto.Corpo,
            Status = ItemStatus.PropostaComunidade,
            CriadoPorUsuarioId = moderador!.Id,
            Ordem = proximaOrdem + 1,
        };
        db.Itens.Add(novoItem);
        await db.SaveChangesAsync();

        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.NovoItemProposto,
            "Novo item proposto",
            $"{moderador.Nome} propôs o item \"{novoItem.Titulo}\" em \"{mandamento.Secular}\".",
            novoItem.Id, null, "/Moderacao/Index", moderador.Id);

        TempData["MensagemSucesso"] = "Item proposto — aguardando aprovação, igual aos da comunidade.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAprovarTagAsync(Guid id)
    {
        var sugestao = await db.TagSugestoes.Include(t => t.Item).FirstOrDefaultAsync(t => t.Id == id);
        if (sugestao is null || sugestao.Status != RevisaoStatus.Pendente || sugestao.Item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, sugestao.ItemId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);

        var atuais = sugestao.Item.TagsLista.ToList();
        if (!atuais.Any(t => string.Equals(t, sugestao.Nome, StringComparison.OrdinalIgnoreCase)))
        {
            atuais.Add(sugestao.Nome);
            sugestao.Item.Tags = string.Join(", ", atuais);
            sugestao.Item.DataAtualizacao = DateTimeOffset.UtcNow;
        }

        sugestao.Status = RevisaoStatus.Aprovada;
        sugestao.RevisadoPorUsuarioId = moderador?.Id;
        sugestao.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await notificacoes.NotificarUsuarioAsync(
            sugestao.UsuarioId,
            TipoNotificacao.TagAprovada,
            "Sua tag foi aprovada",
            $"Sua tag \"{sugestao.Nome}\" em \"{sugestao.Item.Titulo}\" foi aprovada e publicada.",
            sugestao.ItemId, $"/Itens/{sugestao.Item.Slug}", moderador?.Id);

        TempData["MensagemSucesso"] = "Tag aprovada e publicada no item.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejeitarTagAsync()
    {
        if (string.IsNullOrWhiteSpace(RejeitarTag.Motivo))
        {
            TempData["MensagemErro"] = "Informe o motivo da rejeição.";
            return RedirectToPage();
        }

        var sugestao = await db.TagSugestoes.Include(t => t.Item).FirstOrDefaultAsync(t => t.Id == RejeitarTag.AlvoId);
        if (sugestao is null || sugestao.Status != RevisaoStatus.Pendente || sugestao.Item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, sugestao.ItemId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);
        sugestao.Status = RevisaoStatus.Rejeitada;
        sugestao.MotivoRejeicao = RejeitarTag.Motivo.Trim();
        sugestao.RevisadoPorUsuarioId = moderador?.Id;
        sugestao.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await notificacoes.NotificarUsuarioAsync(
            sugestao.UsuarioId,
            TipoNotificacao.TagRejeitada,
            "Sua tag foi rejeitada",
            $"Sua tag \"{sugestao.Nome}\" em \"{sugestao.Item.Titulo}\" foi rejeitada: {sugestao.MotivoRejeicao}",
            sugestao.ItemId, $"/Itens/{sugestao.Item.Slug}", moderador?.Id);

        TempData["MensagemSucesso"] = "Tag rejeitada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAprovarBannerAsync(Guid id)
    {
        var sugestao = await db.BannerSugestoes.Include(b => b.Item).FirstOrDefaultAsync(b => b.Id == id);
        if (sugestao is null || sugestao.Status != RevisaoStatus.Pendente || sugestao.Item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, sugestao.ItemId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);

        RemoverArquivoBanner(sugestao.Item.BannerImagem);
        sugestao.Item.BannerImagem = sugestao.CaminhoImagem;
        sugestao.Item.DataAtualizacao = DateTimeOffset.UtcNow;

        sugestao.Status = RevisaoStatus.Aprovada;
        sugestao.RevisadoPorUsuarioId = moderador?.Id;
        sugestao.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await notificacoes.NotificarUsuarioAsync(
            sugestao.UsuarioId,
            TipoNotificacao.BannerAprovado,
            "Seu banner foi aprovado",
            $"Seu banner para \"{sugestao.Item.Titulo}\" foi aprovado e publicado.",
            sugestao.ItemId, $"/Itens/{sugestao.Item.Slug}", moderador?.Id);

        TempData["MensagemSucesso"] = "Banner aprovado e publicado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejeitarBannerAsync()
    {
        if (string.IsNullOrWhiteSpace(RejeitarBanner.Motivo))
        {
            TempData["MensagemErro"] = "Informe o motivo da rejeição.";
            return RedirectToPage();
        }

        var sugestao = await db.BannerSugestoes.Include(b => b.Item).FirstOrDefaultAsync(b => b.Id == RejeitarBanner.AlvoId);
        if (sugestao is null || sugestao.Status != RevisaoStatus.Pendente || sugestao.Item is null)
        {
            return RedirectToPage();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(userManager.GetUserId(User)!, sugestao.ItemId))
        {
            return Forbid();
        }

        var moderador = await userManager.GetUserAsync(User);
        RemoverArquivoBanner(sugestao.CaminhoImagem);
        sugestao.Status = RevisaoStatus.Rejeitada;
        sugestao.MotivoRejeicao = RejeitarBanner.Motivo.Trim();
        sugestao.RevisadoPorUsuarioId = moderador?.Id;
        sugestao.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await notificacoes.NotificarUsuarioAsync(
            sugestao.UsuarioId,
            TipoNotificacao.BannerRejeitado,
            "Seu banner foi rejeitado",
            $"Seu banner para \"{sugestao.Item.Titulo}\" foi rejeitado: {sugestao.MotivoRejeicao}",
            sugestao.ItemId, $"/Itens/{sugestao.Item.Slug}", moderador?.Id);

        TempData["MensagemSucesso"] = "Banner rejeitado.";
        return RedirectToPage();
    }

    private void RemoverArquivoBanner(string? caminhoPublico)
    {
        if (string.IsNullOrWhiteSpace(caminhoPublico))
        {
            return;
        }

        var caminhoCompleto = Path.Combine(env.WebRootPath, caminhoPublico.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(caminhoCompleto))
        {
            System.IO.File.Delete(caminhoCompleto);
        }
    }
}
