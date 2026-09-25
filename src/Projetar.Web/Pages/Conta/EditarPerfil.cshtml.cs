using System.ComponentModel.DataAnnotations;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projetar.Web.Pages.Conta;

/// <summary>Ao contrário de CompletarPerfil (etapa única de onboarding, se tranca depois de preenchida),
/// esta página fica sempre disponível pra revisar dados e trocar a senha.</summary>
[Authorize]
public class EditarPerfilModel(UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public PerfilInputModel Input { get; set; } = new();

    [BindProperty]
    public SenhaInputModel Senha { get; set; } = new();

    public IReadOnlyList<(string Sigla, string Nome)> Estados => EstadosBrasil.Todos;

    public string? Email { get; set; }

    /// <summary>Login só por Google (sem senha local) não tem "senha atual" pra conferir —
    /// nesse caso o formulário pula direto pra definir a primeira senha.</summary>
    public bool TemSenhaLocal { get; set; }

    public class PerfilInputModel
    {
        [Required(ErrorMessage = "Informe seu nome.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu sobrenome.")]
        public string Sobrenome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione seu estado.")]
        public string Estado { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu município.")]
        [Display(Name = "Município")]
        public string Municipio { get; set; } = string.Empty;

        [Display(Name = "Telefone (opcional)")]
        public string? Telefone { get; set; }

        [Display(Name = "CPF (opcional)")]
        public string? Cpf { get; set; }
    }

    public class SenhaInputModel
    {
        [Display(Name = "Senha atual")]
        public string? SenhaAtual { get; set; }

        [Required(ErrorMessage = "Informe a nova senha.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        [Display(Name = "Nova senha")]
        public string NovaSenha { get; set; } = string.Empty;

        [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não coincidem.")]
        [Display(Name = "Confirmar nova senha")]
        public string ConfirmarNovaSenha { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return RedirectToPage("./Entrar");
        }

        await CarregarAsync(usuario);
        return Page();
    }

    private async Task CarregarAsync(ApplicationUser usuario)
    {
        Email = usuario.Email;
        TemSenhaLocal = await userManager.HasPasswordAsync(usuario);

        Input = new PerfilInputModel
        {
            Nome = usuario.Nome,
            Sobrenome = usuario.Sobrenome,
            Estado = usuario.Estado ?? string.Empty,
            Municipio = usuario.Municipio ?? string.Empty,
            Telefone = usuario.Telefone,
            Cpf = usuario.Cpf,
        };
    }

    public async Task<IActionResult> OnPostSalvarPerfilAsync()
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return RedirectToPage("./Entrar");
        }

        DescartarValidacaoDeOutrosFormularios(nameof(Input));
        if (!ModelState.IsValid)
        {
            await CarregarAsync(usuario);
            return Page();
        }

        usuario.Nome = Input.Nome.Trim();
        usuario.Sobrenome = Input.Sobrenome.Trim();
        usuario.Estado = Input.Estado;
        usuario.Municipio = Input.Municipio.Trim();
        usuario.Telefone = string.IsNullOrWhiteSpace(Input.Telefone) ? null : Input.Telefone.Trim();
        usuario.Cpf = string.IsNullOrWhiteSpace(Input.Cpf) ? null : Input.Cpf.Trim();
        usuario.PerfilCompleto = true;

        await userManager.UpdateAsync(usuario);

        TempData["MensagemSucesso"] = "Perfil atualizado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTrocarSenhaAsync()
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return RedirectToPage("./Entrar");
        }

        DescartarValidacaoDeOutrosFormularios(nameof(Senha));

        var temSenha = await userManager.HasPasswordAsync(usuario);
        if (temSenha && string.IsNullOrWhiteSpace(Senha.SenhaAtual))
        {
            ModelState.AddModelError("Senha.SenhaAtual", "Informe sua senha atual.");
        }

        if (!ModelState.IsValid)
        {
            await CarregarAsync(usuario);
            return Page();
        }

        var resultado = temSenha
            ? await userManager.ChangePasswordAsync(usuario, Senha.SenhaAtual!, Senha.NovaSenha)
            : await userManager.AddPasswordAsync(usuario, Senha.NovaSenha);

        if (!resultado.Succeeded)
        {
            foreach (var erro in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, TraduzErro(erro));
            }
            await CarregarAsync(usuario);
            return Page();
        }

        TempData["MensagemSucesso"] = "Senha atualizada.";
        return RedirectToPage();
    }

    private void DescartarValidacaoDeOutrosFormularios(params string[] prefixosParaManter)
    {
        var chaves = ModelState.Keys
            .Where(chave => !prefixosParaManter.Any(prefixo => chave.StartsWith(prefixo, StringComparison.Ordinal)))
            .ToList();
        foreach (var chave in chaves)
        {
            ModelState.Remove(chave);
        }
    }

    private static string TraduzErro(IdentityError erro) => erro.Code switch
    {
        "PasswordMismatch" => "Senha atual incorreta.",
        "PasswordTooShort" => "A senha é muito curta.",
        "PasswordRequiresNonAlphanumeric" => "A senha precisa de pelo menos um caractere especial.",
        "PasswordRequiresDigit" => "A senha precisa de pelo menos um número.",
        "PasswordRequiresUpper" => "A senha precisa de pelo menos uma letra maiúscula.",
        "PasswordRequiresLower" => "A senha precisa de pelo menos uma letra minúscula.",
        _ => erro.Description,
    };
}
