namespace Projetar.Web.Services;

/// <summary>Iniciais e cor de avatar a partir de um nome — usado na caixa de conversas da Moderação.</summary>
public static class AvatarUtils
{
    private static readonly string[] Cores = ["#17305A", "#127A36", "#8A6410", "#5B3F86", "#B4322A"];

    public static string Initials(string nome) =>
        string.Concat(nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));

    public static string Color(string nome) => Cores[Math.Abs(nome.Sum(c => c)) % Cores.Length];
}
