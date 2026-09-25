using System.ComponentModel.DataAnnotations;

namespace Projetar.Web.Models;

public enum ReferenciaTipo
{
    [Display(Name = "Artigo")]
    Artigo = 0,

    [Display(Name = "Vídeo")]
    Video = 1,

    [Display(Name = "Lei ou norma")]
    LeiOuNorma = 2,

    [Display(Name = "Outro")]
    Outro = 3,

    [Display(Name = "Livro")]
    Livro = 4,

    [Display(Name = "Projeto de lei")]
    ProjetoDeLei = 5,
}

/// <summary>Rótulo em português e classe de cor da tag, usados nos cards de referência.</summary>
public static class ReferenciaTipoExtensions
{
    public static string Rotulo(this ReferenciaTipo tipo)
    {
        var campo = typeof(ReferenciaTipo).GetField(tipo.ToString());
        var display = campo?.GetCustomAttributes(typeof(DisplayAttribute), false)
            .OfType<DisplayAttribute>()
            .FirstOrDefault();
        return display?.Name ?? tipo.ToString();
    }

    public static string ClasseTag(this ReferenciaTipo tipo) => tipo switch
    {
        ReferenciaTipo.Livro => "gbr-tag--livro",
        ReferenciaTipo.Artigo => "gbr-tag--artigo",
        ReferenciaTipo.ProjetoDeLei => "gbr-tag--lei",
        ReferenciaTipo.LeiOuNorma => "gbr-tag--lei",
        ReferenciaTipo.Video => "gbr-tag--video",
        _ => "gbr-tag--outro",
    };
}

/// <summary>Link externo (artigo, vídeo, lei) usado para fortalecer o argumento de um Item.</summary>
public class Referencia
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ReferenciaTipo Tipo { get; set; } = ReferenciaTipo.Outro;
    public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Mesma fila de moderação usada em ItemRevisao — só aparece publicamente quando Aprovada.</summary>
    public RevisaoStatus Status { get; set; } = RevisaoStatus.Pendente;
    public string? MotivoRejeicao { get; set; }
    public string? RevisadoPorUsuarioId { get; set; }
    public ApplicationUser? RevisadoPorUsuario { get; set; }
    public DateTimeOffset? DataModeracao { get; set; }
}
