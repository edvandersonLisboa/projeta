using System.ComponentModel.DataAnnotations;
using Mandamentos.Web.Models;
using Mandamentos.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mandamentos.Web.Pages.Conta;

public class RedefinirSenhaModel(
    UserManager<ApplicationUser> userManager,
    IPasswordResetService passwordReset) : PageModel
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

        [Required(ErrorMessage = "Informe a nova senha.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova senha")]
        public string NovaSenha { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nova senha")]
        [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmarNovaSenha { get; set; } = string.Empty;
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
            ModelState.AddModelError(string.Empty, "Código inválido.");
            return Page();
        }

        var (resultado, erroSenha) = await passwordReset.RedefinirAsync(usuario, Input.Codigo, Input.NovaSenha);
        if (resultado == RedefinicaoSenhaResultado.Sucesso)
        {
            TempData["MensagemSucesso"] = "Senha redefinida com sucesso. Entre com a nova senha.";
            return RedirectToPage("./Entrar");
        }

        ModelState.AddModelError(string.Empty, resultado switch
        {
            RedefinicaoSenhaResultado.CodigoExpirado => "Código expirado. Peça um novo abaixo.",
            RedefinicaoSenhaResultado.MuitasTentativas => "Muitas tentativas erradas. Peça um novo código.",
            RedefinicaoSenhaResultado.SenhaInvalida => erroSenha ?? "Senha inválida.",
            _ => "Código inválido.",
        });
        return Page();
    }

    public async Task<IActionResult> OnPostReenviarAsync()
    {
        var usuario = await userManager.FindByEmailAsync(Email);
        if (usuario is not null && usuario.EmailConfirmed)
        {
            await passwordReset.GerarEEnviarAsync(usuario);
        }

        Mensagem = "Se esse e-mail estiver cadastrado, um novo código foi enviado.";
        return Page();
    }
}
