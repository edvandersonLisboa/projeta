// Gerenciar seções — mostra/esconde a barra de ações em lote conforme os checkboxes de item marcados,
// e mantém o checkbox "selecionar todos" sincronizado. Puro DOM, sem framework.
(function () {
  const form = document.getElementById('formItens');
  if (!form) return;

  const selectAll = form.querySelector('[data-select-all]');
  const bar = form.querySelector('[data-bulk-bar]');
  const countEl = form.querySelector('[data-bulk-count]');
  const clearBtn = form.querySelector('[data-bulk-clear]');

  function itens() {
    return Array.from(form.querySelectorAll('[data-select-item]'));
  }

  function atualizar() {
    const marcados = itens().filter(function (c) { return c.checked; });
    if (bar) bar.hidden = marcados.length === 0;
    if (countEl) countEl.textContent = marcados.length === 1 ? '1 item selecionado' : marcados.length + ' itens selecionados';
    if (selectAll) {
      const todos = itens();
      selectAll.checked = todos.length > 0 && marcados.length === todos.length;
      selectAll.indeterminate = marcados.length > 0 && marcados.length < todos.length;
    }
  }

  itens().forEach(function (c) { c.addEventListener('change', atualizar); });

  if (selectAll) {
    selectAll.addEventListener('change', function () {
      itens().forEach(function (c) { c.checked = selectAll.checked; });
      atualizar();
    });
  }

  if (clearBtn) {
    clearBtn.addEventListener('click', function () {
      itens().forEach(function (c) { c.checked = false; });
      atualizar();
    });
  }

  atualizar();
})();
