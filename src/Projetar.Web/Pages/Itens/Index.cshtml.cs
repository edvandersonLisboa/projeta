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

    public string Redline(ItemRevisao revisao) => diff.RenderRedline(revisao.CorpoAnterior, revisao.CorpoNovo);

    /// <summary>Todas as edições pendentes do item, visíveis a qualquer participante — não só ao autor de cada uma.</summary>
    public List<ItemRevisao> RevisoesPendentesDoItem { get; set; } = [];

    /// <summary>Autor original do item + autores de toda edição aprovada, na ordem em que contribuíram.</summary>
    public List<ApplicationUser> Participantes { get; set; } = [];

    public bool JaApoiei(ItemRevisao revisao) => UsuarioAtualId is not null && revisao.Apoios.Any(a => a.UsuarioId == UsuarioAtualId);

    public List<Comentario> Comentarios { get; set; } = [];
    public string? UsuarioAtualId { get; set; }
    public bool UsuarioAtualEhModerador { get; set; }

    public double? MediaEstrelas { get; set; }
    public int TotalVotos { get; set; }
    public int? MinhaAvaliacao { get; set; }
    public string? MinhaObservacao { get; set; }
    public List<Voto> HistoricoVotos { get; set; } = [];

    public List<Referencia> Referencias { get; set; } = [];
    public List<Documento> Documentos { get; set; } = [];

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

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var item = await db.Itens
            .Include(i => i.Mandamento)
            .Include(i => i.CriadoPorUsuario)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Slug == slug);

        if (item is null || item.Mandamento is null)
        {
            return NotFound();
        }

        var usuario = await userManager.GetUserAsync(User);
        UsuarioAtualId = usuario?.Id;
        UsuarioAtualEhModerador = User.IsInRole("Admin") || User.IsInRole("Revisor");

        if ((!item.Mandamento.Visivel || !item.Visivel) && !User.IsInRole("Admin"))
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

        db.Comentarios.Add(new Comentario
        {
            ItemId = item.Id,
            UsuarioId = usuario.Id,
            ComentarioPaiId = NovoComentario.ComentarioPaiId,
            Texto = NovoComentario.Texto.Trim(),
        });
        await db.SaveChangesAsync();

        return RedirectToPage("./Index", null, new { slug }, "comentarios");
    }

    public async Task<IActionResult> OnPostRemoverComentarioAsync(string slug, Guid id)
    {
        var comentario = await db.Comentarios.FirstOrDefaultAsync(c => c.Id == id);
        var usuario = await userManager.GetUserAsync(User);
        if (comentario is null || usuario is null)
        {
            return NotFound();
        }

        var ehDono = comentario.UsuarioId == usuario.Id;
        var ehModerador = User.IsInRole("Admin") || User.IsInRole("Revisor");
        if (!ehDono && !ehModerador)
        {
            return Forbid();
        }

        comentario.Removido = true;
        comentario.DataEdicao = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

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

        db.Referencias.Add(new Referencia
        {
            ItemId = item.Id,
            UsuarioId = usuario.Id,
            Titulo = NovaReferencia.Titulo.Trim(),
            Url = NovaReferencia.Url.Trim(),
            Tipo = NovaReferencia.Tipo,
            Status = RevisaoStatus.Pendente,
        });
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Referência enviada e aguardando revisão.";
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

        db.Documentos.Add(new Documento
        {
            ItemId = item.Id,
            UsuarioId = usuario.Id,
            NomeOriginal = NovoArquivo.FileName,
            CaminhoArmazenado = nomeArmazenado,
            ContentType = NovoArquivo.ContentType,
            TamanhoBytes = NovoArquivo.Length,
            Status = RevisaoStatus.Pendente,
        });
        await db.SaveChangesAsync();

        TempData["MensagemSucesso"] = "Documento enviado e aguardando revisão.";
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
        }

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
        var ehModerador = User.IsInRole("Admin") || User.IsInRole("Revisor");
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
        ItemAtual.Mandamento = await db.Mandamentos.FindAsync(item.MandamentoId);
        CorpoHtml = content.ToSafeHtml(item.Corpo);
        UsuarioAtualId = usuario.Id;
        UsuarioAtualEhModerador = User.IsInRole("Admin") || User.IsInRole("Revisor");
        PodeParticipar = usuario.PerfilCompleto;
        await CarregarComentariosEVotosAsync(item.Id);
        await CarregarMateriaisAsync(item.Id);
        await CarregarHistoricoAsync(item.Id);
        await CarregarRevisoesPendentesAsync(item.Id);
        await CarregarParticipantesAsync(item);
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
