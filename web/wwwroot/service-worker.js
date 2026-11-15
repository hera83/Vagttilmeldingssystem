// Service worker for Vagttilmelding (PWA).
//
// Regler (step 09):
// - Kun statiske filer (css, js, ikoner, manifest) og offline-siden caches. Sider, POST-svar, live-opdateringer
//   og alt med persondata hentes altid fra netværket og gemmes aldrig.
// - Sider (navigationer) hentes fra netværket. Fejler netværket, vises /offline.html.
// - Push (step 10): viser beskeden som notifikation og åbner adressen i appen, når der trykkes på den.
// - Statiske filer: stale-while-revalidate. Filer med versionsnøgle i adressen (asp-append-version) skifter
//   adresse, når indholdet ændres, så der aldrig bruges en forældet version på en ny side.
'use strict';

const CACHE = 'vagttilmelding-static-v1';
const OFFLINE_URL = '/offline.html';
const PRECACHE = [
    OFFLINE_URL,
    '/css/site.css',
    '/favicon.svg',
    '/icons/icon.svg',
    '/icons/icon-192.png',
    '/manifest.webmanifest'
];
const STATIC_PREFIXES = ['/css/', '/js/', '/lib/', '/icons/'];
const STATIC_FILES = ['/favicon.svg', '/manifest.webmanifest'];
const MAX_ENTRIES = 60;
const PRECACHE_REFRESH_MS = 60 * 60 * 1000;

let lastPrecacheRefresh = 0;

async function precache() {
    const cache = await caches.open(CACHE);
    // no-cache: hent altid den aktuelle version fra serveren, ikke fra browserens HTTP-cache.
    await cache.addAll(PRECACHE.map((url) => new Request(url, { cache: 'no-cache' })));
    lastPrecacheRefresh = Date.now();
}

async function trim(cache) {
    const keys = await cache.keys();
    const precached = new Set(PRECACHE.map((url) => new URL(url, self.location.origin).href));
    const removable = keys.filter((request) => !precached.has(request.url));
    for (let i = 0; i < removable.length - MAX_ENTRIES; i++) {
        await cache.delete(removable[i]);
    }
}

function isStatic(url) {
    return url.origin === self.location.origin &&
        (STATIC_FILES.includes(url.pathname) || STATIC_PREFIXES.some((prefix) => url.pathname.startsWith(prefix)));
}

async function staleWhileRevalidate(event) {
    const cache = await caches.open(CACHE);
    const cached = await cache.match(event.request);
    const network = fetch(event.request)
        .then(async (response) => {
            if (response.ok && response.type === 'basic') {
                await cache.put(event.request, response.clone());
                await trim(cache);
            }
            return response;
        })
        .catch(() => cached);

    if (cached) {
        event.waitUntil(network);
        return cached;
    }
    return network;
}

async function networkWithOfflineFallback(event) {
    try {
        const response = await fetch(event.request);
        // Hold offline-siden og dens css friske, mens der er net (højst én gang i timen).
        if (Date.now() - lastPrecacheRefresh > PRECACHE_REFRESH_MS) {
            event.waitUntil(precache().catch(() => undefined));
        }
        return response;
    } catch {
        const offline = await caches.match(OFFLINE_URL);
        return offline || new Response('Du er offline.', { status: 503, headers: { 'Content-Type': 'text/plain; charset=utf-8' } });
    }
}

self.addEventListener('install', (event) => {
    event.waitUntil(precache().then(() => self.skipWaiting()));
});

self.addEventListener('activate', (event) => {
    event.waitUntil((async () => {
        const names = await caches.keys();
        await Promise.all(names.filter((name) => name !== CACHE).map((name) => caches.delete(name)));
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', (event) => {
    const request = event.request;

    // Sider (også POST af formularer): altid netværket, offline-siden ved netværksfejl. Gemmes aldrig.
    if (request.mode === 'navigate') {
        event.respondWith(networkWithOfflineFallback(event));
        return;
    }

    if (request.method !== 'GET') {
        return;
    }

    const url = new URL(request.url);
    if (isStatic(url)) {
        event.respondWith(staleWhileRevalidate(event));
    }

    // Alt andet (live-opdateringer, /hubs/shifts, data) går direkte til netværket uden service workeren.
});

// Push: serveren sender JSON { title, body, url, tag }. Indholdet er altid generisk (ingen persondata).
self.addEventListener('push', (event) => {
    let data = {};
    try {
        data = event.data ? event.data.json() : {};
    } catch {
        data = { body: event.data ? event.data.text() : '' };
    }

    event.waitUntil(self.registration.showNotification(data.title || 'Vagttilmelding', {
        body: data.body || '',
        icon: '/icons/icon-192.png',
        badge: '/icons/icon-192.png',
        lang: 'da',
        tag: data.tag || undefined,
        renotify: Boolean(data.tag),
        data: { url: data.url || '/' }
    }));
});

// Tryk på notifikationen: brug et åbent vindue af appen, ellers åbn et nyt. Kun adresser i appen.
self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    let target = new URL((event.notification.data && event.notification.data.url) || '/', self.location.origin);
    if (target.origin !== self.location.origin) {
        target = new URL('/', self.location.origin);
    }

    event.waitUntil((async () => {
        const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        for (const client of windows) {
            if (new URL(client.url).origin === self.location.origin && 'focus' in client) {
                await client.focus();
                if ('navigate' in client) {
                    await client.navigate(target.href);
                }
                return;
            }
        }
        await self.clients.openWindow(target.href);
    })());
});
