// Ledige vagter: opdaterer panelet "Valgte vagter", mens brugeren krydser af.
// Kun visning. Serveren afgør alt ved Bekræft tilmelding, og formularen virker også uden JavaScript.
// Elementerne slås op hver gang, fordi live-opdateringen (realtime.js) kan udskifte hele formularen.
(function () {
    'use strict';

    function update() {
        var form = document.querySelector('[data-signup]');
        if (!form) {
            return;
        }

        var list = form.querySelector('[data-selected-list]');
        var count = form.querySelector('[data-selected-count]');
        var empty = form.querySelector('[data-selected-empty]');
        var submit = form.querySelector('[data-submit]');
        var profileComplete = form.getAttribute('data-profile-complete') === 'true';
        var checked = Array.prototype.slice.call(form.querySelectorAll('input[name="ShiftIds"]:checked'));

        list.textContent = '';
        checked.forEach(function (input) {
            var item = document.createElement('li');
            var title = document.createElement('strong');
            title.textContent = input.getAttribute('data-title');
            var when = document.createElement('span');
            when.className = 'muted small';
            when.textContent = input.getAttribute('data-when');
            item.appendChild(title);
            item.appendChild(when);
            list.appendChild(item);
        });

        form.querySelectorAll('input[name="ShiftIds"]').forEach(function (input) {
            var row = input.closest('tr');
            if (row) { row.classList.toggle('is-selected', input.checked); }
        });

        count.textContent = String(checked.length);
        empty.hidden = checked.length > 0;
        // Uden net kan der ikke tilmeldes (pwa.js viser bjælken "Du er offline").
        submit.disabled = !profileComplete || checked.length === 0 || navigator.onLine === false;
    }

    document.addEventListener('change', function (event) {
        if (event.target && event.target.name === 'ShiftIds') {
            update();
        }
    });

    document.addEventListener('live:updated', update);
    window.addEventListener('online', update);
    window.addEventListener('offline', update);

    // Undgå dobbelt-klik, der sender formularen to gange.
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (form instanceof HTMLFormElement && form.hasAttribute('data-signup') && !event.defaultPrevented) {
            var submit = form.querySelector('[data-submit]');
            if (submit) {
                submit.disabled = true;
                submit.setAttribute('aria-busy', 'true');
            }
        }
    });

    update();
})();
