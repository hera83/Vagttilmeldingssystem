// Fælles, små forbedringer. Siderne virker også uden JavaScript.
(function () {
    'use strict';

    // Formularer med data-confirm="…" beder om bekræftelse, før de sendes (fx sletning).
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }
        var message = form.getAttribute('data-confirm');
        if (message && !window.confirm(message)) {
            event.preventDefault();
        }
    });
})();
