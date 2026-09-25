namespace Projetar.Web.Services;

public static class TempoRelativo
{
    public static string Formatar(DateTimeOffset data)
    {
        var diferenca = DateTimeOffset.UtcNow - data;

        if (diferenca < TimeSpan.Zero) diferenca = TimeSpan.Zero;
        if (diferenca.TotalSeconds < 60) return "agora mesmo";
        if (diferenca.TotalMinutes < 60) return $"há {(int)diferenca.TotalMinutes} min";
        if (diferenca.TotalHours < 24) return $"há {(int)diferenca.TotalHours}h";
        if (diferenca.TotalDays < 30) return $"há {(int)diferenca.TotalDays}d";
        if (diferenca.TotalDays < 365) return $"há {(int)(diferenca.TotalDays / 30)} mês(es)";
        return $"há {(int)(diferenca.TotalDays / 365)} ano(s)";
    }
}
