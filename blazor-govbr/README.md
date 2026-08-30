# Projeto Brasil — Tema gov.br para Blazor + MudBlazor

Tema drop-in para aplicações **Blazor** com **MudBlazor**, na identidade nacional gov.br.

> Azul `#1351B4` · Amarelo `#FFCB00` · Verde `#168821` · Branco — tipografia **Raleway** — claro e escuro.

## Conteúdo do pacote

```
blazor-govbr/
├─ wwwroot/
│  ├─ css/govbr.css          CSS único: tokens, temas claro/escuro,
│  │                          esquema de botões e componentes .gbr-*
│  └─ js/govbr-theme.js       interop de tema (localStorage, anti-flash)
├─ ProjetoBrasil.Theme/
│  ├─ GovBrMudTheme.cs         MudTheme nativo (paleta + Raleway)
│  ├─ GovBrTheme.cs            enum Claro/Escuro
│  └─ GovBrThemeService.cs     serviço de troca de tema
├─ MUDBLAZOR.md                guia de integração com MudBlazor (comece aqui)
└─ README.md                   este arquivo
```

## Início rápido

1. Copie `wwwroot/` e `ProjetoBrasil.Theme/` para o seu projeto.
2. Siga o **MUDBLAZOR.md** (passos 2 a 5): fontes + CSS no `<head>`, `MudThemeProvider Theme="GovBrMudTheme.Build()"`, e `AddScoped<GovBrThemeService>()`.
3. Use `Color.Primary` (azul), `Color.Secondary`/`Color.Success` (verde) e `Color.Tertiary`/`Color.Warning` (amarelo) nos componentes MudBlazor.

## Duas formas de usar

- **Tema nativo do MudBlazor** — `GovBrMudTheme.Build()` deixa todos os componentes Mud na cara do gov.br. Recomendado.
- **Classes `.gbr-*`** — biblioteca própria (`.gbr-btn`, `.gbr-card`, `.gbr-flag-strip`, navbar, sidebar, tabela, alertas…) para telas com visual exato da marca fora do grid do Mud.

## Esquema de botões (`.gbr-btn`)

`--primary` (azul) · `--secondary` (verde) · `--accent` (amarelo) · `--outline` · `--ghost` · `--danger`
Tamanhos: `--sm` · `--lg` · `--block` · `--icon`.

## Temas

Aplique no `<html>`: `data-theme="claro"` ou `data-theme="escuro"`. Toda a UI consome tokens semânticos (`--gbr-bg`, `--gbr-text`, `--gbr-primary`…), então tudo se adapta sozinho.

## Personalização

Sobrescreva os tokens `--gbr-*` num bloco próprio depois do `govbr.css`. Se ajustar uma cor, atualize também o hex correspondente em `GovBrMudTheme.cs` para manter Mud e classes coerentes.
