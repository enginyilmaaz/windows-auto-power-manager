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

        // Nav link clicks
        document.querySelectorAll('.nav-link').forEach(function (link) {
            link.addEventListener('click', function (e) {
                e.preventDefault();
                var page = this.getAttribute('data-page');
                self.navigate(page);
            });
        });

        // Time update
        Bridge.on('updateTime', function (data) {
            var el = document.getElementById('status-time');
            if (el) el.textContent = data;
        });

        // Wait for init from C#
        Bridge.on('init', function () {
            self._applyLanguageToNav();
            self.navigate('main');
        });

        // Listen for navigate messages from C# (tray icon)
        Bridge.on('navigate', function (page) {
            self.navigate(page);
        });
    },

    _applyLanguageToNav() {
        var L = Bridge.lang.bind(Bridge);
        var title = document.getElementById('nav-title');
        if (title) title.textContent = L('main_FormName') || 'Windows Shutdown Helper';

        var navMain = document.getElementById('nav-main');
        if (navMain) navMain.textContent = L('main_groupbox_newAction') || 'Actions';

        var navSettings = document.getElementById('nav-settings');
        if (navSettings) navSettings.textContent = L('settingsForm_Name') || 'Settings';

        var navLogs = document.getElementById('nav-logs');
        if (navLogs) navLogs.textContent = L('logViewerForm_Name') || 'Logs';
    },

    navigate(page) {
        if (!this._pages[page]) return;
        this._currentPage = page;

        // Update nav active state
        document.querySelectorAll('.nav-link').forEach(function (link) {
            link.classList.toggle('active', link.getAttribute('data-page') === page);
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
