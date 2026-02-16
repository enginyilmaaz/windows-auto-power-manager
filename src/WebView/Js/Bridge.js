// Bridge: C# <-> JS communication layer
const Bridge = {
    _language: {},
    _actions: [],
    _settings: {},
    _handlers: {},
    _isPaused: false,
    _pauseRemainingSeconds: 0,

    // Send message to C#
    send(type, data) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify({ type, data }));
        }
    },

    // Register handler for messages from C#
    on(type, handler) {
        if (!this._handlers[type]) this._handlers[type] = [];
        this._handlers[type].push(handler);
    },

    // Emit event to registered handlers
    _emit(type, data) {
        const handlers = this._handlers[type];
        if (handlers) {
            handlers.forEach(h => h(data));
        }
    },

    // Apply theme to document
    applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme || 'system');
    },

    // Initialize: called when C# sends init data
    init(data) {
        this._language = data.language || {};
        this._actions = data.actions || [];
        this._settings = data.settings || {};

        // Apply theme on init
        this.applyTheme(this._settings.theme || 'system');

        this._emit('init', data);
    },

    lang(key) {
        return this._language[key] || key;
    }
};

// Listen for messages from C#
if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', function (e) {
        let msg;
        try {
            msg = typeof e.data === 'string' ? JSON.parse(e.data) : e.data;
        } catch (_) {
            return;
        }

        switch (msg.type) {
            case 'init':
                Bridge.init(msg.data);
                break;
            case 'updateTime':
                Bridge._emit('updateTime', msg.data);
                break;
            case 'refreshActions':
                Bridge._actions = msg.data || [];
                Bridge._emit('refreshActions', Bridge._actions);
                break;
            case 'settingsLoaded':
                Bridge._settings = msg.data || {};
                Bridge._emit('settingsLoaded', Bridge._settings);
                break;
            case 'logsLoaded':
                Bridge._emit('logsLoaded', msg.data);
                break;
            case 'showToast':
                Toast.show(msg.data.title, msg.data.message, msg.data.type, msg.data.duration);
                break;
            case 'languageList':
                Bridge._emit('languageList', msg.data);
                break;
            case 'navigate':
                Bridge._emit('navigate', msg.data);
                break;
            case 'pauseStatus':
                Bridge._isPaused = msg.data.isPaused;
                Bridge._pauseRemainingSeconds = msg.data.remainingSeconds || 0;
                Bridge._emit('pauseStatus', msg.data);
                break;
            case 'themeChanged':
                Bridge.applyTheme(msg.data);
                break;
            case 'addActionResult':
                Bridge._emit('addActionResult', msg.data);
                break;
        }
    });
}
