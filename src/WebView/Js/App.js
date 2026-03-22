// SPA Router and main application
const App = {
    _currentPage: 'main',
    _pages: {},
    _pageLoadPromises: {},
    _navigationToken: 0,
    _confirmModalCallback: null,
    _pageConfig: {
        main: { scriptPath: 'Js/Pages/Main.js', globalName: 'MainPage' },
        settings: { scriptPath: 'Js/Pages/Settings.js', globalName: 'SettingsPage' },
        logs: { scriptPath: 'Js/Pages/Logs.js', globalName: 'LogsPage' },
        help: { scriptPath: 'Js/Pages/Help.js', globalName: 'HelpPage' },
        about: { scriptPath: 'Js/Pages/About.js', globalName: 'AboutPage' }
    },

    init() {
        var self = this;
        this.ensurePageLoaded('main').catch(function () { });

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

        // Menu item clicks
        document.querySelectorAll('.menu-item[data-page]').forEach(function (item) {
            item.addEventListener('click', function (e) {
                e.preventDefault();
                var page = this.getAttribute('data-page');
                if (page === 'main') {
                    self.navigate('main');
                    self.openNewActionModal();
                } else {
                    Bridge.send('openWindow', { page: page });
                }
                overlay.classList.add('hidden');
            });
        });

        // Exit button
        document.getElementById('mi-exit').addEventListener('click', function (e) {
            e.preventDefault();
            overlay.classList.add('hidden');
            var L = Bridge.lang.bind(Bridge);
            var shouldConfirmExit = !(Bridge._settings && Bridge._settings.confirmExitOnProgramExit === false);
            var exitConfirmMessage = L('MessageContentConfirmExitProgram');
            if (!exitConfirmMessage || exitConfirmMessage === 'MessageContentConfirmExitProgram') {
                exitConfirmMessage = 'Are you sure you want to exit the program?';
            }

            if (!shouldConfirmExit) {
                Bridge.send('exitApp', {});
                return;
            }

            self.openConfirmModal({
                title: L('MessageTitleWarn') || 'Warning',
                message: exitConfirmMessage,
                cancelText: L('SettingsFormButtonCancel') || 'Cancel',
                confirmText: L('ContextMenuStripNotifyIconExitProgram') || 'Exit',
                onConfirm: function () {
                    Bridge.send('exitApp', {});
                }
            });
        });

        // ─── Toolbar: Add button → open modal ───
        document.getElementById('btn-toolbar-add').addEventListener('click', function () {
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

        // Close modal only after backend confirms action creation.
        Bridge.on('addActionResult', function (data) {
            if (data && data.success) {
                self.closeModal();
            }
        });

        Bridge.on('updateActionResult', function (data) {
            if (data && data.success) {
                self.closeModal();
            }
        });

        // ─── Tab bar filtering ───
        document.querySelectorAll('.tab-item[data-filter]').forEach(function (tab) {
            tab.addEventListener('click', function () {
                document.querySelectorAll('.tab-item').forEach(function (t) { t.classList.remove('active'); });
                this.classList.add('active');

                var mainPage = self._getPage('main');
                if (!mainPage || !mainPage.renderTable) return;

                var filter = this.getAttribute('data-filter');
                mainPage._currentFilter = filter;
                mainPage.renderTable(Bridge._actions);
            });
        });

        // ─── Search ───
        document.getElementById('toolbar-search-input').addEventListener('input', function () {
            var mainPage = self._getPage('main');
            if (!mainPage || !mainPage.renderTable) return;

            mainPage._searchQuery = this.value.toLowerCase();
            mainPage.renderTable(Bridge._actions);
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

        // Open "New Action" modal from native events (tray/menu)
        Bridge.on('openNewAction', function () {
            overlay.classList.add('hidden');
            self._closePauseDropdown();
            self.navigate('main');
            self.openNewActionModal();
        });
    },

    _getPage(page) {
        if (this._pages[page]) {
            return this._pages[page];
        }

        var config = this._pageConfig[page];
        if (!config) {
            return null;
        }

        var pageObject = window[config.globalName];
        if (pageObject) {
            this._pages[page] = pageObject;
            return pageObject;
        }

        return null;
    },

    _disposePageHandlers(page) {
        var pageObject = this._getPage(page);
        if (pageObject && typeof pageObject.beforeLeave === 'function') {
            pageObject.beforeLeave();
        }
    },

    ensurePageLoaded(page) {
        var existing = this._getPage(page);
        if (existing) {
            return Promise.resolve(existing);
        }

        if (this._pageLoadPromises[page]) {
            return this._pageLoadPromises[page];
        }

        var config = this._pageConfig[page];
        if (!config) {
            return Promise.reject(new Error('Unknown page: ' + page));
        }

        var self = this;
        this._pageLoadPromises[page] = new Promise(function (resolve, reject) {
            var script = document.createElement('script');
            script.src = config.scriptPath;
            script.async = true;

            script.onload = function () {
                var pageObject = self._getPage(page);
                if (!pageObject) {
                    delete self._pageLoadPromises[page];
                    reject(new Error('Page object not found after loading: ' + config.globalName));
                    return;
                }

                resolve(pageObject);
            };

            script.onerror = function () {
                delete self._pageLoadPromises[page];
                reject(new Error('Failed to load page script: ' + config.scriptPath));
            };

            document.head.appendChild(script);
        });

        return this._pageLoadPromises[page];
    },

    _getLoadingText() {
        var L = Bridge.lang.bind(Bridge);
        return L('CommonLoading') || 'Loading...';
    },

    _showPageLoading() {
        var container = document.getElementById('page-container');
        if (!container) return;
        container.innerHTML = '<div class="loading-container"><div class="loading-spinner"></div><div class="loading-text">' + this._getLoadingText() + '</div></div>';
    },

    _showPageLoadError() {
        var container = document.getElementById('page-container');
        if (!container) return;

        var L = Bridge.lang.bind(Bridge);
        container.innerHTML = '<div class="table-empty">' + (L('MessageTitleError') || 'Error') + '</div>';
    },

    _applyLanguage() {
        var L = Bridge.lang.bind(Bridge);

        // Menu items
        var miMain = document.getElementById('mi-main-text');
        if (miMain) miMain.textContent = L('MainGroupboxNewAction') || 'Actions';

        var miSettings = document.getElementById('mi-settings-text');
        if (miSettings) miSettings.textContent = L('SettingsFormName') || 'Settings';

        var miLogs = document.getElementById('mi-logs-text');
        if (miLogs) miLogs.textContent = L('LogViewerFormName') || 'Logs';

        var miHelp = document.getElementById('mi-help-text');
        if (miHelp) miHelp.textContent = L('HelpMenuItem') || 'Help';

        var miExit = document.getElementById('mi-exit-text');
        if (miExit) miExit.textContent = L('ContextMenuStripNotifyIconExitProgram') || 'Exit';

        var miAbout = document.getElementById('mi-about-text');
        if (miAbout) miAbout.textContent = L('AboutMenuItem') || 'About';

        // Toolbar
        var addBtn = document.getElementById('btn-toolbar-add');
        if (addBtn) addBtn.title = L('ToolbarAddAction') || 'New Action';

        var pauseBtn = document.getElementById('btn-toolbar-pause');
        if (pauseBtn) pauseBtn.title = L('ToolbarPause') || 'Pause';

        var resumeBtn = document.getElementById('btn-toolbar-resume');
        if (resumeBtn) resumeBtn.title = L('ToolbarResume') || 'Resume';

        var searchInput = document.getElementById('toolbar-search-input');
        if (searchInput) searchInput.placeholder = L('ToolbarSearch') || 'Search...';

        // Tabs
        var tabAll = document.getElementById('tab-all');
        if (tabAll) tabAll.textContent = L('TabAll') || 'All';

        var tabShutdown = document.getElementById('tab-shutdown');
        if (tabShutdown) tabShutdown.textContent = L('TabShutdown') || 'Shutdown';

        var tabRestart = document.getElementById('tab-restart');
        if (tabRestart) tabRestart.textContent = L('TabRestart') || 'Restart';

        var tabSleep = document.getElementById('tab-sleep');
        if (tabSleep) tabSleep.textContent = L('TabSleep') || 'Sleep';

        var tabLock = document.getElementById('tab-lock');
        if (tabLock) tabLock.textContent = L('TabLock') || 'Lock';

        var tabMonitor = document.getElementById('tab-monitor');
        if (tabMonitor) tabMonitor.textContent = L('TabMonitor') || 'Monitor';

        var tabLogoff = document.getElementById('tab-logoff');
        if (tabLogoff) tabLogoff.textContent = L('TabLogoff') || 'Log Off';

        // Pause dropdown
        var p30 = document.getElementById('pause-opt-30');
        if (p30) p30.textContent = L('Pause30min') || '30 minutes';

        var p60 = document.getElementById('pause-opt-60');
        if (p60) p60.textContent = L('Pause1hour') || '1 hour';

        var p120 = document.getElementById('pause-opt-120');
        if (p120) p120.textContent = L('Pause2hours') || '2 hours';

        var p240 = document.getElementById('pause-opt-240');
        if (p240) p240.textContent = L('Pause4hours') || '4 hours';

        var pEod = document.getElementById('pause-opt-eod');
        if (pEod) pEod.textContent = L('PauseUntilEndOfDay') || 'Until end of day';

        var pLabel = document.getElementById('pause-custom-label');
        if (pLabel) pLabel.textContent = L('PauseCustomTitle') || 'Custom duration (min)';

        var pInput = document.getElementById('pause-custom-input');
        if (pInput) pInput.placeholder = L('PauseCustomPlaceholder') || 'Enter minutes...';

        // Pause banner
        var bannerText = document.getElementById('pause-banner-text');
        if (bannerText) bannerText.textContent = L('PauseBanner') || 'Actions paused';
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
            bannerTime.textContent = timeStr + ' ' + (L('PauseRemaining') || 'remaining');
        } else {
            banner.classList.add('hidden');
        }
    },

    openNewActionModal() {
        var self = this;
        this.ensurePageLoaded('main').then(function (mainPage) {
            if (!mainPage || !mainPage.renderForm || !mainPage.afterRenderForm) return;

            var L = Bridge.lang.bind(Bridge);
            var modalTitle = document.getElementById('modal-title');
            modalTitle.textContent = L('ModalTitleNewAction') || 'New Action';

            var modalBody = document.getElementById('modal-body');
            modalBody.innerHTML = mainPage.renderForm({ mode: 'add' });
            mainPage.afterRenderForm(modalBody, { mode: 'add' });

            document.getElementById('modal-overlay').classList.remove('hidden');
        }).catch(function () {
            self._showPageLoadError();
        });
    },

    openEditActionModal(editableAction) {
        var self = this;
        if (!editableAction || editableAction.index < 0) return;

        this.ensurePageLoaded('main').then(function (mainPage) {
            if (!mainPage || !mainPage.renderForm || !mainPage.afterRenderForm) return;

            var L = Bridge.lang.bind(Bridge);
            var modalTitle = document.getElementById('modal-title');
            modalTitle.textContent = L('ModalTitleEditAction') || (L('ContextMenuStripMainGridEditSelectedAction') || 'Edit Action');

            var modalBody = document.getElementById('modal-body');
            modalBody.innerHTML = mainPage.renderForm({ mode: 'edit' });
            mainPage.afterRenderForm(modalBody, {
                mode: 'edit',
                index: editableAction.index,
                initialData: editableAction
            });

            document.getElementById('modal-overlay').classList.remove('hidden');
        }).catch(function () {
            self._showPageLoadError();
        });
    },

    openConfirmModal(options) {
        options = options || {};
        var self = this;
        var L = Bridge.lang.bind(Bridge);

        var modalTitle = document.getElementById('modal-title');
        modalTitle.textContent = options.title || (L('MessageTitleWarn') || 'Confirm');

        var modalBody = document.getElementById('modal-body');
        modalBody.innerHTML = '' +
            '<div class="form-row">' +
                '<span class="form-label">' + (options.message || '') + '</span>' +
            '</div>' +
            '<div class="settings-actions">' +
                '<button class="btn btn-secondary" id="confirm-cancel">' +
                    (options.cancelText || L('SettingsFormButtonCancel') || 'Cancel') +
                '</button>' +
                '<button class="btn btn-danger" id="confirm-approve">' +
                    (options.confirmText || 'OK') +
                '</button>' +
            '</div>';

        this._confirmModalCallback = typeof options.onConfirm === 'function'
            ? options.onConfirm
            : null;

        document.getElementById('confirm-cancel').addEventListener('click', function () {
            self.closeModal();
        });

        document.getElementById('confirm-approve').addEventListener('click', function () {
            var callback = self._confirmModalCallback;
            self.closeModal();
            if (callback) {
                callback();
            }
        });

        document.getElementById('modal-overlay').classList.remove('hidden');
    },

    closeModal() {
        this._confirmModalCallback = null;
        document.getElementById('modal-overlay').classList.add('hidden');
    },

    navigate(page) {
        if (!this._pageConfig[page]) return;

        this._disposePageHandlers(this._currentPage);

        this._currentPage = page;
        var targetPage = page;

        // Update menu active state
        document.querySelectorAll('.menu-item[data-page]').forEach(function (item) {
            item.classList.toggle('active', item.getAttribute('data-page') === page);
        });

        var token = ++this._navigationToken;
        var self = this;
        this._showPageLoading();
        var loadingContainer = document.getElementById('page-container');
        if (loadingContainer) {
            loadingContainer.scrollTop = 0;
        }

        this.ensurePageLoaded(page)
            .then(function (pageObject) {
                if (token !== self._navigationToken) return;

                var container = document.getElementById('page-container');
                if (!container) return;

                self._disposePageHandlers(targetPage);
                container.innerHTML = pageObject.render();
                container.scrollTop = 0;
                if (pageObject.afterRender) {
                    pageObject.afterRender();
                }
            })
            .catch(function (err) {
                if (token !== self._navigationToken) return;
                self._showPageLoadError();
                if (window.console && window.console.error) {
                    window.console.error(err);
                }
            });
    }
};

window.App = App;

// Start app
App.init();
