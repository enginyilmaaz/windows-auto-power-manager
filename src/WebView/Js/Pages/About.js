// About Page
window.AboutPage = {
    render() {
        var L = Bridge.lang.bind(Bridge);
        var version = Bridge._settings.appVersion || '1.0.0.0';
        var buildId = Bridge._settings.buildId || 'dev';

        return '' +
        '<div class="subpage-layout">' +
            '<div class="card about-page-card subpage-card">' +
                '<div class="card-title">' +
                    '<span class="mi">info</span>' +
                    (L('AboutMenuItem') || 'About') +
                '</div>' +
                '<div class="subpage-scroll about-shell">' +
                    '<div class="about-content">' +
                        '<div class="about-app-name">' + (L('MainFormName') || 'Windows Auto Power Manager') + '</div>' +
                        '<img src="Assets/app-icon.png" class="about-app-icon" alt="">' +
                        '<div class="about-row about-version-row">' +
                            '<span class="about-label">' + (L('AboutLabelVersion') || 'Version') + '</span>' +
                            '<span class="about-value">' + version + '</span>' +
                            '<button class="btn btn-secondary about-update-btn" id="about-check-update">' +
                                (L('UpdateCheckButton') || 'Check for updates') +
                            '</button>' +
                        '</div>' +
                        '<div class="about-update-status" id="about-update-status"></div>' +
                        '<div class="about-row">' +
                            '<span class="about-label">' + (L('AboutLabelBuildId') || 'Build ID') + '</span>' +
                            '<span class="about-value">' + buildId + '</span>' +
                        '</div>' +
                        '<div class="about-divider"></div>' +
                        '<div class="about-row">' +
                            '<span class="about-label">' + (L('AboutLabelAuthor') || 'Author') + '</span>' +
                            '<span class="about-value">enginyilmaaz</span>' +
                        '</div>' +
                        '<div class="about-row">' +
                            '<span class="about-label">GitHub</span>' +
                            '<a class="about-link" id="about-github-link" href="#">github.com/enginyilmaaz</a>' +
                        '</div>' +
                    '</div>' +
                '</div>' +
            '</div>' +
            '<div class="about-actions subpage-footer">' +
                '<button class="btn btn-secondary" id="about-close">' + (L('LogViewerFormButtonCancel') || L('SettingsFormButtonCancel') || 'Close') + '</button>' +
            '</div>' +
        '</div>';
    },

    afterRender() {
        var link = document.getElementById('about-github-link');
        if (link) {
            link.addEventListener('click', function (e) {
                e.preventDefault();
                Bridge.send('openUrl', { url: 'https://github.com/enginyilmaaz' });
            });
        }

        var closeBtn = document.getElementById('about-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', function () {
                Bridge.send('closeWindow', {});
            });
        }

        this._wireUpdateCheck();
    },

    _wireUpdateCheck() {
        var button = document.getElementById('about-check-update');
        var status = document.getElementById('about-update-status');
        if (!button || !status) return;

        var self = this;
        button.addEventListener('click', function () {
            button.disabled = true;
            status.className = 'about-update-status';
            status.textContent = self._text('UpdateChecking', 'Checking...');
            Bridge.send('checkUpdate', {});
        });

        // Subscribed once for the lifetime of the window: afterRender runs again on every
        // navigation, and re-subscribing would stack a handler per visit. The elements are looked
        // up at delivery time because a later render replaces them.
        if (this._updateListenersBound) return;
        this._updateListenersBound = true;

        function report(message, tone) {
            var el = document.getElementById('about-update-status');
            var btn = document.getElementById('about-check-update');
            if (btn) btn.disabled = false;
            if (!el) return;
            el.textContent = message;
            el.className = 'about-update-status' + (tone ? ' about-update-status-' + tone : '');
        }

        // The result arrives as a broadcast, so the outcome is shown here rather than only in
        // the main window where the download dialog lives.
        Bridge.on('updateAvailable', function (data) {
            report(self._text('UpdateAvailableTitle', 'Update available') + ': v' + ((data && data.latest) || '?'), 'available');
        });

        Bridge.on('updateStatus', function (data) {
            var reason = data && data.reason;
            if (reason === 'installing') return;
            if (reason === 'up-to-date') {
                report(self._text('UpdateUpToDate', 'You are on the latest version.'), 'ok');
                return;
            }
            report(self._text('UpdateFailed', 'Could not complete the update.'), 'error');
        });
    },

    _text(key, fallback) {
        var value = Bridge.lang(key);
        return (value && value !== key) ? value : fallback;
    }
};
