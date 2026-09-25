using Projetar.Web.Models;

namespace Projetar.Web.Pages.Shared;

/// <summary>Dados pro partial _ChatThread — usado tanto em Moderação » Conversas quanto em Meu Histórico.</summary>
public class ChatThreadViewModel
{
    public List<MensagemModeracao> Mensagens { get; set; } = [];
    public string MeuUsuarioId { get; set; } = string.Empty;

    /// <summary>Nome do handler POST pro qual o formulário de resposta envia (ex: "Responder", "ResponderModeracao").</summary>
    public string FormHandler { get; set; } = "Responder";

    public TipoSubmissao Tipo { get; set; }
    public Guid AlvoId { get; set; }

    /// <summary>Campos extras a preservar no reenvio do formulário (ex: filtro, aba atual).</summary>
    public Dictionary<string, string> CamposExtras { get; set; } = [];

    public bool SomenteLeitura { get; set; }
    public string? NotaSomenteLeitura { get; set; }
    public string Placeholder { get; set; } = "Escreva uma mensagem…";
    public string TituloVazio { get; set; } = "Nenhuma mensagem ainda.";
    public string? DicaVazia { get; set; }
}
