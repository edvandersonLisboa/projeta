using Projetar.Web.Data;
using Projetar.Web.Data.Seed;
using Projetar.Web.Models;
using Projetar.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Conta/Entrar";
    options.AccessDeniedPath = "/Index";
});

var authBuilder = builder.Services.AddAuthentication();
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
    });
}

builder.Services.AddScoped<IContentRenderer, ContentRenderer>();
builder.Services.AddScoped<IRevisaoDiffService, RevisaoDiffService>();
builder.Services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<INotificacaoService, NotificacaoService>();
builder.Services.AddScoped<ISubmissaoModeracaoService, SubmissaoModeracaoService>();
builder.Services.AddScoped<IPermissaoService, PermissaoService>();

var smtpHost = builder.Configuration["Smtp:Host"];
if (!string.IsNullOrWhiteSpace(smtpHost))
{
    builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
}

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// MapStaticAssets() abaixo só conhece os arquivos presentes em wwwroot no momento do build —
// banners enviados em runtime (wwwroot/uploads/...) precisam do middleware clássico pra serem servidos.
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Resumo leve do sino de notificações — consultado por polling (js/notificacoes-sino.js) pra
// atualizar o contador e a lista sem precisar recarregar a página.
app.MapGet("/api/notificacoes/resumo", async (
    System.Security.Claims.ClaimsPrincipal usuarioLogado,
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) =>
{
    var usuarioId = userManager.GetUserId(usuarioLogado);
    if (usuarioId is null)
    {
        return Results.Unauthorized();
    }

    var totalNaoLidas = await db.Notificacoes.CountAsync(n => n.UsuarioDestinoId == usuarioId && !n.Lida);

    var recentes = await db.Notificacoes
        .Where(n => n.UsuarioDestinoId == usuarioId && !n.Lida)
        .OrderByDescending(n => n.DataCriacao)
        .Take(5)
        .AsNoTracking()
        .ToListAsync();

    var itens = recentes.Select(n => new
    {
        n.Id,
        n.Titulo,
        n.Mensagem,
        Situacao = n.Tipo.Situacao(),
        Tempo = TempoRelativo.Formatar(n.DataCriacao),
        LinkUrl = $"/Notificacoes?handler=Abrir&id={n.Id}",
    });

    return Results.Ok(new { totalNaoLidas, itens });
}).RequireAuthorization();

// Sitemap dinâmico — só o que é público (princípio e item visíveis, item aprovado). Gerado a
// cada requisição a partir do banco em vez de arquivo estático, pra nunca ficar desatualizado.
app.MapGet("/sitemap.xml", async (HttpContext http, ApplicationDbContext db) =>
{
    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";

    var itens = await db.Itens
        .Where(i => i.Visivel && i.Principio!.Visivel &&
            (i.Status == ItemStatus.Original || i.Status == ItemStatus.Aprovado))
        .Select(i => new { i.Slug, i.DataAtualizacao })
        .AsNoTracking()
        .ToListAsync();

    var paginasEstaticas = new[] { "", "Sobre", "Ideias" };

    var xml = new System.Text.StringBuilder();
    xml.Append("""<?xml version="1.0" encoding="UTF-8"?>""");
    xml.Append("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

    foreach (var pagina in paginasEstaticas)
    {
        xml.Append($"<url><loc>{baseUrl}/{pagina}</loc><changefreq>weekly</changefreq></url>");
    }

    foreach (var item in itens)
    {
        xml.Append($"<url><loc>{baseUrl}/Itens/{item.Slug}</loc><lastmod>{item.DataAtualizacao:yyyy-MM-dd}</lastmod><changefreq>monthly</changefreq></url>");
    }

    xml.Append("</urlset>");

    return Results.Text(xml.ToString(), "application/xml");
});

using (var scope = app.Services.CreateScope())
{
    await DbInitializer.RunAsync(scope.ServiceProvider);
}

app.Run();
