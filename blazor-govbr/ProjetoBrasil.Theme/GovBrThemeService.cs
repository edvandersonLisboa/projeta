using Microsoft.JSInterop;

namespace ProjetoBrasil.Theme;

/// <summary>
/// Serviço de tema (claro/escuro). Aplica o atributo data-theme no &lt;html&gt;,
/// persiste em localStorage via govbr-theme.js e notifica a UI (OnChange).
/// Registre em Program.cs:  builder.Services.AddScoped&lt;GovBrThemeService&gt;();
/// </summary>
public class GovBrThemeService
{
    private readonly IJSRuntime _js;
    public GovBrTheme Theme { get; private set; } = GovBrTheme.Claro;
    public bool IsDark => Theme == GovBrTheme.Escuro;
    public event Action? OnChange;

    public GovBrThemeService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync()
    {
        var t = await _js.InvokeAsync<string>("gbrTheme.restore");
        Theme = t == "escuro" ? GovBrTheme.Escuro : GovBrTheme.Claro;
        OnChange?.Invoke();
    }

    public async Task SetThemeAsync(GovBrTheme theme)
    {
        Theme = theme;
        await _js.InvokeVoidAsync("gbrTheme.set", theme == GovBrTheme.Escuro ? "escuro" : "claro");
        OnChange?.Invoke();
    }

    public Task ToggleThemeAsync()
        => SetThemeAsync(IsDark ? GovBrTheme.Claro : GovBrTheme.Escuro);
}
