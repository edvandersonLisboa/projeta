using System.ComponentModel.DataAnnotations;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projetar.Web.Pages.Conta;

public class ConfirmarCodigoModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailConfirmationService emailConfirmation) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? Mensagem { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Informe o código.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "O código tem 6 dígitos.")]
        [Display(Name = "Código de 6 dígitos")]
        public string Codigo { get; set; } = string.Empty;
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

        var usuario = await userManager.FindByEmailAsync(Email);
        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Conta não encontrada.");
            return Page();
        }

        var resultado = await emailConfirmation.ConfirmarAsync(usuario, Input.Codigo);
        if (resultado == ConfirmacaoCodigoResultado.Sucesso)
        {
            await signInManager.SignInAsync(usuario, isPersistent: false);
            return usuario.PerfilCompleto ? RedirectToPage("/Index") : RedirectToPage("./CompletarPerfil");
        }

        ModelState.AddModelError(string.Empty, resultado switch
        {
            ConfirmacaoCodigoResultado.CodigoExpirado => "Código expirado. Peça um novo abaixo.",
            ConfirmacaoCodigoResultado.MuitasTentativas => "Muitas tentativas erradas. Peça um novo código.",
            _ => "Código inválido.",
        });
        return Page();
    }

    public async Task<IActionResult> OnPostReenviarAsync()
    {
        var usuario = await userManager.FindByEmailAsync(Email);
        if (usuario is not null && !usuario.EmailConfirmed)
        {
            await emailConfirmation.GerarEEnviarAsync(usuario);
        }

        Mensagem = "Um novo código foi enviado para o seu e-mail.";
        return Page();
    }
}
