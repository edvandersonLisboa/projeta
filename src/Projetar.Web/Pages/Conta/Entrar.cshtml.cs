using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Projetar.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projetar.Web.Pages.Conta;

public class EntrarModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? MensagemSucesso { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe sua senha.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;
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

        var resultado = await signInManager.PasswordSignInAsync(Input.Email, Input.Senha, isPersistent: false, lockoutOnFailure: true);
        if (resultado.Succeeded)
        {
            var usuario = await userManager.FindByEmailAsync(Input.Email);
            return RedirectPosLogin(usuario);
        }

        if (resultado.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Confirme seu e-mail antes de entrar. Verifique sua caixa de entrada ou peça um novo código.");
        }
        else if (resultado.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Muitas tentativas. Tente novamente em alguns minutos.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
        }

        return Page();
    }

    public IActionResult OnPostGoogle()
    {
        var redirectUrl = Url.Page("./Entrar", pageHandler: "GoogleCallback");
        var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        return new ChallengeResult("Google", properties);
    }

    public async Task<IActionResult> OnGetGoogleCallbackAsync()
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ModelState.AddModelError(string.Empty, "Não foi possível entrar com o Google. Tente novamente.");
            return Page();
        }

        var resultado = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (resultado.Succeeded)
        {
            var usuarioExistente = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            return RedirectPosLogin(usuarioExistente);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "O Google não retornou um e-mail válido.");
            return Page();
        }

        // Um e-mail = uma conta: se já existir cadastro (local ou de outro provedor),
        // vincula o login do Google a ele em vez de criar um segundo usuário.
        var usuario = await userManager.FindByEmailAsync(email);
        if (usuario is null)
        {
            usuario = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Nome = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
                Sobrenome = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
            };

            var criar = await userManager.CreateAsync(usuario);
            if (!criar.Succeeded)
            {
                foreach (var erro in criar.Errors)
                {
                    ModelState.AddModelError(string.Empty, erro.Description);
                }
                return Page();
            }
        }

        await userManager.AddLoginAsync(usuario, info);

        if (!usuario.EmailConfirmed)
        {
            usuario.EmailConfirmed = true;
            await userManager.UpdateAsync(usuario);
        }

        await signInManager.SignInAsync(usuario, isPersistent: false);
        return RedirectPosLogin(usuario);
    }

    private IActionResult RedirectPosLogin(ApplicationUser? usuario)
    {
        if (usuario is not null && !usuario.PerfilCompleto)
        {
            return RedirectToPage("./CompletarPerfil");
        }
        return RedirectToPage("/Index");
    }
}
