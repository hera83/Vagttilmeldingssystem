// Ledige vagter: opdaterer panelet "Valgte vagter", mens brugeren krydser af.
// Kun visning. Serveren afgør alt ved Bekræft tilmelding, og formularen virker også uden JavaScript.
(function () {
    'use strict';

    var form = document.querySelector('[data-signup]');
    if (!form) {
        return;
    }

    var list = form.querySelector('[data-selected-list]');
    var count = form.querySelector('[data-selected-count]');
    var empty = form.querySelector('[data-selected-empty]');
    var submit = form.querySelector('[data-submit]');
    var profileComplete = form.getAttribute('data-profile-complete') === 'true';

    function update() {
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

            var row = input.closest('tr');
            if (row) { row.classList.add('is-selected'); }
        });

        form.querySelectorAll('input[name="ShiftIds"]:not(:checked)').forEach(function (input) {
            var row = input.closest('tr');
            if (row) { row.classList.remove('is-selected'); }
        });

        count.textContent = String(checked.length);
        empty.hidden = checked.length > 0;
        submit.disabled = !profileComplete || checked.length === 0;
    }

    form.addEventListener('change', function (event) {
        if (event.target && event.target.name === 'ShiftIds') {
            update();
        }
    });

    // Undgå dobbelt-klik, der sender formularen to gange.
    form.addEventListener('submit', function () {
        submit.disabled = true;
        submit.setAttribute('aria-busy', 'true');
    });

    update();
})();
