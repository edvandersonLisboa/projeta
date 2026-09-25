using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projetar.Web.Pages;

public class SobreModel : PageModel
{
    public record Principle(string Tag, string Text);
    public record Card(string Title, string Text);
    public record Stage(string Title, string Sub, string Color, Card[] Items);
    public record Value(string Title, string Text, string Color);

    public static readonly Principle[] Principles =
    [
        new("Interesse público", "Lealdade ao país acima de partido, lobby e financiador"),
        new("Representação", "Mandato é função revogável, não figura de culto"),
        new("Laicidade", "A fé é do cidadão; o Estado é de todos"),
        new("Dignidade", "A pessoa é o fim da política, nunca o meio"),
        new("Responsabilidade entre gerações", "Decidir hoje respondendo por quem vem depois"),
        new("Direito à vida", "Segurança pública como política de Estado, não como bandeira"),
        new("Compromisso", "O programa registrado obriga quem foi eleito"),
        new("Integridade", "Dinheiro público é intocável"),
        new("Verdade e informação", "Desinformação é ataque à democracia"),
        new("Justiça distributiva", "Desigualdade extrema é falha de política pública, não destino"),
    ];

    public static readonly Stage[] Stages =
    [
        new("Participar", "O que qualquer cidadão pode fazer em um item.", "var(--pj-green)",
        [
            new("Explorar", "Navegue pelos dez princípios e leia cada item — o texto completo da proposta, com dados e comparações, fica aberto pra qualquer visitante."),
            new("Propor", "Qualquer cidadão com perfil completo (nome, estado e município) pode propor um item novo dentro de um princípio existente."),
            new("Editar", "Achou um jeito melhor de escrever um item que já existe? Proponha a edição — ela fica em análise até um revisor decidir."),
            new("Avaliar com justificativa", "Dê de 1 a 5 estrelas pra importância de um item — e escreva por quê. O histórico completo de quem avaliou, quando e com que justificativa fica público."),
            new("Comentar", "Discuta, questione, responda em thread. O debate acontece junto do texto, não em outro lugar."),
            new("Anexar prova", "Sustente um item com uma referência (link) ou documento de apoio — dado, reportagem, estudo."),
            new("Sugerir tag e banner", "Classifique um item com uma tag ou proponha uma imagem de capa pra ele — como qualquer outra contribuição, entra em análise antes de aparecer pra todo mundo."),
        ]),
        new("Revisão", "Como uma contribuição vira parte do texto.", "var(--pj-gold)",
        [
            new("Moderar", "Todo item, edição, referência, documento, tag ou banner proposto passa por revisão antes de valer — com um motivo declarado na aprovação ou na rejeição, nunca em silêncio. Cada revisor responde só pelo que é dele: um princípio inteiro que ele acompanha, ou um item específico — nunca a plataforma toda por padrão. Quando quem propõe já modera aquele item, a mudança vale na hora, sem fila."),
            new("Publicar", "Quando um princípio ou um item está pronto pra sair do rascunho, quem modera aquele espaço decide — um administrador, ou o revisor responsável por aquele princípio ou item."),
        ]),
        new("Acompanhar", "Depois de enviar, você não fica no escuro.", "var(--pj-navy)",
        [
            new("Acompanhar e responder", "Em \"Meu Histórico\" você vê em que pé está cada proposta sua — em análise, aprovada ou rejeitada —, conversa direto com quem avaliou e, se algo foi recusado, corrige e reenvia sem perder o fio da conversa."),
            new("Ser avisado", "O sino no topo da página avisa quando uma proposta sua é decidida, quando alguém responde sua mensagem ou quando chega algo novo pra você revisar — sem precisar ficar checando a fila."),
        ]),
    ];

    public static readonly Value[] Values =
    [
        new("Escrito de baixo pra cima", "Ninguém recebe os dez princípios prontos de um só autor. Cada item nasce de uma proposta de alguém, é discutido e só vira \"oficial\" depois de revisado.", "var(--pj-navy)"),
        new("Moderado, não silenciado", "Toda rejeição tem motivo escrito e visível pro autor, que pode responder, corrigir e reenviar sem perder o histórico da conversa. Curadoria não é censura sem explicação — e quem modera responde só pelo que é dele: um princípio ou um item, nunca a plataforma inteira por padrão.", "var(--pj-gold)"),
        new("Transparente por padrão", "Voto tem nome, data e justificativa pública, e a página de cada item mostra exatamente quem contribuiu com o quê — quem escreveu, quem sugeriu uma tag, quem trouxe uma referência. Não existe avaliação anônima nem contribuição escondida atrás do resultado final.", "var(--pj-green)"),
        new("Cívico e laico", "Nenhuma proposta se apoia em doutrina. O argumento aqui é lei, dado e política pública — e vale para quem tem fé e para quem não tem.", "var(--pj-navy)"),
    ];

    public void OnGet()
    {
    }
}
