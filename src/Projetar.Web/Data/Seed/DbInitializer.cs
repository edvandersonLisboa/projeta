using System.Text.Json;
using Projetar.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Data.Seed;

public static class DbInitializer
{
    private const string AdminEmail = "edvandersonlisboa123@gmail.com";
    private const string AdminRole = "Admin";
    private const string RevisorRole = "Revisor";

    public static async Task RunAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        await SeedMandamentosAsync(db, services);
        await SeedAdminAsync(services);
    }

    private static async Task SeedMandamentosAsync(ApplicationDbContext db, IServiceProvider services)
    {
        if (db.Mandamentos.Any())
        {
            return;
        }

        var env = services.GetRequiredService<IWebHostEnvironment>();
        var seedPath = Path.Combine(env.ContentRootPath, "Data", "Seed", "mandamentos-seed.json");
        var json = await File.ReadAllTextAsync(seedPath);

        var seedData = JsonSerializer.Deserialize<List<MandamentoSeedDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? [];

        foreach (var dto in seedData)
        {
            var mandamento = new Mandamento
            {
                Id = dto.Id,
                Biblical = dto.Biblical,
                Secular = dto.Secular,
                Intro = dto.Intro,
                Ordem = dto.Ordem,
            };

            foreach (var itemDto in dto.Itens)
            {
                mandamento.Itens.Add(new Item
                {
                    Id = Guid.NewGuid(),
                    Slug = itemDto.Slug,
                    Titulo = itemDto.Titulo,
                    Corpo = itemDto.Corpo,
                    Ordem = itemDto.Ordem,
                    Status = ItemStatus.Original,
                    CriadoPorUsuarioId = null,
                });
            }

            db.Mandamentos.Add(mandamento);
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
