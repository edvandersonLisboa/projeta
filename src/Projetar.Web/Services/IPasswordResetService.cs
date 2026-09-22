using Projetar.Web.Models;

namespace Projetar.Web.Services;

public enum RedefinicaoSenhaResultado
{
    Sucesso,
    CodigoInvalido,
    CodigoExpirado,
    MuitasTentativas,
    SenhaInvalida,
}

public interface IPasswordResetService
{
    /// <summary>Gera um código de 6 dígitos, invalida códigos anteriores e envia por e-mail.</summary>
    Task GerarEEnviarAsync(ApplicationUser usuario);

    /// <summary>Valida o código e, em caso de sucesso, define a nova senha do usuário.</summary>
    Task<(RedefinicaoSenhaResultado Resultado, string? ErroSenha)> RedefinirAsync(
        ApplicationUser usuario, string codigoInformado, string novaSenha);
}
