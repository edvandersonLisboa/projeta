document.addEventListener('click', function (event) {
    const principioHeader = event.target.closest('[data-toggle-principio]');
    if (principioHeader) {
        const card = principioHeader.closest('.principio');
        const wasOpen = card.classList.contains('open');

        document.querySelectorAll('.principio').forEach(m => m.classList.remove('open'));
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
