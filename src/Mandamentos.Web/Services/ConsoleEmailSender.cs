using Microsoft.Extensions.Logging;

namespace Mandamentos.Web.Services;

/// <summary>
/// Fallback para ambientes sem SMTP configurado (dev local): grava o e-mail no log
/// em vez de enviar. Permite testar o fluxo de confirmação sem um provedor real.
/// </summary>
public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(string destinatario, string assunto, string corpoHtml)
    {
        logger.LogWarning(
            "SMTP não configurado — e-mail não enviado de verdade.\nPara: {Destinatario}\nAssunto: {Assunto}\nCorpo:\n{Corpo}",
            destinatario, assunto, corpoHtml);
        return Task.CompletedTask;
    }
}
