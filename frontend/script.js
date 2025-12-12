const API_CONFIG = {
    BASE_URL: 'http://localhost:8080',
    ENDPOINTS: {
        CREATE: '/api/shortUrls',
        REDIRECT: '/api/shortUrls/{code}',
        META: '/api/shortUrls/{code}/meta'
    },
    SHORT_URL_BASE: 'https://short.my'
};

let history = [];

// ==================== СОЗДАНИЕ КОРОТКОЙ ССЫЛКИ ====================
async function createShortUrl() {
    const urlInput = document.getElementById('urlInput');
    const originalUrl = urlInput.value.trim();
    
    if (!originalUrl) {
        showError('Введите ссылку');
        return;
    }
    
    if (!isValidUrl(originalUrl)) {
        showError('Введите корректную ссылку');
        return;
    }
    
    const createBtn = document.querySelector('.create-btn');
    const originalBtnText = createBtn.innerHTML;
    createBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Создание...';
    createBtn.disabled = true;
    
    try {
        const requestData = {
            longUrl: originalUrl,
            ttl: 604800
        };
        
        const response = await fetch(`${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.CREATE}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
        });
        
        if (!response.ok) {
            let errorMessage = `Ошибка: ${response.status}`;
            try {
                const errorData = await response.json();
                if (errorData.error) {
                    errorMessage = errorData.error;
                }
            } catch (e) {}
            throw new Error(errorMessage);
        }
        
        const result = await response.json();
        
        // Получаем метаданные для отображения в плашке
        const metaResponse = await fetch(
            `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.META.replace('{code}', result.code)}`
        );
        
        if (!metaResponse.ok) {
            throw new Error('Не удалось получить метаданные');
        }
        
        const metaData = await metaResponse.json();
        
        const urlData = {
            id: Date.now(),
            code: result.code,
            shortUrl: result.shortUrl,
            createdAt: result.createdAt,
            expiresAt: result.expiresAt
        };
        
        // Показываем результат создания
        showCreationResult(urlData);
        
        // Добавляем в историю
        addToHistory(urlData);
        
        // Очищаем поле ввода
        urlInput.value = '';
        
        showNotification('Ссылка создана!', 'success');
        
    } catch (error) {
        showError(error.message || 'Ошибка создания');
        
    } finally {
        createBtn.innerHTML = originalBtnText;
        createBtn.disabled = false;
    }
}

function showCreationResult(data) {
    const createdAt = new Date(data.createdAt).toLocaleString('ru-RU');
    const expiresAt = new Date(data.expiresAt).toLocaleString('ru-RU');
    
    document.getElementById('shortLink').href = data.shortUrl;
    document.getElementById('shortLink').textContent = data.shortUrl;
    document.getElementById('urlCode').textContent = data.code;
    document.getElementById('createdAt').textContent = createdAt;
    document.getElementById('expiresAt').textContent = expiresAt;
    
    document.getElementById('result').classList.remove('hidden');
    document.getElementById('result').scrollIntoView({ 
        behavior: 'smooth', 
        block: 'center' 
    });
}

// ==================== ПОИСК МЕТАДАННЫХ ====================
async function lookupShortUrl() {
    const lookupInput = document.getElementById('lookupInput');
    let codeOrUrl = lookupInput.value.trim();
    
    if (!codeOrUrl) {
        showError('Введите код или короткую ссылку');
        return;
    }
    
    // Если ввели полную короткую ссылку — извлекаем код
    if (codeOrUrl.includes('/')) {
        try {
            const url = new URL(codeOrUrl);
            codeOrUrl = url.pathname.replace(/^\//, '');
        } catch {
            // Если не удалось распарсить как URL, считаем что это уже код
        }
    }
    
    // Убираем возможные пробелы и лишние символы
    codeOrUrl = codeOrUrl.trim();
    
    const lookupBtn = document.querySelector('.lookup-card .create-btn');
    const originalBtnText = lookupBtn.innerHTML;
    lookupBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Поиск...';
    lookupBtn.disabled = true;
    
    try {
        const metaResponse = await fetch(
            `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.META.replace('{code}', codeOrUrl)}`
        );
        
        if (!metaResponse.ok) {
            if (metaResponse.status === 404) {
                throw new Error('Ссылка не найдена');
            } else if (metaResponse.status === 410) {
                throw new Error('Ссылка истекла');
            } else {
                throw new Error('Ошибка получения данных');
            }
        }
        
        const metaData = await metaResponse.json();
        
        // Определяем статус ссылки
        const now = new Date();
        const expiresAt = new Date(metaData.expiresAt);
        let status = 'Активна';
        let statusColor = '#10b981';
        
        if (expiresAt < now) {
            status = 'Истекла';
            statusColor = '#ef4444';
        }
        
        // Форматируем даты
        const createdAt = new Date(metaData.createdAt).toLocaleString('ru-RU');
        const expiresAtFormatted = new Date(metaData.expiresAt).toLocaleString('ru-RU');
        
        // Заполняем плашку
        document.getElementById('lookupCode').textContent = codeOrUrl;
        document.getElementById('lookupCreatedAt').textContent = createdAt;
        document.getElementById('lookupExpiresAt').textContent = expiresAtFormatted;
        
        const statusElement = document.getElementById('lookupStatus');
        statusElement.textContent = status;
        statusElement.style.color = statusColor;
        
        // Показываем результат
        document.getElementById('lookupResult').classList.remove('hidden');
        document.getElementById('lookupResult').scrollIntoView({ 
            behavior: 'smooth', 
            block: 'center' 
        });
        
        showNotification('Данные получены', 'success');
        
    } catch (error) {
        document.getElementById('lookupResult').classList.add('hidden');
        showError(error.message);
        
    } finally {
        lookupBtn.innerHTML = originalBtnText;
        lookupBtn.disabled = false;
    }
}

// ==================== РАБОТА С БУФЕРОМ ОБМЕНА ====================
async function copyToClipboard() {
    const shortUrl = document.getElementById('shortLink').textContent;
    
    try {
        await navigator.clipboard.writeText(shortUrl);
        
        const copyBtn = document.querySelector('.create-card .copy-btn');
        const originalText = copyBtn.innerHTML;
        copyBtn.innerHTML = '<i class="fas fa-check"></i> Скопировано!';
        copyBtn.style.background = '#059669';
        
        showNotification('Ссылка скопирована!', 'info');
        
        setTimeout(() => {
            copyBtn.innerHTML = originalText;
            copyBtn.style.background = '';
        }, 2000);
        
    } catch (err) {
        showError('Не удалось скопировать');
    }
}

function copyHistoryUrl(url) {
    navigator.clipboard.writeText(url)
        .then(() => showNotification('Ссылка скопирована!', 'success'))
        .catch(() => showError('Не удалось скопировать'));
}

// ==================== ИСТОРИЯ ====================
function addToHistory(urlData) {
    history.unshift(urlData);
    if (history.length > 10) {
        history = history.slice(0, 10);
    }
    updateHistoryList();
    saveToLocalStorage();
}

function updateHistoryList() {
    const container = document.getElementById('historyList');
    
    if (history.length === 0) {
        container.innerHTML = `
            <div class="empty-history">
                <i class="far fa-file-alt"></i>
                <p>Создайте первую ссылку</p>
            </div>
        `;
        return;
    }
    
    let html = '';
    history.forEach(item => {
        const timeAgo = getTimeAgo(item.createdAt);
        
        html += `
            <div class="history-item">
                <div class="history-url">
                    <a href="${item.shortUrl}" target="_blank" class="history-short">
                        ${item.shortUrl}
                    </a>
                    <div class="history-meta">
                        <span class="history-time">
                            <i class="far fa-clock"></i> ${timeAgo}
                        </span>
                        <span class="history-code">
                            <i class="fas fa-hashtag"></i> ${item.code}
                        </span>
                    </div>
                </div>
                <button class="history-copy-btn" onclick="copyHistoryUrl('${item.shortUrl}')">
                    <i class="far fa-copy"></i>
                </button>
            </div>
        `;
    });
    
    container.innerHTML = html;
}

// ==================== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ====================
function getTimeAgo(isoDate) {
    const date = new Date(isoDate);
    const now = new Date();
    const seconds = Math.floor((now - date) / 1000);
    
    if (seconds < 60) return 'только что';
    if (seconds < 3600) return `${Math.floor(seconds / 60)} мин. назад`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)} ч. назад`;
    return `${Math.floor(seconds / 86400)} дн. назад`;
}

function isValidUrl(url) {
    try {
        new URL(url);
        return true;
    } catch {
        return false;
    }
}

function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `notification ${type}`;
    notification.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle'}"></i>
        <span>${message}</span>
    `;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.style.animation = 'slideInRight 0.3s ease reverse';
        setTimeout(() => {
            if (notification.parentNode) {
                document.body.removeChild(notification);
            }
        }, 300);
    }, 4000);
}

function showError(message) {
    showNotification(message, 'error');
}

function saveToLocalStorage() {
    localStorage.setItem('urlHistory', JSON.stringify(history));
}

function loadFromLocalStorage() {
    const saved = localStorage.getItem('urlHistory');
    if (saved) {
        try {
            history = JSON.parse(saved);
            updateHistoryList();
        } catch (e) {
            console.log('Ошибка загрузки истории');
        }
    }
}

// ==================== ИНИЦИАЛИЗАЦИЯ ====================
document.addEventListener('DOMContentLoaded', function() {
    loadFromLocalStorage();
    
    // Enter для создания
    document.getElementById('urlInput').addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            createShortUrl();
        }
    });
    
    // Enter для поиска
    document.getElementById('lookupInput').addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            lookupShortUrl();
        }
    });
});

// Экспорт в глобальную область видимости
window.createShortUrl = createShortUrl;
window.copyToClipboard = copyToClipboard;
window.copyHistoryUrl = copyHistoryUrl;
window.lookupShortUrl = lookupShortUrl;