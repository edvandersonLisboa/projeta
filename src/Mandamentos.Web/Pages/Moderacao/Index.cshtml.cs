using System.ComponentModel.DataAnnotations;
using Mandamentos.Web.Data;
using Mandamentos.Web.Models;
using Mandamentos.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Mandamentos.Web.Pages.Moderacao;

[Authorize(Roles = "Admin,Revisor")]
public class IndexModel(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IMarkdownRenderer markdown) : PageModel
{
    public string Render(string corpo, FormatoTexto formato) => markdown.ToSafeHtml(corpo, formato);
    public List<ItemRevisao> Pendentes { get; set; } = [];
    public List<Referencia> ReferenciasPendentes { get; set; } = [];
    public List<Documento> DocumentosPendentes { get; set; } = [];
    public List<Item> ItensPropostos { get; set; } = [];

    [BindProperty]
    public RejeitarInput Rejeitar { get; set; } = new();

    [BindProperty]
    public RejeitarInput RejeitarReferencia { get; set; } = new();

    [BindProperty]
    public RejeitarInput RejeitarDocumento { get; set; } = new();

    [BindProperty]
    public RejeitarInput RejeitarItem { get; set; } = new();

    public class RejeitarInput
    {
        public Guid AlvoId { get; set; }

        [Required(ErrorMessage = "Informe o motivo da rejeição.")]
        public string Motivo { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        Pendentes = await db.ItemRevisoes
            .Where(r => r.Status == RevisaoStatus.Pendente)
            .Include(r => r.Item).ThenInclude(i => i!.Mandamento)
            .Include(r => r.AutorUsuario)
            .OrderBy(r => r.DataRevisao)
            .AsNoTracking()
            .ToListAsync();

        ReferenciasPendentes = await db.Referencias
            .Where(r => r.Status == RevisaoStatus.Pendente)
            .Include(r => r.Item).ThenInclude(i => i!.Mandamento)
            .Include(r => r.Usuario)
            .OrderBy(r => r.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

        DocumentosPendentes = await db.Documentos
            .Where(d => d.Status == RevisaoStatus.Pendente)
            .Include(d => d.Item).ThenInclude(i => i!.Mandamento)
            .Include(d => d.Usuario)
            .OrderBy(d => d.DataUpload)
            .AsNoTracking()
            .ToListAsync();

        ItensPropostos = await db.Itens
            .Where(i => i.Status == ItemStatus.PropostaComunidade)
            .Include(i => i.Mandamento)
            .Include(i => i.CriadoPorUsuario)
            .OrderBy(i => i.DataCriacao)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAprovarAsync(Guid id)
    {
        var revisao = await db.ItemRevisoes.Include(r => r.Item).FirstOrDefaultAsync(r => r.Id == id);
        if (revisao is null || revisao.Status != RevisaoStatus.Pendente || revisao.Item is null)
        {
            return RedirectToPage();
        }

        var moderador = await userManager.GetUserAsync(User);

        revisao.Item.Corpo = revisao.CorpoNovo;
        revisao.Item.FormatoCorpo = FormatoTexto.Html; // propostas de edição sempre chegam do editor rich text
        revisao.Item.DataAtualizacao = DateTimeOffset.UtcNow;
        revisao.Status = RevisaoStatus.Aprovada;
        revisao.RevisadoPorUsuarioId = moderador?.Id;
        revisao.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Edição aprovada e publicada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejeitarAsync()
    {
        if (string.IsNullOrWhiteSpace(Rejeitar.Motivo))
        {
            TempData["MensagemErro"] = "Informe o motivo da rejeição.";
            return RedirectToPage();
        }

        var revisao = await db.ItemRevisoes.FirstOrDefaultAsync(r => r.Id == Rejeitar.AlvoId);
        if (revisao is null || revisao.Status != RevisaoStatus.Pendente)
        {
            return RedirectToPage();
        }

        var moderador = await userManager.GetUserAsync(User);

        revisao.Status = RevisaoStatus.Rejeitada;
        revisao.MotivoRejeicao = Rejeitar.Motivo.Trim();
        revisao.RevisadoPorUsuarioId = moderador?.Id;
        revisao.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Edição rejeitada. O autor verá o motivo.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAprovarReferenciaAsync(Guid id)
    {
        var referencia = await db.Referencias.FirstOrDefaultAsync(r => r.Id == id);
        if (referencia is null || referencia.Status != RevisaoStatus.Pendente)
        {
            return RedirectToPage();
        }

        var moderador = await userManager.GetUserAsync(User);
        referencia.Status = RevisaoStatus.Aprovada;
        referencia.RevisadoPorUsuarioId = moderador?.Id;
        referencia.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
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

        var referencia = await db.Referencias.FirstOrDefaultAsync(r => r.Id == RejeitarReferencia.AlvoId);
        if (referencia is null || referencia.Status != RevisaoStatus.Pendente)
        {
            return RedirectToPage();
        }

        var moderador = await userManager.GetUserAsync(User);
        referencia.Status = RevisaoStatus.Rejeitada;
        referencia.MotivoRejeicao = RejeitarReferencia.Motivo.Trim();
        referencia.RevisadoPorUsuarioId = moderador?.Id;
        referencia.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Referência rejeitada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAprovarDocumentoAsync(Guid id)
    {
        var documento = await db.Documentos.FirstOrDefaultAsync(d => d.Id == id);
        if (documento is null || documento.Status != RevisaoStatus.Pendente)
        {
            return RedirectToPage();
        }

        var moderador = await userManager.GetUserAsync(User);
        documento.Status = RevisaoStatus.Aprovada;
        documento.RevisadoPorUsuarioId = moderador?.Id;
        documento.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
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

        var documento = await db.Documentos.FirstOrDefaultAsync(d => d.Id == RejeitarDocumento.AlvoId);
        if (documento is null || documento.Status != RevisaoStatus.Pendente)
        {
            return RedirectToPage();
        }

        var moderador = await userManager.GetUserAsync(User);
        documento.Status = RevisaoStatus.Rejeitada;
        documento.MotivoRejeicao = RejeitarDocumento.Motivo.Trim();
        documento.RevisadoPorUsuarioId = moderador?.Id;
        documento.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
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

        var moderador = await userManager.GetUserAsync(User);
        item.Status = ItemStatus.Aprovado;
        item.RevisadoPorUsuarioId = moderador?.Id;
        item.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
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

        var moderador = await userManager.GetUserAsync(User);
        item.Status = ItemStatus.Arquivado;
        item.MotivoRejeicao = RejeitarItem.Motivo.Trim();
        item.RevisadoPorUsuarioId = moderador?.Id;
        item.DataModeracao = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Item rejeitado. O autor verá o motivo.";
        return RedirectToPage();
    }
}
