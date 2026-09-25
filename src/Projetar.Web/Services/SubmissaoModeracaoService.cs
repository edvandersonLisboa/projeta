using Projetar.Web.Data;
using Projetar.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Services;

public class SubmissaoModeracaoService(ApplicationDbContext db) : ISubmissaoModeracaoService
{
    public async Task<List<SubmissaoResumo>> ListarPorAutorAsync(string usuarioId)
    {
        var lista = new List<SubmissaoResumo>();

        var itens = await db.Itens
            .Where(i => i.CriadoPorUsuarioId == usuarioId)
            .Include(i => i.Mandamento)
            .AsNoTracking()
            .ToListAsync();
        lista.AddRange(itens.Where(i => i.Mandamento is not null).Select(MapearItem));

        var revisoes = await db.ItemRevisoes
            .Where(r => r.AutorUsuarioId == usuarioId)
            .Include(r => r.Item)
            .AsNoTracking()
            .ToListAsync();
        lista.AddRange(revisoes.Where(r => r.Item is not null).Select(MapearRevisao));

        var referencias = await db.Referencias
            .Where(r => r.UsuarioId == usuarioId)
            .Include(r => r.Item)
            .AsNoTracking()
            .ToListAsync();
        lista.AddRange(referencias.Where(r => r.Item is not null).Select(MapearReferencia));

        var documentos = await db.Documentos
            .Where(d => d.UsuarioId == usuarioId)
            .Include(d => d.Item)
            .AsNoTracking()
            .ToListAsync();
        lista.AddRange(documentos.Where(d => d.Item is not null).Select(MapearDocumento));

        var tags = await db.TagSugestoes
            .Where(t => t.UsuarioId == usuarioId)
            .Include(t => t.Item)
            .AsNoTracking()
            .ToListAsync();
        lista.AddRange(tags.Where(t => t.Item is not null).Select(MapearTag));

        var banners = await db.BannerSugestoes
            .Where(b => b.UsuarioId == usuarioId)
            .Include(b => b.Item)
            .AsNoTracking()
            .ToListAsync();
        lista.AddRange(banners.Where(b => b.Item is not null).Select(MapearBanner));

        await PreencherContagemDeMensagensAsync(lista);

        return lista.OrderByDescending(s => s.DataCriacao).ToList();
    }

    public async Task<List<SubmissaoResumo>> ListarComConversaAsync()
    {
        var alvos = await db.MensagensModeracao
            .Select(m => new { m.TipoAlvo, m.AlvoId })
            .Distinct()
            .ToListAsync();

        var lista = new List<SubmissaoResumo>();

        foreach (var grupo in alvos.GroupBy(a => a.TipoAlvo))
        {
            var ids = grupo.Select(a => a.AlvoId).ToList();
            switch (grupo.Key)
            {
                case TipoSubmissao.Item:
                    var itens = await db.Itens.Where(i => ids.Contains(i.Id)).Include(i => i.Mandamento).AsNoTracking().ToListAsync();
                    lista.AddRange(itens.Where(i => i.Mandamento is not null).Select(MapearItem));
                    break;
                case TipoSubmissao.EdicaoTexto:
                    var revisoes = await db.ItemRevisoes.Where(r => ids.Contains(r.Id)).Include(r => r.Item).AsNoTracking().ToListAsync();
                    lista.AddRange(revisoes.Where(r => r.Item is not null).Select(MapearRevisao));
                    break;
                case TipoSubmissao.Referencia:
                    var referencias = await db.Referencias.Where(r => ids.Contains(r.Id)).Include(r => r.Item).AsNoTracking().ToListAsync();
                    lista.AddRange(referencias.Where(r => r.Item is not null).Select(MapearReferencia));
                    break;
                case TipoSubmissao.Documento:
                    var documentos = await db.Documentos.Where(d => ids.Contains(d.Id)).Include(d => d.Item).AsNoTracking().ToListAsync();
                    lista.AddRange(documentos.Where(d => d.Item is not null).Select(MapearDocumento));
                    break;
                case TipoSubmissao.Tag:
                    var tags = await db.TagSugestoes.Where(t => ids.Contains(t.Id)).Include(t => t.Item).AsNoTracking().ToListAsync();
                    lista.AddRange(tags.Where(t => t.Item is not null).Select(MapearTag));
                    break;
                case TipoSubmissao.Banner:
                    var banners = await db.BannerSugestoes.Where(b => ids.Contains(b.Id)).Include(b => b.Item).AsNoTracking().ToListAsync();
                    lista.AddRange(banners.Where(b => b.Item is not null).Select(MapearBanner));
                    break;
            }
        }

        await PreencherContagemDeMensagensAsync(lista);

        return lista.OrderByDescending(s => s.UltimaMensagemEm).ToList();
    }

    public async Task<SubmissaoResumo?> ObterAsync(TipoSubmissao tipo, Guid alvoId)
    {
        SubmissaoResumo? resumo = tipo switch
        {
            TipoSubmissao.Item => await ObterItemAsync(alvoId),
            TipoSubmissao.EdicaoTexto => await ObterRevisaoAsync(alvoId),
            TipoSubmissao.Referencia => await ObterReferenciaAsync(alvoId),
            TipoSubmissao.Documento => await ObterDocumentoAsync(alvoId),
            TipoSubmissao.Tag => await ObterTagAsync(alvoId),
            TipoSubmissao.Banner => await ObterBannerAsync(alvoId),
            _ => null,
        };

        if (resumo is not null)
        {
            await PreencherContagemDeMensagensAsync([resumo]);
        }

        return resumo;
    }

    private async Task<SubmissaoResumo?> ObterItemAsync(Guid id)
    {
        var item = await db.Itens.Include(i => i.Mandamento).AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return item?.Mandamento is null ? null : MapearItem(item);
    }

    private async Task<SubmissaoResumo?> ObterRevisaoAsync(Guid id)
    {
        var revisao = await db.ItemRevisoes.Include(r => r.Item).AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        return revisao?.Item is null ? null : MapearRevisao(revisao);
    }

    private async Task<SubmissaoResumo?> ObterReferenciaAsync(Guid id)
    {
        var referencia = await db.Referencias.Include(r => r.Item).AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        return referencia?.Item is null ? null : MapearReferencia(referencia);
    }

    private async Task<SubmissaoResumo?> ObterDocumentoAsync(Guid id)
    {
        var documento = await db.Documentos.Include(d => d.Item).AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        return documento?.Item is null ? null : MapearDocumento(documento);
    }

    private async Task<SubmissaoResumo?> ObterTagAsync(Guid id)
    {
        var tag = await db.TagSugestoes.Include(t => t.Item).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        return tag?.Item is null ? null : MapearTag(tag);
    }

    private async Task<SubmissaoResumo?> ObterBannerAsync(Guid id)
    {
        var banner = await db.BannerSugestoes.Include(b => b.Item).AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        return banner?.Item is null ? null : MapearBanner(banner);
    }

    private async Task PreencherContagemDeMensagensAsync(List<SubmissaoResumo> lista)
    {
        if (lista.Count == 0)
        {
            return;
        }

        var alvoIds = lista.Select(s => s.AlvoId).ToList();
        var mensagens = await db.MensagensModeracao
            .Where(m => alvoIds.Contains(m.AlvoId))
            .AsNoTracking()
            .ToListAsync();

        var agrupado = mensagens
            .GroupBy(m => m.AlvoId)
            .ToDictionary(g => g.Key, g => (Quantidade: g.Count(), Ultima: g.Max(m => m.DataCriacao)));

        foreach (var submissao in lista)
        {
            if (agrupado.TryGetValue(submissao.AlvoId, out var info))
            {
                submissao.QuantidadeMensagens = info.Quantidade;
                submissao.UltimaMensagemEm = info.Ultima;
            }
        }
    }

    private static (string rotulo, string situacao) SituacaoDe(RevisaoStatus status) => status switch
    {
        RevisaoStatus.Aprovada => ("Aprovada", "aprovado"),
        RevisaoStatus.Rejeitada => ("Rejeitada", "rejeitado"),
        _ => ("Pendente", "pendente"),
    };

    private static (string rotulo, string situacao) SituacaoDe(ItemStatus status) => status switch
    {
        ItemStatus.Aprovado => ("Aprovado", "aprovado"),
        ItemStatus.Arquivado => ("Rejeitado", "rejeitado"),
        _ => ("Pendente", "pendente"),
    };

    private static SubmissaoResumo MapearItem(Item item)
    {
        var (rotulo, situacao) = SituacaoDe(item.Status);
        return new SubmissaoResumo
        {
            Tipo = TipoSubmissao.Item,
            AlvoId = item.Id,
            ItemId = item.Id,
            ItemTitulo = item.Titulo,
            ItemSlug = item.Slug,
            TipoRotulo = "Item novo",
            Resumo = $"Item proposto para o princípio {item.Mandamento!.Id}",
            StatusRotulo = rotulo,
            Situacao = situacao,
            DataCriacao = item.DataCriacao,
            DataModeracao = item.DataModeracao,
            MotivoRejeicao = item.MotivoRejeicao,
            LinkAcao = $"/Itens/{item.Slug}",
            AutorUsuarioId = item.CriadoPorUsuarioId ?? string.Empty,
            RevisorUsuarioId = item.RevisadoPorUsuarioId,
        };
    }

    private static SubmissaoResumo MapearRevisao(ItemRevisao r)
    {
        var (rotulo, situacao) = SituacaoDe(r.Status);
        return new SubmissaoResumo
        {
            Tipo = TipoSubmissao.EdicaoTexto,
            AlvoId = r.Id,
            ItemId = r.ItemId,
            ItemTitulo = r.Item!.Titulo,
            ItemSlug = r.Item.Slug,
            TipoRotulo = "Edição de texto",
            Resumo = string.IsNullOrWhiteSpace(r.ComentarioDaMudanca) ? "Edição de texto" : r.ComentarioDaMudanca!,
            StatusRotulo = rotulo,
            Situacao = situacao,
            DataCriacao = r.DataRevisao,
            DataModeracao = r.DataModeracao,
            MotivoRejeicao = r.MotivoRejeicao,
            LinkAcao = $"/Itens/{r.Item.Slug}/Editar",
            AutorUsuarioId = r.AutorUsuarioId,
            RevisorUsuarioId = r.RevisadoPorUsuarioId,
        };
    }

    private static SubmissaoResumo MapearReferencia(Referencia r)
    {
        var (rotulo, situacao) = SituacaoDe(r.Status);
        return new SubmissaoResumo
        {
            Tipo = TipoSubmissao.Referencia,
            AlvoId = r.Id,
            ItemId = r.ItemId,
            ItemTitulo = r.Item!.Titulo,
            ItemSlug = r.Item.Slug,
            TipoRotulo = "Referência",
            Resumo = r.Titulo,
            StatusRotulo = rotulo,
            Situacao = situacao,
            DataCriacao = r.DataCriacao,
            DataModeracao = r.DataModeracao,
            MotivoRejeicao = r.MotivoRejeicao,
            LinkAcao = $"/Itens/{r.Item.Slug}#material",
            AutorUsuarioId = r.UsuarioId,
            RevisorUsuarioId = r.RevisadoPorUsuarioId,
        };
    }

    private static SubmissaoResumo MapearDocumento(Documento d)
    {
        var (rotulo, situacao) = SituacaoDe(d.Status);
        return new SubmissaoResumo
        {
            Tipo = TipoSubmissao.Documento,
            AlvoId = d.Id,
            ItemId = d.ItemId,
            ItemTitulo = d.Item!.Titulo,
            ItemSlug = d.Item.Slug,
            TipoRotulo = "Documento",
            Resumo = d.NomeOriginal,
            StatusRotulo = rotulo,
            Situacao = situacao,
            DataCriacao = d.DataUpload,
            DataModeracao = d.DataModeracao,
            MotivoRejeicao = d.MotivoRejeicao,
            LinkAcao = $"/Itens/{d.Item.Slug}#material",
            AutorUsuarioId = d.UsuarioId,
            RevisorUsuarioId = d.RevisadoPorUsuarioId,
        };
    }

    private static SubmissaoResumo MapearTag(TagSugestao t)
    {
        var (rotulo, situacao) = SituacaoDe(t.Status);
        return new SubmissaoResumo
        {
            Tipo = TipoSubmissao.Tag,
            AlvoId = t.Id,
            ItemId = t.ItemId,
            ItemTitulo = t.Item!.Titulo,
            ItemSlug = t.Item.Slug,
            TipoRotulo = "Tag",
            Resumo = t.Nome,
            StatusRotulo = rotulo,
            Situacao = situacao,
            DataCriacao = t.DataCriacao,
            DataModeracao = t.DataModeracao,
            MotivoRejeicao = t.MotivoRejeicao,
            LinkAcao = $"/Itens/{t.Item.Slug}#tags",
            AutorUsuarioId = t.UsuarioId,
            RevisorUsuarioId = t.RevisadoPorUsuarioId,
        };
    }

    private static SubmissaoResumo MapearBanner(BannerSugestao b)
    {
        var (rotulo, situacao) = SituacaoDe(b.Status);
        return new SubmissaoResumo
        {
            Tipo = TipoSubmissao.Banner,
            AlvoId = b.Id,
            ItemId = b.ItemId,
            ItemTitulo = b.Item!.Titulo,
            ItemSlug = b.Item.Slug,
            TipoRotulo = "Banner",
            Resumo = "Banner proposto",
            StatusRotulo = rotulo,
            Situacao = situacao,
            DataCriacao = b.DataCriacao,
            DataModeracao = b.DataModeracao,
            MotivoRejeicao = b.MotivoRejeicao,
            LinkAcao = $"/Itens/{b.Item.Slug}/Editar",
            AutorUsuarioId = b.UsuarioId,
            RevisorUsuarioId = b.RevisadoPorUsuarioId,
        };
    }
}
