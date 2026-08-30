namespace Mandamentos.Web.Models;

/// <summary>
/// Fases de evolução de um Item, inspiradas no módulo de Legislação
/// Colaborativa do CONSUL: texto nasce fixo/proposto, passa por debate
/// e revisão colaborativa, e pode ser promovido até minuta de lei.
/// </summary>
public enum ItemStatus
{
    Original = 0,
    PropostaComunidade = 1,
    EmDebate = 2,
    EmRevisaoColaborativa = 3,
    MinutaDeLei = 4,
    Aprovado = 5,
    Arquivado = 6,
}
