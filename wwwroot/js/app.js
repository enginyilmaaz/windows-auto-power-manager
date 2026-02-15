// SPA Router and main application
const App = {
    _currentPage: 'main',
    _pages: {
        main: MainPage,
        settings: SettingsPage,
        logs: LogsPage
    },

    init() {
        var self = this;

        // Hamburger menu toggle
        var menuBtn = document.getElementById('menu-btn');
        var overlay = document.getElementById('menu-overlay');

        menuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            overlay.classList.toggle('hidden');
        });

        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) {
                overlay.classList.add('hidden');
            }
        });

        // Menu item clicks
        document.querySelectorAll('.menu-item[data-page]').forEach(function (item) {
            item.addEventListener('click', function (e) {
                e.preventDefault();
                var page = this.getAttribute('data-page');
                self.navigate(page);
                overlay.classList.add('hidden');
            });
        });

        // Exit button
        document.getElementById('mi-exit').addEventListener('click', function (e) {
            e.preventDefault();
            overlay.classList.add('hidden');
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

        var miMain = document.getElementById('mi-main-text');
        if (miMain) miMain.textContent = L('main_groupbox_newAction') || 'Actions';

        var miSettings = document.getElementById('mi-settings-text');
        if (miSettings) miSettings.textContent = L('settingsForm_Name') || 'Settings';

        var miLogs = document.getElementById('mi-logs-text');
        if (miLogs) miLogs.textContent = L('logViewerForm_Name') || 'Logs';

        var miExit = document.getElementById('mi-exit-text');
        if (miExit) miExit.textContent = L('contextMenuStrip_notifyIcon_exitTheProgram') || 'Exit';
    },

    navigate(page) {
        if (!this._pages[page]) return;
        this._currentPage = page;

        // Update menu active state
        document.querySelectorAll('.menu-item[data-page]').forEach(function (item) {
            item.classList.toggle('active', item.getAttribute('data-page') === page);
        });

        // Render page
        var container = document.getElementById('page-container');
        container.innerHTML = this._pages[page].render();

        // Call afterRender
        if (this._pages[page].afterRender) {
            this._pages[page].afterRender();
        }
    }
};

// Start app
App.init();
