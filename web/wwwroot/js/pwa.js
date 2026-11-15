// PWA: service worker, offline-bjælke og installation. Kun visning: serveren afgør alt, og siderne virker uden JavaScript.
//
//   data-offline-status        bjælken "Du er offline", vises kun uden net
//   data-install-button        knappen "Installér app", vises kun når browseren tilbyder installation (Android, Chrome/Edge)
//   data-install-installed     vises, når siden kører som installeret app
//   data-hide-when-installed   skjules, når siden kører som installeret app
//   data-platform="ios|android|desktop"  vejledningen for platformen; den aktuelle får klassen is-current
//                              og dens data-platform-hint vises
(function () {
    'use strict';

    // 1. Service worker (kræver https eller localhost).
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/service-worker.js').catch(function () {
                // Uden service worker virker siderne stadig, bare uden offline-side og installation.
            });
        });
    }

    // 2. Offline: bjælke, og formularer, der ændrer noget, sendes ikke uden net.
    function isOffline() {
        return navigator.onLine === false;
    }

    function updateOnline() {
        var offline = isOffline();
        document.documentElement.classList.toggle('is-offline', offline);
        document.querySelectorAll('[data-offline-status]').forEach(function (bar) { bar.hidden = !offline; });
    }

    window.addEventListener('online', updateOnline);
    window.addEventListener('offline', updateOnline);
    updateOnline();

    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement) || (form.getAttribute('method') || 'get').toLowerCase() !== 'post' || !isOffline()) {
            return;
        }
        event.preventDefault();
        event.stopPropagation();
        var bar = document.querySelector('[data-offline-status]');
        if (bar) {
            bar.hidden = false;
            bar.focus();
        }
    }, true);

    // 3. Installation.
    var deferredPrompt = null;
    var standalone = window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;

    function show(selector, visible) {
        document.querySelectorAll(selector).forEach(function (element) { element.hidden = !visible; });
    }

    function platform() {
        var ua = navigator.userAgent;
        if (/iPad|iPhone|iPod/.test(ua) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)) { return 'ios'; }
        if (/Android/.test(ua)) { return 'android'; }
        return 'desktop';
    }

    if (standalone) {
        show('[data-install-installed]', true);
        show('[data-hide-when-installed]', false);
    }

    var current = document.querySelector('[data-platform="' + platform() + '"]');
    if (current) {
        current.classList.add('is-current');
        current.querySelectorAll('[data-platform-hint]').forEach(function (hint) { hint.hidden = false; });
    }

    window.addEventListener('beforeinstallprompt', function (event) {
        // Vis vores egen knap i stedet for browserens bjælke.
        event.preventDefault();
        deferredPrompt = event;
        show('[data-install-button]', !standalone);
    });

    window.addEventListener('appinstalled', function () {
        deferredPrompt = null;
        show('[data-install-button]', false);
        show('[data-install-installed]', true);
    });

    document.addEventListener('click', function (event) {
        var button = event.target instanceof Element ? event.target.closest('[data-install-button]') : null;
        if (!button || !deferredPrompt) {
            return;
        }
        var prompt = deferredPrompt;
        deferredPrompt = null;
        prompt.prompt();
        prompt.userChoice.finally(function () { show('[data-install-button]', false); });
    });
})();
