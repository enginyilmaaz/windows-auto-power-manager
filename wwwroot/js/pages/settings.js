// Settings Page
const SettingsPage = {
    _languageList: [],

    render() {
        var L = Bridge.lang.bind(Bridge);
        var s = Bridge._settings || {};

        return '' +
        '<div class="section-box">' +
            '<div class="section-title">' + (L('settingsForm_Name') || 'Settings') + '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('settingsForm_label_language') || 'Language') + '</span>' +
                '<select id="set-lang" class="form-select" style="max-width:200px"></select>' +
            '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('settingsForm_label_logs') || 'Record Logs') + '</span>' +
                '<label class="toggle-switch">' +
                    '<input type="checkbox" id="set-logs"' + (s.logsEnabled ? ' checked' : '') + '>' +
                    '<span class="toggle-slider"></span>' +
                '</label>' +
            '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('settingsForm_label_startWithWindows') || 'Start with Windows') + '</span>' +
                '<label class="toggle-switch">' +
                    '<input type="checkbox" id="set-startup"' + (s.startWithWindows ? ' checked' : '') + '>' +
                    '<span class="toggle-slider"></span>' +
                '</label>' +
            '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('settingsForm_label_runInTaskbarWhenClosed') || 'Run in Taskbar When Closed') + '</span>' +
                '<label class="toggle-switch">' +
                    '<input type="checkbox" id="set-taskbar"' + (s.runInTaskbarWhenClosed ? ' checked' : '') + '>' +
                    '<span class="toggle-slider"></span>' +
                '</label>' +
            '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('settingsForm_label_isCountdownNotifierEnabled') || 'Countdown Notifier') + '</span>' +
                '<label class="toggle-switch">' +
                    '<input type="checkbox" id="set-countdown"' + (s.isCountdownNotifierEnabled ? ' checked' : '') + '>' +
                    '<span class="toggle-slider"></span>' +
                '</label>' +
            '</div>' +

            '<div class="settings-row">' +
                '<span class="settings-label">' + (L('settingsForm_label_countdownNotifierSeconds') || 'Countdown Seconds') + '</span>' +
                '<input type="number" id="set-seconds" class="form-input" style="max-width:80px;text-align:center" min="0" max="30" value="' + (s.countdownNotifierSeconds || 5) + '">' +
            '</div>' +

            '<div class="settings-actions">' +
                '<button class="btn btn-secondary" id="set-cancel">' + (L('settingsForm_button_cancel') || 'Cancel') + '</button>' +
                '<button class="btn btn-success" id="set-save">' + (L('settingsForm_button_save') || 'Save') + '</button>' +
            '</div>' +
        '</div>';
    },

    afterRender() {
        var self = this;

        // Request settings and language list from C#
        Bridge.send('loadSettings', {});
        Bridge.send('getLanguageList', {});

        Bridge.on('settingsLoaded', function (s) {
            var el;
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
        });

        Bridge.on('languageList', function (list) {
            self._languageList = list || [];
            var sel = document.getElementById('set-lang');
            if (!sel) return;
            sel.innerHTML = '';
            for (var i = 0; i < list.length; i++) {
                var opt = document.createElement('option');
                opt.value = list[i].langCode;
                opt.textContent = list[i].langName;
                if (list[i].langCode === (Bridge._settings.language || 'auto')) {
                    opt.selected = true;
                }
                sel.appendChild(opt);
            }
        });

        document.getElementById('set-save').addEventListener('click', function () {
            Bridge.send('saveSettings', {
                logsEnabled: document.getElementById('set-logs').checked,
                startWithWindows: document.getElementById('set-startup').checked,
                runInTaskbarWhenClosed: document.getElementById('set-taskbar').checked,
                isCountdownNotifierEnabled: document.getElementById('set-countdown').checked,
                countdownNotifierSeconds: parseInt(document.getElementById('set-seconds').value) || 5,
                language: document.getElementById('set-lang').value
            });
        });

        document.getElementById('set-cancel').addEventListener('click', function () {
            App.navigate('main');
        });
    }
};
