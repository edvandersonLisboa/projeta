using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Mandamentos.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;

    [NotMapped]
    public string NomeCompleto => $"{Nome} {Sobrenome}".Trim();

    /// <summary>Opcional — não exigido no cadastro.</summary>
    public string? Telefone { get; set; }

    /// <summary>Opcional — não exigido no cadastro.</summary>
    public string? Cpf { get; set; }

    public string? Estado { get; set; }
    public string? Municipio { get; set; }
    public DateTimeOffset DataCadastro { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// True quando Estado e Município estão preenchidos. É esse o requisito
    /// (não o perfil "completo" como um todo) para liberar ações de escrita:
    /// propor edição, comentar, votar, referenciar, enviar documento.
    /// Login via Google não fornece Estado/Município, então o fluxo de
    /// ExternalLogin sempre manda para /Conta/CompletarPerfil antes de liberar.
    /// </summary>
    public bool PerfilCompleto { get; set; }
}
