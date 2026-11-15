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

    // Valideringsfejl fra serveren: felterne markeres for skærmlæsere (aria-invalid og aria-describedby til fejlteksten),
    // oversigten læses op, og fokus flyttes til det første felt med fejl (WCAG 3.3.1).
    var invalid = Array.prototype.slice.call(document.querySelectorAll('.input-validation-error'));
    invalid.forEach(function (field) {
        field.setAttribute('aria-invalid', 'true');
        var message = field.name ? document.querySelector('[data-valmsg-for="' + field.name + '"]') : null;
        if (message) {
            if (!message.id) { message.id = 'fejl-' + field.name.replace(/[^A-Za-z0-9_-]/g, '-'); }
            field.setAttribute('aria-describedby', ((field.getAttribute('aria-describedby') || '') + ' ' + message.id).trim());
        }
    });
    document.querySelectorAll('.validation-summary-errors').forEach(function (summary) { summary.setAttribute('role', 'alert'); });
    if (invalid.length > 0 && !document.activeElement.closest('.validation-summary-errors')) {
        invalid[0].focus();
    }
})();
