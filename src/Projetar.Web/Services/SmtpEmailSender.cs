using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Projetar.Web.Services;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Projeta";
    public bool EnableSsl { get; set; } = true;
}

/// <summary>Envio real de e-mail via SMTP. Usado quando Smtp:Host está configurado (produção).</summary>
public class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendEmailAsync(string destinatario, string assunto, string corpoHtml)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            Credentials = new NetworkCredential(_options.User, _options.Password),
            EnableSsl = _options.EnableSsl,
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = assunto,
            Body = corpoHtml,
            IsBodyHtml = true,
        };
        message.To.Add(destinatario);

        await client.SendMailAsync(message);
    }
}
