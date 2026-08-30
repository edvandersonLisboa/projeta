using Mandamentos.Web.Data;
using Mandamentos.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mandamentos.Web.Services;

public class PasswordResetService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender) : IPasswordResetService
{
    public async Task GerarEEnviarAsync(ApplicationUser usuario)
    {
        var pendentes = await db.PasswordResetCodes
            .Where(c => c.UsuarioId == usuario.Id && !c.Usado)
            .ToListAsync();
        foreach (var pendente in pendentes)
        {
            pendente.Usado = true;
        }

        var codigo = CodigoVerificacao.Gerar();
        db.PasswordResetCodes.Add(new PasswordResetCode
        {
            UsuarioId = usuario.Id,
            CodigoHash = CodigoVerificacao.Hash(usuario.Id, codigo),
            ExpiraEm = DateTimeOffset.UtcNow.Add(CodigoVerificacao.Validade),
        });
        await db.SaveChangesAsync();

        var corpo = $"""
            <p>Olá, {usuario.Nome}!</p>
            <p>Recebemos um pedido para redefinir sua senha. Seu código é:</p>
            <p style="font-size:28px;font-weight:bold;letter-spacing:4px;">{codigo}</p>
            <p>Ele expira em {CodigoVerificacao.Validade.TotalMinutes:0} minutos. Se você não pediu essa redefinição, ignore este e-mail — sua senha continua a mesma.</p>
            """;
        await emailSender.SendEmailAsync(usuario.Email!, "Redefinição de senha — 10 Mandamentos", corpo);
    }

    public async Task<(RedefinicaoSenhaResultado Resultado, string? ErroSenha)> RedefinirAsync(
        ApplicationUser usuario, string codigoInformado, string novaSenha)
    {
        var registro = await db.PasswordResetCodes
            .Where(c => c.UsuarioId == usuario.Id && !c.Usado)
            .OrderByDescending(c => c.CriadoEm)
            .FirstOrDefaultAsync();

        if (registro is null)
        {
            return (RedefinicaoSenhaResultado.CodigoInvalido, null);
        }

        if (registro.ExpiraEm < DateTimeOffset.UtcNow)
        {
            registro.Usado = true;
            await db.SaveChangesAsync();
            return (RedefinicaoSenhaResultado.CodigoExpirado, null);
        }

        if (registro.Tentativas >= CodigoVerificacao.MaxTentativas)
        {
            registro.Usado = true;
            await db.SaveChangesAsync();
            return (RedefinicaoSenhaResultado.MuitasTentativas, null);
        }

        if (CodigoVerificacao.Hash(usuario.Id, codigoInformado.Trim()) != registro.CodigoHash)
        {
            registro.Tentativas++;
            var resultado = registro.Tentativas >= CodigoVerificacao.MaxTentativas
                ? RedefinicaoSenhaResultado.MuitasTentativas
                : RedefinicaoSenhaResultado.CodigoInvalido;
            if (resultado == RedefinicaoSenhaResultado.MuitasTentativas)
            {
                registro.Usado = true;
            }
            await db.SaveChangesAsync();
            return (resultado, null);
        }

        var tokenReset = await userManager.GeneratePasswordResetTokenAsync(usuario);
        var resultadoSenha = await userManager.ResetPasswordAsync(usuario, tokenReset, novaSenha);
        if (!resultadoSenha.Succeeded)
        {
            var erro = string.Join(" ", resultadoSenha.Errors.Select(e => e.Description));
            return (RedefinicaoSenhaResultado.SenhaInvalida, erro);
        }

        registro.Usado = true;
        await db.SaveChangesAsync();
        return (RedefinicaoSenhaResultado.Sucesso, null);
    }
}
