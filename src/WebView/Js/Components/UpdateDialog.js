// Update dialog: version prompt, download progress, and a background pill the download
// keeps reporting into once the dialog is dismissed.
const UpdateDialog = {
    _overlay: null,
    _pill: null,
    _state: 'idle', // idle | offered | downloading | installing | failed

    _t(key, fallback) {
        const value = Bridge.lang(key);
        return (value && value !== key) ? value : fallback;
    },

    _formatMb(bytes) {
        return (bytes / 1048576).toFixed(1);
    },

    show(info) {
        // Never stack two dialogs: a scheduled check can land while one is already open.
        if (this._overlay || this._state === 'downloading' || this._state === 'installing') return;

        this._state = 'offered';

        const overlay = document.createElement('div');
        overlay.className = 'modal-overlay';
        overlay.id = 'update-overlay';

        overlay.innerHTML =
            '<div class="modal-dialog update-dialog">' +
                '<div class="modal-header">' +
                    '<span class="modal-title">' + this._t('UpdateAvailableTitle', 'Update available') + '</span>' +
                    '<button type="button" class="modal-close" data-role="close">' +
                        '<span class="mi">close</span>' +
                    '</button>' +
                '</div>' +
                '<div class="modal-body">' +
                    '<div class="update-message">' +
                        this._t('UpdateAvailableMessage', 'A newer version is available.') +
                    '</div>' +
                    '<div class="update-versions">v' + (info.current || '?') +
                        ' <span class="mi update-arrow">arrow_forward</span> v' + (info.latest || '?') + '</div>' +
                    '<div class="progress-bar" data-role="progress" hidden>' +
                        '<div class="progress-fill" data-role="fill"></div>' +
                    '</div>' +
                    '<div class="update-status" data-role="status"></div>' +
                '</div>' +
                '<div class="modal-footer">' +
                    '<button type="button" class="btn btn-secondary" data-role="later">' +
                        this._t('UpdateLater', 'Later') + '</button>' +
                    '<button type="button" class="btn btn-primary" data-role="install">' +
                        this._t('UpdateInstall', 'Update now') + '</button>' +
                '</div>' +
            '</div>';

        document.body.appendChild(overlay);
        this._overlay = overlay;

        const self = this;
        overlay.querySelector('[data-role="close"]').addEventListener('click', function () { self._dismiss(); });
        overlay.querySelector('[data-role="later"]').addEventListener('click', function () { self._dismiss(); });
        overlay.querySelector('[data-role="install"]').addEventListener('click', function () { self._startDownload(); });
    },

    // Closing while a download runs hands it to the background pill instead of cancelling it.
    _dismiss() {
        if (this._state === 'downloading') {
            this._showPill();
        }

        if (this._overlay) {
            this._overlay.remove();
            this._overlay = null;
        }

        if (this._state === 'offered' || this._state === 'failed') {
            this._state = 'idle';
        }
    },

    _startDownload() {
        this._state = 'downloading';

        const install = this._overlay.querySelector('[data-role="install"]');
        const later = this._overlay.querySelector('[data-role="later"]');
        install.disabled = true;
        install.textContent = this._t('UpdateDownloading', 'Downloading...');
        later.textContent = this._t('UpdateRunInBackground', 'Continue in background');

        this._overlay.querySelector('[data-role="progress"]').hidden = false;
        Bridge.send('startUpdateDownload', {});
    },

    setProgress(data) {
        if (this._state !== 'downloading') return;

        const received = data.received || 0;
        const total = data.total || 0;
        let label;
        let percent = 0;

        if (total > 0) {
            percent = Math.min(100, Math.round((received / total) * 100));
            label = percent + '%  (' + this._formatMb(received) + ' / ' + this._formatMb(total) + ' MB)';
        } else {
            // No content-length: show transferred bytes rather than a misleading 0%.
            label = this._formatMb(received) + ' MB';
        }

        if (this._overlay) {
            this._overlay.querySelector('[data-role="fill"]').style.width = percent + '%';
            this._overlay.querySelector('[data-role="status"]').textContent = label;
        }

        if (this._pill) {
            this._pill.querySelector('[data-role="pill-fill"]').style.width = percent + '%';
            this._pill.querySelector('[data-role="pill-text"]').textContent = label;
        }
    },

    setStatus(data) {
        const reason = data && data.reason;

        // Only report on a flow this dialog owns. The outcome of a check started from the About
        // page is shown there, and repeating it as a toast here would say the same thing twice.
        if (this._state !== 'downloading' && this._state !== 'installing') return;

        if (reason === 'installing') {
            this._state = 'installing';
            const message = this._t('UpdateInstalling', 'Installing, the app will restart...');
            if (this._overlay) {
                this._overlay.querySelector('[data-role="status"]').textContent = message;
            }
            if (this._pill) {
                this._pill.querySelector('[data-role="pill-text"]').textContent = message;
            }
            return;
        }

        // Anything else here is a failed download.
        this._state = this._overlay ? 'failed' : 'idle';
        this._removePill();
        Toast.show(this._t('UpdateTitle', 'Update'),
            this._t('UpdateFailed', 'Could not complete the update.'), 'error', 4000);

        if (this._overlay) {
            const install = this._overlay.querySelector('[data-role="install"]');
            install.disabled = false;
            install.textContent = this._t('UpdateInstall', 'Update now');
            this._overlay.querySelector('[data-role="later"]').textContent = this._t('UpdateLater', 'Later');
        }
    },

    _showPill() {
        if (this._pill) return;

        const pill = document.createElement('div');
        pill.className = 'update-pill';
        pill.innerHTML =
            '<div class="update-pill-body">' +
                '<span class="update-pill-title">' + this._t('UpdateDownloading', 'Downloading...') + '</span>' +
                '<span class="update-pill-text" data-role="pill-text"></span>' +
            '</div>' +
            '<div class="progress-bar progress-bar-slim">' +
                '<div class="progress-fill" data-role="pill-fill"></div>' +
            '</div>';

        document.body.appendChild(pill);
        this._pill = pill;
    },

    _removePill() {
        if (!this._pill) return;
        this._pill.remove();
        this._pill = null;
    }
};

if (typeof Bridge !== 'undefined') {
    Bridge.on('updateAvailable', function (data) { UpdateDialog.show(data || {}); });
    Bridge.on('updateProgress', function (data) { UpdateDialog.setProgress(data || {}); });
    Bridge.on('updateStatus', function (data) { UpdateDialog.setStatus(data || {}); });
}
