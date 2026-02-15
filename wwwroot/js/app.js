// Tab-based navigation and main application
const App = {
    _currentPage: 'main',
    _pages: {
        main: MainPage,
        settings: SettingsPage,
        logs: LogsPage
    },
    _initialized: {},

    init() {
        var self = this;

        // Tab clicks
        document.querySelectorAll('.tab-btn[data-page]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                self.navigate(btn.getAttribute('data-page'));
            });
        });

        // Exit button
        document.getElementById('tab-exit').addEventListener('click', function () {
            Bridge.send('exitApp', {});
        });

        // Time update
        Bridge.on('updateTime', function (data) {
            var el = document.getElementById('header-time');
            if (el) el.textContent = data;
        });

        // Wait for init from C#
        Bridge.on('init', function () {
            self._applyLanguage();
            self.navigate('main');
        });

        // Listen for navigate messages from C# (tray icon)
        Bridge.on('navigate', function (page) {
            self.navigate(page);
        });
    },

    _applyLanguage() {
        var L = Bridge.lang.bind(Bridge);

        var title = document.getElementById('header-title');
        if (title) title.textContent = L('main_FormName') || 'Windows Shutdown Helper';

        var tabMain = document.getElementById('tab-main-text');
        if (tabMain) tabMain.textContent = L('main_groupbox_newAction') || 'Actions';

        var tabSettings = document.getElementById('tab-settings-text');
        if (tabSettings) tabSettings.textContent = L('settingsForm_Name') || 'Settings';

        var tabLogs = document.getElementById('tab-logs-text');
        if (tabLogs) tabLogs.textContent = L('logViewerForm_Name') || 'Logs';

        var tabExit = document.getElementById('tab-exit-text');
        if (tabExit) tabExit.textContent = L('contextMenuStrip_notifyIcon_exitTheProgram') || 'Exit';
    },

    navigate(page) {
        if (!this._pages[page]) return;
        this._currentPage = page;

        // Update tab active state
        document.querySelectorAll('.tab-btn[data-page]').forEach(function (btn) {
            btn.classList.toggle('active', btn.getAttribute('data-page') === page);
        });

        // Show/hide panels
        document.querySelectorAll('.page-panel').forEach(function (panel) {
            panel.classList.remove('active');
        });
        var target = document.getElementById('panel-' + page);
        if (target) target.classList.add('active');

        // Render if not initialized
        if (!this._initialized[page]) {
            target.innerHTML = this._pages[page].render();
            if (this._pages[page].afterRender) {
                this._pages[page].afterRender();
            }
            this._initialized[page] = true;
        }
    }
};

// Start app
App.init();
