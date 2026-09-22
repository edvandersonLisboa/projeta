// Liga o Quill (rich text) a um <textarea> existente: esconde o textarea original,
// mostra o editor visual no lugar dele, e copia o HTML de volta pro textarea antes do submit
// (é o textarea que de fato viaja no POST — o Quill é só a interface).
function initRichEditor(textareaId) {
    var textarea = document.getElementById(textareaId);
    if (!textarea || typeof Quill === 'undefined') {
        return;
    }

    var container = document.createElement('div');
    container.id = textareaId + '_quill';
    container.className = 'gbr-richtext-editor';
    textarea.parentNode.insertBefore(container, textarea);
    textarea.style.display = 'none';

    var quill = new Quill(container, {
        theme: 'snow',
        modules: {
            toolbar: [
                [{ header: [2, 3, false] }],
                ['bold', 'italic', 'underline'],
                [{ list: 'ordered' }, { list: 'bullet' }],
                ['link', 'blockquote'],
                ['clean'],
            ],
        },
    });

    quill.root.innerHTML = textarea.value;

    var form = textarea.closest('form');
    if (form) {
        form.addEventListener('submit', function () {
            textarea.value = quill.root.innerHTML;
        });
    }
}
