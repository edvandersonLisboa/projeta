using Projetar.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Data.Seed;

public static class DbInitializer
{
    private const string AdminEmail = "edvandersonlisboa123@gmail.com";
    private const string AdminRole = "Admin";
    private const string RevisorRole = "Revisor";

    /// <summary>Catálogo inicial de tags sugeridas — temas comuns de política pública no Brasil, pra dar
    /// um ponto de partida no modal de tags em vez de começar totalmente vazio.</summary>
    private static readonly string[] TagsCatalogoSeed =
    [
        "Saúde",
        "Educação",
        "Segurança Pública",
        "Meio Ambiente",
        "Economia",
        "Direitos Humanos",
        "Transparência",
        "Infraestrutura",
        "Cultura",
        "Tecnologia",
        "Trabalho e Renda",
        "Habitação",
    ];

    public static async Task RunAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        await SeedTagsCatalogoAsync(db);
        await SeedAdminAsync(services);
    }

    private static async Task SeedTagsCatalogoAsync(ApplicationDbContext db)
    {
        if (db.TagsCatalogo.Any())
        {
            return;
        }

        for (var i = 0; i < TagsCatalogoSeed.Length; i++)
        {
            db.TagsCatalogo.Add(new TagCatalogo { Nome = TagsCatalogoSeed[i], Ordem = i + 1 });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRole));
        }

        if (!await roleManager.RoleExistsAsync(RevisorRole))
        {
            await roleManager.CreateAsync(new IdentityRole(RevisorRole));
        }

        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is null)
        {
            // Sem senha local definida: o admin faz o primeiro acesso via login Google
            // com este mesmo email. Se preferir senha local, defina-a manualmente depois.
            admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                Nome = "Admin",
                Sobrenome = "Sistema",
                PerfilCompleto = false,
            };

            var result = await userManager.CreateAsync(admin);
            if (!result.Succeeded)
            {
                return;
            }
        }
        else if (string.IsNullOrWhiteSpace(admin.Nome))
        {
            // Conta seedada antes da coluna Nome existir (renomeada a partir de NomeCompleto).
            admin.Nome = "Admin";
            admin.Sobrenome = "Sistema";
            await userManager.UpdateAsync(admin);
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            await userManager.AddToRoleAsync(admin, AdminRole);
        }
    }
}
