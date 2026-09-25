// Intercepta todo botão/link "Propor ideia" (data-propor-toggle) e abre o modal de escolha de
// princípio em vez de navegar direto. Sem JS, o href continua funcionando como fallback (ancora
// pra lista de princípios), então nada quebra se o script falhar em carregar.
(function () {
  const modal = document.querySelector('[data-propor-modal]');
  if (!modal) return;

  document.querySelectorAll('[data-propor-toggle]').forEach(function (gatilho) {
    gatilho.addEventListener('click', function (evento) {
      evento.preventDefault();
      modal.showModal();
    });
  });

  const fechar = modal.querySelector('[data-propor-fechar]');
  if (fechar) {
    fechar.addEventListener('click', function () { modal.close(); });
  }

  modal.addEventListener('click', function (evento) {
    if (evento.target === modal) modal.close();
  });
})();
