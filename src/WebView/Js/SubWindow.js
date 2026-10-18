// Sub Window - renders a single page based on URL query parameter
(function () {
    var params = new URLSearchParams(window.location.search);
    var pageName = params.get('page') || 'main';
    var pageLoadPromises = {};
    var loadedPages = {};
    var navigationToken = 0;

    var pageConfig = {
        main: { scriptPath: 'Js/Pages/Main.js', globalName: 'MainPage' },
        settings: { scriptPath: 'Js/Pages/Settings.js', globalName: 'SettingsPage' },
        logs: { scriptPath: 'Js/Pages/Logs.js', globalName: 'LogsPage' },
        help: { scriptPath: 'Js/Pages/Help.js', globalName: 'HelpPage' },
        about: { scriptPath: 'Js/Pages/About.js', globalName: 'AboutPage' }
    };

    function getPage(page) {
        if (loadedPages[page]) {
            return loadedPages[page];
        }

        var config = pageConfig[page];
        if (!config) {
            return null;
        }

        var pageObject = window[config.globalName];
        if (pageObject) {
            loadedPages[page] = pageObject;
            return pageObject;
        }

        return null;
    }

    function ensurePageLoaded(page) {
        var existing = getPage(page);
        if (existing) {
            return Promise.resolve(existing);
        }

        if (pageLoadPromises[page]) {
            return pageLoadPromises[page];
        }

        var config = pageConfig[page];
        if (!config) {
            return Promise.reject(new Error('Unknown page: ' + page));
        }

        pageLoadPromises[page] = new Promise(function (resolve, reject) {
            var script = document.createElement('script');
            script.src = config.scriptPath;
            script.async = true;

            script.onload = function () {
                var pageObject = getPage(page);
                if (!pageObject) {
                    delete pageLoadPromises[page];
                    reject(new Error('Page object not found after loading: ' + config.globalName));
                    return;
                }

                resolve(pageObject);
            };

            script.onerror = function () {
                delete pageLoadPromises[page];
                reject(new Error('Failed to load page script: ' + config.scriptPath));
            };

            document.head.appendChild(script);
        });

        return pageLoadPromises[page];
    }

    function getLoadingText() {
        var L = Bridge.lang.bind(Bridge);
        return L('CommonLoading') || 'Loading...';
    }

    function syncPageLayoutClass() {
        if (!document.body) return;
        var footerLayoutActive = pageName === 'settings' ||
            pageName === 'logs' ||
            pageName === 'help' ||
            pageName === 'about';
        document.body.classList.toggle('help-page-active', pageName === 'help');
        document.body.classList.toggle('footer-layout-active', footerLayoutActive);
    }

    function applyLanguage() {
        var L = Bridge.lang.bind(Bridge);
        var title = document.getElementById('header-title');
        if (!title) return;

        if (pageName === 'main') {
            title.textContent = L('MainGroupboxNewAction') || 'Actions';
        } else if (pageName === 'settings') {
            title.textContent = L('SettingsFormName') || 'Settings';
        } else if (pageName === 'logs') {
            title.textContent = L('LogViewerFormName') || 'Logs';
        } else if (pageName === 'help') {
            title.textContent = L('HelpMenuItem') || 'Help';
        } else if (pageName === 'about') {
            title.textContent = L('AboutMenuItem') || 'About';
        } else {
            title.textContent = L('MainFormName') || 'Windows Auto Power Manager';
        }
    }

    function showLoadError() {
        var wrap = document.getElementById('page-container');
        if (!wrap) return;

        var L = Bridge.lang.bind(Bridge);
        wrap.innerHTML = '<div class="table-empty">' + (L('MessageTitleError') || 'Error') + '</div>';
    }

    function disposePageHandlers(page) {
        var pageObject = getPage(page);
        if (pageObject && typeof pageObject.beforeLeave === 'function') {
            pageObject.beforeLeave();
        }
    }

    function renderPage() {
        var container = document.getElementById('page-container');
        if (!container) return;

        syncPageLayoutClass();
        container.innerHTML = '<div class="loading-container"><div class="loading-spinner"></div><div class="loading-text">' + getLoadingText() + '</div></div>';
        container.scrollTop = 0;
        var token = ++navigationToken;
        var targetPage = pageName;
        disposePageHandlers(targetPage);

        ensurePageLoaded(targetPage)
            .then(function (page) {
                if (token !== navigationToken) return;

                container.innerHTML = page.render();
                container.scrollTop = 0;
                if (page.afterRender) {
                    page.afterRender();
                }
            })
            .catch(function (err) {
                if (token !== navigationToken) return;
                showLoadError();
                if (window.console && window.console.error) {
                    window.console.error(err);
                }
            });
    }

    function navigate(page) {
        if (!pageConfig[page]) return;
        disposePageHandlers(pageName);
        pageName = page;
        syncPageLayoutClass();
        applyLanguage();
        renderPage();
    }

    ensurePageLoaded(pageName).catch(function () { });

    var confirmCallback = null;

    window.App = {
        navigate: navigate,
        openEditActionModal: function (editableAction) {
            if (!editableAction || editableAction.index < 0) return;

            ensurePageLoaded('main').then(function (mainPage) {
                if (!mainPage || !mainPage.renderForm || !mainPage.afterRenderForm) return;

                var modalOverlay = document.getElementById('modal-overlay');
                var modalTitle = document.getElementById('modal-title');
                var modalBody = document.getElementById('modal-body');
                if (!modalOverlay || !modalTitle || !modalBody) return;

                var L = Bridge.lang.bind(Bridge);
                modalTitle.textContent = L('ModalTitleEditAction') || (L('ContextMenuStripMainGridEditSelectedAction') || 'Edit Action');
                modalBody.innerHTML = mainPage.renderForm({ mode: 'edit' });
                mainPage.afterRenderForm(modalBody, {
                    mode: 'edit',
                    index: editableAction.index,
                    initialData: editableAction
                });

                modalOverlay.classList.remove('hidden');
            }).catch(function () { });
        },
        // Mirrors App.openConfirmModal so a page shown in this window can confirm a destructive
        // action the same way it does in the main window. The history page is opened here, so
        // without it "clear records" would have no confirmation in the window people actually use.
        openConfirmModal: function (options) {
            options = options || {};

            var modalOverlay = document.getElementById('modal-overlay');
            var modalTitle = document.getElementById('modal-title');
            var modalBody = document.getElementById('modal-body');
            if (!modalOverlay || !modalTitle || !modalBody) return;

            var L = Bridge.lang.bind(Bridge);
            modalTitle.textContent = options.title || (L('MessageTitleWarn') || 'Confirm');

            modalBody.innerHTML = '' +
                '<div class="confirm-text">' + (options.message || '') + '</div>' +
                (options.warning
                    ? '<div class="confirm-warning"><span class="mi">warning_amber</span><span>' +
                      options.warning + '</span></div>'
                    : '') +
                '<div class="settings-actions">' +
                    '<button class="btn btn-secondary" id="confirm-cancel">' +
                        (options.cancelText || L('SettingsFormButtonCancel') || 'Cancel') +
                    '</button>' +
                    '<button class="btn btn-danger" id="confirm-approve">' +
                        (options.confirmText || 'OK') +
                    '</button>' +
                '</div>';

            confirmCallback = typeof options.onConfirm === 'function' ? options.onConfirm : null;

            document.getElementById('confirm-cancel').addEventListener('click', function () {
                window.App.closeModal();
            });

            document.getElementById('confirm-approve').addEventListener('click', function () {
                var callback = confirmCallback;
                window.App.closeModal();
                if (callback) {
                    callback();
                }
            });

            modalOverlay.classList.remove('hidden');
        },
        closeModal: function () {
            confirmCallback = null;
            var modalOverlay = document.getElementById('modal-overlay');
            if (modalOverlay) {
                modalOverlay.classList.add('hidden');
            }
        }
    };

    var modalClose = document.getElementById('modal-close');
    if (modalClose) {
        modalClose.addEventListener('click', function () {
            window.App.closeModal();
        });
    }

    var modalOverlay = document.getElementById('modal-overlay');
    if (modalOverlay) {
        modalOverlay.addEventListener('click', function (e) {
            if (e.target === modalOverlay) {
                window.App.closeModal();
            }
        });
    }

    Bridge.on('updateActionResult', function (data) {
        if (data && data.success) {
            window.App.closeModal();
        }
    });

    // Time update
    Bridge.on('updateTime', function (data) {
        var el = document.getElementById('header-time');
        if (el) el.textContent = data;
    });

    // Wait for init from C#
    Bridge.on('init', function () {
        applyLanguage();
        renderPage();
    });

    // Listen for navigate messages (for re-render if needed)
    Bridge.on('navigate', function (page) {
        navigate(page);
    });
})();
