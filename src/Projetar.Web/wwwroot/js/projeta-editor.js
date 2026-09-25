// Projeta — editor de "Propor edição" (página do item).
// Texto completo livre, sem edição por parágrafo nem comparação em tempo real —
// a validação/comparação de mudanças é feita pelo revisor na moderação.

(function () {
    'use strict';

    function initEditor(root) {
        var originalHost = root.querySelector('[data-pj-original]');
        var content = root.querySelector('[data-pj-content]');
        var hiddenInput = root.querySelector('[data-pj-hidden-input]');
        if (!originalHost || !content || !hiddenInput) return;

        var originalHtml = originalHost.innerHTML;
        var submitBtn = root.querySelector('[data-pj-submit]');

        function norm(html) { return (html || '').trim(); }

        function recompute() {
            if (submitBtn) {
                submitBtn.disabled = norm(content.innerHTML) === norm(originalHtml);
            }
        }

        content.addEventListener('input', recompute);

        root.querySelectorAll('[data-pj-exec]').forEach(function (btn) {
            btn.addEventListener('mousedown', function (e) {
                e.preventDefault();
                content.focus();
                document.execCommand(btn.getAttribute('data-pj-exec'), false, btn.getAttribute('data-pj-value') || null);
                recompute();
            });
        });

        var linkBtn = root.querySelector('[data-pj-link]');
        if (linkBtn) {
            linkBtn.addEventListener('mousedown', function (e) {
                e.preventDefault();
                var url = window.prompt('Endereço do link (https://...)');
                if (url) { content.focus(); document.execCommand('createLink', false, url); recompute(); }
            });
        }

        var restoreBtn = root.querySelector('[data-pj-restore]');
        if (restoreBtn) {
            restoreBtn.addEventListener('click', function () {
                content.innerHTML = originalHtml;
                recompute();
            });
        }

        root.addEventListener('submit', function () {
            hiddenInput.value = content.innerHTML;
        });

        recompute();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-pj-editor]').forEach(initEditor);
    });
})();
