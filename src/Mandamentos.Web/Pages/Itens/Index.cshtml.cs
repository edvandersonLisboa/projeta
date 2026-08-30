using System.ComponentModel.DataAnnotations;
using Mandamentos.Web.Data;
using Mandamentos.Web.Models;
using Mandamentos.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Mandamentos.Web.Pages.Itens;

public class IndexModel(
    ApplicationDbContext db,
    IMarkdownRenderer markdown,
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
        CorpoHtml = markdown.ToSafeHtml(item.Corpo, item.FormatoCorpo);

        await CarregarComentariosEVotosAsync(item.Id);
        await CarregarMateriaisAsync(item.Id);

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
        CorpoHtml = markdown.ToSafeHtml(item.Corpo, item.FormatoCorpo);
        UsuarioAtualId = usuario.Id;
        UsuarioAtualEhModerador = User.IsInRole("Admin") || User.IsInRole("Revisor");
        PodeParticipar = usuario.PerfilCompleto;
        await CarregarComentariosEVotosAsync(item.Id);
        await CarregarMateriaisAsync(item.Id);
        return Page();
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
