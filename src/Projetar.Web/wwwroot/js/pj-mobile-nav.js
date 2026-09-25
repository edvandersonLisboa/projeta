// Menu hambúrguer do header mobile (< 760px) — abre/fecha o painel, fecha ao clicar
// num link, ao clicar fora, no Esc, ou se a tela crescer além do breakpoint.
(function () {
  const raiz = document.querySelector('[data-mnav]');
  if (!raiz) return;

  const botao = raiz.querySelector('[data-mnav-toggle]');
  const painel = raiz.querySelector('[data-mnav-panel]');
  if (!botao || !painel) return;

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

  painel.querySelectorAll('a, button').forEach(function (item) {
    item.addEventListener('click', fechar);
  });

  document.addEventListener('click', function (evento) {
    if (!raiz.contains(evento.target)) fechar();
  });

  document.addEventListener('keydown', function (evento) {
    if (evento.key === 'Escape') fechar();
  });

  window.addEventListener('resize', function () {
    if (window.innerWidth > 759) fechar();
  });
})();
