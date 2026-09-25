// Acordeão dos princípios (Home) — troca a classe is-open, sem framework.
document.addEventListener('DOMContentLoaded', function () {
    var accordion = document.querySelector('.pj-accordion');
    if (!accordion) {
        return;
    }

    function setOpen(principle, open) {
        principle.classList.toggle('is-open', open);
        var head = principle.querySelector('.pj-principle__head');
        if (head) {
            head.setAttribute('aria-expanded', open ? 'true' : 'false');
        }
    }

    accordion.addEventListener('click', function (event) {
        var head = event.target.closest('.pj-principle__head');
        if (!head) {
            return;
        }
        var principle = head.closest('.pj-principle');
        setOpen(principle, !principle.classList.contains('is-open'));
    });

    var expandAll = document.getElementById('pjExpandAll');
    var collapseAll = document.getElementById('pjCollapseAll');

    if (expandAll) {
        expandAll.addEventListener('click', function () {
            accordion.querySelectorAll('.pj-principle').forEach(function (p) { setOpen(p, true); });
        });
    }
    if (collapseAll) {
        collapseAll.addEventListener('click', function () {
            accordion.querySelectorAll('.pj-principle').forEach(function (p) { setOpen(p, false); });
        });
    }
});
