// cca PWA registration. Registers the service worker on `load` and dispatches
// a `cca:sw-update` window event when a *replacement* worker is installed
// (i.e. an update — not the first-ever install).

// Bridge for Blazor: register a DotNetObjectReference whose [JSInvokable]
// "OnSwUpdate" method should be called when an update is detected. Created
// outside the IIFE so it survives even if `serviceWorker` is unsupported.
window.ccaPwa = window.ccaPwa || (() => {
    let dotnetRef = null;
    return {
        registerUpdateHandler(ref) { dotnetRef = ref; },
        clearUpdateHandler() { dotnetRef = null; },
        _invokeHandler() {
            if (dotnetRef) {
                try { dotnetRef.invokeMethodAsync('OnSwUpdate'); } catch { /* ignore */ }
            }
        }
    };
})();

(function () {
    if (!('serviceWorker' in navigator)) return;

    function notifyUpdateAvailable() {
        window.ccaPwa._invokeHandler();
        try {
            window.dispatchEvent(new CustomEvent('cca:sw-update'));
        } catch { /* ignore */ }
        if (typeof window.ccaShowUpdate === 'function') {
            try { window.ccaShowUpdate(); } catch { /* ignore */ }
        }
    }

    function watchInstall(reg) {
        const newWorker = reg.installing;
        if (!newWorker) return;
        newWorker.addEventListener('statechange', () => {
            if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                // Existing controller means this is an update, not a first install.
                notifyUpdateAvailable();
            }
        });
    }

    window.addEventListener('load', async () => {
        try {
            // Resolve relative to the document's <base href> so the worker scope
            // matches GitHub Pages' /CodeComplexityAnalyzer/ subpath in prod and
            // / in dev.
            const swUrl = new URL('service-worker.js', document.baseURI).toString();
            const reg = await navigator.serviceWorker.register(swUrl);

            if (reg.waiting && navigator.serviceWorker.controller) {
                // A waiting worker already exists from a previous tab/session.
                notifyUpdateAvailable();
            }

            reg.addEventListener('updatefound', () => watchInstall(reg));
        } catch (err) {
            console.warn('[cca] Service worker registration failed:', err);
        }
    });
})();
