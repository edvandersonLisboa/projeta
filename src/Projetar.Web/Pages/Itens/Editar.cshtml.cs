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

namespace Projetar.Web.Pages.Itens;

[Authorize]
public class EditarModel(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    INotificacaoService notificacoes,
    IPermissaoService permissoes,
    IWebHostEnvironment env) : PageModel
{
    private static readonly string[] ExtensoesBannerPermitidas = [".jpg", ".jpeg", ".png", ".webp"];
    private const long TamanhoMaximoBannerBytes = 5 * 1024 * 1024;

    public Item ItemAtual { get; set; } = null!;
    public bool EhAdmin { get; set; }
    public BannerSugestao? BannerPendente { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? NovoBanner { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "O texto não pode ficar vazio.")]
        public string CorpoNovo { get; set; } = string.Empty;

        [Display(Name = "O que mudou e por quê (opcional)")]
        public string? ComentarioDaMudanca { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var item = await db.Itens.Include(i => i.Principio).FirstOrDefaultAsync(i => i.Slug == slug);
        if (item is null)
        {
            return NotFound();
        }

        ItemAtual = item;
        EhAdmin = User.IsInRole("Admin");
        Input.CorpoNovo = item.Corpo;

        if (EhAdmin)
        {
            BannerPendente = await db.BannerSugestoes
                .Where(b => b.ItemId == item.Id && b.Status == RevisaoStatus.Pendente)
                .OrderByDescending(b => b.DataCriacao)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null || !usuario.PerfilCompleto)
        {
            return RedirectToPage("/Conta/CompletarPerfil");
        }

        var item = await db.Itens.Include(i => i.Principio).FirstOrDefaultAsync(i => i.Slug == slug);
        if (item is null)
        {
            return NotFound();
        }

        ItemAtual = item;
        EhAdmin = User.IsInRole("Admin");

        if (RichTextUtils.EhVazio(Input.CorpoNovo))
        {
            ModelState.AddModelError("Input.CorpoNovo", "O texto não pode ficar vazio.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ehModeradorDoItem = User.IsInRole("Admin") || await permissoes.PodeModerarItemAsync(usuario.Id, item.Id);

        var revisao = new ItemRevisao
        {
            ItemId = item.Id,
            AutorUsuarioId = usuario.Id,
            CorpoAnterior = item.Corpo,
            CorpoNovo = Input.CorpoNovo,
            ComentarioDaMudanca = string.IsNullOrWhiteSpace(Input.ComentarioDaMudanca) ? null : Input.ComentarioDaMudanca,
            Status = ehModeradorDoItem ? RevisaoStatus.Aprovada : RevisaoStatus.Pendente,
        };

        if (ehModeradorDoItem)
        {
            revisao.RevisadoPorUsuarioId = usuario.Id;
            revisao.DataModeracao = DateTimeOffset.UtcNow;
            item.Corpo = Input.CorpoNovo;
            item.DataAtualizacao = DateTimeOffset.UtcNow;
        }

        db.ItemRevisoes.Add(revisao);
        await db.SaveChangesAsync();

        if (ehModeradorDoItem)
        {
            TempData["MensagemSucesso"] = "Sua edição foi aplicada direto, já que você modera este item.";
        }
        else
        {
            await notificacoes.NotificarModeradoresAsync(
                TipoNotificacao.EdicaoTextoProposta,
                "Nova edição de texto para aprovar",
                $"{usuario.Nome} propôs uma edição no texto de \"{item.Titulo}\".",
                item.Id, null, "/Moderacao/Index", usuario.Id);

            TempData["MensagemSucesso"] = "Sua edição foi enviada e está aguardando revisão.";
        }
        return RedirectToPage("./Index", new { slug });
    }

    /// <summary>Só administradores enviam banner — o arquivo é salvo na hora, mas só troca o banner publicado depois de aprovado na moderação.</summary>
    public async Task<IActionResult> OnPostBannerAsync(string slug)
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

        if (NovoBanner is null || NovoBanner.Length == 0)
        {
            TempData["MensagemErroBanner"] = "Escolha uma imagem.";
            return RedirectToPage("./Editar", new { slug });
        }

        if (NovoBanner.Length > TamanhoMaximoBannerBytes)
        {
            TempData["MensagemErroBanner"] = "Imagem maior que 5 MB.";
            return RedirectToPage("./Editar", new { slug });
        }

        var extensao = Path.GetExtension(NovoBanner.FileName).ToLowerInvariant();
        if (!ExtensoesBannerPermitidas.Contains(extensao))
        {
            TempData["MensagemErroBanner"] = "Formato não permitido. Use JPG, PNG ou WEBP.";
            return RedirectToPage("./Editar", new { slug });
        }

        var pastaBanners = Path.Combine(env.WebRootPath, "uploads", "banners");
        Directory.CreateDirectory(pastaBanners);

        var nomeArmazenado = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = Path.Combine(pastaBanners, nomeArmazenado);
        await using (var destino = System.IO.File.Create(caminhoCompleto))
        {
            await NovoBanner.CopyToAsync(destino);
        }

        var usuario = await userManager.GetUserAsync(User);
        var ehModeradorDoItem = User.IsInRole("Admin") || await permissoes.PodeModerarItemAsync(usuario!.Id, item.Id);

        // Se já existia uma sugestão pendente pra este item, substitui em vez de empilhar.
        var pendenteAnterior = await db.BannerSugestoes.FirstOrDefaultAsync(b => b.ItemId == item.Id && b.Status == RevisaoStatus.Pendente);
        if (pendenteAnterior is not null)
        {
            RemoverArquivoBanner(pendenteAnterior.CaminhoImagem);
            db.BannerSugestoes.Remove(pendenteAnterior);
        }

        var caminhoPublico = $"/uploads/banners/{nomeArmazenado}";
        var sugestao = new BannerSugestao
        {
            ItemId = item.Id,
            UsuarioId = usuario!.Id,
            CaminhoImagem = caminhoPublico,
            Status = ehModeradorDoItem ? RevisaoStatus.Aprovada : RevisaoStatus.Pendente,
        };

        if (ehModeradorDoItem)
        {
            sugestao.RevisadoPorUsuarioId = usuario.Id;
            sugestao.DataModeracao = DateTimeOffset.UtcNow;
            RemoverArquivoBanner(item.BannerImagem);
            item.BannerImagem = caminhoPublico;
            item.DataAtualizacao = DateTimeOffset.UtcNow;
        }

        db.BannerSugestoes.Add(sugestao);
        await db.SaveChangesAsync();

        if (ehModeradorDoItem)
        {
            TempData["MensagemSucesso"] = "Banner atualizado — você modera este item.";
        }
        else
        {
            await notificacoes.NotificarModeradoresAsync(
                TipoNotificacao.BannerProposto,
                "Novo banner para aprovar",
                $"{usuario.Nome} propôs um novo banner para \"{item.Titulo}\".",
                item.Id, null, "/Moderacao/Index", usuario.Id);

            TempData["MensagemSucesso"] = "Banner enviado — aguardando aprovação de um revisor.";
        }
        return RedirectToPage("./Editar", new { slug });
    }

    /// <summary>Só administradores removem o banner, voltando ao banner padrão (sem imagem) — ação imediata, é uma remoção, não uma proposta de conteúdo.</summary>
    public async Task<IActionResult> OnPostRemoverBannerAsync(string slug)
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

        RemoverArquivoBanner(item.BannerImagem);
        item.BannerImagem = null;
        item.DataAtualizacao = DateTimeOffset.UtcNow;

        var pendente = await db.BannerSugestoes.FirstOrDefaultAsync(b => b.ItemId == item.Id && b.Status == RevisaoStatus.Pendente);
        if (pendente is not null)
        {
            RemoverArquivoBanner(pendente.CaminhoImagem);
            db.BannerSugestoes.Remove(pendente);
        }

        await db.SaveChangesAsync();

        var admin = await userManager.GetUserAsync(User);
        await notificacoes.NotificarModeradoresAsync(
            TipoNotificacao.BannerRemovido,
            "Banner removido",
            $"{admin?.Nome ?? "Um admin"} removeu o banner de \"{item.Titulo}\".",
            item.Id, null, $"/Itens/{item.Slug}", admin?.Id);

        TempData["MensagemSucesso"] = "Banner removido.";
        return RedirectToPage("./Editar", new { slug });
    }

    /// <summary>Cancela a sugestão de banner pendente sem mexer no banner já publicado.</summary>
    public async Task<IActionResult> OnPostCancelarBannerPendenteAsync(string slug)
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

        var pendente = await db.BannerSugestoes.FirstOrDefaultAsync(b => b.ItemId == item.Id && b.Status == RevisaoStatus.Pendente);
        if (pendente is not null)
        {
            RemoverArquivoBanner(pendente.CaminhoImagem);
            db.BannerSugestoes.Remove(pendente);
            await db.SaveChangesAsync();
        }

        TempData["MensagemSucesso"] = "Sugestão de banner cancelada.";
        return RedirectToPage("./Editar", new { slug });
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
