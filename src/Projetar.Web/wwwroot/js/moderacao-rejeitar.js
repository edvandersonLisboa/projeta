// Moderação — botão "Rejeitar" abre o painel de motivo logo abaixo do card.
(function () {
  document.querySelectorAll('[data-reject-toggle]').forEach(function (botao) {
    botao.addEventListener('click', function () {
      var card = botao.closest('.pj-mod-card');
      var painel = card?.querySelector('[data-reject-panel]');
      if (!painel) return;
      painel.hidden = !painel.hidden;
      if (!painel.hidden) {
        painel.querySelector('textarea')?.focus();
      }
    });
  });
})();
