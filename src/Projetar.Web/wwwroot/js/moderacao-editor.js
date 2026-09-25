// Moderação — editor simples pra ajustar o texto de uma proposta antes de aprovar.
// Sem framework: contenteditable + document.execCommand, igual ao editor da
// página "Propor edição", só que com toolbar no estilo gbr (tema da moderação).

(function () {
    'use strict';

    function initEditor(root) {
        var content = root.querySelector('[data-mod-content]');
        var hiddenInput = root.querySelector('[data-mod-hidden]');
        if (!content || !hiddenInput) return;

        root.querySelectorAll('[data-mod-exec]').forEach(function (btn) {
            btn.addEventListener('mousedown', function (e) {
                e.preventDefault();
                content.focus();
                document.execCommand(btn.getAttribute('data-mod-exec'), false, btn.getAttribute('data-mod-value') || null);
            });
        });

        var linkBtn = root.querySelector('[data-mod-link]');
        if (linkBtn) {
            linkBtn.addEventListener('mousedown', function (e) {
                e.preventDefault();
                var url = window.prompt('Endereço do link (https://...)');
                if (url) { content.focus(); document.execCommand('createLink', false, url); }
            });
        }

        root.addEventListener('submit', function () {
            hiddenInput.value = content.innerHTML;
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-mod-editor]').forEach(initEditor);
    });
})();
