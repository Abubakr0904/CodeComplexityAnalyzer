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
