using System.ComponentModel.DataAnnotations;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projetar.Web.Pages.Conta;

public class RegistrarModel(
    UserManager<ApplicationUser> userManager,
    IEmailConfirmationService emailConfirmation) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Informe seu nome.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu sobrenome.")]
        public string Sobrenome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe uma senha.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        [Compare(nameof(Senha), ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmarSenha { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuario = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            Nome = Input.Nome,
            Sobrenome = Input.Sobrenome,
        };

        var resultado = await userManager.CreateAsync(usuario, Input.Senha);
        if (!resultado.Succeeded)
        {
            foreach (var erro in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, TraduzErro(erro));
            }
            return Page();
        }

        await emailConfirmation.GerarEEnviarAsync(usuario);
        return RedirectToPage("./ConfirmarCodigo", new { email = Input.Email });
    }

    private static string TraduzErro(IdentityError erro) => erro.Code switch
    {
        "DuplicateEmail" or "DuplicateUserName" => "Já existe uma conta cadastrada com esse e-mail.",
        "PasswordTooShort" => "A senha é muito curta.",
        "PasswordRequiresNonAlphanumeric" => "A senha precisa de pelo menos um caractere especial.",
        "PasswordRequiresDigit" => "A senha precisa de pelo menos um número.",
        "PasswordRequiresUpper" => "A senha precisa de pelo menos uma letra maiúscula.",
        "PasswordRequiresLower" => "A senha precisa de pelo menos uma letra minúscula.",
        _ => erro.Description,
    };
}
