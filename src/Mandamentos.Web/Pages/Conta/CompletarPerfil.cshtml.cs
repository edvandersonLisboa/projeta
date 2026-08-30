using System.ComponentModel.DataAnnotations;
using Mandamentos.Web.Models;
using Mandamentos.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mandamentos.Web.Pages.Conta;

[Authorize]
public class CompletarPerfilModel(UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<(string Sigla, string Nome)> Estados => EstadosBrasil.Todos;

    public class InputModel
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

    public async Task<IActionResult> OnGetAsync()
    {
        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return RedirectToPage("./Entrar");
        }

        if (usuario.PerfilCompleto)
        {
            return RedirectToPage("/Index");
        }

        Input = new InputModel
        {
            Nome = usuario.Nome,
            Sobrenome = usuario.Sobrenome,
            Estado = usuario.Estado ?? string.Empty,
            Municipio = usuario.Municipio ?? string.Empty,
            Telefone = usuario.Telefone,
            Cpf = usuario.Cpf,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return RedirectToPage("./Entrar");
        }

        usuario.Nome = Input.Nome;
        usuario.Sobrenome = Input.Sobrenome;
        usuario.Estado = Input.Estado;
        usuario.Municipio = Input.Municipio;
        usuario.Telefone = string.IsNullOrWhiteSpace(Input.Telefone) ? null : Input.Telefone;
        usuario.Cpf = string.IsNullOrWhiteSpace(Input.Cpf) ? null : Input.Cpf;
        usuario.PerfilCompleto = true;

        await userManager.UpdateAsync(usuario);
        return RedirectToPage("/Index");
    }
}
