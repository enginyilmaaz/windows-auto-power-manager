// Countdown notifier page logic
var Countdown = {
    _seconds: 0,
    _template1: '',
    _template2: '',
    _isReadySent: false,

    init() {
        var self = this;

        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.addEventListener('message', function (e) {
                var msg;
                try {
                    msg = typeof e.data === 'string' ? JSON.parse(e.data) : e.data;
                } catch (_) { return; }

                if (msg.type === 'initCountdown') {
                    self.setup(msg.data);
                }
                if (msg.type === 'updateCountdown') {
                    self._seconds = msg.data.seconds;
                    self._updateText();
                }
                if (msg.type === 'closeCountdown') {
                    // C# will handle close
                }
            });
        }

        // Button handlers
        document.getElementById('btn-delete').addEventListener('click', function () {
            self._send('delete');
        });
        document.getElementById('btn-ignore').addEventListener('click', function () {
            self._send('ignore');
        });
        document.getElementById('btn-skip').addEventListener('click', function () {
            self._send('skip');
        });

        // Drag support
        var panel = document.getElementById('panel');
        panel.addEventListener('mousedown', function (e) {
            self._send('dragStart', { x: e.screenX, y: e.screenY });
        });

        this._notifyReady();
    },

    setup(data) {
        this._seconds = data.seconds || 0;
        this._template1 = data.countdownText1 || '';
        this._template2 = data.countdownText2 || '';

        document.getElementById('header-title').textContent = data.title || '';
        document.getElementById('action-type').textContent = data.actionType || '';
        document.getElementById('info-text').textContent = data.infoText || '';

        document.getElementById('btn-delete').textContent = data.btnDelete || 'Delete';
        document.getElementById('btn-ignore').textContent = data.btnIgnore || 'Ignore';
        document.getElementById('btn-skip').textContent = data.btnSkip || 'Skip';

        document.getElementById('btn-delete').disabled = !data.enableDelete;
        document.getElementById('btn-ignore').disabled = !data.enableIgnore;
        document.getElementById('btn-skip').disabled = !data.enableSkip;

        this._updateText();
        this._send('initialized');
    },

    _updateText() {
        var el = document.getElementById('countdown-text');
        if (el) {
            el.textContent = this._template1 + ' ' + this._seconds + ' ' + this._template2;
        }
    },

    _send(action, data) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify({ type: action, data: data || {} }));
        }
    },

    _notifyReady() {
        if (this._isReadySent) return;
        this._isReadySent = true;
        this._send('ready');
    }
};

Countdown.init();
