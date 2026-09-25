// Dropdown "minha conta" do header — abre no hover (desktop) e no clique (mobile/teclado),
// igual ao padrão do sino de notificações.
(function () {
  const raiz = document.querySelector('[data-user-menu]');
  if (!raiz) return;

  const botao = raiz.querySelector('[data-user-menu-toggle]');
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
})();
