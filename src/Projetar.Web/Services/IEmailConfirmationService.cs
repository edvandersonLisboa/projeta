using Projetar.Web.Models;

namespace Projetar.Web.Services;

public enum ConfirmacaoCodigoResultado
{
    Sucesso,
    CodigoInvalido,
    CodigoExpirado,
    MuitasTentativas,
}

public interface IEmailConfirmationService
{
    /// <summary>Gera um código de 6 dígitos, invalida códigos anteriores e envia por e-mail.</summary>
    Task GerarEEnviarAsync(ApplicationUser usuario);

    /// <summary>Confirma o código informado. Em caso de sucesso, marca o e-mail do usuário como confirmado.</summary>
    Task<ConfirmacaoCodigoResultado> ConfirmarAsync(ApplicationUser usuario, string codigoInformado);
}
