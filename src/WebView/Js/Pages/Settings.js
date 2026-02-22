// Settings Page
window.SettingsPage = {
    _languageList: [],
    _cleanupFns: [],

    _registerCleanup(fn) {
        if (typeof fn === 'function') {
            this._cleanupFns.push(fn);
        }
    },

    _disposeHandlers() {
        while (this._cleanupFns.length > 0) {
            var fn = this._cleanupFns.pop();
            try {
                fn();
            } catch (_) {
            }
        }
    },

    render() {
        var L = Bridge.lang.bind(Bridge);
        var s = Bridge._settings || {};

        return '' +
        '<div class="card">' +
            '<div class="card-title">' + (L('SettingsFormName') || 'Settings') + '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('SettingsFormLabelTheme') || 'Theme') + '</span>' +
                '<select id="set-theme" class="form-select" style="max-width:200px">' +
                    '<option value="system"' + (s.theme === 'system' || !s.theme ? ' selected' : '') + '>' + (L('SettingsFormThemeSystem') || 'System Default') + '</option>' +
                    '<option value="dark"' + (s.theme === 'dark' ? ' selected' : '') + '>' + (L('SettingsFormThemeDark') || 'Dark') + '</option>' +
                    '<option value="light"' + (s.theme === 'light' ? ' selected' : '') + '>' + (L('SettingsFormThemeLight') || 'Light') + '</option>' +
                '</select>' +
            '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('SettingsFormLabelLanguage') || 'Language') + '</span>' +
                '<select id="set-lang" class="form-select" style="max-width:200px"></select>' +
            '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('SettingsFormLabelLogs') || 'Record Logs') + '</span>' +
                '<label class="toggle-switch">' +
                    '<input type="checkbox" id="set-logs"' + (s.logsEnabled ? ' checked' : '') + '>' +
                    '<span class="toggle-slider"></span>' +
                '</label>' +
            '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('SettingsFormLabelStartWithWindows') || 'Start with Windows') + '</span>' +
                '<label class="toggle-switch">' +
                    '<input type="checkbox" id="set-startup"' + (s.startWithWindows ? ' checked' : '') + '>' +
                    '<span class="toggle-slider"></span>' +
                '</label>' +
            '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('SettingsFormLabelRunInTaskbarWhenClosed') || 'Run in Taskbar When Closed') + '</span>' +
                '<label class="toggle-switch">' +
                    '<input type="checkbox" id="set-taskbar"' + (s.runInTaskbarWhenClosed ? ' checked' : '') + '>' +
                    '<span class="toggle-slider"></span>' +
                '</label>' +
            '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('SettingsFormLabelIsCountdownNotifierEnabled') || 'Countdown Notifier') + '</span>' +
                '<label class="toggle-switch">' +
                    '<input type="checkbox" id="set-countdown"' + (s.isCountdownNotifierEnabled ? ' checked' : '') + '>' +
                    '<span class="toggle-slider"></span>' +
                '</label>' +
            '</div>' +

            '<div class="settings-row">' +
                '<div class="settings-label-group">' +
                    '<span class="settings-label">' + (L('SettingsFormLabelCountdownNotifierSeconds') || 'Countdown Seconds') + '</span>' +
                    '<span class="mi settings-info-icon" data-tooltip="' + ((L('TooltipCountdownSeconds') || 'How long the warning is shown before the action runs.\nRecommended: 5-10 seconds.').replace(/"/g, '&quot;')) + '">info</span>' +
                '</div>' +
                '<input type="number" id="set-seconds" class="form-input" style="max-width:80px;text-align:center" min="0" max="30" value="' + (s.countdownNotifierSeconds || 5) + '">' +
            '</div>' +

            '<div class="settings-row">' +
                '<div class="settings-label-group">' +
                    '<span class="settings-label">' + (L('SettingsFormLabelBluetoothThreshold') || 'Bluetooth threshold (sec)') + '</span>' +
                    '<span class="mi settings-info-icon" data-tooltip="' + ((L('TooltipBluetoothThreshold') || 'Wait time after device is lost before triggering the action.\nRecommended: 5-15 seconds.').replace(/"/g, '&quot;')) + '">info</span>' +
                '</div>' +
                '<input type="number" id="set-bt-threshold" class="form-input" style="max-width:80px;text-align:center" min="1" max="60" value="' + (s.bluetoothThresholdSeconds || 5) + '">' +
            '</div>' +

            '<div class="settings-row">' +
                '<div class="settings-label-group">' +
                    '<span class="settings-label">' + (L('SettingsFormLabelBluetoothRssiThreshold') || 'Bluetooth signal threshold (dBm)') + '</span>' +
                    '<span class="mi settings-info-icon" data-tooltip="' + ((L('TooltipBluetoothRssi') || 'Minimum signal strength to consider the device connected.\n0 = disables the check.\nRecommended: -70 to -50.').replace(/"/g, '&quot;')) + '">info</span>' +
                '</div>' +
                '<input type="number" id="set-bt-rssi" class="form-input" style="max-width:80px;text-align:center" min="-100" max="0" value="' + (s.bluetoothRssiThreshold || 0) + '">' +
            '</div>' +

            '<div class="settings-actions">' +
                '<button class="btn btn-secondary" id="set-import-conf">' + (L('SettingsFormButtonImportConfig') || 'Import (.conf)') + '</button>' +
                '<button class="btn btn-secondary" id="set-export-conf">' + (L('SettingsFormButtonExportConfig') || 'Export (.conf)') + '</button>' +
                '<button class="btn btn-secondary" id="set-cancel">' + (L('SettingsFormButtonCancel') || 'Cancel') + '</button>' +
                '<button class="btn btn-success" id="set-save">' + (L('SettingsFormButtonSave') || 'Save') + '</button>' +
            '</div>' +
        '</div>';
    },

    beforeLeave() {
        this._disposeHandlers();
    },

    afterRender() {
        var self = this;
        self._disposeHandlers();

        // Request settings and language list from C#
        Bridge.send('loadSettings', {});
        Bridge.send('getLanguageList', {});

        var offSettingsLoaded = Bridge.on('settingsLoaded', function (s) {
            var el;
            el = document.getElementById('set-theme');
            if (el) el.value = s.theme || 'system';
            el = document.getElementById('set-logs');
            if (el) el.checked = !!s.logsEnabled;
            el = document.getElementById('set-startup');
            if (el) el.checked = !!s.startWithWindows;
            el = document.getElementById('set-taskbar');
            if (el) el.checked = !!s.runInTaskbarWhenClosed;
            el = document.getElementById('set-countdown');
            if (el) el.checked = !!s.isCountdownNotifierEnabled;
            el = document.getElementById('set-seconds');
            if (el) el.value = s.countdownNotifierSeconds || 5;
            el = document.getElementById('set-bt-threshold');
            if (el) el.value = s.bluetoothThresholdSeconds || 5;
            el = document.getElementById('set-bt-rssi');
            if (el) el.value = s.bluetoothRssiThreshold || 0;
        });
        self._registerCleanup(offSettingsLoaded);

        var offLanguageList = Bridge.on('languageList', function (list) {
            self._languageList = list || [];
            var sel = document.getElementById('set-lang');
            if (!sel) return;
            sel.innerHTML = '';
            for (var i = 0; i < list.length; i++) {
                var opt = document.createElement('option');
                opt.value = list[i].LangCode;
                opt.textContent = list[i].langName;
                if (list[i].LangCode === (Bridge._settings.language || 'auto')) {
                    opt.selected = true;
                }
                sel.appendChild(opt);
            }
        });
        self._registerCleanup(offLanguageList);

        // Live theme preview
        var themeEl = document.getElementById('set-theme');
        var onThemeChange = function () {
            Bridge.applyTheme(this.value);
        };
        themeEl.addEventListener('change', onThemeChange);
        self._registerCleanup(function () {
            themeEl.removeEventListener('change', onThemeChange);
        });

        var saveEl = document.getElementById('set-save');
        var onSaveClick = function () {
            Bridge.send('saveSettings', {
                logsEnabled: document.getElementById('set-logs').checked,
                startWithWindows: document.getElementById('set-startup').checked,
                runInTaskbarWhenClosed: document.getElementById('set-taskbar').checked,
                isCountdownNotifierEnabled: document.getElementById('set-countdown').checked,
                countdownNotifierSeconds: parseInt(document.getElementById('set-seconds').value) || 5,
                bluetoothThresholdSeconds: parseInt(document.getElementById('set-bt-threshold').value) || 5,
                bluetoothRssiThreshold: parseInt(document.getElementById('set-bt-rssi').value) || 0,
                language: document.getElementById('set-lang').value,
                theme: document.getElementById('set-theme').value
            });
        };
        saveEl.addEventListener('click', onSaveClick);
        self._registerCleanup(function () {
            saveEl.removeEventListener('click', onSaveClick);
        });

        var importEl = document.getElementById('set-import-conf');
        var onImportClick = function () {
            Bridge.send('importSettingsConfig', {});
        };
        importEl.addEventListener('click', onImportClick);
        self._registerCleanup(function () {
            importEl.removeEventListener('click', onImportClick);
        });

        var exportEl = document.getElementById('set-export-conf');
        var onExportClick = function () {
            Bridge.send('exportSettingsConfig', {});
        };
        exportEl.addEventListener('click', onExportClick);
        self._registerCleanup(function () {
            exportEl.removeEventListener('click', onExportClick);
        });

        var cancelEl = document.getElementById('set-cancel');
        var onCancelClick = function () {
            Bridge.send('closeWindow', {});
        };
        cancelEl.addEventListener('click', onCancelClick);
        self._registerCleanup(function () {
            cancelEl.removeEventListener('click', onCancelClick);
        });
    }
};
