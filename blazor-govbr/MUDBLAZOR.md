# Projeto Brasil (gov.br) + MudBlazor

Tema nacional para MudBlazor: paleta **Azul `#1351B4` · Amarelo `#FFCB00` · Verde `#168821`**, tipografia **Raleway**, claro e escuro.

## 1. Copiar os arquivos

```
wwwroot/css/govbr.css            →  seu wwwroot/css/
wwwroot/js/govbr-theme.js        →  seu wwwroot/js/
ProjetoBrasil.Theme/             →  seu projeto (namespace ProjetoBrasil.Theme)
  ├─ GovBrMudTheme.cs            → MudTheme nativo (paleta + tipografia)
  ├─ GovBrTheme.cs               → enum Claro/Escuro
  └─ GovBrThemeService.cs        → serviço de troca de tema
```

## 2. Referenciar CSS, JS e fontes

No `App.razor` / `index.html` / `_Host.cshtml`, dentro do `<head>`:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=Raleway:wght@400;500;600;700;800&display=swap" rel="stylesheet">
<link rel="stylesheet" href="css/govbr.css" />
```

Antes de `</body>`, depois do script do Blazor:

```html
<script src="js/govbr-theme.js"></script>
```

Defina o tema inicial no `<html>` para evitar *flash*:

```html
<html lang="pt-br" data-theme="claro">
```

## 3. Aplicar o MudTheme (recomendado)

Assim **os componentes MudBlazor** (MudButton, MudCard, MudTextField…) já saem no tema gov.br.

`MainLayout.razor`:

```razor
@inject GovBrThemeService Theme

<MudThemeProvider Theme="_mud" @bind-IsDarkMode="_dark" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

@Body

@code {
    private readonly MudTheme _mud = GovBrMudTheme.Build();
    private bool _dark;

    protected override void OnInitialized() => Theme.OnChange += Sync;
    public void Dispose() => Theme.OnChange -= Sync;

    private void Sync() { _dark = Theme.IsDark; StateHasChanged(); }

    protected override async Task OnAfterRenderAsync(bool first)
    {
        if (first) { await Theme.InitializeAsync(); }
    }
}
```

## 4. Registrar o serviço

`Program.cs`:

```csharp
using ProjetoBrasil.Theme;

builder.Services.AddScoped<GovBrThemeService>();
```

Botão de troca de tema em qualquer lugar:

```razor
@inject GovBrThemeService Theme

<MudIconButton Icon="@(Theme.IsDark ? Icons.Material.Filled.LightMode : Icons.Material.Filled.DarkMode)"
               OnClick="@(() => Theme.ToggleThemeAsync())" />
```

## 5. Papéis das cores (MudBlazor `Color`)

| Cor gov.br | Papel MudBlazor | Uso |
|-----------|-----------------|-----|
| Azul `#1351B4` | `Color.Primary` | Ação principal, links, seleção |
| Verde `#168821` | `Color.Secondary` / `Color.Success` | Confirmar, sucesso, switches |
| Amarelo `#FFCB00` | `Color.Tertiary` / `Color.Warning` | Destaque, avisos |
| Vermelho `#C92A2A` | `Color.Error` | Erro, exclusão |

```razor
<MudButton Variant="Variant.Filled" Color="Color.Primary">Salvar</MudButton>
<MudButton Variant="Variant.Filled" Color="Color.Secondary">Confirmar</MudButton>
<MudButton Variant="Variant.Outlined" Color="Color.Primary">Cancelar</MudButton>
<MudButton Variant="Variant.Filled" Color="Color.Tertiary">Destaque</MudButton>
```

## 6. Classes `.gbr-*` lado a lado (opcional)

O `govbr.css` também traz uma biblioteca de classes utilitárias e componentes (`.gbr-btn`, `.gbr-card`, `.gbr-badge`, `.gbr-flag-strip`, navbar, sidebar, tabela, alertas…) para telas onde você quer o visual exato da marca fora do grid do Mud. Basta manter o `govbr.css` linkado.

Faixa tricolor da bandeira:

```html
<div class="gbr-flag-strip"><span></span><span></span><span></span></div>
```

## Notas de versão

- Escrito para **MudBlazor 7+** (`PaletteLight` / `PaletteDark`).
- Em **MudBlazor 6**, renomeie `PaletteLight` para `Palette`.
- `TextTransform = "none"` nos botões preserva o casing (o Mud usa CAPS por padrão).
- A fonte só é referenciada no `MudTheme`; carregue **Raleway** no `<head>` (passo 2).
