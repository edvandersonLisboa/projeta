// Modal "Novo princípio" — reabre sozinho com os valores digitados quando o servidor
// recusa o envio (assim o admin não perde o que já tinha escrito), e mostra um
// contador de caracteres ao vivo para a introdução. Puro DOM, sem framework.
(function () {
  var dialog = document.getElementById('modalNovoPrincipio');
  if (!dialog) return;

  if (dialog.dataset.abrir === '1') {
    dialog.showModal();
  }

  var intro = document.getElementById('novoPrincipioIntro');
  var contador = document.getElementById('novoPrincipioContador');
  if (intro && contador) {
    var max = intro.getAttribute('maxlength');
    var atualizar = function () {
      var len = intro.value.length;
      contador.textContent = len + '/' + max;
      contador.classList.toggle('is-over', len > Number(max) * 0.9);
    };
    intro.addEventListener('input', atualizar);
    atualizar();
  }
})();
