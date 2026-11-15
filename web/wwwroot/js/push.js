// Push-panelet på Min profil: slå push til og fra på denne enhed og send en testbesked.
// Tilladelsen spørges der først om, når brugeren klikker på "Slå push til" (aldrig ved sidevisning).
//
//   data-push-panel     panelet
//   data-push-status    status-badge: Aktiv / Ikke slået til / Blokeret / Ikke understøttet / Ikke sat op
//   data-push-hint      forklaring under status
//   data-push-enable, data-push-disable, data-push-test   knapperne
(function () {
    'use strict';

    var panel = document.querySelector('[data-push-panel]');
    if (!panel) {
        return;
    }

    var statusBadge = panel.querySelector('[data-push-status]');
    var hint = panel.querySelector('[data-push-hint]');
    var enableButton = panel.querySelector('[data-push-enable]');
    var disableButton = panel.querySelector('[data-push-disable]');
    var testButton = panel.querySelector('[data-push-test]');
    var publicKey = '';

    var isIos = /iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    var standalone = window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;

    var states = {
        on: { text: 'Aktiv', badge: 'badge-open', enable: false, disable: true, test: true },
        off: { text: 'Ikke slået til', badge: 'badge-neutral', enable: true, disable: false, test: false },
        denied: { text: 'Blokeret i browseren', badge: 'badge-warning', enable: false, disable: false, test: false },
        unsupported: { text: 'Ikke understøttet', badge: 'badge-neutral', enable: false, disable: false, test: false },
        disabled: { text: 'Ikke sat op', badge: 'badge-neutral', enable: false, disable: false, test: false },
        busy: { text: 'Vent …', badge: 'badge-neutral', enable: false, disable: false, test: false }
    };

    function setState(name, message) {
        var state = states[name];
        statusBadge.textContent = state.text;
        statusBadge.className = 'badge ' + state.badge;
        enableButton.hidden = !state.enable;
        disableButton.hidden = !state.disable;
        testButton.hidden = !state.test;
        showHint(message);
    }

    function showHint(message) {
        hint.textContent = message || '';
        hint.hidden = !message;
    }

    function token() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function post(url, body) {
        return fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
            body: JSON.stringify(body || {})
        });
    }

    function base64UrlToBytes(value) {
        var padded = (value + '==='.slice((value.length + 3) % 4)).replace(/-/g, '+').replace(/_/g, '/');
        var raw = window.atob(padded);
        var bytes = new Uint8Array(raw.length);
        for (var i = 0; i < raw.length; i++) { bytes[i] = raw.charCodeAt(i); }
        return bytes;
    }

    function sameKey(subscription) {
        var key = subscription.options && subscription.options.applicationServerKey;
        if (!key) { return true; }
        var current = new Uint8Array(key);
        var expected = base64UrlToBytes(publicKey);
        if (current.length !== expected.length) { return false; }
        for (var i = 0; i < current.length; i++) {
            if (current[i] !== expected[i]) { return false; }
        }
        return true;
    }

    function saveSubscription(subscription) {
        return post('/Push/Subscribe', subscription.toJSON()).then(function (response) {
            if (!response.ok) { throw new Error('Serveren svarede ' + response.status); }
        });
    }

    function subscribe(registration) {
        return registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: base64UrlToBytes(publicKey) })
            .then(saveSubscription);
    }

    function unsupported() {
        if (isIos && !standalone) {
            setState('unsupported', 'På iPhone og iPad: føj først appen til hjemmeskærmen (kræver iOS 16.4 eller nyere), og åbn den derfra. Se Installér appen.');
        } else {
            setState('unsupported', 'Denne browser kan ikke modtage push-beskeder. Du kan stadig se nye vagter under Ledige vagter.');
        }
    }

    function refresh() {
        if (!('serviceWorker' in navigator) || !('PushManager' in window) || !('Notification' in window)) {
            unsupported();
            return Promise.resolve();
        }

        return fetch('/Push/Config', { credentials: 'same-origin', headers: { 'Accept': 'application/json' } })
            .then(function (response) { return response.json(); })
            .then(function (config) {
                if (!config.enabled) {
                    setState('disabled', 'Push er ikke sat op på serveren endnu. Kontakt administratoren.');
                    return null;
                }
                publicKey = config.publicKey;
                if (Notification.permission === 'denied') {
                    setState('denied', 'Du har blokeret beskeder fra Vagttilmelding. Tillad notifikationer for siden i browserens indstillinger, og genindlæs siden.');
                    return null;
                }
                return navigator.serviceWorker.ready.then(function (registration) {
                    return registration.pushManager.getSubscription().then(function (subscription) {
                        if (!subscription || Notification.permission !== 'granted') {
                            setState('off');
                            return null;
                        }
                        if (!sameKey(subscription)) {
                            // Serverens nøgler er skiftet: tilmeld enheden igen med den nye nøgle.
                            return subscription.unsubscribe().then(function () { return subscribe(registration); })
                                .then(function () { setState('on'); });
                        }
                        // Sørg for, at serveren kender abonnementet (fx efter login som en anden bruger på enheden).
                        return saveSubscription(subscription).then(function () { setState('on'); });
                    });
                });
            })
            .catch(function () {
                setState('off', 'Kunne ikke hente status for push. Prøv igen senere.');
            });
    }

    enableButton.addEventListener('click', function () {
        setState('busy');
        Notification.requestPermission().then(function (permission) {
            if (permission === 'denied') {
                setState('denied', 'Du har blokeret beskeder fra Vagttilmelding. Tillad notifikationer for siden i browserens indstillinger.');
                return null;
            }
            if (permission !== 'granted') {
                setState('off', 'Push blev ikke slået til.');
                return null;
            }
            return navigator.serviceWorker.ready
                .then(subscribe)
                .then(function () { setState('on', 'Push er slået til. Du får besked, når der åbnes for nye vagter.'); });
        }).catch(function () {
            setState('off', 'Push kunne ikke slås til. Prøv igen senere.');
        });
    });

    disableButton.addEventListener('click', function () {
        setState('busy');
        navigator.serviceWorker.ready
            .then(function (registration) { return registration.pushManager.getSubscription(); })
            .then(function (subscription) {
                if (!subscription) { return null; }
                var endpoint = subscription.endpoint;
                return subscription.unsubscribe().then(function () { return post('/Push/Unsubscribe', { endpoint: endpoint }); });
            })
            .then(function () { setState('off', 'Push er slået fra på denne enhed.'); })
            .catch(function () { setState('on', 'Push kunne ikke slås fra. Prøv igen.'); });
    });

    testButton.addEventListener('click', function () {
        testButton.disabled = true;
        post('/Push/Test').then(function (response) {
            if (response.ok) {
                showHint('Testbeskeden er sendt. Den kommer normalt inden for få sekunder.');
            } else if (response.status === 404) {
                showHint('Serveren kender ikke denne enhed. Slå push fra og til igen.');
            } else {
                showHint('Testbeskeden kunne ikke sendes.');
            }
        }).catch(function () {
            showHint('Testbeskeden kunne ikke sendes.');
        }).finally(function () {
            setTimeout(function () { testButton.disabled = false; }, 5000);
        });
    });

    setState('busy');
    refresh();
})();
