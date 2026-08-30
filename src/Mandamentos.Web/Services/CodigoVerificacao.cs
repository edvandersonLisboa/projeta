using System.Security.Cryptography;
using System.Text;

namespace Mandamentos.Web.Services;

/// <summary>Geração e hash do código numérico de 6 dígitos usado em confirmação de e-mail e redefinição de senha.</summary>
public static class CodigoVerificacao
{
    public const int Tamanho = 6;
    public const int MaxTentativas = 5;
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(15);

    public static string Gerar() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString($"D{Tamanho}");

    public static string Hash(string escopo, string codigo)
    {
        var bytes = Encoding.UTF8.GetBytes($"{escopo}:{codigo}");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
