using System.ComponentModel.DataAnnotations;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projetar.Web.Pages.Conta;

public class EsqueciSenhaModel(
    UserManager<ApplicationUser> userManager,
    IPasswordResetService passwordReset) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; } = string.Empty;
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

        var usuario = await userManager.FindByEmailAsync(Input.Email);
        if (usuario is not null && usuario.EmailConfirmed)
        {
            await passwordReset.GerarEEnviarAsync(usuario);
        }

        // Sempre segue para a mesma tela, exista ou não a conta — não dá pra descobrir
        // se um e-mail está cadastrado observando a resposta.
        return RedirectToPage("./RedefinirSenha", new { email = Input.Email });
    }
}
