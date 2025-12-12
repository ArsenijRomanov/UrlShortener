const API_BASE_URL = 'http://localhost:8080';

const ENDPOINTS = {
    CREATE: '/api/shortUrls',
    META: (code) => `/api/shortUrls/${encodeURIComponent(code)}/meta`
};

const LS_KEYS = {
    HISTORY: 'urlshort_history'
};

let history = [];

// ----------------- helpers -----------------
function $(id) { return document.getElementById(id); }

function toast(message, type = 'info') {
    const t = $('toast');
    t.className = `toast ${type}`;
    t.textContent = message;
    t.classList.remove('hidden');
    setTimeout(() => t.classList.add('hidden'), 3500);
}

function showError(containerId, message) {
    const el = $(containerId);
    el.textContent = message;
    el.classList.remove('hidden');
}

function clearError(containerId) {
    const el = $(containerId);
    el.textContent = '';
    el.classList.add('hidden');
}

function setHidden(id, hidden) {
    $(id).classList.toggle('hidden', hidden);
}

function parseCodeFromInput(codeOrUrl) {
    const v = (codeOrUrl || '').trim();
    if (!v) return '';

    if (v.includes('/')) {
        try {
            const u = new URL(v);
            const parts = u.pathname.split('/').filter(Boolean);
            return parts[parts.length - 1] || '';
        } catch {
            return v;
        }
    }
    return v;
}

function formatDate(iso) {
    try {
        return new Date(iso).toLocaleString('ru-RU');
    } catch {
        return String(iso);
    }
}

function isProbablyUrl(str) {
    try { new URL(str); return true; } catch { return false; }
}

// ----------------- api -----------------
async function apiCreate(longUrl, ttl) {
    const url = `${API_BASE_URL}${ENDPOINTS.CREATE}`;

    const body = { longUrl };
    if (ttl !== null && ttl !== undefined && ttl !== '') body.ttl = Number(ttl);

    const resp = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });

    if (!resp.ok) {
        let msg = `Ошибка: ${resp.status}`;
        try {
            const err = await resp.json();
            if (err?.error) msg = err.error;
        } catch { /* ignore */ }
        throw new Error(msg);
    }

    return await resp.json();
}

async function apiMeta(code) {
    const url = `${API_BASE_URL}${ENDPOINTS.META(code)}`;
    const resp = await fetch(url, { method: 'GET' });

    if (!resp.ok) {
        if (resp.status === 404) throw new Error('Ссылка не найдена (404)');
        if (resp.status === 410) throw new Error('Ссылка истекла (410)');
        throw new Error(`Ошибка получения метаданных: ${resp.status}`);
    }

    return await resp.json();
}

// ----------------- ui actions -----------------
async function createShortUrl() {
    clearError('createError');
    setHidden('createResult', true);

    const longUrl = $('longUrl').value.trim();
    const ttl = $('ttl').value;

    if (!longUrl) {
        showError('createError', 'Введите Long URL');
        return;
    }

    // только UX-проверка на фронте
    if (!isProbablyUrl(longUrl)) {
        showError('createError', 'Похоже, это не URL (проверь формат)');
        return;
    }

    const btn = $('createBtn');
    const oldText = btn.textContent;
    btn.textContent = 'Создание...';
    btn.disabled = true;

    try {
        const result = await apiCreate(longUrl, ttl);

        $('shortLink').href = result.shortUrl;
        $('shortLink').textContent = result.shortUrl;

        $('code').textContent = result.code;
        $('createdAt').textContent = formatDate(result.createdAt);
        $('expiresAt').textContent = formatDate(result.expiresAt);
        $('longUrlOut').textContent = result.longUrl;

        setHidden('createResult', false);

        addToHistory({
            id: Date.now(),
                     code: result.code,
                     shortUrl: result.shortUrl,
                     createdAt: result.createdAt,
                     expiresAt: result.expiresAt
        });

        toast('Ссылка создана', 'success');
    } catch (e) {
        showError('createError', e?.message || 'Ошибка создания');
    } finally {
        btn.textContent = oldText;
        btn.disabled = false;
    }
}

async function lookupMeta() {
    clearError('lookupError');
    setHidden('lookupResult', true);

    const input = $('codeOrUrl').value;
    const code = parseCodeFromInput(input);

    if (!code) {
        showError('lookupError', 'Введите code или короткую ссылку');
        return;
    }

    const btn = $('lookupBtn');
    const oldText = btn.textContent;
    btn.textContent = 'Поиск...';
    btn.disabled = true;

    try {
        const meta = await apiMeta(code);

        $('lookupCode').textContent = code;
        $('lookupCreatedAt').textContent = formatDate(meta.createdAt);
        $('lookupExpiresAt').textContent = formatDate(meta.expiresAt);

        const now = new Date();
        const exp = new Date(meta.expiresAt);
        const expired = exp < now;

        $('lookupStatus').textContent = expired ? 'Истекла' : 'Активна';
        $('lookupStatus').style.color = expired ? '#ff9b9b' : '#98f5c8';

        setHidden('lookupResult', false);
        toast('Метаданные получены', 'success');
    } catch (e) {
        showError('lookupError', e?.message || 'Ошибка');
    } finally {
        btn.textContent = oldText;
        btn.disabled = false;
    }
}

async function copyShortUrl() {
    const text = $('shortLink').textContent || '';
    if (!text) return;

    try {
        await navigator.clipboard.writeText(text);
        toast('Скопировано', 'success');
    } catch {
        toast('Не удалось скопировать', 'error');
    }
}

// ----------------- history -----------------
function loadHistory() {
    const raw = localStorage.getItem(LS_KEYS.HISTORY);
    if (!raw) return;
    try { history = JSON.parse(raw) || []; } catch { history = []; }
    renderHistory();
}

function saveHistory() {
    localStorage.setItem(LS_KEYS.HISTORY, JSON.stringify(history.slice(0, 10)));
}

function addToHistory(item) {
    history.unshift(item);
    history = history.slice(0, 10);
    saveHistory();
    renderHistory();
}

function clearHistory() {
    history = [];
    saveHistory();
    renderHistory();
    toast('История очищена', 'info');
}

function timeAgo(iso) {
    const d = new Date(iso);
    const s = Math.floor((Date.now() - d.getTime()) / 1000);
    if (Number.isNaN(s)) return '';
    if (s < 60) return 'только что';
    if (s < 3600) return `${Math.floor(s / 60)} мин. назад`;
    if (s < 86400) return `${Math.floor(s / 3600)} ч. назад`;
    return `${Math.floor(s / 86400)} дн. назад`;
}

function renderHistory() {
    const container = $('history');

    if (!history.length) {
        container.innerHTML = `<div class="muted">Пока пусто. Создай первую ссылку 🙂</div>`;
        return;
    }

    container.innerHTML = history.map(h => `
    <div class="history-item">
    <div>
    <div class="title">
    <a href="${h.shortUrl}" target="_blank" rel="noreferrer">${h.shortUrl}</a>
    </div>
    <div class="sub">
    code: <span class="mono">${h.code}</span> · ${timeAgo(h.createdAt)}
    · expires: ${formatDate(h.expiresAt)}
    </div>
    </div>
    <div class="row">
    <button class="btn secondary" type="button" data-copy="${h.shortUrl}">Copy</button>
    <button class="btn secondary" type="button" data-meta="${h.code}">Meta</button>
    </div>
    </div>
    `).join('');

    container.querySelectorAll('[data-copy]').forEach(btn => {
        btn.addEventListener('click', async () => {
            try {
                await navigator.clipboard.writeText(btn.getAttribute('data-copy'));
                toast('Скопировано', 'success');
            } catch {
                toast('Не удалось скопировать', 'error');
            }
        });
    });

    container.querySelectorAll('[data-meta]').forEach(btn => {
        btn.addEventListener('click', () => {
            $('codeOrUrl').value = btn.getAttribute('data-meta');
            lookupMeta();
        });
    });
}

// ----------------- init -----------------
document.addEventListener('DOMContentLoaded', () => {
    loadHistory();

    $('createBtn').addEventListener('click', createShortUrl);
    $('clearCreateBtn').addEventListener('click', () => {
        $('longUrl').value = '';
        $('ttl').value = '604800';
        clearError('createError');
        setHidden('createResult', true);
    });

    $('lookupBtn').addEventListener('click', lookupMeta);
    $('clearLookupBtn').addEventListener('click', () => {
        $('codeOrUrl').value = '';
        clearError('lookupError');
        setHidden('lookupResult', true);
    });

    $('copyBtn').addEventListener('click', copyShortUrl);
    $('clearHistoryBtn').addEventListener('click', clearHistory);

    $('longUrl').addEventListener('keypress', (e) => {
        if (e.key === 'Enter') createShortUrl();
    });

        $('codeOrUrl').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') lookupMeta();
        });
});
