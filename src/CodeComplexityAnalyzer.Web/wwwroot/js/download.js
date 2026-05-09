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
    let monacoReady = null;

    function ensureMonaco() {
        if (monacoReady) return monacoReady;
        monacoReady = new Promise((resolve) => {
            require.config({ paths: { 'vs': 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs' } });
            require(['vs/editor/editor.main'], () => resolve());
        });
        return monacoReady;
    }

    return {
        async create(elementId, initialValue, dotnetRef) {
            await ensureMonaco();
            const el = document.getElementById(elementId);
            if (!el) return false;
            const editor = monaco.editor.create(el, {
                value: initialValue || '',
                language: 'csharp',
                theme: 'vs',
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
        }
    };
})();
