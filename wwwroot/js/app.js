// SPA Router and main application
const App = {
    _currentPage: 'main',
    _pages: {
        main: MainPage,
        settings: SettingsPage,
        logs: LogsPage,
        about: AboutPage
    },

    init() {
        var self = this;

        // ─── Hamburger menu ───
        var menuBtn = document.getElementById('menu-btn');
        var overlay = document.getElementById('menu-overlay');

        menuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            self._closePauseDropdown();
            overlay.classList.toggle('hidden');
        });

        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) {
                overlay.classList.add('hidden');
            }
        });

        // Menu item clicks - open in separate window
        document.querySelectorAll('.menu-item[data-page]').forEach(function (item) {
            item.addEventListener('click', function (e) {
                e.preventDefault();
                var page = this.getAttribute('data-page');
                Bridge.send('openWindow', { page: page });
                overlay.classList.add('hidden');
            });
        });

        // Exit button
        document.getElementById('mi-exit').addEventListener('click', function (e) {
            e.preventDefault();
            overlay.classList.add('hidden');
            Bridge.send('exitApp', {});
        });

        // ─── Toolbar: Add button → open modal ───
        document.getElementById('btn-toolbar-add').addEventListener('click', function () {
            self.openNewActionModal();
        });

        // Tab bar add button
        document.getElementById('tab-add-btn').addEventListener('click', function () {
            self.openNewActionModal();
        });

        // ─── Toolbar: Pause button → toggle dropdown ───
        document.getElementById('btn-toolbar-pause').addEventListener('click', function (e) {
            e.stopPropagation();
            overlay.classList.add('hidden');
            var dd = document.getElementById('pause-dropdown');
            dd.classList.toggle('hidden');
        });

        // ─── Toolbar: Resume button ───
        document.getElementById('btn-toolbar-resume').addEventListener('click', function () {
            Bridge.send('resumeActions', {});
        });

        // ─── Pause dropdown items ───
        document.querySelectorAll('.pause-dropdown-item').forEach(function (item) {
            item.addEventListener('click', function () {
                var val = this.getAttribute('data-minutes');
                var minutes;
                if (val === 'eod') {
                    var now = new Date();
                    var endOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);
                    minutes = Math.ceil((endOfDay - now) / 60000);
                } else {
                    minutes = parseInt(val);
                }
                Bridge.send('pauseActions', { minutes: minutes });
                self._closePauseDropdown();
            });
        });

        // Pause custom button
        document.getElementById('pause-custom-btn').addEventListener('click', function () {
            var input = document.getElementById('pause-custom-input');
            var val = parseInt(input.value);
            if (val > 0) {
                Bridge.send('pauseActions', { minutes: val });
                input.value = '';
                self._closePauseDropdown();
            }
        });

        // Pause custom enter key
        document.getElementById('pause-custom-input').addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                document.getElementById('pause-custom-btn').click();
            }
        });

        // Pause banner resume button
        document.getElementById('pause-banner-resume').addEventListener('click', function () {
            Bridge.send('resumeActions', {});
        });

        // Close pause dropdown on outside click
        document.addEventListener('click', function (e) {
            var dd = document.getElementById('pause-dropdown');
            if (!dd.classList.contains('hidden')) {
                if (!dd.contains(e.target) && e.target.id !== 'btn-toolbar-pause' && !e.target.closest('#btn-toolbar-pause')) {
                    dd.classList.add('hidden');
                }
            }
        });

        // ─── Pause status updates from C# ───
        Bridge.on('pauseStatus', function (data) {
            self._updatePauseUI(data);
        });

        // ─── Tab bar filtering ───
        document.querySelectorAll('.tab-item[data-filter]').forEach(function (tab) {
            tab.addEventListener('click', function () {
                document.querySelectorAll('.tab-item').forEach(function (t) { t.classList.remove('active'); });
                this.classList.add('active');
                var filter = this.getAttribute('data-filter');
                if (MainPage._currentFilter !== undefined || filter) {
                    MainPage._currentFilter = filter;
                    MainPage.renderTable(Bridge._actions);
                }
            });
        });

        // ─── Search ───
        document.getElementById('toolbar-search-input').addEventListener('input', function () {
            MainPage._searchQuery = this.value.toLowerCase();
            MainPage.renderTable(Bridge._actions);
        });

        // ─── Modal close ───
        document.getElementById('modal-close').addEventListener('click', function () {
            self.closeModal();
        });

        document.getElementById('modal-overlay').addEventListener('click', function (e) {
            if (e.target === this) {
                self.closeModal();
            }
        });

        // ─── Time update ───
        Bridge.on('updateTime', function (data) {
            var el = document.getElementById('header-time');
            if (el) el.textContent = data;
        });

        // ─── Wait for init from C# ───
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

        // Menu items
        var miMain = document.getElementById('mi-main-text');
        if (miMain) miMain.textContent = L('main_groupbox_newAction') || 'Actions';

        var miSettings = document.getElementById('mi-settings-text');
        if (miSettings) miSettings.textContent = L('settingsForm_Name') || 'Settings';

        var miLogs = document.getElementById('mi-logs-text');
        if (miLogs) miLogs.textContent = L('logViewerForm_Name') || 'Logs';

        var miExit = document.getElementById('mi-exit-text');
        if (miExit) miExit.textContent = L('contextMenuStrip_notifyIcon_exitProgram') || 'Exit';

        var miAbout = document.getElementById('mi-about-text');
        if (miAbout) miAbout.textContent = L('about_menuItem') || 'About';

        // Toolbar
        var addBtn = document.getElementById('btn-toolbar-add');
        if (addBtn) addBtn.title = L('toolbar_addAction') || 'New Action';

        var pauseBtn = document.getElementById('btn-toolbar-pause');
        if (pauseBtn) pauseBtn.title = L('toolbar_pause') || 'Pause';

        var resumeBtn = document.getElementById('btn-toolbar-resume');
        if (resumeBtn) resumeBtn.title = L('toolbar_resume') || 'Resume';

        var searchInput = document.getElementById('toolbar-search-input');
        if (searchInput) searchInput.placeholder = L('toolbar_search') || 'Search...';

        // Tabs
        var tabAll = document.getElementById('tab-all');
        if (tabAll) tabAll.textContent = L('tab_all') || 'All';

        var tabShutdown = document.getElementById('tab-shutdown');
        if (tabShutdown) tabShutdown.textContent = L('tab_shutdown') || 'Shutdown';

        var tabRestart = document.getElementById('tab-restart');
        if (tabRestart) tabRestart.textContent = L('tab_restart') || 'Restart';

        var tabSleep = document.getElementById('tab-sleep');
        if (tabSleep) tabSleep.textContent = L('tab_sleep') || 'Sleep';

        var tabLock = document.getElementById('tab-lock');
        if (tabLock) tabLock.textContent = L('tab_lock') || 'Lock';

        var tabMonitor = document.getElementById('tab-monitor');
        if (tabMonitor) tabMonitor.textContent = L('tab_monitor') || 'Monitor';

        var tabLogoff = document.getElementById('tab-logoff');
        if (tabLogoff) tabLogoff.textContent = L('tab_logoff') || 'Log Off';

        // Pause dropdown
        var p30 = document.getElementById('pause-opt-30');
        if (p30) p30.textContent = L('pause_30min') || '30 minutes';

        var p60 = document.getElementById('pause-opt-60');
        if (p60) p60.textContent = L('pause_1hour') || '1 hour';

        var p120 = document.getElementById('pause-opt-120');
        if (p120) p120.textContent = L('pause_2hours') || '2 hours';

        var p240 = document.getElementById('pause-opt-240');
        if (p240) p240.textContent = L('pause_4hours') || '4 hours';

        var pEod = document.getElementById('pause-opt-eod');
        if (pEod) pEod.textContent = L('pause_untilEndOfDay') || 'Until end of day';

        var pLabel = document.getElementById('pause-custom-label');
        if (pLabel) pLabel.textContent = L('pause_customTitle') || 'Custom duration (min)';

        var pInput = document.getElementById('pause-custom-input');
        if (pInput) pInput.placeholder = L('pause_customPlaceholder') || 'Enter minutes...';

        // Pause banner
        var bannerText = document.getElementById('pause-banner-text');
        if (bannerText) bannerText.textContent = L('pause_banner') || 'Actions paused';
    },

    _closePauseDropdown() {
        var dd = document.getElementById('pause-dropdown');
        if (dd) dd.classList.add('hidden');
    },

    _updatePauseUI(data) {
        var banner = document.getElementById('pause-banner');
        var bannerTime = document.getElementById('pause-banner-time');
        var L = Bridge.lang.bind(Bridge);

        if (data.isPaused) {
            banner.classList.remove('hidden');
            var secs = Math.floor(data.remainingSeconds);
            var h = Math.floor(secs / 3600);
            var m = Math.floor((secs % 3600) / 60);
            var s = secs % 60;
            var timeStr = '';
            if (h > 0) timeStr += h + 'h ';
            if (m > 0 || h > 0) timeStr += m + 'm ';
            timeStr += s + 's';
            bannerTime.textContent = timeStr + ' ' + (L('pause_remaining') || 'remaining');
        } else {
            banner.classList.add('hidden');
        }
    },

    openNewActionModal() {
        var L = Bridge.lang.bind(Bridge);
        var modalTitle = document.getElementById('modal-title');
        modalTitle.textContent = L('modal_title_newAction') || 'New Action';

        var modalBody = document.getElementById('modal-body');
        modalBody.innerHTML = MainPage.renderForm();
        MainPage.afterRenderForm(modalBody);

        document.getElementById('modal-overlay').classList.remove('hidden');
    },

    closeModal() {
        document.getElementById('modal-overlay').classList.add('hidden');
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
