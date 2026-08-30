namespace Mandamentos.Web.Models;

/// <summary>Código numérico de 6 dígitos enviado por e-mail para confirmar a conta.</summary>
public class EmailConfirmationCode
{
    public Guid Id { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public string CodigoHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiraEm { get; set; }
    public int Tentativas { get; set; }
    public bool Usado { get; set; }
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
}
