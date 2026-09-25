using System.ComponentModel.DataAnnotations;
using Projetar.Web.Data;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Pages.Itens;

public class IndexModel(
    ApplicationDbContext db,
    IContentRenderer content,
    IRevisaoDiffService diff,
    UserManager<ApplicationUser> userManager,
    INotificacaoService notificacoes,
    IPermissaoService permissoes,
    IWebHostEnvironment env) : PageModel
{
    private static readonly string[] ExtensoesPermitidas = [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt"];
    private const long TamanhoMaximoBytes = 10 * 1024 * 1024;

    public Item ItemAtual { get; set; } = null!;
    public string CorpoHtml { get; set; } = string.Empty;

    /// <summary>Estado/Município preenchidos — libera editar, comentar, votar e enviar material de apoio.</summary>
    public bool PodeParticipar { get; set; }

    public ItemRevisao? MinhaRevisaoPendenteOuRejeitada { get; set; }

    /// <summary>Redações aprovadas do item, mais recente primeiro — histórico público tipo "redação anterior → atual".</summary>
    public List<ItemRevisao> HistoricoAprovado { get; set; } = [];

    public string Redline(ItemRevisao revisao) => diff.RenderRedline(revisao.CorpoAnterior, revisao.CorpoAprovado ?? revisao.CorpoNovo);

    /// <summary>Todas as edições pendentes do item, visíveis a qualquer participante — não só ao autor de cada uma.</summary>
    public List<ItemRevisao> RevisoesPendentesDoItem { get; set; } = [];

    /// <summary>Autor original do item + autores de toda edição aprovada, na ordem em que contribuíram.</summary>
    public List<ApplicationUser> Participantes { get; set; } = [];

    /// <summary>Uma contribuição aprovada de um usuário neste item — vira uma linha no modal "quem construiu esta proposta".</summary>
    public record ContribuicaoItem(TipoSubmissao Tipo, string Resumo, DateTimeOffset Data);

    public record ContribuidorResumo(ApplicationUser Usuario, bool EhAutor, List<ContribuicaoItem> Contribuicoes)
    {
        public int Total => Contribuicoes.Count;
        public DateTimeOffset UltimaEm => Contribuicoes.Max(c => c.Data);
    }

    /// <summary>Card "Quem construiu esta proposta" — todo usuário com pelo menos uma contribuição aprovada neste item.</summary>
    public List<ContribuidorResumo> Contribuidores { get; set; } = [];
    public string OrdemContrib { get; set; } = "contribuicoes";

    public bool JaApoiei(ItemRevisao revisao) => UsuarioAtualId is not null && revisao.Apoios.Any(a => a.UsuarioId == UsuarioAtualId);

    public List<Comentario> Comentarios { get; set; } = [];
    public string? UsuarioAtualId { get; set; }
    public bool UsuarioAtualEhModerador { get; set; }

    /// <summary>Quem modera este item — por escopo direto de item (aceito ou convite pendente) ou por escopo do princípio inteiro.</summary>
    public List<EscopoModeracao> ModeradoresDoItem { get; set; } = [];

    /// <summary>Convite de moderação deste item pendente pro usuário logado, se houver — mostra o banner de aceitar/recusar.</summary>
    public EscopoModeracao? MeuConviteModeracaoPendente { get; set; }

    [BindProperty]
    public string? NovoModeradorEmail { get; set; }

    public double? MediaEstrelas { get; set; }
    public int TotalVotos { get; set; }
    public int? MinhaAvaliacao { get; set; }
    public string? MinhaObservacao { get; set; }
    public List<Voto> HistoricoVotos { get; set; } = [];

    public List<Referencia> Referencias { get; set; } = [];
    public List<Documento> Documentos { get; set; } = [];

    /// <summary>Outros princípios (até 4), cada um com o slug do seu primeiro item público — para a navegação lateral "Propostas".</summary>
    public List<(Principio Principio, string? PrimeiroItemSlug)> OutrosPrincipios { get; set; } = [];

    /// <summary>Até 4 itens de outros princípios, para a seção "Veja também".</summary>
    public List<Item> VejaTambem { get; set; } = [];

    /// <summary>Próximo princípio na ordem — usado no botão "Princípio N →". Null se este for o último.</summary>
    public Principio? ProximoPrincipio { get; set; }
    public string? ProximoPrincipioUrl { get; set; }

    /// <summary>Tags já usadas em outros itens (e ainda não aplicadas neste) — mostradas no modal "adicionar tag" para reaproveitar em vez de duplicar.</summary>
    public List<string> TagsDisponiveis { get; set; } = [];

    /// <summary>Tags sugeridas aguardando aprovação na moderação.</summary>
    public List<TagSugestao> TagsPendentes { get; set; } = [];

    /// <summary>Banner sugerido aguardando aprovação na moderação (só existe um pendente por vez).</summary>
    public BannerSugestao? BannerPendente { get; set; }

    [BindProperty]
    public ComentarioInput NovoComentario { get; set; } = new();

    [BindProperty]
    public ReferenciaInput NovaReferencia { get; set; } = new();

    [BindProperty]
    public IFormFile? NovoArquivo { get; set; }

    public class ComentarioInput
    {
        [Required(ErrorMessage = "Escreva algo antes de enviar.")]
        [StringLength(2000, ErrorMessage = "Comentário muito longo (máx. 2000 caracteres).")]
        public string Texto { get; set; } = string.Empty;

        public Guid? ComentarioPaiId { get; set; }
    }

    public class ReferenciaInput
    {
        [Required(ErrorMessage = "Dê um título para a referência.")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a URL.")]
        [Url(ErrorMessage = "URL inválida.")]
        public string Url { get; set; } = string.Empty;

        public ReferenciaTipo Tipo { get; set; } = ReferenciaTipo.Outro;
    }

    public async Task<IActionResult> OnGetAsync(string slug, string? ordemContrib)
    {
        OrdemContrib = ordemContrib == "recentes" ? "recentes" : "contribuicoes";

        var item = await db.Itens
            .Include(i => i.Principio)
            .Include(i => i.CriadoPorUsuario)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Slug == slug);

        if (item is null || item.Principio is null)
        {
            return NotFound();
        }

        var usuario = await userManager.GetUserAsync(User);
        UsuarioAtualId = usuario?.Id;
        UsuarioAtualEhModerador = usuario is not null &&
            (User.IsInRole("Admin") || await permissoes.PodeModerarItemAsync(usuario.Id, item.Id));

        if (UsuarioAtualEhModerador)
        {
            ModeradoresDoItem = await permissoes.ListarEscoposDoItemAsync(item.Id);
        }

        if (usuario is not null)
        {
            MeuConviteModeracaoPendente = await permissoes.ObterConvitePendenteItemAsync(usuario.Id, item.Id);
        }

        if ((!item.Principio.Visivel || !item.Visivel) && !User.IsInRole("Admin"))
        {
            return NotFound();
        }

        var ehPublico = item.Status is ItemStatus.Original or ItemStatus.Aprovado;
        var ehAutorDoItem = usuario is not null && item.CriadoPorUsuarioId == usuario.Id;
        if (!ehPublico && !ehAutorDoItem && !UsuarioAtualEhModerador)
        {
            return NotFound();
        }

        ItemAtual = item;
        CorpoHtml = content.ToSafeHtml(item.Corpo);

        await CarregarComentariosEVotosAsync(item.Id);
        await CarregarMateriaisAsync(item.Id);
        await CarregarHistoricoAsync(item.Id);
        await CarregarRevisoesPendentesAsync(item.Id);
        await CarregarParticipantesAsync(item);
        await CarregarNavegacaoDoDecalogoAsync(item);
        await CarregarTagsDisponiveisAsync(item, usuario);
        await CarregarPendenciasDeAparenciaAsync(item);
        await CarregarContribuidoresAsync(item);

        if (usuario is not null)
        {
            PodeParticipar = usuario.PerfilCompleto;

            // Só interessa se a edição MAIS RECENTE do autor ainda não virou o texto oficial —
            // se a mais recente já foi aprovada, uma rejeição antiga não deve voltar a aparecer.
            var minhaUltimaRevisao = await db.ItemRevisoes
                .Where(r => r.ItemId == item.Id && r.AutorUsuarioId == usuario.Id)
                .OrderByDescending(r => r.DataRevisao)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (minhaUltimaRevisao is not null && minhaUltimaRevisao.Status != RevisaoStatus.Aprovada)
            {
                MinhaRevisaoPendenteOuRejeitada = minhaUltimaRevisao;
            }

            var meuVoto = await db.Votos.AsNoTracking()
                .FirstOrDefaultAsync(v => v.ItemId == item.Id && v.UsuarioId == usuario.Id);
            MinhaAvaliacao = meuVoto?.Estrelas;
            MinhaObservacao = meuVoto?.Observacao;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostComentarioAsync(string slug)
    {
        var item = await db.Itens.FirstOrDefaultAsync(i => i.Slug == slug);
        if (item is null)
        {
            return NotFound();
        }

        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        DescartarValidacaoDeOutrosFormularios(nameof(NovoComentario));
        if (!ModelState.IsValid)
        {
            return await ReconstruirPaginaAsync(item, usuario);
        }

        var comentario = new Comentario
        {
            ItemId = item.Id,
            UsuarioId = usuario.Id,
            ComentarioPaiId = NovoComentario.ComentarioPaiId,
            Texto = NovoComentario.Texto.Trim(),
        };
        db.Comentarios.Add(comentario);
        await db.SaveChangesAsync();

        if (NovoComentario.ComentarioPaiId is Guid paiId)
        {
            var pai = await db.Comentarios.AsNoTracking().FirstOrDefaultAsync(c => c.Id == paiId);
            if (pai is not null)
            {
                await notificacoes.NotificarUsuarioAsync(
                    pai.UsuarioId,
                    TipoNotificacao.ComentarioRespondido,
                    "Responderam seu comentário",
                    $"{usuario.Nome} respondeu seu comentário em \"{item.Titulo}\".",
                    item.Id, $"/Itens/{item.Slug}", usuario.Id);
            }
        }
        else if (item.CriadoPorUsuarioId is not null)
        {
            await notificacoes.NotificarUsuarioAsync(
                item.CriadoPorUsuarioId,
                TipoNotificacao.ComentarioNovo,
                "Novo comentário no seu item",
                $"{usuario.Nome} comentou em \"{item.Titulo}\".",
                item.Id, $"/Itens/{item.Slug}", usuario.Id);
        }

        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.ComentarioNovo,
            "Novo comentário",
            $"{usuario.Nome} comentou em \"{item.Titulo}\".",
            item.Id, null, $"/Itens/{item.Slug}", usuario.Id);

        TempData["MensagemSucesso"] = "Comentário publicado.";
        return RedirectToPage("./Index", null, new { slug }, "comentarios");
    }

    public async Task<IActionResult> OnPostRemoverComentarioAsync(string slug, Guid id)
    {
        var comentario = await db.Comentarios.Include(c => c.Item).FirstOrDefaultAsync(c => c.Id == id);
        var usuario = await userManager.GetUserAsync(User);
        if (comentario is null || comentario.Item is null || usuario is null)
        {
            return NotFound();
        }

        var ehDono = comentario.UsuarioId == usuario.Id;
        var ehModerador = User.IsInRole("Admin") || await permissoes.PodeModerarItemAsync(usuario.Id, comentario.ItemId);
        if (!ehDono && !ehModerador)
        {
            return Forbid();
        }

        comentario.Removido = true;
        comentario.DataEdicao = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        if (!ehDono)
        {
            await notificacoes.NotificarUsuarioAsync(
                comentario.UsuarioId,
                TipoNotificacao.ComentarioRemovido,
                "Seu comentário foi removido",
                $"Um moderador removeu seu comentário em \"{comentario.Item.Titulo}\".",
                comentario.ItemId, $"/Itens/{comentario.Item.Slug}", usuario.Id);
        }

        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.ComentarioRemovido,
            "Comentário removido",
            $"{usuario.Nome} removeu um comentário em \"{comentario.Item.Titulo}\".",
            comentario.ItemId, null, $"/Itens/{comentario.Item.Slug}", usuario.Id);

        TempData["MensagemSucesso"] = "Comentário removido.";
        return RedirectToPage("./Index", null, new { slug }, "comentarios");
    }

    public async Task<IActionResult> OnPostAvaliarAsync(string slug, int estrelas, string? observacao)
    {
        if (estrelas is < 1 or > 5)
        {
            return BadRequest();
        }

        if (observacao is { Length: > 1000 })
        {
            TempData["MensagemErro"] = "A observação da avaliação pode ter no máximo 1000 caracteres.";
            return RedirectToPage("./Index", null, new { slug }, "avaliacao");
        }

        var item = await db.Itens.FirstOrDefaultAsync(i => i.Slug == slug);
        var usuario = await userManager.GetUserAsync(User);
        if (item is null || usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var observacaoTratada = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();

        var voto = await db.Votos.FirstOrDefaultAsync(v => v.ItemId == item.Id && v.UsuarioId == usuario.Id);
        var ehNovoVoto = voto is null;
        if (voto is null)
        {
            db.Votos.Add(new Voto { ItemId = item.Id, UsuarioId = usuario.Id, Estrelas = estrelas, Observacao = observacaoTratada });
        }
        else
        {
            voto.Estrelas = estrelas;
            voto.Observacao = observacaoTratada;
            voto.DataAtualizacao = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync();

        if (ehNovoVoto)
        {
            if (item.CriadoPorUsuarioId is not null)
            {
                await notificacoes.NotificarUsuarioAsync(
                    item.CriadoPorUsuarioId,
                    TipoNotificacao.AvaliacaoNova,
                    "Seu item recebeu uma avaliação",
                    $"{usuario.Nome} avaliou \"{item.Titulo}\" com {estrelas} estrela(s).",
                    item.Id, $"/Itens/{item.Slug}", usuario.Id);
            }

            await notificacoes.NotificarModeradoresAsync(
                TipoNotificacao.AvaliacaoNova,
                "Nova avaliação",
                $"{usuario.Nome} avaliou \"{item.Titulo}\" com {estrelas} estrela(s).",
                item.Id, null, $"/Itens/{item.Slug}", usuario.Id);
        }

        TempData["MensagemSucesso"] = "Avaliação registrada.";
        return RedirectToPage("./Index", null, new { slug }, "avaliacao");
    }

    public async Task<IActionResult> OnPostReferenciaAsync(string slug)
    {
        var item = await db.Itens.FirstOrDefaultAsync(i => i.Slug == slug);
        if (item is null)
        {
            return NotFound();
        }

        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        DescartarValidacaoDeOutrosFormularios(nameof(NovaReferencia));
        if (!ModelState.IsValid)
        {
            return await ReconstruirPaginaAsync(item, usuario);
        }

        var ehModeradorDoItem = User.IsInRole("Admin") || await permissoes.PodeModerarItemAsync(usuario.Id, item.Id);

        var referencia = new Referencia
        {
            ItemId = item.Id,
            UsuarioId = usuario.Id,
            Titulo = NovaReferencia.Titulo.Trim(),
            Url = NovaReferencia.Url.Trim(),
            Tipo = NovaReferencia.Tipo,
            Status = ehModeradorDoItem ? RevisaoStatus.Aprovada : RevisaoStatus.Pendente,
        };
        if (ehModeradorDoItem)
        {
            referencia.RevisadoPorUsuarioId = usuario.Id;
            referencia.DataModeracao = DateTimeOffset.UtcNow;
        }
        db.Referencias.Add(referencia);
        await db.SaveChangesAsync();

        if (ehModeradorDoItem)
        {
            TempData["MensagemSucesso"] = "Referência publicada — você modera este item.";
        }
        else
        {
            await notificacoes.NotificarModeradoresAsync(
                TipoNotificacao.ReferenciaProposta,
                "Nova referência para aprovar",
                $"{usuario.Nome} enviou a referência \"{NovaReferencia.Titulo.Trim()}\" para \"{item.Titulo}\".",
                item.Id, null, $"/Moderacao/Index", usuario.Id);

            TempData["MensagemSucesso"] = "Referência enviada e aguardando revisão.";
        }
        return RedirectToPage("./Index", null, new { slug }, "material");
    }

    public async Task<IActionResult> OnPostDocumentoAsync(string slug)
    {
        var item = await db.Itens.FirstOrDefaultAsync(i => i.Slug == slug);
        if (item is null)
        {
            return NotFound();
        }

        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        DescartarValidacaoDeOutrosFormularios();

        if (NovoArquivo is null || NovoArquivo.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Escolha um arquivo.");
        }
        else if (NovoArquivo.Length > TamanhoMaximoBytes)
        {
            ModelState.AddModelError(string.Empty, "Arquivo maior que 10 MB.");
        }
        else if (!ExtensoesPermitidas.Contains(Path.GetExtension(NovoArquivo.FileName).ToLowerInvariant()))
        {
            ModelState.AddModelError(string.Empty, "Tipo de arquivo não permitido. Use PDF, Word, imagem (JPG/PNG) ou TXT.");
        }

        if (!ModelState.IsValid)
        {
            return await ReconstruirPaginaAsync(item, usuario);
        }

        var pastaUploads = Path.Combine(env.ContentRootPath, "App_Data", "uploads");
        Directory.CreateDirectory(pastaUploads);

        var extensao = Path.GetExtension(NovoArquivo!.FileName).ToLowerInvariant();
        var nomeArmazenado = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = Path.Combine(pastaUploads, nomeArmazenado);

        await using (var destino = System.IO.File.Create(caminhoCompleto))
        {
            await NovoArquivo.CopyToAsync(destino);
        }

        var ehModeradorDoItem = User.IsInRole("Admin") || await permissoes.PodeModerarItemAsync(usuario.Id, item.Id);

        var documento = new Documento
        {
            ItemId = item.Id,
            UsuarioId = usuario.Id,
            NomeOriginal = NovoArquivo.FileName,
            CaminhoArmazenado = nomeArmazenado,
            ContentType = NovoArquivo.ContentType,
            TamanhoBytes = NovoArquivo.Length,
            Status = ehModeradorDoItem ? RevisaoStatus.Aprovada : RevisaoStatus.Pendente,
        };
        if (ehModeradorDoItem)
        {
            documento.RevisadoPorUsuarioId = usuario.Id;
            documento.DataModeracao = DateTimeOffset.UtcNow;
        }
        db.Documentos.Add(documento);
        await db.SaveChangesAsync();

        if (ehModeradorDoItem)
        {
            TempData["MensagemSucesso"] = "Documento publicado — você modera este item.";
        }
        else
        {
            await notificacoes.NotificarModeradoresAsync(
                TipoNotificacao.DocumentoProposto,
                "Novo documento para aprovar",
                $"{usuario.Nome} enviou o documento \"{NovoArquivo.FileName}\" para \"{item.Titulo}\".",
                item.Id, null, "/Moderacao/Index", usuario.Id);

            TempData["MensagemSucesso"] = "Documento enviado e aguardando revisão.";
        }
        return RedirectToPage("./Index", null, new { slug }, "material");
    }

    public async Task<IActionResult> OnPostApoiarAsync(string slug, Guid revisaoId)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var revisao = await db.ItemRevisoes
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == revisaoId && r.Status == RevisaoStatus.Pendente);
        if (revisao is null || revisao.Item is null || revisao.Item.Slug != slug)
        {
            return NotFound();
        }

        if (revisao.AutorUsuarioId == usuario.Id)
        {
            TempData["MensagemErroDebate"] = "Você não pode apoiar a própria proposta.";
            return RedirectToPage("./Index", null, new { slug }, "debate");
        }

        var jaApoiou = await db.ItemRevisaoApoios.AnyAsync(a => a.ItemRevisaoId == revisaoId && a.UsuarioId == usuario.Id);
        if (!jaApoiou)
        {
            db.ItemRevisaoApoios.Add(new ItemRevisaoApoio { ItemRevisaoId = revisaoId, UsuarioId = usuario.Id });
            await db.SaveChangesAsync();

            await notificacoes.NotificarUsuarioAsync(
                revisao.AutorUsuarioId,
                TipoNotificacao.ApoioRecebido,
                "Sua proposta recebeu apoio",
                $"{usuario.Nome} apoiou sua proposta de edição em \"{revisao.Item.Titulo}\".",
                revisao.ItemId, $"/Itens/{slug}", usuario.Id);

            await notificacoes.NotificarModeradoresAsync(
                TipoNotificacao.ApoioRecebido,
                "Nova apoio a uma proposta",
                $"{usuario.Nome} apoiou uma proposta de edição em \"{revisao.Item.Titulo}\".",
                revisao.ItemId, null, "/Moderacao/Index", usuario.Id);
        }

        TempData["MensagemSucesso"] = "Apoio registrado.";
        return RedirectToPage("./Index", null, new { slug }, "debate");
    }

    public async Task<IActionResult> OnPostRemoverApoioAsync(string slug, Guid revisaoId)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return RedirectToPage("/Conta/Entrar");
        }

        var apoio = await db.ItemRevisaoApoios.FirstOrDefaultAsync(a => a.ItemRevisaoId == revisaoId && a.UsuarioId == usuario.Id);
        if (apoio is not null)
        {
            db.ItemRevisaoApoios.Remove(apoio);
            await db.SaveChangesAsync();
            TempData["MensagemSucesso"] = "Apoio removido.";
        }

        return RedirectToPage("./Index", null, new { slug }, "debate");
    }

    public async Task<IActionResult> OnGetBaixarAsync(string slug, Guid id)
    {
        var documento = await db.Documentos.Include(d => d.Item).FirstOrDefaultAsync(d => d.Id == id);
        if (documento is null || documento.Item is null || documento.Item.Slug != slug)
        {
            return NotFound();
        }

        var usuario = await userManager.GetUserAsync(User);
        var ehDono = usuario is not null && documento.UsuarioId == usuario.Id;
        var ehModerador = usuario is not null &&
            (User.IsInRole("Admin") || await permissoes.PodeModerarItemAsync(usuario.Id, documento.ItemId));
        if (documento.Status != RevisaoStatus.Aprovada && !ehDono && !ehModerador)
        {
            return Forbid();
        }

        var caminhoCompleto = Path.Combine(env.ContentRootPath, "App_Data", "uploads", documento.CaminhoArmazenado);
        if (!System.IO.File.Exists(caminhoCompleto))
        {
            return NotFound();
        }

        return PhysicalFile(caminhoCompleto, documento.ContentType, documento.NomeOriginal);
    }

    /// <summary>Só administradores adicionam tag — seleciona uma já existente no modal ou cria uma nova, sem passar por revisão.</summary>
    /// <summary>Qualquer participante pode sugerir uma tag — ela só entra no item depois de aprovada na moderação.</summary>
    public async Task<IActionResult> OnPostSugerirTagAsync(string slug, string tag)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var item = await db.Itens.FirstOrDefaultAsync(i => i.Slug == slug);
        if (item is null)
        {
            return NotFound();
        }

        tag = tag?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(tag) && tag.Length <= 40)
        {
            var jaTem = item.TagsLista.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            var jaPendente = await db.TagSugestoes.AnyAsync(t =>
                t.ItemId == item.Id && t.Status == RevisaoStatus.Pendente && t.Nome.ToLower() == tag.ToLower());

            if (!jaTem && !jaPendente)
            {
                var ehModeradorDoItem = User.IsInRole("Admin") || await permissoes.PodeModerarItemAsync(usuario.Id, item.Id);

                var sugestao = new TagSugestao
                {
                    ItemId = item.Id,
                    UsuarioId = usuario.Id,
                    Nome = tag,
                    Status = ehModeradorDoItem ? RevisaoStatus.Aprovada : RevisaoStatus.Pendente,
                };

                if (ehModeradorDoItem)
                {
                    sugestao.RevisadoPorUsuarioId = usuario.Id;
                    sugestao.DataModeracao = DateTimeOffset.UtcNow;
                    var atuais = item.TagsLista.ToList();
                    atuais.Add(tag);
                    item.Tags = string.Join(", ", atuais);
                    item.DataAtualizacao = DateTimeOffset.UtcNow;
                }

                db.TagSugestoes.Add(sugestao);
                await db.SaveChangesAsync();

                if (ehModeradorDoItem)
                {
                    TempData["MensagemSucesso"] = "Tag publicada — você modera este item.";
                }
                else
                {
                    await notificacoes.NotificarModeradoresAsync(
                        TipoNotificacao.TagProposta,
                        "Nova tag sugerida",
                        $"{usuario.Nome} sugeriu a tag \"{tag}\" para \"{item.Titulo}\".",
                        item.Id, null, "/Moderacao/Index", usuario.Id);

                    TempData["MensagemSucesso"] = "Tag sugerida — aguardando aprovação de um revisor.";
                }
            }
        }

        return RedirectToPage("./Index", null, new { slug }, "tags");
    }

    /// <summary>Só administradores removem tag — ajuste de catalogação, salvo na hora.</summary>
    public async Task<IActionResult> OnPostRemoverTagAsync(string slug, string tag)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var item = await db.Itens.FirstOrDefaultAsync(i => i.Slug == slug);
        if (item is null)
        {
            return NotFound();
        }

        var restantes = item.TagsLista.Where(t => !string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)).ToList();
        item.Tags = restantes.Count > 0 ? string.Join(", ", restantes) : null;
        item.DataAtualizacao = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var admin = await userManager.GetUserAsync(User);
        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.TagRemovida,
            "Tag removida",
            $"{admin?.Nome ?? "Um admin"} removeu a tag \"{tag}\" de \"{item.Titulo}\".",
            item.Id, null, $"/Itens/{item.Slug}", admin?.Id);

        return RedirectToPage("./Index", null, new { slug }, "tags");
    }

    /// <summary>Quem já modera este item (escopo direto ou de princípio) pode convidar outro moderador por
    /// e-mail — sempre com escopo só deste item, nunca do princípio inteiro (isso só Admin concede). O convite
    /// fica Pendente até o convidado aceitar; não dá nenhum direito de moderação antes disso.</summary>
    public async Task<IActionResult> OnPostAdicionarModeradorAsync(string slug)
    {
        var item = await db.Itens.FirstOrDefaultAsync(i => i.Slug == slug);
        var usuarioAtual = await userManager.GetUserAsync(User);
        if (item is null || usuarioAtual is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(usuarioAtual.Id, item.Id))
        {
            return Forbid();
        }

        var email = NovoModeradorEmail?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["MensagemErroModeradores"] = "Informe o e-mail do novo moderador.";
            return RedirectToPage("./Index", null, new { slug }, "moderadores");
        }

        var alvo = await userManager.FindByEmailAsync(email);
        if (alvo is null)
        {
            TempData["MensagemErroModeradores"] = "Não existe usuário cadastrado com esse e-mail.";
            return RedirectToPage("./Index", null, new { slug }, "moderadores");
        }

        var (sucesso, erro) = await permissoes.ConvidarModeradorItemAsync(alvo.Id, item.Id, usuarioAtual.Id);
        if (!sucesso)
        {
            TempData["MensagemErroModeradores"] = erro;
            return RedirectToPage("./Index", null, new { slug }, "moderadores");
        }

        await notificacoes.NotificarUsuarioAsync(
            alvo.Id, TipoNotificacao.ConviteModeracaoItem,
            "Convite para moderar um item",
            $"{usuarioAtual.Nome} convidou você para moderar \"{item.Titulo}\". Aceite ou recuse na página do item.",
            item.Id, $"/Itens/{item.Slug}", usuarioAtual.Id);

        TempData["MensagemSucesso"] = $"Convite enviado para {alvo.Nome} — a moderação começa quando ele(a) aceitar.";
        return RedirectToPage("./Index", null, new { slug }, "moderadores");
    }

    public async Task<IActionResult> OnPostRemoverModeradorAsync(string slug, Guid escopoId)
    {
        var item = await db.Itens.FirstOrDefaultAsync(i => i.Slug == slug);
        var usuarioAtual = await userManager.GetUserAsync(User);
        if (item is null || usuarioAtual is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin") && !await permissoes.PodeModerarItemAsync(usuarioAtual.Id, item.Id))
        {
            return Forbid();
        }

        var (sucesso, erro) = await permissoes.RevogarEscopoAsync(escopoId);
        TempData[sucesso ? "MensagemSucesso" : "MensagemErroModeradores"] = sucesso ? "Moderador removido." : erro;
        return RedirectToPage("./Index", null, new { slug }, "moderadores");
    }

    /// <summary>Aceita um convite de moderação deste item — só o próprio convidado pode aceitar o dele.</summary>
    public async Task<IActionResult> OnPostAceitarConviteModeracaoAsync(string slug, Guid escopoId)
    {
        var usuarioAtual = await userManager.GetUserAsync(User);
        if (usuarioAtual is null)
        {
            return NotFound();
        }

        var (sucesso, erro) = await permissoes.AceitarConviteModeracaoAsync(escopoId, usuarioAtual.Id);
        TempData[sucesso ? "MensagemSucesso" : "MensagemErro"] = sucesso ? "Convite aceito — você agora modera este item." : erro;
        return RedirectToPage("./Index", null, new { slug });
    }

    /// <summary>Recusa um convite de moderação deste item — o registro é removido, dá pra convidar de novo depois.</summary>
    public async Task<IActionResult> OnPostRecusarConviteModeracaoAsync(string slug, Guid escopoId)
    {
        var usuarioAtual = await userManager.GetUserAsync(User);
        if (usuarioAtual is null)
        {
            return NotFound();
        }

        var (sucesso, erro) = await permissoes.RecusarConviteModeracaoAsync(escopoId, usuarioAtual.Id);
        TempData[sucesso ? "MensagemSucesso" : "MensagemErro"] = sucesso ? "Convite recusado." : erro;
        return RedirectToPage("./Index", null, new { slug });
    }

    /// <summary>
    /// A página tem vários formulários (comentar, avaliar, referência, documento) e todos os
    /// [BindProperty] são vinculados e validados juntos a cada POST, não importa o handler.
    /// Descarta erros de ModelState de propriedades fora do prefixo do formulário atual.
    /// </summary>
    private void DescartarValidacaoDeOutrosFormularios(params string[] prefixosParaManter)
    {
        var chaves = ModelState.Keys
            .Where(chave => !prefixosParaManter.Any(prefixo => chave.StartsWith(prefixo, StringComparison.Ordinal)))
            .ToList();
        foreach (var chave in chaves)
        {
            ModelState.Remove(chave);
        }
    }

    private async Task<IActionResult> ReconstruirPaginaAsync(Item item, ApplicationUser usuario)
    {
        ItemAtual = item;
        ItemAtual.Principio = await db.Principios.FindAsync(item.PrincipioId);
        CorpoHtml = content.ToSafeHtml(item.Corpo);
        UsuarioAtualId = usuario.Id;
        UsuarioAtualEhModerador = User.IsInRole("Admin") || await permissoes.PodeModerarItemAsync(usuario.Id, item.Id);
        PodeParticipar = usuario.PerfilCompleto;
        await CarregarComentariosEVotosAsync(item.Id);
        await CarregarMateriaisAsync(item.Id);
        await CarregarHistoricoAsync(item.Id);
        await CarregarRevisoesPendentesAsync(item.Id);
        await CarregarParticipantesAsync(item);
        await CarregarNavegacaoDoDecalogoAsync(item);
        await CarregarTagsDisponiveisAsync(item, usuario);
        await CarregarPendenciasDeAparenciaAsync(item);
        await CarregarContribuidoresAsync(item);
        return Page();
    }

    private async Task CarregarHistoricoAsync(Guid itemId)
    {
        HistoricoAprovado = await db.ItemRevisoes
            .Where(r => r.ItemId == itemId && r.Status == RevisaoStatus.Aprovada)
            .Include(r => r.AutorUsuario)
            .OrderByDescending(r => r.DataRevisao)
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task CarregarRevisoesPendentesAsync(Guid itemId)
    {
        RevisoesPendentesDoItem = await db.ItemRevisoes
            .Where(r => r.ItemId == itemId && r.Status == RevisaoStatus.Pendente)
            .Include(r => r.AutorUsuario)
            .Include(r => r.Apoios).ThenInclude(a => a.Usuario)
            .OrderBy(r => r.DataRevisao)
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task CarregarParticipantesAsync(Item item)
    {
        var participantes = new List<ApplicationUser>();

        if (item.CriadoPorUsuarioId is not null)
        {
            var criador = item.CriadoPorUsuario
                ?? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == item.CriadoPorUsuarioId);
            if (criador is not null)
            {
                participantes.Add(criador);
            }
        }

        var colaboradores = await db.ItemRevisoes
            .AsNoTracking()
            .Where(r => r.ItemId == item.Id && r.Status == RevisaoStatus.Aprovada)
            .OrderBy(r => r.DataRevisao)
            .Select(r => r.AutorUsuario)
            .ToListAsync();

        foreach (var colaborador in colaboradores)
        {
            if (colaborador is not null && participantes.All(p => p.Id != colaborador.Id))
            {
                participantes.Add(colaborador);
            }
        }

        Participantes = participantes;
    }

    /// <summary>Junta toda contribuição aprovada neste item (criação, edições, referências, documentos, tags,
    /// banners) agrupada por usuário — alimenta o card "Quem construiu esta proposta".</summary>
    private async Task CarregarContribuidoresAsync(Item item)
    {
        var porUsuario = new Dictionary<string, (ApplicationUser Usuario, bool EhAutor, List<ContribuicaoItem> Itens)>();

        void Registrar(ApplicationUser? usuario, ContribuicaoItem contribuicao)
        {
            if (usuario is null)
            {
                return;
            }

            if (!porUsuario.TryGetValue(usuario.Id, out var entrada))
            {
                entrada = (usuario, usuario.Id == item.CriadoPorUsuarioId, []);
                porUsuario[usuario.Id] = entrada;
            }

            entrada.Itens.Add(contribuicao);
        }

        if (item.CriadoPorUsuarioId is not null && item.Status is ItemStatus.Aprovado)
        {
            var criador = item.CriadoPorUsuario
                ?? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == item.CriadoPorUsuarioId);
            Registrar(criador, new ContribuicaoItem(TipoSubmissao.Item, "Propôs este item", item.DataCriacao));
        }

        var revisoes = await db.ItemRevisoes.AsNoTracking()
            .Where(r => r.ItemId == item.Id && r.Status == RevisaoStatus.Aprovada)
            .Include(r => r.AutorUsuario)
            .ToListAsync();
        foreach (var r in revisoes)
        {
            var resumo = string.IsNullOrWhiteSpace(r.ComentarioDaMudanca) ? "Editou o texto" : r.ComentarioDaMudanca!;
            Registrar(r.AutorUsuario, new ContribuicaoItem(TipoSubmissao.EdicaoTexto, resumo, r.DataModeracao ?? r.DataRevisao));
        }

        var referencias = await db.Referencias.AsNoTracking()
            .Where(r => r.ItemId == item.Id && r.Status == RevisaoStatus.Aprovada)
            .Include(r => r.Usuario)
            .ToListAsync();
        foreach (var r in referencias)
        {
            Registrar(r.Usuario, new ContribuicaoItem(TipoSubmissao.Referencia, $"Adicionou a referência \"{r.Titulo}\"", r.DataModeracao ?? r.DataCriacao));
        }

        var documentos = await db.Documentos.AsNoTracking()
            .Where(d => d.ItemId == item.Id && d.Status == RevisaoStatus.Aprovada)
            .Include(d => d.Usuario)
            .ToListAsync();
        foreach (var d in documentos)
        {
            Registrar(d.Usuario, new ContribuicaoItem(TipoSubmissao.Documento, $"Enviou o documento \"{d.NomeOriginal}\"", d.DataModeracao ?? d.DataUpload));
        }

        var tags = await db.TagSugestoes.AsNoTracking()
            .Where(t => t.ItemId == item.Id && t.Status == RevisaoStatus.Aprovada)
            .Include(t => t.Usuario)
            .ToListAsync();
        foreach (var t in tags)
        {
            Registrar(t.Usuario, new ContribuicaoItem(TipoSubmissao.Tag, $"Sugeriu a tag \"{t.Nome}\"", t.DataModeracao ?? t.DataCriacao));
        }

        var banners = await db.BannerSugestoes.AsNoTracking()
            .Where(b => b.ItemId == item.Id && b.Status == RevisaoStatus.Aprovada)
            .Include(b => b.Usuario)
            .ToListAsync();
        foreach (var b in banners)
        {
            Registrar(b.Usuario, new ContribuicaoItem(TipoSubmissao.Banner, "Propôs um novo banner", b.DataModeracao ?? b.DataCriacao));
        }

        var lista = porUsuario.Values
            .Select(e => new ContribuidorResumo(e.Usuario, e.EhAutor, e.Itens))
            .ToList();

        Contribuidores = OrdemContrib == "recentes"
            ? lista.OrderByDescending(c => c.UltimaEm).ToList()
            : lista.OrderByDescending(c => c.Total).ThenByDescending(c => c.UltimaEm).ToList();
    }

    /// <summary>Tags do catálogo sugerido do projeto + já usadas em qualquer item, sem repetir as que este
    /// item já tem — só carrega pra quem pode sugerir (participante com perfil completo).</summary>
    private async Task CarregarTagsDisponiveisAsync(Item item, ApplicationUser? usuario)
    {
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return;
        }

        var tagsDoCatalogo = await db.TagsCatalogo
            .OrderBy(t => t.Ordem)
            .Select(t => t.Nome)
            .AsNoTracking()
            .ToListAsync();

        var tagsDeOutrosItens = await db.Itens
            .Where(i => i.Tags != null && i.Tags != "")
            .Select(i => i.Tags!)
            .AsNoTracking()
            .ToListAsync();

        var jaNesteItem = item.TagsLista;
        TagsDisponiveis = tagsDoCatalogo
            .Concat(tagsDeOutrosItens.SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(t => !jaNesteItem.Any(existente => string.Equals(existente, t, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Tags e banner sugeridos ainda não avaliados pela moderação — mostrados como "em análise" na página do item.</summary>
    private async Task CarregarPendenciasDeAparenciaAsync(Item item)
    {
        TagsPendentes = await db.TagSugestoes
            .Where(t => t.ItemId == item.Id && t.Status == RevisaoStatus.Pendente)
            .Include(t => t.Usuario)
            .OrderBy(t => t.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

        BannerPendente = await db.BannerSugestoes
            .Where(b => b.ItemId == item.Id && b.Status == RevisaoStatus.Pendente)
            .OrderByDescending(b => b.DataCriacao)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    /// <summary>Carrega os dados de navegação entre princípios: mini-lista "Propostas", "Veja também" e o botão de próximo princípio.</summary>
    private async Task CarregarNavegacaoDoDecalogoAsync(Item item)
    {
        var ehAdmin = User.IsInRole("Admin");

        var principiosQuery = db.Principios.AsQueryable();
        if (!ehAdmin)
        {
            principiosQuery = principiosQuery.Where(m => m.Visivel);
        }

        var outrosPrincipios = await principiosQuery
            .Where(m => m.Id != item.PrincipioId)
            .Include(m => m.Itens
                .Where(i => (i.Status == ItemStatus.Original || i.Status == ItemStatus.Aprovado) && (ehAdmin || i.Visivel))
                .OrderBy(i => i.Ordem))
            .OrderBy(m => m.Ordem)
            .AsNoTracking()
            .ToListAsync();

        OutrosPrincipios = outrosPrincipios
            .Take(4)
            .Select(m => (m, m.Itens.FirstOrDefault()?.Slug))
            .ToList();

        VejaTambem = outrosPrincipios
            .Select(m => m.Itens.FirstOrDefault())
            .Where(i => i is not null)
            .Cast<Item>()
            .Take(4)
            .ToList();
        foreach (var relacionado in VejaTambem)
        {
            relacionado.Principio = outrosPrincipios.First(m => m.Id == relacionado.PrincipioId);
        }

        ProximoPrincipio = outrosPrincipios.FirstOrDefault(m => m.Ordem > item.Principio!.Ordem)
            ?? outrosPrincipios.OrderBy(m => m.Ordem).FirstOrDefault();
        ProximoPrincipioUrl = ProximoPrincipio is null
            ? null
            : (ProximoPrincipio.Itens.FirstOrDefault()?.Slug is { } slug ? $"/Itens/{slug}" : "/");
    }

    private async Task CarregarComentariosEVotosAsync(Guid itemId)
    {
        Comentarios = await db.Comentarios
            .Where(c => c.ItemId == itemId && c.ComentarioPaiId == null)
            .Include(c => c.Usuario)
            .Include(c => c.Respostas).ThenInclude(r => r.Usuario)
            .OrderBy(c => c.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

        HistoricoVotos = await db.Votos
            .Where(v => v.ItemId == itemId)
            .Include(v => v.Usuario)
            .OrderByDescending(v => v.DataAtualizacao)
            .AsNoTracking()
            .ToListAsync();
        TotalVotos = HistoricoVotos.Count;
        MediaEstrelas = HistoricoVotos.Count > 0 ? HistoricoVotos.Average(v => v.Estrelas) : null;
    }

    private async Task CarregarMateriaisAsync(Guid itemId)
    {
        var usuarioId = UsuarioAtualId;

        Referencias = await db.Referencias
            .Where(r => r.ItemId == itemId && (r.Status == RevisaoStatus.Aprovada || r.UsuarioId == usuarioId))
            .Include(r => r.Usuario)
            .OrderByDescending(r => r.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

        Documentos = await db.Documentos
            .Where(d => d.ItemId == itemId && (d.Status == RevisaoStatus.Aprovada || d.UsuarioId == usuarioId))
            .Include(d => d.Usuario)
            .OrderByDescending(d => d.DataUpload)
            .AsNoTracking()
            .ToListAsync();
    }
}
