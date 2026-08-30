// Menu hambúrguer da navbar — abre/fecha o menu de navegação em telas menores que 1000px.
(function () {
  const toggle = document.getElementById('gbrMenuToggle');
  const menu = document.getElementById('gbrNavLinks');
  if (!toggle || !menu) return;

  function fecharMenu() {
    menu.classList.remove('is-open');
    toggle.setAttribute('aria-expanded', 'false');
  }

  function abrirMenu() {
    menu.classList.add('is-open');
    toggle.setAttribute('aria-expanded', 'true');
  }

  toggle.addEventListener('click', function () {
    if (menu.classList.contains('is-open')) {
      fecharMenu();
    } else {
      abrirMenu();
    }
  });

  window.addEventListener('resize', function () {
    if (window.innerWidth > 1000) fecharMenu();
  });
})();
