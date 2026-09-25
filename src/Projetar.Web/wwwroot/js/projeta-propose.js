// Projeta — página "Propor novo item". Título + editor rich text (com inserção de
// imagem via upload assíncrono) + tags em chips + checklist lateral, tudo em JS puro.

(function () {
    'use strict';

    function countWords(el) {
        var t = (el.innerText || '').trim();
        return t ? t.split(/\s+/).length : 0;
    }

    function init(root) {
        var titleInput = root.querySelector('[data-pp-title]');
        var titleCounter = root.querySelector('[data-pp-title-counter]');
        var content = root.querySelector('[data-pp-content]');
        var hiddenContent = root.querySelector('[data-pp-hidden-content]');
        var wordsCounter = root.querySelector('[data-pp-words]');
        var submitBtn = root.querySelector('[data-pp-submit]');
        var missingLabel = root.querySelector('[data-pp-missing]');
        var token = root.querySelector('input[name="__RequestVerificationToken"]');

        var MIN_TITLE = 8;
        var MIN_WORDS = 30;
        var MAX_TAGS = 5;

        function tokenValue() { return token ? token.value : ''; }

        // ---------- Título ----------
        function updateTitleCounter() {
            if (titleCounter) titleCounter.textContent = titleInput.value.length + ' / 160';
        }
        if (titleInput) {
            titleInput.addEventListener('input', function () { updateTitleCounter(); updateChecklist(); });
            updateTitleCounter();
        }

        // ---------- Editor ----------
        var lastRange = null;
        if (content) {
            document.addEventListener('selectionchange', function () {
                var sel = window.getSelection();
                if (sel.rangeCount && content.contains(sel.anchorNode)) {
                    lastRange = sel.getRangeAt(0).cloneRange();
                }
            });

            content.addEventListener('input', function () {
                if (wordsCounter) {
                    var n = countWords(content);
                    wordsCounter.textContent = n === 1 ? '1 palavra' : n + ' palavras';
                }
                updateChecklist();
            });
        }

        root.querySelectorAll('[data-pp-exec]').forEach(function (btn) {
            btn.addEventListener('mousedown', function (e) {
                e.preventDefault();
                content.focus();
                document.execCommand(btn.getAttribute('data-pp-exec'), false, btn.getAttribute('data-pp-value') || null);
                content.dispatchEvent(new Event('input'));
            });
        });

        var linkBtn = root.querySelector('[data-pp-link]');
        if (linkBtn) {
            linkBtn.addEventListener('mousedown', function (e) {
                e.preventDefault();
                var url = window.prompt('Endereço do link (https://...)');
                if (url) { content.focus(); document.execCommand('createLink', false, url); content.dispatchEvent(new Event('input')); }
            });
        }

        function insertImageAtCursor(url) {
            content.focus();
            var sel = window.getSelection();
            sel.removeAllRanges();
            if (lastRange) {
                sel.addRange(lastRange);
            } else {
                var r = document.createRange();
                r.selectNodeContents(content);
                r.collapse(false);
                sel.addRange(r);
            }
            document.execCommand('insertImage', false, url);
            content.dispatchEvent(new Event('input'));
        }

        async function uploadImagem(file, pasta) {
            var formData = new FormData();
            formData.append('arquivo', file);
            formData.append('pasta', pasta);
            formData.append('__RequestVerificationToken', tokenValue());

            var resposta = await fetch('?handler=UploadImagem', { method: 'POST', body: formData });
            if (!resposta.ok) {
                var erro = 'Não foi possível enviar a imagem.';
                try { var corpo = await resposta.json(); erro = corpo.erro || erro; } catch (e) { }
                throw new Error(erro);
            }
            return resposta.json();
        }

        var inlineImageInput = root.querySelector('[data-pp-inline-image]');
        if (inlineImageInput) {
            inlineImageInput.addEventListener('change', function () {
                var file = inlineImageInput.files[0];
                inlineImageInput.value = '';
                if (!file) return;
                uploadImagem(file, 'conteudo')
                    .then(function (r) { insertImageAtCursor(r.url); })
                    .catch(function (err) { showBannerError(err.message); });
            });
        }

        root.querySelectorAll('[data-pp-template]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                content.insertAdjacentHTML('beforeend', btn.getAttribute('data-pp-template'));
                content.dispatchEvent(new Event('input'));
            });
        });

        // ---------- Banner ----------
        var bannerInput = root.querySelector('[data-pp-banner-input]');
        var bannerHiddenUrl = root.querySelector('[data-pp-banner-url]');
        var bannerEmpty = root.querySelector('[data-pp-banner-empty]');
        var bannerFilled = root.querySelector('[data-pp-banner-filled]');
        var bannerImg = root.querySelector('[data-pp-banner-img]');
        var bannerErrorBox = root.querySelector('[data-pp-banner-error]');
        var bannerMenu = root.querySelector('[data-pp-banner-menu]');
        var bannerMenuToggle = root.querySelector('[data-pp-banner-menu-toggle]');
        var bannerRemove = root.querySelector('[data-pp-banner-remove]');

        function showBannerError(msg) {
            if (bannerErrorBox) { bannerErrorBox.textContent = msg; bannerErrorBox.hidden = false; }
        }
        function clearBannerError() {
            if (bannerErrorBox) { bannerErrorBox.hidden = true; }
        }

        if (bannerInput) {
            bannerInput.addEventListener('change', function () {
                var file = bannerInput.files[0];
                if (!file) return;
                clearBannerError();
                uploadImagem(file, 'banners')
                    .then(function (r) {
                        if (bannerHiddenUrl) bannerHiddenUrl.value = r.url;
                        if (bannerImg) bannerImg.src = r.url;
                        if (bannerEmpty) bannerEmpty.hidden = true;
                        if (bannerFilled) bannerFilled.hidden = false;
                        updateChecklist();
                    })
                    .catch(function (err) { showBannerError(err.message); });
            });
        }
        if (bannerMenuToggle) {
            bannerMenuToggle.addEventListener('click', function () {
                if (bannerMenu) bannerMenu.hidden = !bannerMenu.hidden;
            });
        }
        if (bannerRemove) {
            bannerRemove.addEventListener('click', function () {
                if (bannerHiddenUrl) bannerHiddenUrl.value = '';
                if (bannerEmpty) bannerEmpty.hidden = false;
                if (bannerFilled) bannerFilled.hidden = true;
                if (bannerMenu) bannerMenu.hidden = true;
                updateChecklist();
            });
        }
        // Reexibição após falha de validação — o servidor já devolve a URL no hidden input.
        if (bannerHiddenUrl && bannerHiddenUrl.value) {
            if (bannerEmpty) bannerEmpty.hidden = true;
            if (bannerFilled) bannerFilled.hidden = false;
        }

        // ---------- Tags ----------
        var tagsList = root.querySelector('[data-pp-tags-list]');
        var tagsHidden = root.querySelector('[data-pp-tags-hidden]');
        // Reexibição após falha de validação — o servidor já devolve as tags no hidden input.
        var tags = (tagsHidden && tagsHidden.value)
            ? tagsHidden.value.split(',').map(function (t) { return t.trim(); }).filter(Boolean)
            : [];
        var tagAddBtn = root.querySelector('[data-pp-tag-add-btn]');
        var tagForm = root.querySelector('[data-pp-tag-form]');
        var tagInput = root.querySelector('[data-pp-tag-input]');
        var tagFormAdd = root.querySelector('[data-pp-tag-form-add]');
        var tagFormClose = root.querySelector('[data-pp-tag-form-close]');
        var tagSuggestWrap = root.querySelector('[data-pp-tag-suggest]');
        var allSuggestions = Array.prototype.map.call(
            root.querySelectorAll('[data-pp-suggestion]'),
            function (el) { return el.getAttribute('data-pp-suggestion'); }
        );

        function syncTagsHidden() {
            if (tagsHidden) tagsHidden.value = tags.join(', ');
        }

        function renderTags() {
            if (!tagsList) return;
            tagsList.innerHTML = '';
            tags.forEach(function (tag) {
                var chip = document.createElement('span');
                chip.className = 'pj-tag-chip';
                chip.textContent = tag + ' ';
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.title = 'Remover tag';
                btn.textContent = '×';
                btn.addEventListener('click', function () {
                    tags = tags.filter(function (t) { return t !== tag; });
                    renderAll();
                });
                chip.appendChild(btn);
                tagsList.appendChild(chip);
            });

            if (tagAddBtn) tagAddBtn.hidden = tags.length >= MAX_TAGS;
        }

        function renderSuggestions() {
            if (!tagSuggestWrap) return;
            var remaining = allSuggestions.filter(function (s) {
                return tags.indexOf(s) === -1 && !tags.some(function (t) { return t.toLowerCase() === s.toLowerCase(); });
            }).slice(0, 4);

            tagSuggestWrap.innerHTML = '';
            if (tags.length >= MAX_TAGS || remaining.length === 0) {
                tagSuggestWrap.hidden = true;
                return;
            }
            tagSuggestWrap.hidden = false;

            var label = document.createElement('span');
            label.className = 'label';
            label.textContent = 'Sugestões:';
            tagSuggestWrap.appendChild(label);

            remaining.forEach(function (s) {
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.textContent = '+ ' + s;
                btn.addEventListener('click', function () { addTag(s); });
                tagSuggestWrap.appendChild(btn);
            });
        }

        function renderAll() {
            renderTags();
            renderSuggestions();
            syncTagsHidden();
            updateChecklist();
        }

        function addTag(raw) {
            var label = (raw || '').trim().replace(/\s+/g, ' ');
            if (!label || tags.length >= MAX_TAGS) return;
            if (tags.some(function (t) { return t.toLowerCase() === label.toLowerCase(); })) {
                if (tagInput) tagInput.value = '';
                return;
            }
            tags.push(label);
            if (tagInput) tagInput.value = '';
            if (tags.length >= MAX_TAGS) closeTagForm();
            renderAll();
        }

        function openTagForm() {
            if (tagForm) tagForm.hidden = false;
            if (tagAddBtn) tagAddBtn.hidden = true;
            if (tagInput) tagInput.focus();
        }
        function closeTagForm() {
            if (tagForm) tagForm.hidden = true;
            if (tagAddBtn) tagAddBtn.hidden = tags.length >= MAX_TAGS;
        }

        if (tagAddBtn) tagAddBtn.addEventListener('click', openTagForm);
        if (tagFormClose) tagFormClose.addEventListener('click', closeTagForm);
        if (tagFormAdd) tagFormAdd.addEventListener('click', function () { addTag(tagInput ? tagInput.value : ''); });
        if (tagInput) {
            tagInput.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ',') { e.preventDefault(); addTag(tagInput.value.replace(/,$/, '')); }
                else if (e.key === 'Escape') { closeTagForm(); }
            });
        }

        renderAll();

        // ---------- Checklist + habilitar envio ----------
        function updateChecklist() {
            var titleOk = titleInput && titleInput.value.trim().length >= MIN_TITLE;
            var textOk = content && countWords(content) >= MIN_WORDS;
            var hasTags = tags.length > 0;
            var hasBanner = !!(bannerHiddenUrl && bannerHiddenUrl.value);

            root.querySelectorAll('[data-pp-check]').forEach(function (row) {
                var key = row.getAttribute('data-pp-check');
                var ok = key === 'title' ? titleOk : key === 'text' ? textOk : key === 'tags' ? hasTags : hasBanner;
                row.classList.toggle('done', !!ok);
                var box = row.querySelector('.box');
                if (box) box.textContent = ok ? '✓' : '';
            });

            var canSubmit = titleOk && textOk;
            if (submitBtn) submitBtn.disabled = !canSubmit;
            if (missingLabel) {
                if (canSubmit) {
                    missingLabel.textContent = 'Tudo certo para enviar.';
                } else {
                    var missing = [];
                    if (!titleOk) missing.push('título');
                    if (!textOk) missing.push('texto com pelo menos ' + MIN_WORDS + ' palavras');
                    missingLabel.textContent = 'Falta: ' + missing.join(' e ') + '.';
                }
            }
        }
        updateChecklist();

        // ---------- Submit ----------
        root.addEventListener('submit', function () {
            if (hiddenContent) hiddenContent.value = content.innerHTML;
            syncTagsHidden();
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-pp-root]').forEach(init);
    });
})();
