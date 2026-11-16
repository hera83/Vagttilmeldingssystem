// Fælles, små forbedringer. Siderne virker også uden JavaScript.
(function () {
    'use strict';

    // ---------- Toast-beskeder ----------
    // window.showToast(tekst, { type, duration }) viser en kort besked øverst til højre (på mobil øverst), som lukker af sig selv.
    //   type      'success' | 'error' | 'warning' | 'info' (standard 'info')
    //   duration  millisekunder. Standard: efter tekstens længde, mindst 5 sek. (fejl og advarsler 8 sek.), højst 15 sek.
    //             0 = bliver stående, til brugeren lukker den.
    // Returnerer { close() }. Uret står stille, mens musen er over beskeden, den har fokus, eller fanen er skjult.
    // Skærmlæsere får beskeden læst op via de skjulte live-regioner i _Toasts (fejl med det samme).
    // Fra serveren: <template data-toast="success">Tekst</template> vises ved sidevisning (sådan vises TempData i _Toasts).
    var svgNs = 'http://www.w3.org/2000/svg';
    var toastRegion = document.querySelector('[data-toast-region]');
    var toastIcons = { success: 'check-circle', error: 'alert-triangle', warning: 'alert-triangle', info: 'info' };
    var maxToasts = 4;
    var activeToasts = [];

    function icon(name) {
        var svg = document.createElementNS(svgNs, 'svg');
        svg.setAttribute('class', 'icon');
        svg.setAttribute('aria-hidden', 'true');
        var use = document.createElementNS(svgNs, 'use');
        use.setAttribute('href', '#i-' + name);
        svg.appendChild(use);
        return svg;
    }

    function defaultDuration(text, type) {
        var minimum = type === 'error' || type === 'warning' ? 8000 : 5000;
        return Math.min(Math.max(minimum, 1500 + text.length * 60), 15000);
    }

    function announce(text, politeness) {
        var region = document.querySelector('[data-toast-announce="' + politeness + '"]');
        if (!region) { return; }
        // Lidt forsinket, så skærmlæseren opdager ændringen, også lige efter sidevisning.
        setTimeout(function () {
            var line = document.createElement('p');
            line.textContent = text;
            region.appendChild(line);
            setTimeout(function () { line.remove(); }, 15000);
        }, 150);
    }

    function showToast(message, options) {
        options = options || {};
        var type = toastIcons.hasOwnProperty(options.type) ? options.type : 'info';
        var text = String(message == null ? '' : message).replace(/\s+/g, ' ').trim();
        if (!toastRegion || !text) {
            return { close: function () { } };
        }
        var duration = typeof options.duration === 'number' && options.duration >= 0 ? options.duration : defaultDuration(text, type);

        var toast = document.createElement('div');
        toast.className = 'toast toast-' + type;
        toast.appendChild(icon(toastIcons[type]));
        var body = document.createElement('p');
        body.className = 'toast-message';
        body.textContent = text;
        toast.appendChild(body);
        var closeButton = document.createElement('button');
        closeButton.type = 'button';
        closeButton.className = 'toast-close';
        closeButton.setAttribute('aria-label', 'Luk besked');
        closeButton.appendChild(icon('x'));
        toast.appendChild(closeButton);
        if (duration > 0) {
            var progress = document.createElement('span');
            progress.className = 'toast-progress';
            progress.style.animationDuration = duration + 'ms';
            toast.appendChild(progress);
        }

        var remaining = duration;
        var startedAt = 0;
        var timer = null;
        var hovered = false;
        var focused = false;
        var closed = false;

        function sync() {
            if (closed || duration <= 0) { return; }
            var run = !hovered && !focused && document.visibilityState !== 'hidden';
            if (run && !timer) {
                startedAt = Date.now();
                timer = setTimeout(close, remaining);
                toast.classList.remove('is-paused');
            } else if (!run && timer) {
                clearTimeout(timer);
                timer = null;
                remaining = Math.max(0, remaining - (Date.now() - startedAt));
                toast.classList.add('is-paused');
            }
        }

        function close() {
            if (closed) { return; }
            closed = true;
            clearTimeout(timer);
            activeToasts.splice(activeToasts.indexOf(handle), 1);
            var hadFocus = toast.contains(document.activeElement);
            toast.classList.add('is-leaving');
            setTimeout(function () { toast.remove(); }, 200);
            if (hadFocus) {
                var main = document.getElementById('main');
                if (main) { main.focus(); }
            }
        }

        var handle = { close: close, sync: sync };
        closeButton.addEventListener('click', close);
        toast.addEventListener('mouseenter', function () { hovered = true; sync(); });
        toast.addEventListener('mouseleave', function () { hovered = false; sync(); });
        toast.addEventListener('focusin', function () { focused = true; sync(); });
        toast.addEventListener('focusout', function (event) {
            if (!toast.contains(event.relatedTarget)) { focused = false; sync(); }
        });

        while (activeToasts.length >= maxToasts) { activeToasts[0].close(); }
        activeToasts.push(handle);
        toastRegion.appendChild(toast);
        announce(text, type === 'error' ? 'assertive' : 'polite');
        sync();
        return { close: close };
    }

    window.showToast = showToast;

    document.addEventListener('visibilitychange', function () {
        activeToasts.slice().forEach(function (toast) { toast.sync(); });
    });

    document.querySelectorAll('template[data-toast]').forEach(function (template) {
        showToast(template.content.textContent, { type: template.getAttribute('data-toast') });
        template.remove();
    });

    // Formularer med data-confirm="…" beder om bekræftelse i dialogen fra _ConfirmDialog, før de sendes (fx sletning).
    // data-confirm-title er dialogens overskrift. Dialogens knap får samme tekst som formularens knap og er rød,
    // når formularens knap er det (btn-danger). Fokus starter på Annuller. Esc, Luk og klik uden for dialogen fortryder.
    var dialog = document.querySelector('[data-confirm-dialog]');
    var useDialog = !!dialog && typeof dialog.showModal === 'function';
    var confirmTitle = useDialog ? dialog.querySelector('[data-confirm-title]') : null;
    var confirmMessage = useDialog ? dialog.querySelector('[data-confirm-message]') : null;
    var confirmOk = useDialog ? dialog.querySelector('[data-confirm-ok]') : null;
    var confirmDefault = useDialog ? dialog.querySelector('[data-confirm-default]') : null;
    var pending = null;
    var confirmedForm = null;

    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement) || form === confirmedForm) {
            return;
        }
        var message = form.getAttribute('data-confirm');
        if (!message) {
            return;
        }
        if (!useDialog) {
            // Meget gamle browsere uden <dialog>.
            if (!window.confirm(message)) { event.preventDefault(); }
            return;
        }
        event.preventDefault();
        openConfirm(form, event.submitter || form.querySelector('[type="submit"]'), message);
    });

    function openConfirm(form, submitter, message) {
        pending = { form: form, submitter: submitter };
        var danger = !!submitter && submitter.classList.contains('btn-danger');
        confirmTitle.textContent = form.getAttribute('data-confirm-title') || 'Er du sikker?';
        confirmMessage.textContent = message;
        confirmOk.textContent = visibleText(submitter) || 'Bekræft';
        confirmOk.classList.toggle('btn-danger-solid', danger);
        confirmOk.classList.toggle('btn-primary', !danger);
        dialog.returnValue = '';
        dialog.showModal();
        confirmDefault.focus();
    }

    // Knappens synlige tekst uden ikon og skjult skærmlæsertekst, fx "Slet" og ikke "Slet Anna Jensen".
    function visibleText(button) {
        if (!button) { return ''; }
        var copy = button.cloneNode(true);
        copy.querySelectorAll('.visually-hidden').forEach(function (hidden) { hidden.remove(); });
        return copy.textContent.replace(/\s+/g, ' ').trim();
    }

    function submitConfirmed(form, submitter) {
        // En live-opdatering kan have skiftet formularen ud, mens dialogen var åben. Så sendes den oprindelige (skjult).
        if (!form.isConnected) {
            form.hidden = true;
            document.body.appendChild(form);
        }
        if (typeof form.requestSubmit !== 'function') {
            form.submit();
            return;
        }
        // requestSubmit sender submit-hændelsen med det samme, så andre lyttere (fx offline-spærringen) kører stadig.
        confirmedForm = form;
        try {
            form.requestSubmit(submitter && submitter.form === form ? submitter : null);
        } finally {
            confirmedForm = null;
        }
    }

    if (useDialog) {
        confirmOk.addEventListener('click', function () { dialog.close('confirm'); });
        dialog.querySelectorAll('[data-confirm-cancel]').forEach(function (button) {
            button.addEventListener('click', function () { dialog.close('cancel'); });
        });
        // Klik på den mørke baggrund rammer selve <dialog>-elementet.
        dialog.addEventListener('click', function (event) {
            if (event.target === dialog) { dialog.close('cancel'); }
        });
        dialog.addEventListener('close', function () {
            var current = pending;
            pending = null;
            if (!current) { return; }
            if (dialog.returnValue === 'confirm') {
                submitConfirmed(current.form, current.submitter);
            } else if (current.submitter && current.submitter.isConnected) {
                current.submitter.focus();
            }
        });
    }

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
