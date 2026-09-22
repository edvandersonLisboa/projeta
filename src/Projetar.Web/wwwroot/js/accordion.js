document.addEventListener('click', function (event) {
    const mandamentoHeader = event.target.closest('[data-toggle-mandamento]');
    if (mandamentoHeader) {
        const card = mandamentoHeader.closest('.mandamento');
        const wasOpen = card.classList.contains('open');

        document.querySelectorAll('.mandamento').forEach(m => m.classList.remove('open'));
        document.querySelectorAll('.subitem').forEach(s => s.classList.remove('open'));

        if (!wasOpen) card.classList.add('open');
        return;
    }

    const subitemHeader = event.target.closest('[data-toggle-subitem]');
    if (subitemHeader) {
        event.stopPropagation();
        subitemHeader.closest('.subitem').classList.toggle('open');
    }
});
