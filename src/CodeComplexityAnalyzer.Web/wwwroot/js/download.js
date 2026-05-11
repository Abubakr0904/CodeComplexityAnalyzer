window.ccaDownload = (filename, content, mimeType) => {
    const blob = new Blob([content], { type: mimeType || 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.ccaStorage = {
    get: (key) => {
        const raw = localStorage.getItem(key);
        return raw ?? null;
    },
    set: (key, value) => {
        localStorage.setItem(key, value);
    },
    remove: (key) => {
        localStorage.removeItem(key);
    }
};

(() => {
    let pendingPromise = null;

    document.addEventListener('drop', (e) => {
        if (!e.dataTransfer || !e.dataTransfer.files || e.dataTransfer.files.length === 0) return;
        e.preventDefault();
        const files = Array.from(e.dataTransfer.files);
        pendingPromise = Promise.all(files.map(async (f) => ({
            name: f.name,
            content: await f.text()
        })));
    }, true);

    document.addEventListener('dragover', (e) => {
        if (e.dataTransfer && e.dataTransfer.types && e.dataTransfer.types.includes('Files')) {
            e.preventDefault();
        }
    }, true);

    window.ccaConsumeDroppedFiles = async () => {
        if (!pendingPromise) return [];
        const out = await pendingPromise;
        pendingPromise = null;
        return out;
    };
})();

window.ccaMonaco = (() => {
    const editors = new Map(); // elementId -> monaco editor instance
    const MONACO_BASE = 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs';
    let monacoReady = null;
    let loaderReady = null;

    // Inject Monaco's AMD loader on demand. Removed from <head> so it no longer
    // blocks the initial page paint or competes with Blazor's WASM download.
    function loadLoaderScript() {
        // `typeof require` alone is not sufficient — pages might embed
        // RequireJS or another AMD loader. Skip injection only if Monaco's
        // loader appears present (Monaco's loader exposes require.config as a
        // function) or if monaco itself is already on the global.
        if (typeof window.monaco !== 'undefined') return Promise.resolve();
        if (typeof require !== 'undefined' && typeof require.config === 'function') return Promise.resolve();
        if (loaderReady) return loaderReady;
        loaderReady = new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = `${MONACO_BASE}/loader.js`;
            script.async = true;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error('Failed to load Monaco loader'));
            document.head.appendChild(script);
        });
        return loaderReady;
    }

    function ensureMonaco() {
        if (monacoReady) return monacoReady;
        monacoReady = loadLoaderScript().then(() =>
            new Promise((resolve) => {
                require.config({ paths: { 'vs': MONACO_BASE } });
                require(['vs/editor/editor.main'], () => resolve());
            })
        );
        return monacoReady;
    }

    return {
        async create(elementId, initialValue, dotnetRef) {
            await ensureMonaco();
            const el = document.getElementById(elementId);
            if (!el) return false;
            const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
            const editor = monaco.editor.create(el, {
                value: initialValue || '',
                language: 'csharp',
                theme: isDark ? 'vs-dark' : 'vs',
                automaticLayout: true,
                minimap: { enabled: false },
                fontSize: 14,
                lineNumbers: 'on',
                scrollBeyondLastLine: false,
                tabSize: 4,
                wordWrap: 'off'
            });
            editor.onDidChangeModelContent(() => {
                dotnetRef.invokeMethodAsync('OnEditorContentChanged', editor.getValue());
            });
            editors.set(elementId, editor);
            return true;
        },
        setTheme(theme) {
            if (typeof monaco !== 'undefined' && monaco.editor && monaco.editor.setTheme) {
                monaco.editor.setTheme(theme);
            }
        },
        setValue(elementId, value) {
            const ed = editors.get(elementId);
            if (ed && ed.getValue() !== value) {
                ed.setValue(value || '');
            }
        },
        dispose(elementId) {
            const ed = editors.get(elementId);
            if (ed) {
                ed.dispose();
                editors.delete(elementId);
            }
        },
        revealLine(elementId, lineNumber) {
            const ed = editors.get(elementId);
            if (!ed) return false;
            ed.revealLineInCenter(lineNumber);
            ed.setPosition({ lineNumber: lineNumber, column: 1 });
            ed.focus();
            const decoration = ed.createDecorationsCollection([{
                range: new monaco.Range(lineNumber, 1, lineNumber, 1),
                options: {
                    isWholeLine: true,
                    className: 'cca-highlighted-line',
                }
            }]);
            setTimeout(() => decoration.clear(), 2000);
            return true;
        }
    };
})();

window.ccaTheme = (() => {
    const KEY = 'cca-theme';
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    function resolveEffective(setting) {
        if (setting === 'dark') return 'dark';
        if (setting === 'light') return 'light';
        return mediaQuery.matches ? 'dark' : 'light';
    }

    function apply(setting) {
        const effective = resolveEffective(setting);
        document.documentElement.setAttribute('data-theme', effective);
        return effective;
    }

    return {
        init() {
            const saved = localStorage.getItem(KEY) || 'system';
            const effective = apply(saved);
            mediaQuery.addEventListener('change', () => {
                const current = localStorage.getItem(KEY) || 'system';
                if (current === 'system') {
                    const newEffective = apply('system');
                    if (typeof monaco !== 'undefined' && monaco.editor && monaco.editor.setTheme) {
                        monaco.editor.setTheme(newEffective === 'dark' ? 'vs-dark' : 'vs');
                    }
                }
            });
            return { setting: saved, effective };
        },
        set(setting) {
            localStorage.setItem(KEY, setting);
            return apply(setting);
        },
        get() {
            return localStorage.getItem(KEY) || 'system';
        },
        getEffective() {
            const setting = localStorage.getItem(KEY) || 'system';
            return resolveEffective(setting);
        }
    };
})();
