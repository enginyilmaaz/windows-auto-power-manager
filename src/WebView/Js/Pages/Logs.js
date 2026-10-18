// Action history page.
//
// Every filter is answered by C# over the whole stored history. The previous version asked for the
// last 250 entries and then filtered those in the browser, so a date range or a search pointing
// past them reported "no records" while the records sat in the file.
window.LogsPage = {
    _rows: [],
    _meta: null,
    _cleanupFns: [],
    _searchTimer: null,
    _expandedId: null,
    _panelOpen: false,

    _state: {
        window: 'today',
        facets: null,          // null = every facet; a Set once the person narrows it
        onlyErrors: false,
        query: '',
        sort: 'newest',
        from: '',
        to: '',
        offset: 0
    },

    _facetOrder: ['locks', 'unlocks', 'monitor', 'sleeps', 'shutdowns', 'restarts', 'logOffs', 'app'],

    _facetMeta: {
        locks: { icon: 'lock', key: 'LogViewerFormFilterLocks', fallback: 'Locks', tone: 'lock' },
        unlocks: { icon: 'lock_open', key: 'LogViewerFormFilterUnlocks', fallback: 'Unlocks', tone: 'unlock' },
        monitor: { icon: 'desktop_access_disabled', key: 'LogViewerFormFilterTurnOffsMonitor', fallback: 'Monitor off', tone: 'mon' },
        sleeps: { icon: 'bedtime', key: 'LogViewerFormFilterSleeps', fallback: 'Sleeps', tone: 'mon' },
        shutdowns: { icon: 'power_settings_new', key: 'LogViewerFormFilterShutdowns', fallback: 'Shutdowns', tone: 'app' },
        restarts: { icon: 'restart_alt', key: 'LogViewerFormFilterRestarts', fallback: 'Restarts', tone: 'app' },
        logOffs: { icon: 'logout', key: 'LogViewerFormFilterLogOffs', fallback: 'Log offs', tone: 'app' },
        app: { icon: 'apps', key: 'LogViewerFormFilterApp', fallback: 'App start / exit', tone: 'app' }
    },

    _windowOrder: ['today', '7d', '30d', 'all'],

    _windowMeta: {
        today: { icon: 'today', key: 'LogViewerFormWindowToday', fallback: 'Today' },
        '7d': { icon: 'date_range', key: 'LogViewerFormWindowLast7', fallback: 'Last 7 days' },
        '30d': { icon: 'calendar_month', key: 'LogViewerFormWindowLast30', fallback: 'Last 30 days' },
        all: { icon: 'history', key: 'LogViewerFormWindowAll', fallback: 'All' }
    },

    // ───────────────────────── helpers ─────────────────────────

    _t(key, fallback) {
        var translated = Bridge.lang(key);
        return (!translated || translated === key) ? fallback : translated;
    },

    _format(template, values) {
        return String(template).replace(/\{(\d+)\}/g, function (match, index) {
            var value = values[Number(index)];
            return value === undefined || value === null ? match : value;
        });
    },

    _escape(value) {
        return String(value === undefined || value === null ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    },

    _number(value) {
        try {
            return Number(value || 0).toLocaleString();
        } catch (_) {
            return String(value || 0);
        }
    },

    _registerCleanup(fn) {
        if (typeof fn === 'function') {
            this._cleanupFns.push(fn);
        }
    },

    _disposeHandlers() {
        if (this._searchTimer) {
            clearTimeout(this._searchTimer);
            this._searchTimer = null;
        }

        while (this._cleanupFns.length > 0) {
            var fn = this._cleanupFns.pop();
            try {
                fn();
            } catch (_) {
            }
        }
    },

    _isSubWindowHost() {
        var path = window.location.pathname || '';
        var href = window.location.href || '';
        return /SubWindow\.html/i.test(path) || href.indexOf('SubWindow.html') >= 0;
    },

    _on(id, event, handler) {
        var el = document.getElementById(id);
        if (!el) return null;
        el.addEventListener(event, handler);
        this._registerCleanup(function () {
            el.removeEventListener(event, handler);
        });
        return el;
    },

    /// Delegated so the rows can be replaced without rebinding every one of them.
    _delegate(containerId, event, selector, handler) {
        var container = document.getElementById(containerId);
        if (!container) return;

        var listener = function (e) {
            var node = e.target && e.target.closest ? e.target.closest(selector) : null;
            if (node && container.contains(node)) {
                handler(node, e);
            }
        };

        container.addEventListener(event, listener);
        this._registerCleanup(function () {
            container.removeEventListener(event, listener);
        });
    },

    _facetSelected(key) {
        return this._state.facets === null || this._state.facets.has(key);
    },

    // ───────────────────────── render ─────────────────────────

    render() {
        var t = this._t.bind(this);

        return '' +
        '<div class="subpage-layout logs-layout">' +
            '<div class="card logs-page-card subpage-card">' +
                '<div class="card-title">' + this._escape(t('LogViewerFormName', 'Logs')) + '</div>' +
                '<div class="logs-body">' +
                    '<button class="btn btn-secondary logs-filter-toggle" id="log-filter-toggle">' +
                        '<span class="mi">filter_list</span>' +
                        '<span>' + this._escape(t('LogViewerFormFilterButton', 'Filter')) + '</span>' +
                        '<span class="logs-filter-badge hidden" id="log-filter-badge"></span>' +
                    '</button>' +
                    '<aside class="logs-facets" id="log-facets"></aside>' +
                    '<div class="logs-main">' +
                        '<div class="logs-toolbar">' +
                            '<div class="toolbar-search logs-toolbar-search">' +
                                '<span class="mi toolbar-search-icon">search</span>' +
                                '<input type="text" id="log-search" class="toolbar-search-input" ' +
                                    'placeholder="' + this._escape(t('ToolbarSearch', 'Search...')) + '">' +
                                '<button class="logs-search-clear hidden" id="log-search-clear" tabindex="-1">' +
                                    '<span class="mi">close</span>' +
                                '</button>' +
                            '</div>' +
                            '<select id="log-sort" class="form-select logs-sort">' +
                                '<option value="newest">' + this._escape(t('LogViewerFormSortingNewestToOld', 'Newest first')) + '</option>' +
                                '<option value="oldest">' + this._escape(t('LogViewerFormSortingOldestToNewest', 'Oldest first')) + '</option>' +
                            '</select>' +
                        '</div>' +
                        '<div class="logs-table-card" id="log-table-wrap"></div>' +
                        '<div class="logs-result-meta" id="log-result-meta"></div>' +
                    '</div>' +
                '</div>' +
            '</div>' +
            '<div class="logs-actions subpage-footer">' +
                '<button class="btn btn-ghost" id="log-export">' +
                    '<span class="mi">download</span>' +
                    '<span>' + this._escape(t('LogViewerFormExport', 'Export')) + '</span>' +
                '</button>' +
                '<div class="logs-export-menu hidden" id="log-export-menu">' +
                    '<div class="logs-export-item" data-format="csv">' +
                        '<span class="mi">table_view</span>' + this._escape(t('LogViewerFormExportCsv', 'As CSV (Excel)')) +
                    '</div>' +
                    '<div class="logs-export-item" data-format="jsonl">' +
                        '<span class="mi">data_object</span>' + this._escape(t('LogViewerFormExportJsonl', 'As JSONL (raw)')) +
                    '</div>' +
                    '<div class="logs-export-divider"></div>' +
                    '<div class="logs-export-item" data-format="visible">' +
                        '<span class="mi">filter_alt</span>' + this._escape(t('LogViewerFormExportVisible', 'Only the rows shown')) +
                    '</div>' +
                '</div>' +
                '<span class="logs-actions-spacer"></span>' +
                '<button class="btn btn-danger" id="log-clear">' +
                    this._escape(t('LogViewerFormButtonClearLogs', 'Clear Logs')) +
                '</button>' +
                '<button class="btn btn-secondary" id="log-back">' +
                    this._escape(t('LogViewerFormButtonCancel', 'Back')) +
                '</button>' +
            '</div>' +
        '</div>';
    },

    _renderFacets() {
        var host = document.getElementById('log-facets');
        if (!host) return;

        var self = this;
        var t = this._t.bind(this);
        var windowCounts = (this._meta && this._meta.windowCounts) || {};
        var facetCounts = (this._meta && this._meta.facetCounts) || {};
        var errorCount = (this._meta && this._meta.errorCount) || 0;

        var html = '<div class="logs-facet-group">' +
            '<div class="logs-facet-title">' + this._escape(t('LogViewerFormGroupTime', 'Time')) + '</div>';

        this._windowOrder.forEach(function (key) {
            var meta = self._windowMeta[key];
            var count = windowCounts[key];
            html += '<button class="logs-facet-row' + (self._state.window === key ? ' is-on' : '') + '" ' +
                    'data-window="' + key + '">' +
                '<span class="mi">' + meta.icon + '</span>' +
                '<span class="logs-facet-label">' + self._escape(t(meta.key, meta.fallback)) + '</span>' +
                '<span class="logs-facet-count">' + (count === undefined ? '' : self._number(count)) + '</span>' +
            '</button>';
        });

        html += '<button class="logs-facet-row' + (this._state.window === 'custom' ? ' is-on' : '') + '" ' +
                'data-window="custom">' +
            '<span class="mi">edit_calendar</span>' +
            '<span class="logs-facet-label">' + this._escape(t('LogViewerFormWindowCustom', 'Pick dates...')) + '</span>' +
        '</button>';

        if (this._state.window === 'custom') {
            html += '<div class="logs-date-range">' +
                '<label class="logs-date-field">' +
                    '<span>' + this._escape(t('LogViewerFormDateFrom', 'From')) + '</span>' +
                    '<input type="date" class="form-input" id="log-date-from" value="' + this._escape(this._state.from) + '">' +
                '</label>' +
                '<label class="logs-date-field">' +
                    '<span>' + this._escape(t('LogViewerFormDateTo', 'To')) + '</span>' +
                    '<input type="date" class="form-input" id="log-date-to" value="' + this._escape(this._state.to) + '">' +
                '</label>' +
            '</div>';
        }

        html += '</div><div class="logs-facet-group">' +
            '<div class="logs-facet-title">' + this._escape(t('LogViewerFormGroupType', 'Action type')) + '</div>';

        this._facetOrder.forEach(function (key) {
            var meta = self._facetMeta[key];
            var count = facetCounts[key] || 0;
            var selected = self._facetSelected(key);

            html += '<button class="logs-facet-row logs-facet-check' +
                    (count === 0 ? ' is-empty' : '') + '" data-facet="' + key + '">' +
                '<span class="logs-checkbox' + (selected ? ' is-on' : '') + '"></span>' +
                '<span class="mi logs-facet-icon tone-' + meta.tone + '">' + meta.icon + '</span>' +
                '<span class="logs-facet-label">' + self._escape(t(meta.key, meta.fallback)) + '</span>' +
                '<span class="logs-facet-count">' + self._number(count) + '</span>' +
            '</button>';
        });

        html += '<button class="logs-facet-row logs-facet-check' + (errorCount === 0 ? ' is-empty' : '') +
                (this._state.onlyErrors ? ' is-on' : '') + '" data-errors="1">' +
            '<span class="logs-checkbox' + (this._state.onlyErrors ? ' is-on' : '') + '"></span>' +
            '<span class="mi logs-facet-icon tone-err">error_outline</span>' +
            '<span class="logs-facet-label">' + this._escape(t('LogViewerFormOnlyErrors', 'Failures only')) + '</span>' +
            '<span class="logs-facet-count">' + this._number(errorCount) + '</span>' +
        '</button>';

        html += '</div>' +
            '<button class="logs-facet-reset" id="log-reset-filters">' +
                '<span class="mi">restart_alt</span>' +
                this._escape(t('LogViewerFormButtonResetFilters', 'Reset filters')) +
            '</button>';

        host.innerHTML = html;
    },

    _renderTable() {
        var wrap = document.getElementById('log-table-wrap');
        if (!wrap) return;

        var t = this._t.bind(this);

        if (!this._rows || this._rows.length === 0) {
            wrap.innerHTML = this._renderEmpty();
            return;
        }

        var self = this;
        var html = '<table class="data-table logs-table"><thead><tr>' +
            '<th class="logs-col-icon"></th>' +
            '<th class="logs-col-time">' + this._escape(t('LogViewerFormActionExecutionTime', 'Time')) + '</th>' +
            '<th>' + this._escape(t('LogViewerFormActionType', 'Action')) + '</th>' +
            '<th class="logs-col-source">' + this._escape(t('LogViewerFormColumnSource', 'Source')) + '</th>' +
            '</tr></thead><tbody>';

        var lastDay = null;

        this._rows.forEach(function (row) {
            if (row.day && row.day !== lastDay) {
                lastDay = row.day;
                html += '<tr class="logs-day-row"><td colspan="4">' +
                    self._escape(self._describeDay(row.day)) + '</td></tr>';
            }

            var meta = self._facetMeta[row.facet] || { icon: 'help_outline', tone: 'mon' };
            var tone = row.isError ? 'err' : meta.tone;
            var expanded = self._expandedId && row.id === self._expandedId;

            html += '<tr class="logs-row' + (expanded ? ' is-open' : '') + '" data-row-id="' + self._escape(row.id) + '">' +
                '<td class="logs-col-icon"><span class="mi logs-row-icon tone-' + tone + '">' +
                    (row.isError ? 'error_outline' : meta.icon) + '</span></td>' +
                '<td class="logs-col-time">' + self._escape(row.time || '') + '</td>' +
                '<td class="logs-row-label">' + self._escape(row.label || row.type || '') + '</td>' +
                '<td class="logs-col-source">' + self._escape(row.sourceLabel || '') + '</td>' +
            '</tr>';

            if (expanded) {
                html += self._renderDetail(row);
            }
        });

        html += '</tbody></table>';

        if (this._meta && this._meta.hasMore) {
            html += '<button class="logs-load-more" id="log-load-more">' +
                this._escape(t('LogViewerFormLoadMore', 'Load more')) + '</button>';
        }

        wrap.innerHTML = html;
    },

    _renderDetail(row) {
        var t = this._t.bind(this);
        var items = [];

        items.push([t('LogViewerFormDetailFullTime', 'Exact time'), '<code>' + this._escape(row.at || '') + '</code>']);
        items.push([t('LogViewerFormColumnSource', 'Source'), this._escape(row.sourceLabel || '')]);

        if (row.rule) {
            items.push([t('LogViewerFormDetailRule', 'Triggered by'), '<code>' + this._escape(row.rule) + '</code>']);
        }

        var outcome = row.isError
            ? '<span class="logs-outcome is-error"><span class="mi">cancel</span>' +
              this._escape(t('LogViewerFormOutcomeError', 'Failed')) +
              (row.error ? ' — ' + this._escape(row.error) : '') + '</span>'
            : '<span class="logs-outcome is-ok"><span class="mi">check_circle</span>' +
              this._escape(t('LogViewerFormOutcomeOk', 'Succeeded')) + '</span>';
        items.push([t('LogViewerFormColumnOutcome', 'Outcome'), outcome]);

        var html = '<tr class="logs-detail-row"><td colspan="4"><dl class="logs-detail">';
        items.forEach(function (pair) {
            html += '<dt>' + pair[0] + '</dt><dd>' + pair[1] + '</dd>';
        });
        return html + '</dl></td></tr>';
    },

    _renderEmpty() {
        var t = this._t.bind(this);
        var grandTotal = (this._meta && this._meta.grandTotal) || 0;

        // "Nothing stored" and "nothing matches" are different problems, and only the second one
        // has a way out. The old page showed the same sentence for both.
        if (grandTotal === 0) {
            return '<div class="logs-empty">' +
                '<span class="mi">inbox</span>' +
                '<span class="logs-empty-title">' + this._escape(t('MessageContentNoLog', 'No records yet')) + '</span>' +
                '<span class="logs-empty-hint">' +
                    this._escape(t('LogViewerFormEmptyHint', 'Actions will appear here as they happen.')) +
                '</span>' +
            '</div>';
        }

        var canWiden = this._state.window !== 'all' || this._state.onlyErrors ||
            this._state.facets !== null;

        return '<div class="logs-empty">' +
            '<span class="mi">search_off</span>' +
            '<span class="logs-empty-title">' +
                this._escape(t('LogViewerFormNoMatchTitle', 'No records match these filters')) +
            '</span>' +
            '<span class="logs-empty-hint">' +
                this._escape(t('LogViewerFormNoMatchHint', 'Nothing found in the selected range.')) + ' ' +
                this._escape(this._format(t('LogViewerFormRecordCount', '{0} records'), [this._number(grandTotal)])) +
            '</span>' +
            '<div class="logs-empty-actions">' +
                (canWiden
                    ? '<button class="btn btn-ghost" id="log-widen">' +
                        '<span class="mi">history</span>' +
                        this._escape(t('LogViewerFormSearchAllHistory', 'Search all history')) +
                      '</button>'
                    : '') +
                '<button class="btn btn-secondary" id="log-reset-empty">' +
                    this._escape(t('LogViewerFormButtonResetFilters', 'Reset filters')) +
                '</button>' +
            '</div>' +
        '</div>';
    },

    _describeDay(day) {
        var t = this._t.bind(this);
        var parts = /^(\d{4})-(\d{2})-(\d{2})$/.exec(day || '');
        if (!parts) return day || '';

        var date = new Date(Number(parts[1]), Number(parts[2]) - 1, Number(parts[3]));
        var today = new Date();
        today.setHours(0, 0, 0, 0);

        var diffDays = Math.round((today - date) / 86400000);
        if (diffDays === 0) return t('LogViewerFormDayToday', 'Today');
        if (diffDays === 1) return t('LogViewerFormDayYesterday', 'Yesterday');

        try {
            return date.toLocaleDateString(undefined, { day: 'numeric', month: 'long', weekday: 'long' });
        } catch (_) {
            return day;
        }
    },

    _updateResultMeta() {
        var el = document.getElementById('log-result-meta');
        if (!el || !this._meta) return;

        var t = this._t.bind(this);
        var shownText = this._format(t('LogViewerFormRecordCount', '{0} records'), [this._number(this._meta.total)]);

        var retention = this._meta.retentionDays;
        var historyText = retention > 0
            ? this._format(t('LogViewerFormHistoryNote', '{0} days of history · {1} records'),
                [this._number(retention), this._number(this._meta.grandTotal)])
            : this._format(t('LogViewerFormRecordCount', '{0} records'), [this._number(this._meta.grandTotal)]);

        el.innerHTML = '<span>' + this._escape(shownText) + '</span><span>' + this._escape(historyText) + '</span>';
    },

    _updateFilterBadge() {
        var badge = document.getElementById('log-filter-badge');
        if (!badge) return;

        var active = 0;
        if (this._state.window !== 'all') active += 1;
        if (this._state.facets !== null) active += 1;
        if (this._state.onlyErrors) active += 1;

        badge.textContent = String(active);
        badge.classList.toggle('hidden', active === 0);
    },

    // ───────────────────────── data ─────────────────────────

    _request(options) {
        options = options || {};
        if (!options.keepOffset) {
            this._state.offset = 0;
            this._expandedId = null;
        }

        var payload = {
            window: this._state.window,
            sort: this._state.sort,
            query: this._state.query,
            onlyErrors: this._state.onlyErrors,
            offset: this._state.offset,
            limit: 200
        };

        if (this._state.facets !== null) {
            payload.facets = Array.from(this._state.facets);
        }

        if (this._state.window === 'custom') {
            payload.from = this._state.from;
            payload.to = this._state.to;
        }

        Bridge.send('loadLogs', payload);
    },

    _onLogsLoaded(data) {
        if (!data) return;

        var rows = data.rows || [];
        // A "load more" reply extends the list; anything else replaces it.
        this._rows = this._state.offset > 0 ? this._rows.concat(rows) : rows;
        this._meta = data;

        this._renderFacets();
        this._renderTable();
        this._updateResultMeta();
        this._updateFilterBadge();
    },

    // ───────────────────────── lifecycle ─────────────────────────

    beforeLeave() {
        this._disposeHandlers();
        this._panelOpen = false;
        document.body.classList.remove('logs-panel-open');
    },

    afterRender() {
        var self = this;
        this._disposeHandlers();
        this._rows = [];
        this._meta = null;
        this._expandedId = null;

        this._renderFacets();
        this._request();

        this._registerCleanup(Bridge.on('logsLoaded', function (data) {
            self._onLogsLoaded(data);
        }));

        this._registerCleanup(Bridge.on('logsInvalidated', function () {
            self._request();
        }));

        // Facet clicks are delegated: the panel is re-rendered on every reply.
        this._delegate('log-facets', 'click', '[data-window]', function (node) {
            self._state.window = node.getAttribute('data-window');
            self._renderFacets();
            self._bindDateInputs();
            if (self._state.window !== 'custom' || self._state.from || self._state.to) {
                self._request();
            } else {
                self._updateFilterBadge();
            }
        });

        this._delegate('log-facets', 'click', '[data-facet]', function (node) {
            var key = node.getAttribute('data-facet');

            // Starts as "everything", so the first click means "only this one".
            if (self._state.facets === null) {
                self._state.facets = new Set(self._facetOrder);
            }

            if (self._state.facets.has(key)) {
                self._state.facets.delete(key);
            } else {
                self._state.facets.add(key);
            }

            // Back to every facet rather than an empty list that would match nothing.
            if (self._state.facets.size === 0 || self._state.facets.size === self._facetOrder.length) {
                self._state.facets = null;
            }

            self._request();
        });

        this._delegate('log-facets', 'click', '[data-errors]', function () {
            self._state.onlyErrors = !self._state.onlyErrors;
            self._request();
        });

        this._delegate('log-facets', 'click', '#log-reset-filters', function () {
            self._resetFilters();
        });

        this._delegate('log-table-wrap', 'click', '.logs-row', function (node) {
            var id = node.getAttribute('data-row-id');
            self._expandedId = self._expandedId === id ? null : id;
            self._renderTable();
        });

        this._delegate('log-table-wrap', 'click', '#log-load-more', function () {
            self._state.offset = self._rows.length;
            self._request({ keepOffset: true });
        });

        this._delegate('log-table-wrap', 'click', '#log-widen', function () {
            self._state.window = 'all';
            self._state.facets = null;
            self._state.onlyErrors = false;
            self._request();
        });

        this._delegate('log-table-wrap', 'click', '#log-reset-empty', function () {
            self._resetFilters();
        });

        var searchEl = this._on('log-search', 'input', function () {
            var clear = document.getElementById('log-search-clear');
            if (clear) clear.classList.toggle('hidden', !searchEl.value);

            // Debounced: the query runs over the whole history, and the old page rebuilt the
            // entire table on every keystroke.
            if (self._searchTimer) clearTimeout(self._searchTimer);
            self._searchTimer = setTimeout(function () {
                self._state.query = searchEl.value || '';
                self._request();
            }, 200);
        });

        this._on('log-search-clear', 'click', function () {
            if (!searchEl) return;
            searchEl.value = '';
            document.getElementById('log-search-clear').classList.add('hidden');
            self._state.query = '';
            self._request();
        });

        this._on('log-sort', 'change', function (e) {
            self._state.sort = e.target.value;
            self._request();
        });

        this._on('log-filter-toggle', 'click', function () {
            self._panelOpen = !self._panelOpen;
            document.body.classList.toggle('logs-panel-open', self._panelOpen);
        });

        this._on('log-export', 'click', function (e) {
            e.stopPropagation();
            var menu = document.getElementById('log-export-menu');
            if (menu) menu.classList.toggle('hidden');
        });

        this._delegate('log-export-menu', 'click', '.logs-export-item', function (node) {
            var menu = document.getElementById('log-export-menu');
            if (menu) menu.classList.add('hidden');

            var format = node.getAttribute('data-format');
            var payload = { format: format === 'visible' ? 'csv' : format };

            if (format === 'visible') {
                payload.window = self._state.window;
                payload.sort = self._state.sort;
                payload.query = self._state.query;
                payload.onlyErrors = self._state.onlyErrors;
                payload.from = self._state.from;
                payload.to = self._state.to;
                if (self._state.facets !== null) {
                    payload.facets = Array.from(self._state.facets);
                }
            }

            Bridge.send('exportLogs', payload);
        });

        var closeMenu = function () {
            var menu = document.getElementById('log-export-menu');
            if (menu) menu.classList.add('hidden');
        };
        document.addEventListener('click', closeMenu);
        this._registerCleanup(function () {
            document.removeEventListener('click', closeMenu);
        });

        this._on('log-clear', 'click', function () {
            self._confirmClear();
        });

        this._on('log-back', 'click', function () {
            if (self._isSubWindowHost()) {
                Bridge.send('closeWindow', {});
                return;
            }

            App.navigate('main');
        });
    },

    _bindDateInputs() {
        var self = this;
        ['log-date-from', 'log-date-to'].forEach(function (id) {
            var el = document.getElementById(id);
            if (!el) return;

            var handler = function () {
                self._state.from = document.getElementById('log-date-from').value || '';
                self._state.to = document.getElementById('log-date-to').value || '';
                self._request();
            };

            el.addEventListener('change', handler);
            self._registerCleanup(function () {
                el.removeEventListener('change', handler);
            });
        });
    },

    _resetFilters() {
        this._state.window = 'today';
        this._state.facets = null;
        this._state.onlyErrors = false;
        this._state.query = '';
        this._state.sort = 'newest';
        this._state.from = '';
        this._state.to = '';

        var searchEl = document.getElementById('log-search');
        if (searchEl) searchEl.value = '';
        var clear = document.getElementById('log-search-clear');
        if (clear) clear.classList.add('hidden');
        var sortEl = document.getElementById('log-sort');
        if (sortEl) sortEl.value = 'newest';

        this._request();
    },

    _confirmClear() {
        var self = this;
        var t = this._t.bind(this);
        var grandTotal = (this._meta && this._meta.grandTotal) || 0;
        var oldest = this._meta && this._meta.oldestAt;

        var since = oldest ? oldest.slice(0, 10) : '';
        try {
            if (oldest) {
                since = new Date(oldest).toLocaleDateString(undefined,
                    { day: 'numeric', month: 'long', year: 'numeric' });
            }
        } catch (_) {
        }

        App.openConfirmModal({
            title: t('LogViewerFormButtonClearLogs', 'Clear Logs'),
            message: this._escape(this._format(
                t('LogViewerFormClearConfirmBody', 'All {0} records will be deleted — the whole action history since {1}.'),
                [this._number(grandTotal), since])),
            warning: this._escape(t('LogViewerFormClearConfirmWarning',
                'This cannot be undone. Export a copy first if you want to keep it.')),
            confirmText: t('LogViewerFormButtonClearLogs', 'Clear Logs'),
            onConfirm: function () {
                Bridge.send('clearLogs', {});
                self._rows = [];
                self._meta = Object.assign({}, self._meta || {}, {
                    total: 0, grandTotal: 0, hasMore: false, errorCount: 0,
                    facetCounts: {}, windowCounts: {}, oldestAt: null
                });
                self._renderFacets();
                self._renderTable();
                self._updateResultMeta();
            }
        });
    }
};
