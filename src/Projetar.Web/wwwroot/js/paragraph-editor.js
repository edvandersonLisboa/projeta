// Edição por parágrafo: mostra o texto original em blocos somente-leitura — cada um com
// botão pra editar só aquele trecho (Quill isolado) — mais separadores "+ Adicionar parágrafo"
// entre os blocos. Nada permite apagar/reescrever o documento inteiro de uma vez.
// No submit, remonta tudo num único HTML e grava no <textarea> real (mesmo contrato do rich-editor.js).
function initParagraphEditor(textareaId, containerId) {
    var textarea = document.getElementById(textareaId);
    var container = document.getElementById(containerId);
    if (!textarea || !container || typeof Quill === 'undefined') {
        return;
    }

    var toolbarOptions = [
        ['bold', 'italic', 'underline'],
        [{ list: 'ordered' }, { list: 'bullet' }],
        ['link', 'blockquote'],
        ['clean'],
    ];

    var doc = new DOMParser().parseFromString(textarea.value, 'text/html');
    var blocos = Array.prototype.slice.call(doc.body.children);

    textarea.style.display = 'none';
    container.innerHTML = '';

    var itens = []; // { quill: Quill|null, originalHtml: string|null }

    function criarSeparadorAdicionar() {
        var wrapper = document.createElement('div');
        wrapper.className = 'gbr-paragrafo-add';

        var botao = document.createElement('button');
        botao.type = 'button';
        botao.className = 'gbr-btn gbr-btn--ghost gbr-btn--sm';
        botao.textContent = '+ Adicionar parágrafo aqui';

        var item = { quill: null, originalHtml: null };

        botao.addEventListener('click', function () {
            botao.style.display = 'none';
            var editorDiv = document.createElement('div');
            editorDiv.className = 'gbr-richtext-editor';
            wrapper.insertBefore(editorDiv, botao);
            item.quill = new Quill(editorDiv, { theme: 'snow', modules: { toolbar: toolbarOptions } });
            item.quill.focus();
        });

        itens.push(item);
        return wrapper;
    }

    function criarBlocoOriginal(el) {
        var wrapper = document.createElement('div');
        wrapper.className = 'gbr-paragrafo';

        var preview = document.createElement('div');
        preview.className = 'gbr-paragrafo__preview';
        preview.appendChild(el);

        var botaoEditar = document.createElement('button');
        botaoEditar.type = 'button';
        botaoEditar.className = 'gbr-btn gbr-btn--outline gbr-btn--sm';
        botaoEditar.textContent = 'Editar este parágrafo';

        var item = { quill: null, originalHtml: el.outerHTML };

        botaoEditar.addEventListener('click', function () {
            var htmlOriginal = preview.innerHTML;
            preview.style.display = 'none';
            botaoEditar.style.display = 'none';

            var editorDiv = document.createElement('div');
            editorDiv.className = 'gbr-richtext-editor';
            wrapper.insertBefore(editorDiv, botaoEditar);
            item.quill = new Quill(editorDiv, { theme: 'snow', modules: { toolbar: toolbarOptions } });
            item.quill.root.innerHTML = htmlOriginal;
        });

        wrapper.appendChild(preview);
        wrapper.appendChild(botaoEditar);
        itens.push(item);
        return wrapper;
    }

    container.appendChild(criarSeparadorAdicionar());
    blocos.forEach(function (el) {
        container.appendChild(criarBlocoOriginal(el));
        container.appendChild(criarSeparadorAdicionar());
    });

    var form = textarea.closest('form');
    if (form) {
        form.addEventListener('submit', function () {
            var html = '';
            itens.forEach(function (item) {
                if (item.quill) {
                    var conteudo = item.quill.root.innerHTML;
                    if (conteudo && conteudo !== '<p><br></p>') {
                        html += conteudo;
                    }
                } else if (item.originalHtml) {
                    html += item.originalHtml;
                }
            });
            textarea.value = html;
        });
    }
}
