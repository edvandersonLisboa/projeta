// Projeto Brasil — interop de tema gov.br para Blazor.
// Persiste a preferência em localStorage e aplica no <html data-theme>.
(function () {
  const KEY = 'gbr-theme';
  window.gbrTheme = {
    get() {
      return document.documentElement.getAttribute('data-theme') || 'claro';
    },
    set(theme) {
      document.documentElement.setAttribute('data-theme', theme);
      try { localStorage.setItem(KEY, theme); } catch (e) {}
      return theme;
    },
    toggle() {
      return this.set(this.get() === 'escuro' ? 'claro' : 'escuro');
    },
    restore() {
      let saved = null;
      try { saved = localStorage.getItem(KEY); } catch (e) {}
      const theme = saved || document.documentElement.getAttribute('data-theme') || 'claro';
      document.documentElement.setAttribute('data-theme', theme);
      return theme;
    }
  };
  // Evita flash antes do Blazor iniciar.
  try {
    const saved = localStorage.getItem(KEY);
    if (saved) document.documentElement.setAttribute('data-theme', saved);
  } catch (e) {}
})();
