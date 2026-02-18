// Logs Page
window.LogsPage = {
    _logs: [],
    _allLogs: [],
    _cleanupFns: [],
    _searchQuery: '',

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
        var self = this;
        var L = Bridge.lang.bind(Bridge);
        var t = function (key, fallback) {
            var translated = L(key);
            return (!translated || translated === key) ? fallback : translated;
        };

        return '' +
        '<div class="card">' +
            '<div class="card-title">' + (L('LogViewerFormName') || 'Logs') + '</div>' +
            '<div class="logs-filter-panel">' +
                '<div class="logs-search-row">' +
                    '<div class="toolbar-search logs-toolbar-search">' +
                        '<span class="mi toolbar-search-icon">search</span>' +
                        '<input type="text" id="log-search" class="toolbar-search-input" placeholder="' +
                            t('ToolbarSearch', 'Search...') + '">' +
                    '</div>' +
                '</div>' +
                '<div class="logs-filter-grid">' +
                    '<div class="logs-filter-item">' +
                        '<span class="form-label">' + t('LogViewerFormLabelFiltering', 'Filter') + '</span>' +
                        '<select id="log-filter" class="form-select">' + self._renderActionTypeOptions() + '</select>' +
                    '</div>' +
                    '<div class="logs-filter-item">' +
                        '<span class="form-label">' + t('LogViewerFormDateFrom', 'From date') + '</span>' +
                        '<input type="date" id="log-date-from" class="form-input">' +
                    '</div>' +
                    '<div class="logs-filter-item">' +
                        '<span class="form-label">' + t('LogViewerFormDateTo', 'To date') + '</span>' +
                        '<input type="date" id="log-date-to" class="form-input">' +
                    '</div>' +
                    '<div class="logs-filter-item">' +
                        '<span class="form-label">' + t('LogViewerFormLabelSorting', 'Sort') + '</span>' +
                        '<select id="log-sort" class="form-select">' +
                            '<option value="newestToOld">' + t('LogViewerFormSortingNewestToOld', 'Newest first') + '</option>' +
                            '<option value="oldestToNewest">' + t('LogViewerFormSortingOldestToNewest', 'Oldest first') + '</option>' +
                            '<option value="actionAsc">' + t('LogViewerFormActionType', 'Action') + ' (A-Z)</option>' +
                            '<option value="actionDesc">' + t('LogViewerFormActionType', 'Action') + ' (Z-A)</option>' +
                        '</select>' +
                    '</div>' +
                    '<div class="logs-filter-item logs-filter-actions-row">' +
                        '<button class="btn btn-secondary logs-filter-reset" id="log-reset-filters">' +
                            t('LogViewerFormButtonResetFilters', 'Reset filters') +
                        '</button>' +
                    '</div>' +
                '</div>' +
                '<div id="log-result-meta" class="logs-result-meta"></div>' +
            '</div>' +
            '<div id="log-table-wrap"></div>' +
            '<div class="logs-actions">' +
                '<button class="btn btn-danger" id="log-clear">' + (L('LogViewerFormButtonClearLogs') || 'Clear Logs') + '</button>' +
                '<button class="btn btn-secondary" id="log-back">' + (L('LogViewerFormButtonCancel') || 'Back') + '</button>' +
            '</div>' +
        '</div>';
    },

    beforeLeave() {
        this._disposeHandlers();
    },

    afterRender() {
        var self = this;
        self._disposeHandlers();
        self._searchQuery = '';

        Bridge.send('loadLogs', {});

        var offLogsLoaded = Bridge.on('logsLoaded', function (data) {
            self._allLogs = data || [];
            self._applyFilterSort();
        });
        self._registerCleanup(offLogsLoaded);

        var filterEl = document.getElementById('log-filter');
        var onFilterChange = function () {
            self._applyFilterSort();
        };
        filterEl.addEventListener('change', onFilterChange);
        self._registerCleanup(function () {
            filterEl.removeEventListener('change', onFilterChange);
        });

        var sortEl = document.getElementById('log-sort');
        var onSortChange = function () {
            self._applyFilterSort();
        };
        sortEl.addEventListener('change', onSortChange);
        self._registerCleanup(function () {
            sortEl.removeEventListener('change', onSortChange);
        });

        var dateFromEl = document.getElementById('log-date-from');
        var dateToEl = document.getElementById('log-date-to');
        var onDateChange = function () {
            self._applyFilterSort();
        };
        dateFromEl.addEventListener('change', onDateChange);
        dateToEl.addEventListener('change', onDateChange);
        self._registerCleanup(function () {
            dateFromEl.removeEventListener('change', onDateChange);
            dateToEl.removeEventListener('change', onDateChange);
        });

        var searchEl = document.getElementById('log-search');
        var onSearchInput = function () {
            self._searchQuery = (searchEl.value || '').toLowerCase();
            self._applyFilterSort();
        };
        searchEl.addEventListener('input', onSearchInput);
        self._registerCleanup(function () {
            searchEl.removeEventListener('input', onSearchInput);
        });

        var resetEl = document.getElementById('log-reset-filters');
        var onResetClick = function () {
            if (filterEl) filterEl.value = 'all';
            if (sortEl) sortEl.value = 'newestToOld';
            if (dateFromEl) dateFromEl.value = '';
            if (dateToEl) dateToEl.value = '';
            if (searchEl) searchEl.value = '';
            self._searchQuery = '';
            self._applyFilterSort();
        };
        resetEl.addEventListener('click', onResetClick);
        self._registerCleanup(function () {
            resetEl.removeEventListener('click', onResetClick);
        });

        var clearEl = document.getElementById('log-clear');
        var onClearClick = function () {
            Bridge.send('clearLogs', {});
            self._allLogs = [];
            self._logs = [];
            self._renderTable();
            self._updateResultMeta();
        };
        clearEl.addEventListener('click', onClearClick);
        self._registerCleanup(function () {
            clearEl.removeEventListener('click', onClearClick);
        });

        var backEl = document.getElementById('log-back');
        var onBackClick = function () {
            App.navigate('main');
        };
        backEl.addEventListener('click', onBackClick);
        self._registerCleanup(function () {
            backEl.removeEventListener('click', onBackClick);
        });
    },

    _filterMap: {
        'locks': ['LockComputer', 'LockComputerManually'],
        'unlocks': ['UnlockComputer'],
        'turnOffsMonitor': ['TurnOffMonitor'],
        'sleeps': ['SleepComputer'],
        'logOffs': ['LogOffWindows'],
        'shutdowns': ['ShutdownComputer'],
        'restarts': ['RestartComputer'],
        'appStarts': ['AppStarted'],
        'appTerminates': ['AppTerminated']
    },

    _renderActionTypeOptions() {
        var L = Bridge.lang.bind(Bridge);
        var options = [
            { value: 'all', text: L('LogViewerFormFilterChoose') || 'All actions' },
            { value: 'locks', text: L('LogViewerFormFilterLocks') || 'Locks' },
            { value: 'unlocks', text: L('LogViewerFormFilterUnlocks') || 'Unlocks' },
            { value: 'turnOffsMonitor', text: L('LogViewerFormFilterTurnOffsMonitor') || 'Monitor off' },
            { value: 'sleeps', text: L('LogViewerFormFilterSleeps') || 'Sleeps' },
            { value: 'logOffs', text: L('LogViewerFormFilterLogOffs') || 'Log offs' },
            { value: 'shutdowns', text: L('LogViewerFormFilterShutdowns') || 'Shutdowns' },
            { value: 'restarts', text: L('LogViewerFormFilterRestarts') || 'Restarts' },
            { value: 'appStarts', text: L('LogViewerFormFilterAppStarts') || 'App starts' },
            { value: 'appTerminates', text: L('LogViewerFormFilterAppTerminates') || 'App terminates' }
        ];

        var html = '';
        for (var i = 0; i < options.length; i++) {
            html += '<option value="' + options[i].value + '">' + options[i].text + '</option>';
        }

        return html;
    },

    _applyFilterSort() {
        var self = this;
        var filterEl = document.getElementById('log-filter');
        var sortEl = document.getElementById('log-sort');
        var dateFromEl = document.getElementById('log-date-from');
        var dateToEl = document.getElementById('log-date-to');
        if (!filterEl || !sortEl || !dateFromEl || !dateToEl) return;

        var filterVal = filterEl.value;
        var sortVal = sortEl.value;
        var logs = this._allLogs.slice();

        if (filterVal !== 'all' && this._filterMap[filterVal]) {
            var allowedTypes = this._filterMap[filterVal];
            logs = logs.filter(function (l) {
                return allowedTypes.indexOf(l.actionTypeRaw) >= 0;
            });
        }

        var fromDate = this._parsePickerDate(dateFromEl.value, false);
        var toDate = this._parsePickerDate(dateToEl.value, true);
        if (fromDate || toDate) {
            logs = logs.filter(function (l) {
                var executedDate = self._parseLogDate(l.actionExecutedDate);
                if (!executedDate) return false;
                if (fromDate && executedDate < fromDate) return false;
                if (toDate && executedDate > toDate) return false;
                return true;
            });
        }

        var query = (this._searchQuery || '').trim();
        if (query) {
            logs = logs.filter(function (l) {
                var haystack = (
                    (l.actionType || '') + ' ' +
                    (l.actionTypeRaw || '') + ' ' +
                    (l.actionExecutedDate || '')
                ).toLowerCase();
                return haystack.indexOf(query) >= 0;
            });
        }

        logs.sort(function (a, b) {
            var aDate = self._parseLogDate(a.actionExecutedDate);
            var bDate = self._parseLogDate(b.actionExecutedDate);
            var aTicks = aDate ? aDate.getTime() : 0;
            var bTicks = bDate ? bDate.getTime() : 0;

            if (sortVal === 'actionAsc' || sortVal === 'actionDesc') {
                var aType = (a.actionTypeRaw || a.actionType || '').toLowerCase();
                var bType = (b.actionTypeRaw || b.actionType || '').toLowerCase();
                var typeCompare = aType.localeCompare(bType);
                if (typeCompare !== 0) {
                    return sortVal === 'actionAsc' ? typeCompare : -typeCompare;
                }
                return bTicks - aTicks;
            }

            if (sortVal === 'oldestToNewest') {
                return aTicks - bTicks;
            }
            return bTicks - aTicks;
        });

        this._logs = logs;
        this._renderTable();
        this._updateResultMeta();
    },

    _parsePickerDate(value, setDayEnd) {
        if (!value) return null;
        var m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
        if (!m) return null;

        var year = parseInt(m[1], 10);
        var month = parseInt(m[2], 10) - 1;
        var day = parseInt(m[3], 10);

        return setDayEnd
            ? new Date(year, month, day, 23, 59, 59, 999)
            : new Date(year, month, day, 0, 0, 0, 0);
    },

    _parseLogDate(value) {
        var m = /^(\d{2})\.(\d{2})\.(\d{4}) (\d{2}):(\d{2}):(\d{2})$/.exec(value || '');
        if (!m) return null;

        var date = new Date(
            parseInt(m[3], 10),
            parseInt(m[2], 10) - 1,
            parseInt(m[1], 10),
            parseInt(m[4], 10),
            parseInt(m[5], 10),
            parseInt(m[6], 10)
        );

        return isNaN(date.getTime()) ? null : date;
    },

    _updateResultMeta() {
        var metaEl = document.getElementById('log-result-meta');
        if (!metaEl) return;
        metaEl.textContent = (this._logs.length || 0) + ' / ' + (this._allLogs.length || 0);
    },

    _renderTable() {
        var wrap = document.getElementById('log-table-wrap');
        if (!wrap) return;

        var L = Bridge.lang.bind(Bridge);

        if (!this._logs || this._logs.length === 0) {
            wrap.innerHTML = '<div class="table-empty">' + (L('MessageContentNoLog') || 'No logs found') + '</div>';
            return;
        }

        var html = '<table class="data-table"><thead><tr>' +
            '<th style="width:50px">#</th>' +
            '<th>' + (L('LogViewerFormActionExecutionTime') || 'Date') + '</th>' +
            '<th>' + (L('LogViewerFormActionType') || 'Action') + '</th>' +
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
