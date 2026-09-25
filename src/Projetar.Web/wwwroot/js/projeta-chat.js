// Projeta — conversa (moderação e meu histórico).
// Adaptado do componente ChatThread.razor pra Razor Pages: sem interop, o textarea
// só dispara o submit do <form> que o envolve (o servidor cuida do resto).
(function () {
  function rolarParaFinal(el) {
    if (el) requestAnimationFrame(function () { el.scrollTop = el.scrollHeight; });
  }

  document.querySelectorAll('[data-chat-messages]').forEach(rolarParaFinal);

  // Enter envia, Shift+Enter quebra linha.
  document.querySelectorAll('[data-chat-input]').forEach(function (textarea) {
    textarea.addEventListener('keydown', function (e) {
      if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) {
        e.preventDefault();
        if (textarea.value.trim()) {
          textarea.form?.requestSubmit();
        }
      }
    });
  });

  // Chips de motivo rápido — preenchem o textarea de rejeição sem recarregar a página.
  document.querySelectorAll('[data-quick-reason]').forEach(function (botao) {
    botao.addEventListener('click', function () {
      var alvo = document.getElementById(botao.dataset.quickReason);
      if (alvo) {
        alvo.value = botao.textContent.trim() + '.';
        alvo.focus();
        alvo.dispatchEvent(new Event('input'));
      }
    });
  });
})();
