// cca service worker — versioned cache, cache-first for hashed framework assets,
// stale-while-revalidate for everything else inside scope. Cross-origin requests
// (Monaco CDN, GitHub API, fonts) are passthrough.

const CACHE_VERSION = '__CCA_BUILD_HASH__';
const CACHE_NAME = `cca-cache-${CACHE_VERSION}`;
const SCOPE_URL = new URL(self.registration.scope);

// Files we want to seed the cache with on install. Kept short; the rest is
// picked up opportunistically on first fetch via stale-while-revalidate.
const APP_SHELL = [
    './',
    'index.html',
    'css/app.css',
    'js/download.js',
    'js/pwa-register.js',
    'manifest.webmanifest',
];

self.addEventListener('install', (event) => {
    event.waitUntil((async () => {
        const cache = await caches.open(CACHE_NAME);
        await Promise.all(APP_SHELL.map(async (path) => {
            const url = new URL(path, SCOPE_URL).toString();
            try {
                const response = await fetch(url, { cache: 'reload' });
                if (response && response.ok) {
                    await cache.put(url, response.clone());
                }
            } catch {
                // Individual asset failure must not abort the install.
            }
        }));
        await self.skipWaiting();
    })());
});

self.addEventListener('activate', (event) => {
    event.waitUntil((async () => {
        const names = await caches.keys();
        await Promise.all(
            names
                .filter((n) => n.startsWith('cca-cache-') && n !== CACHE_NAME)
                .map((n) => caches.delete(n))
        );
        await self.clients.claim();
    })());
});

function isInScope(url) {
    return url.origin === SCOPE_URL.origin && url.pathname.startsWith(SCOPE_URL.pathname);
}

function isHashedFrameworkAsset(url) {
    // Blazor emits /_framework/* with content-hash query strings or hashed filenames.
    return url.pathname.includes('/_framework/');
}

async function cacheFirst(request) {
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);
    if (cached) return cached;
    const response = await fetch(request);
    if (response && response.ok && request.method === 'GET') {
        try { await cache.put(request, response.clone()); } catch { /* opaque or non-cacheable */ }
    }
    return response;
}

async function staleWhileRevalidate(request) {
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);
    const networkFetch = fetch(request).then((response) => {
        if (response && response.ok && request.method === 'GET') {
            cache.put(request, response.clone()).catch(() => { /* ignore */ });
        }
        return response;
    }).catch(() => undefined);
    return cached || networkFetch || fetch(request);
}

self.addEventListener('fetch', (event) => {
    const request = event.request;
    if (request.method !== 'GET') return;

    let url;
    try {
        url = new URL(request.url);
    } catch {
        return;
    }

    // Cross-origin (Monaco CDN, GitHub API, Google Fonts): pass through.
    if (!isInScope(url)) return;

    if (isHashedFrameworkAsset(url)) {
        event.respondWith(cacheFirst(request));
        return;
    }

    event.respondWith(staleWhileRevalidate(request));
});
