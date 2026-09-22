using Projetar.Web.Data;
using Projetar.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Services;

public class EmailConfirmationService(ApplicationDbContext db, IEmailSender emailSender) : IEmailConfirmationService
{
    public async Task GerarEEnviarAsync(ApplicationUser usuario)
    {
        var pendentes = await db.EmailConfirmationCodes
            .Where(c => c.UsuarioId == usuario.Id && !c.Usado)
            .ToListAsync();
        foreach (var pendente in pendentes)
        {
            pendente.Usado = true;
        }

        var codigo = CodigoVerificacao.Gerar();
        db.EmailConfirmationCodes.Add(new EmailConfirmationCode
        {
            UsuarioId = usuario.Id,
            CodigoHash = CodigoVerificacao.Hash(usuario.Id, codigo),
            ExpiraEm = DateTimeOffset.UtcNow.Add(CodigoVerificacao.Validade),
        });
        await db.SaveChangesAsync();

        var corpo = $"""
            <p>Olá, {usuario.Nome}!</p>
            <p>Seu código de confirmação é:</p>
            <p style="font-size:28px;font-weight:bold;letter-spacing:4px;">{codigo}</p>
            <p>Ele expira em {CodigoVerificacao.Validade.TotalMinutes:0} minutos. Se você não pediu esse código, ignore este e-mail.</p>
            """;
        await emailSender.SendEmailAsync(usuario.Email!, "Confirme seu e-mail — 10 Mandamentos", corpo);
    }

    public async Task<ConfirmacaoCodigoResultado> ConfirmarAsync(ApplicationUser usuario, string codigoInformado)
    {
        var registro = await db.EmailConfirmationCodes
            .Where(c => c.UsuarioId == usuario.Id && !c.Usado)
            .OrderByDescending(c => c.CriadoEm)
            .FirstOrDefaultAsync();

        if (registro is null)
        {
            return ConfirmacaoCodigoResultado.CodigoInvalido;
        }

        if (registro.ExpiraEm < DateTimeOffset.UtcNow)
        {
            registro.Usado = true;
            await db.SaveChangesAsync();
            return ConfirmacaoCodigoResultado.CodigoExpirado;
        }

        if (registro.Tentativas >= CodigoVerificacao.MaxTentativas)
        {
            registro.Usado = true;
            await db.SaveChangesAsync();
            return ConfirmacaoCodigoResultado.MuitasTentativas;
        }

        if (CodigoVerificacao.Hash(usuario.Id, codigoInformado.Trim()) != registro.CodigoHash)
        {
            registro.Tentativas++;
            var resultado = registro.Tentativas >= CodigoVerificacao.MaxTentativas
                ? ConfirmacaoCodigoResultado.MuitasTentativas
                : ConfirmacaoCodigoResultado.CodigoInvalido;
            if (resultado == ConfirmacaoCodigoResultado.MuitasTentativas)
            {
                registro.Usado = true;
            }
            await db.SaveChangesAsync();
            return resultado;
        }

        registro.Usado = true;
        usuario.EmailConfirmed = true;
        await db.SaveChangesAsync();
        return ConfirmacaoCodigoResultado.Sucesso;
    }
}
