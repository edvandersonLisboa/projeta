namespace Projetar.Web.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string destinatario, string assunto, string corpoHtml);
}
