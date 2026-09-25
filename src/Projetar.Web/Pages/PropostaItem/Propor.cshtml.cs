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

namespace Projetar.Web.Pages.PropostaItem;

[Authorize]
public class ProporModel(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    INotificacaoService notificacoes,
    IWebHostEnvironment env) : PageModel
{
    private static readonly string[] ExtensoesImagemPermitidas = [".jpg", ".jpeg", ".png", ".webp"];
    private const long TamanhoMaximoImagemBytes = 5 * 1024 * 1024;
    private const int MaxTags = 5;

    public Mandamento MandamentoAtual { get; set; } = null!;

    /// <summary>Tags já usadas em qualquer item do site — sugeridas no formulário pra reaproveitar em vez de duplicar.</summary>
    public List<string> TagsSugeridas { get; set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Dê um título para o item.")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escreva o texto da lei proposta.")]
        public string Corpo { get; set; } = string.Empty;

        /// <summary>Tags separadas por vírgula — montado no navegador a partir dos chips.</summary>
        public string? Tags { get; set; }

        /// <summary>Já enviado pro servidor via upload assíncrono no momento da escolha do arquivo — aqui só guarda a URL.</summary>
        public string? BannerUrl { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int mandamentoId)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var mandamento = await db.Mandamentos.FindAsync(mandamentoId);
        if (mandamento is null)
        {
            return NotFound();
        }

        MandamentoAtual = mandamento;
        await CarregarTagsSugeridasAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int mandamentoId)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var mandamento = await db.Mandamentos.FindAsync(mandamentoId);
        if (mandamento is null)
        {
            return NotFound();
        }

        MandamentoAtual = mandamento;

        if (RichTextUtils.EhVazio(Input.Corpo))
        {
            ModelState.AddModelError("Input.Corpo", "Escreva o texto da lei proposta.");
        }

        if (!ModelState.IsValid)
        {
            await CarregarTagsSugeridasAsync();
            return Page();
        }

        var slug = await GerarSlugUnicoAsync(Input.Titulo);
        var proximaOrdem = await db.Itens
            .Where(i => i.MandamentoId == mandamentoId)
            .Select(i => (int?)i.Ordem)
            .MaxAsync() ?? 0;

        var tags = (Input.Tags ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTags)
            .ToList();

        var item = new Item
        {
            MandamentoId = mandamentoId,
            Slug = slug,
            Titulo = Input.Titulo.Trim(),
            Corpo = Input.Corpo,
            Tags = tags.Count > 0 ? string.Join(", ", tags) : null,
            Status = ItemStatus.PropostaComunidade,
            CriadoPorUsuarioId = usuario.Id,
            Ordem = proximaOrdem + 1,
            BannerImagem = string.IsNullOrWhiteSpace(Input.BannerUrl) ? null : Input.BannerUrl,
        };

        db.Itens.Add(item);
        await db.SaveChangesAsync();

        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.NovoItemProposto,
            "Novo item proposto",
            $"{usuario.Nome} propôs o item \"{item.Titulo}\" em \"{mandamento.Secular}\".",
            item.Id, null, "/Moderacao/Index", usuario.Id);

        TempData["MensagemSucesso"] = "Item proposto! Ele fica visível só pra você até um revisor aprovar.";
        return RedirectToPage("/Itens/Index", new { slug });
    }

    /// <summary>Upload assíncrono de uma imagem (banner ou imagem dentro do texto) — chamado via fetch pelo editor, devolve a URL em JSON.</summary>
    public async Task<IActionResult> OnPostUploadImagemAsync(IFormFile? arquivo, string? pasta)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return Forbid();
        }

        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(new { erro = "Escolha uma imagem." });
        }

        if (arquivo.Length > TamanhoMaximoImagemBytes)
        {
            return BadRequest(new { erro = "Imagem maior que 5 MB." });
        }

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!ExtensoesImagemPermitidas.Contains(extensao))
        {
            return BadRequest(new { erro = "Formato não permitido. Use JPG, PNG ou WEBP." });
        }

        var subpasta = pasta == "banners" ? "banners" : "conteudo";
        var url = await SalvarImagemAsync(arquivo, subpasta);
        return new JsonResult(new { url, nome = arquivo.FileName });
    }

    private async Task<string> SalvarImagemAsync(IFormFile arquivo, string subpasta)
    {
        var pasta = Path.Combine(env.WebRootPath, "uploads", subpasta);
        Directory.CreateDirectory(pasta);

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        var nomeArmazenado = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = Path.Combine(pasta, nomeArmazenado);

        await using (var destino = System.IO.File.Create(caminhoCompleto))
        {
            await arquivo.CopyToAsync(destino);
        }

        return $"/uploads/{subpasta}/{nomeArmazenado}";
    }

    private async Task CarregarTagsSugeridasAsync()
    {
        var tagsDeItens = await db.Itens
            .Where(i => i.Tags != null && i.Tags != "")
            .Select(i => i.Tags!)
            .AsNoTracking()
            .ToListAsync();

        TagsSugeridas = tagsDeItens
            .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<string> GerarSlugUnicoAsync(string titulo)
    {
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

        return slug;
    }
}
