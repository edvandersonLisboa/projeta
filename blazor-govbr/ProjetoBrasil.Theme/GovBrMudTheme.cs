using MudBlazor;

namespace ProjetoBrasil.Theme;

/// <summary>
/// Ponte Projeto Brasil (gov.br) → MudBlazor. Gera um MudTheme com a paleta
/// nacional (Azul #1351B4 · Amarelo #FFCB00 · Verde #168821) e a tipografia
/// Raleway. Claro e escuro vão juntos no mesmo tema — o MudThemeProvider
/// alterna via IsDarkMode.
///
/// Uso (ex.: MainLayout.razor):
///   &lt;MudThemeProvider Theme="_theme" @bind-IsDarkMode="_dark" /&gt;
///   @code { private MudTheme _theme = GovBrMudTheme.Build(); private bool _dark; }
///
/// (compatível com MudBlazor 7+; na v6 troque PaletteLight por Palette)
/// </summary>
public static class GovBrMudTheme
{
    // Paleta primária gov.br
    private const string Azul      = "#1351B4";
    private const string AzulVivo  = "#2B6FE0";
    private const string Amarelo   = "#FFCB00";
    private const string Verde     = "#168821";
    private const string Branco    = "#FFFFFF";

    public static MudTheme Build()
    {
        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = Azul,
                PrimaryContrastText = Branco,
                Secondary = Verde,
                SecondaryContrastText = Branco,
                Tertiary = Amarelo,
                TertiaryContrastText = "#14202E",
                Background = "#F2F5F9",
                BackgroundGray = "#E3E8EF",
                Surface = Branco,
                AppbarBackground = Branco,
                AppbarText = "#1B2A3A",
                DrawerBackground = Branco,
                DrawerText = "#1B2A3A",
                DrawerIcon = "#51606F",
                TextPrimary = "#1B2A3A",
                TextSecondary = "#51606F",
                ActionDefault = "#51606F",
                Divider = "#E3E8EF",
                DividerLight = "#F2F5F9",
                LinesDefault = "#E3E8EF",
                LinesInputs = "#CBD5E1",
                TableLines = "#E3E8EF",
                Success = Verde,
                Warning = "#E0B200",
                Error = "#C92A2A",
                Info = Azul,
            },
            PaletteDark = new PaletteDark
            {
                Primary = AzulVivo,
                PrimaryContrastText = Branco,
                Secondary = "#1D9E2A",
                SecondaryContrastText = "#04240A",
                Tertiary = Amarelo,
                TertiaryContrastText = "#14202E",
                Background = "#0A1626",
                BackgroundGray = "#0F2138",
                Surface = "#142B48",
                AppbarBackground = "#0F2138",
                AppbarText = "#EAF1F9",
                DrawerBackground = "#0F2138",
                DrawerText = "#EAF1F9",
                DrawerIcon = "#9BB0C9",
                TextPrimary = "#EAF1F9",
                TextSecondary = "#9BB0C9",
                ActionDefault = "#9BB0C9",
                Divider = "#1B3A5E",
                DividerLight = "#142B48",
                LinesDefault = "#1B3A5E",
                LinesInputs = "#2C4A6E",
                TableLines = "#1B3A5E",
                Success = "#1D9E2A",
                Warning = Amarelo,
                Error = "#E5484D",
                Info = AzulVivo,
            },
            Typography = new Typography
            {
                Default = new DefaultTypography { FontFamily = new[] { "Raleway", "Segoe UI", "system-ui", "sans-serif" } },
                H1 = new H1Typography { FontFamily = new[] { "Raleway", "sans-serif" }, FontWeight = "800" },
                H2 = new H2Typography { FontFamily = new[] { "Raleway", "sans-serif" }, FontWeight = "800" },
                H3 = new H3Typography { FontFamily = new[] { "Raleway", "sans-serif" }, FontWeight = "700" },
                H4 = new H4Typography { FontFamily = new[] { "Raleway", "sans-serif" }, FontWeight = "700" },
                H5 = new H5Typography { FontFamily = new[] { "Raleway", "sans-serif" }, FontWeight = "600" },
                H6 = new H6Typography { FontFamily = new[] { "Raleway", "sans-serif" }, FontWeight = "600" },
                Button = new ButtonTypography { FontFamily = new[] { "Raleway", "sans-serif" }, FontWeight = "700", TextTransform = "none" },
                Subtitle1 = new Subtitle1Typography { FontFamily = new[] { "Raleway", "sans-serif" } },
                Subtitle2 = new Subtitle2Typography { FontFamily = new[] { "Raleway", "sans-serif" } },
                Body1 = new Body1Typography { FontFamily = new[] { "Raleway", "sans-serif" } },
                Body2 = new Body2Typography { FontFamily = new[] { "Raleway", "sans-serif" } },
                Caption = new CaptionTypography { FontFamily = new[] { "Raleway", "sans-serif" } },
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "10px",
                DrawerWidthLeft = "256px",
            },
        };
    }
}
