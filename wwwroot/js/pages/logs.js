// Logs Page
const LogsPage = {
    _logs: [],
    _allLogs: [],

    render() {
        var L = Bridge.lang.bind(Bridge);

        return '' +
        '<div class="section-box">' +
            '<div class="section-title">' + (L('logViewerForm_Name') || 'Logs') + '</div>' +
            '<div class="logs-toolbar">' +
                '<span class="form-label">' + (L('logViewerForm_label_filtering') || 'Filter') + '</span>' +
                '<select id="log-filter" class="form-select">' +
                    '<option value="all">' + (L('logViewerForm_filter_choose') || 'All') + '</option>' +
                    '<option value="locks">' + (L('logViewerForm_filter_locks') || 'Locks') + '</option>' +
                    '<option value="unlocks">' + (L('logViewerForm_filter_unlocks') || 'Unlocks') + '</option>' +
                    '<option value="turnOffsMonitor">' + (L('logViewerForm_filter_turnOffsMonitor') || 'Monitor Off') + '</option>' +
                    '<option value="sleeps">' + (L('logViewerForm_filter_sleeps') || 'Sleeps') + '</option>' +
                    '<option value="logOffs">' + (L('logViewerForm_filter_logOffs') || 'Log Offs') + '</option>' +
                    '<option value="shutdowns">' + (L('logViewerForm_filter_shutdowns') || 'Shutdowns') + '</option>' +
                    '<option value="restarts">' + (L('logViewerForm_filter_restarts') || 'Restarts') + '</option>' +
                    '<option value="appStarts">' + (L('logViewerForm_filter_appStarts') || 'App Starts') + '</option>' +
                    '<option value="appTerminates">' + (L('logViewerForm_filter_appTerminates') || 'App Terminates') + '</option>' +
                '</select>' +
                '<span class="form-label">' + (L('logViewerForm_label_sorting') || 'Sort') + '</span>' +
                '<select id="log-sort" class="form-select">' +
                    '<option value="newestToOld">' + (L('logViewerForm_sorting_newestToOld') || 'Newest first') + '</option>' +
                    '<option value="oldestToNewest">' + (L('logViewerForm_sorting_OldestToNewest') || 'Oldest first') + '</option>' +
                '</select>' +
            '</div>' +
            '<div id="log-table-wrap"></div>' +
            '<div class="logs-actions">' +
                '<button class="btn btn-danger" id="log-clear">' + (L('logViewerForm_button_clearLogs') || 'Clear Logs') + '</button>' +
                '<button class="btn btn-secondary" id="log-back">' + (L('logViewerForm_button_cancel') || 'Back') + '</button>' +
            '</div>' +
        '</div>';
    },

    afterRender() {
        var self = this;

        Bridge.send('loadLogs', {});

        Bridge.on('logsLoaded', function (data) {
            self._allLogs = data || [];
            self._applyFilterSort();
        });

        document.getElementById('log-filter').addEventListener('change', function () {
            self._applyFilterSort();
        });

        document.getElementById('log-sort').addEventListener('change', function () {
            self._applyFilterSort();
        });

        document.getElementById('log-clear').addEventListener('click', function () {
            Bridge.send('clearLogs', {});
            self._allLogs = [];
            self._logs = [];
            self._renderTable();
        });

        document.getElementById('log-back').addEventListener('click', function () {
            App.navigate('main');
        });
    },

    _filterMap: {
        'locks': ['lockComputer', 'lockComputerManually'],
        'unlocks': ['unlockComputer'],
        'turnOffsMonitor': ['turnOffMonitor'],
        'sleeps': ['sleepComputer'],
        'logOffs': ['logOffWindows'],
        'shutdowns': ['shutdownComputer'],
        'restarts': ['restartComputer'],
        'appStarts': ['appStarted'],
        'appTerminates': ['appTerminated']
    },

    _applyFilterSort() {
        var filterEl = document.getElementById('log-filter');
        var sortEl = document.getElementById('log-sort');
        if (!filterEl || !sortEl) return;

        var filterVal = filterEl.value;
        var sortVal = sortEl.value;
        var logs = this._allLogs.slice();

        // Filter
        if (filterVal !== 'all' && this._filterMap[filterVal]) {
            var types = this._filterMap[filterVal];
            logs = logs.filter(function (l) {
                return types.indexOf(l.actionTypeRaw) >= 0;
            });
        }

        // Sort
        logs.sort(function (a, b) {
            if (sortVal === 'oldestToNewest') {
                return (a.actionExecutedDate || '').localeCompare(b.actionExecutedDate || '');
            }
            return (b.actionExecutedDate || '').localeCompare(a.actionExecutedDate || '');
        });

        this._logs = logs;
        this._renderTable();
    },

    _renderTable() {
        var wrap = document.getElementById('log-table-wrap');
        if (!wrap) return;

        var L = Bridge.lang.bind(Bridge);

        if (!this._logs || this._logs.length === 0) {
            wrap.innerHTML = '<div class="table-empty">' + (L('messageContent_noLog') || 'No logs found') + '</div>';
            return;
        }

        var html = '<table class="data-table"><thead><tr>' +
            '<th style="width:50px">#</th>' +
            '<th>' + (L('logViewerForm_actionExecutionTime') || 'Date') + '</th>' +
            '<th>' + (L('logViewerForm_actionType') || 'Action') + '</th>' +
            '</tr></thead><tbody>';

        for (var i = 0; i < this._logs.length; i++) {
            var l = this._logs[i];
            html += '<tr>' +
                '<td>' + (i + 1) + '</td>' +
                '<td>' + (l.actionExecutedDate || '') + '</td>' +
                '<td>' + (l.actionType || '') + '</td>' +
                '</tr>';
        }

        html += '</tbody></table>';
        wrap.innerHTML = html;
    }
};
