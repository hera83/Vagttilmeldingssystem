// Live-opdatering via SignalR (/hubs/shifts).
// Serveren sender kun signaler ("noget har ændret sig"). Siden henter selv sin egen HTML igen og udskifter
// de dele, der er markeret med data-live-region. Så gælder sidens normale adgangskontrol altid.
//
//   data-live-region="navn"          området, der udskiftes
//   data-live-on="shiftsOpened ..."  hvilke signaler der opdaterer området
//   data-live-event-id="<guid>"      (valgfri) kun signaler for den begivenhed
//   data-live-url="/Signup"          (valgfri) GET-adressen for området (standard: sidens adresse)
(function () {
    'use strict';

    if (!window.signalR || !document.querySelector('[data-live-region]')) {
        return;
    }

    var status = document.querySelector('[data-live-status]');
    var pending = {};
    var timer = null;

    function showStatus(text) {
        if (!status) { return; }
        status.textContent = text;
        status.hidden = false;
    }

    function hideStatus() {
        if (status) { status.hidden = true; }
    }

    function regions() {
        return Array.prototype.slice.call(document.querySelectorAll('[data-live-region]'));
    }

    function wants(region, name, payload) {
        var on = (region.getAttribute('data-live-on') || '').split(/\s+/);
        if (on.indexOf(name) < 0) { return false; }
        var eventId = region.getAttribute('data-live-event-id');
        return !eventId || !payload || !payload.eventId || eventId === payload.eventId;
    }

    function refresh(names) {
        // Områder med samme adresse hentes samlet.
        var byUrl = {};
        names.forEach(function (name) {
            var region = document.querySelector('[data-live-region="' + name + '"]');
            if (!region) { return; }
            var url = region.getAttribute('data-live-url') || window.location.pathname + window.location.search;
            (byUrl[url] = byUrl[url] || []).push(name);
        });

        Object.keys(byUrl).forEach(function (url) {
            fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'text/html' } })
                .then(function (response) {
                    if (response.redirected && response.url.indexOf('/Account/Login') >= 0) {
                        showStatus('Du er blevet logget ud. Log ind igen for at se de nyeste tal.');
                        return null;
                    }
                    return response.ok ? response.text() : null;
                })
                .then(function (html) {
                    if (!html) { return; }
                    var doc = new DOMParser().parseFromString(html, 'text/html');
                    byUrl[url].forEach(function (name) { swap(name, doc); });
                })
                .catch(function () {
                    showStatus('Kunne ikke hente de nyeste tal. Prøver igen ved næste ændring.');
                });
        });
    }

    function swap(name, doc) {
        var current = document.querySelector('[data-live-region="' + name + '"]');
        var fresh = doc.querySelector('[data-live-region="' + name + '"]');
        if (!current || !fresh) { return; }

        // Bevar brugerens afkrydsninger (fx valgte vagter), så længe de stadig kan vælges.
        var checkedValues = Array.prototype.slice.call(current.querySelectorAll('input[type="checkbox"]:checked'))
            .map(function (input) { return input.value; });
        var focusedId = document.activeElement && current.contains(document.activeElement) ? document.activeElement.id : null;

        current.replaceWith(fresh);

        checkedValues.forEach(function (value) {
            var input = fresh.querySelector('input[type="checkbox"][value="' + value + '"]');
            if (input && !input.disabled) { input.checked = true; }
        });
        if (focusedId) {
            var focus = document.getElementById(focusedId);
            if (focus) { focus.focus(); }
        }

        fresh.dispatchEvent(new CustomEvent('live:updated', { bubbles: true }));
    }

    function schedule(name, payload) {
        regions().forEach(function (region) {
            if (wants(region, name, payload)) {
                pending[region.getAttribute('data-live-region')] = true;
            }
        });

        clearTimeout(timer);
        timer = setTimeout(function () {
            var names = Object.keys(pending);
            pending = {};
            if (names.length > 0) { refresh(names); }
        }, 300);
    }

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/shifts')
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.None)
        .build();

    ['shiftsOpened', 'shiftsChanged', 'eventChanged', 'myShiftsChanged'].forEach(function (name) {
        connection.on(name, function (payload) { schedule(name, payload); });
    });

    connection.onreconnecting(function () {
        // Uden net viser pwa.js bjælken "Du er offline" i stedet.
        if (navigator.onLine !== false) {
            showStatus('Forbindelsen er afbrudt – prøver igen …');
        }
    });

    connection.onreconnected(function () {
        hideStatus();
        // Noget kan være ændret, mens forbindelsen var væk.
        refreshAll();
    });

    connection.onclose(function () {
        if (navigator.onLine === false) {
            // Start igen, når nettet er tilbage, og hent de nyeste tal.
            window.addEventListener('online', function () { start(0, true); }, { once: true });
            return;
        }
        showStatus('Live-opdatering er stoppet. Genindlæs siden for at se de nyeste tal.');
    });

    function refreshAll() {
        refresh(regions().map(function (region) { return region.getAttribute('data-live-region'); }));
    }

    function start(attempt, refreshAfter) {
        connection.start().then(function () {
            hideStatus();
            if (refreshAfter) { refreshAll(); }
        }).catch(function () {
            if (attempt < 5) {
                setTimeout(function () { start(attempt + 1, refreshAfter); }, 2000 * (attempt + 1));
            } else {
                showStatus('Live-opdatering kunne ikke starte. Genindlæs siden for at se de nyeste tal.');
            }
        });
    }

    start(0);
})();
