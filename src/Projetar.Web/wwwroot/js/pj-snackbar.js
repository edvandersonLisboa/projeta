// Snackbar global — qualquer ação (salvou, copiou link, adicionou moderador, etc.) chama
// window.pjSnackbar(mensagem, tipo) pra avisar o usuário. Empilha, some sozinho, sem framework.
(function () {
  function raiz() {
    var el = document.getElementById('pjSnackbarRoot');
    if (!el) {
      el = document.createElement('div');
      el.id = 'pjSnackbarRoot';
      el.className = 'pj-snackbar-root';
      document.body.appendChild(el);
    }
    return el;
  }

  window.pjSnackbar = function (mensagem, tipo) {
    if (!mensagem) return;

    var toast = document.createElement('div');
    toast.className = 'pj-snackbar' + (tipo === 'error' ? ' is-error' : '');
    toast.setAttribute('role', tipo === 'error' ? 'alert' : 'status');

    var dot = document.createElement('span');
    dot.className = 'dot';
    var msg = document.createElement('span');
    msg.className = 'msg';
    msg.textContent = mensagem;

    toast.appendChild(dot);
    toast.appendChild(msg);
    raiz().appendChild(toast);

    requestAnimationFrame(function () { toast.classList.add('show'); });

    setTimeout(function () {
      toast.classList.remove('show');
      setTimeout(function () { toast.remove(); }, 250);
    }, 4000);
  };
})();
