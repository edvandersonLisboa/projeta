// Sino de notificações do header — abre no hover (desktop) e também no toque/clique (mobile).
// Também faz polling periódico de /api/notificacoes/resumo pra atualizar contador e lista sem recarregar a página.
(function () {
  const raiz = document.querySelector('[data-notif]');
  if (!raiz) return;

  const botao = raiz.querySelector('[data-notif-toggle]');
  if (!botao) return;

  function fechar() {
    raiz.classList.remove('is-open');
    botao.setAttribute('aria-expanded', 'false');
  }

  function alternar(evento) {
    evento.stopPropagation();
    const abrir = !raiz.classList.contains('is-open');
    raiz.classList.toggle('is-open', abrir);
    botao.setAttribute('aria-expanded', abrir ? 'true' : 'false');
  }

  botao.addEventListener('click', alternar);

  document.addEventListener('click', function (evento) {
    if (!raiz.contains(evento.target)) fechar();
  });

  document.addEventListener('keydown', function (evento) {
    if (evento.key === 'Escape') fechar();
  });

  // ---------- Polling ----------
  const INTERVALO_MS = 30000;
  const badge = raiz.querySelector('[data-notif-badge]');
  const lista = raiz.querySelector('[data-notif-lista]');
  const rotuloContagem = raiz.querySelector('[data-notif-count-label]');

  function escaparHtml(texto) {
    const div = document.createElement('div');
    div.textContent = texto == null ? '' : texto;
    return div.innerHTML;
  }

  function renderizarItens(itens) {
    if (!lista) return;

    if (!itens || itens.length === 0) {
      lista.innerHTML = '<div class="pj-notif__vazio">Nenhuma notificação nova.</div>';
      return;
    }

    lista.innerHTML = itens.map(function (n) {
      return '<a class="pj-notif__item" href="' + escaparHtml(n.linkUrl) + '">' +
        '<span class="pj-notif__icone pj-notif__icone--' + escaparHtml(n.situacao) + '"></span>' +
        '<span class="pj-notif__item-body">' +
          '<span class="pj-notif__item-titulo">' + escaparHtml(n.titulo) + '</span>' +
          '<span class="pj-notif__item-msg">' + escaparHtml(n.mensagem) + '</span>' +
          '<span class="pj-notif__item-tempo">' + escaparHtml(n.tempo) + '</span>' +
        '</span>' +
      '</a>';
    }).join('');
  }

  async function atualizar() {
    try {
      const resposta = await fetch('/api/notificacoes/resumo', { credentials: 'same-origin' });
      if (!resposta.ok) return;

      const dados = await resposta.json();

      if (badge) {
        if (dados.totalNaoLidas > 0) {
          badge.hidden = false;
          badge.textContent = dados.totalNaoLidas > 99 ? '99+' : String(dados.totalNaoLidas);
        } else {
          badge.hidden = true;
        }
      }

      if (rotuloContagem) {
        rotuloContagem.textContent = dados.totalNaoLidas + (dados.totalNaoLidas === 1 ? ' não lida' : ' não lidas');
      }

      renderizarItens(dados.itens);
    } catch (erro) {
      // Falha de rede silenciosa — a próxima rodada do polling tenta de novo.
    }
  }

  let intervalo = setInterval(atualizar, INTERVALO_MS);

  document.addEventListener('visibilitychange', function () {
    clearInterval(intervalo);
    if (!document.hidden) {
      atualizar();
      intervalo = setInterval(atualizar, INTERVALO_MS);
    }
  });
})();
