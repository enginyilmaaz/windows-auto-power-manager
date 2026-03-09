// Main Page - Action list (table only), form moved to modal
window.MainPage = {
    _selectedRow: -1,
    _currentFilter: 'all',
    _searchQuery: '',
    _rawActions: [],
    _actionTypeByLabel: null,
    _triggerTypeByLabel: null,
    _cleanupFns: [],
    _perfSamples: [],
    _perfCount: 0,

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

    _recordRenderPerf(durationMs) {
        if (!window.__WSHPerfEnabled) {
            return;
        }

        this._perfCount += 1;
        if (this._perfSamples.length >= 200) {
            this._perfSamples.shift();
        }

        this._perfSamples.push(durationMs);

        if (this._perfCount % 50 !== 0) {
            return;
        }

        var sorted = this._perfSamples.slice().sort(function (a, b) { return a - b; });
        if (sorted.length === 0) {
            return;
        }

        function percentile(arr, p) {
            var idx = Math.floor((arr.length - 1) * p);
            return arr[Math.max(0, Math.min(idx, arr.length - 1))];
        }

        var p95 = percentile(sorted, 0.95).toFixed(2);
        var p99 = percentile(sorted, 0.99).toFixed(2);
        var avg = (sorted.reduce(function (sum, val) { return sum + val; }, 0) / sorted.length).toFixed(2);
        if (window.console && window.console.debug) {
            window.console.debug('[PERF] MainPage.renderTable count=' + this._perfCount + ' avg=' + avg + 'ms p95=' + p95 + 'ms p99=' + p99 + 'ms');
        }
    },

    _resetLabelCaches() {
        this._actionTypeByLabel = null;
        this._triggerTypeByLabel = null;
    },

    _buildLabelCaches() {
        if (this._actionTypeByLabel && this._triggerTypeByLabel) {
            return;
        }

        var L = Bridge.lang.bind(Bridge);
        this._actionTypeByLabel = {};
        this._triggerTypeByLabel = {};

        this._actionTypeByLabel[L('MainCboxActionTypeItemLockComputer')] = 'LockComputer';
        this._actionTypeByLabel[L('MainCboxActionTypeItemSleepComputer')] = 'SleepComputer';
        this._actionTypeByLabel[L('MainCboxActionTypeItemTurnOffMonitor')] = 'TurnOffMonitor';
        this._actionTypeByLabel[L('MainCboxActionTypeItemShutdownComputer')] = 'ShutdownComputer';
        this._actionTypeByLabel[L('MainCboxActionTypeItemRestartComputer')] = 'RestartComputer';
        this._actionTypeByLabel[L('MainCboxActionTypeItemLogOffWindows')] = 'LogOffWindows';

        this._triggerTypeByLabel[L('MainCboxTriggerTypeItemSystemIdle')] = 'SystemIdle';
        this._triggerTypeByLabel[L('MainCboxTriggerTypeItemFromNow')] = 'FromNow';
        this._triggerTypeByLabel[L('MainCboxTriggerTypeItemCertainTime')] = 'CertainTime';
    },

    _getSortLocale() {
        var settingsLang = Bridge && Bridge._settings ? Bridge._settings.language : '';
        if (settingsLang && settingsLang !== 'auto') {
            return settingsLang;
        }

        if (navigator.languages && navigator.languages.length > 0) {
            return navigator.languages[0];
        }

        return navigator.language || 'en';
    },

    _sortLocalizedOptions(options) {
        var locale = this._getSortLocale();
        return (options || []).slice().sort(function (a, b) {
            return String(a.label || '').localeCompare(String(b.label || ''), locale, {
                sensitivity: 'base'
            });
        });
    },

    _buildActionOptions() {
        var L = Bridge.lang.bind(Bridge);
        return this._sortLocalizedOptions([
            { value: 'ShutdownComputer', label: L('MainCboxActionTypeItemShutdownComputer') },
            { value: 'RestartComputer', label: L('MainCboxActionTypeItemRestartComputer') },
            { value: 'LogOffWindows', label: L('MainCboxActionTypeItemLogOffWindows') },
            { value: 'SleepComputer', label: L('MainCboxActionTypeItemSleepComputer') },
            { value: 'LockComputer', label: L('MainCboxActionTypeItemLockComputer') },
            { value: 'TurnOffMonitor', label: L('MainCboxActionTypeItemTurnOffMonitor') }
        ]);
    },

    _buildTriggerOptions() {
        var L = Bridge.lang.bind(Bridge);
        return this._sortLocalizedOptions([
            { value: 'SystemIdle', label: L('MainCboxTriggerTypeItemSystemIdle') },
            { value: 'FromNow', label: L('MainCboxTriggerTypeItemFromNow') },
            { value: 'CertainTime', label: L('MainCboxTriggerTypeItemCertainTime') }
        ]);
    },

    _renderOptionList(options) {
        var html = '';
        for (var i = 0; i < options.length; i++) {
            html += '<option value="' + options[i].value + '">' + options[i].label + '</option>';
        }
        return html;
    },

    _normalizeTriggerRaw(value) {
        if (!value) return '';
        var v = String(value).trim();
        if (v === 'FromNow') return 'FromNow';
        if (v === 'SystemIdle') return 'SystemIdle';
        if (v === 'CertainTime') return 'CertainTime';
        return v;
    },

    _normalizeActionRaw(value) {
        if (!value) return '';
        return String(value).trim();
    },

    _parseActionDate(value) {
        var m = /^(\d{2})\.(\d{2})\.(\d{4}) (\d{2}):(\d{2}):(\d{2})$/.exec(value || '');
        if (!m) return null;

        var dt = new Date(
            parseInt(m[3], 10),
            parseInt(m[2], 10) - 1,
            parseInt(m[1], 10),
            parseInt(m[4], 10),
            parseInt(m[5], 10),
            parseInt(m[6], 10)
        );

        return isNaN(dt.getTime()) ? null : dt;
    },

    _resolveActionRaw(action) {
        this._buildLabelCaches();
        var raw = this._normalizeActionRaw(action && (action.actionTypeRaw || action.actionType));
        return raw || this._actionTypeByLabel[action && action.actionType] || '';
    },

    _resolveTriggerRaw(action) {
        this._buildLabelCaches();
        var raw = this._normalizeTriggerRaw(action && (action.triggerTypeRaw || action.triggerType));
        return raw || this._triggerTypeByLabel[action && action.triggerType] || '';
    },

    _toEditableAction(index, action) {
        var triggerRaw = this._resolveTriggerRaw(action);
        var actionRaw = this._resolveActionRaw(action);
        var value = '1';
        var timeUnit = '1';
        var time = '';

        if (triggerRaw === 'SystemIdle') {
            var seconds = parseInt(action && action.value, 10);
            if (!isNaN(seconds) && seconds > 0) {
                value = String(seconds);
                timeUnit = '0';
            }
        } else if (triggerRaw === 'CertainTime') {
            var rawTime = String(action && action.value || '');
            var timeMatch = /^(\d{2}:\d{2})/.exec(rawTime);
            time = timeMatch ? timeMatch[1] : '';
        } else if (triggerRaw === 'FromNow') {
            var targetDate = this._parseActionDate(action && action.value);
            if (targetDate) {
                var remainingSeconds = Math.max(1, Math.round((targetDate.getTime() - Date.now()) / 1000));
                if (remainingSeconds % 3600 === 0) {
                    value = String(Math.max(1, remainingSeconds / 3600));
                    timeUnit = '2';
                } else if (remainingSeconds % 60 === 0) {
                    value = String(Math.max(1, remainingSeconds / 60));
                    timeUnit = '1';
                } else {
                    value = String(remainingSeconds);
                    timeUnit = '0';
                }
            }
        }

        return {
            index: index,
            actionType: actionRaw || '0',
            triggerType: triggerRaw || '0',
            value: value,
            timeUnit: timeUnit,
            time: time
        };
    },

    requestDeleteAction(index) {
        if (index < 0) return;
        var L = Bridge.lang.bind(Bridge);
        var confirmMessage = L('MessageContentConfirmDeleteAction')
            || ((L('ContextMenuStripMainGridDeleteSelectedAction') || 'Delete selected action') + '?');

        if (window.App && typeof window.App.openConfirmModal === 'function') {
            window.App.openConfirmModal({
                title: L('MessageTitleWarn') || 'Confirm',
                message: confirmMessage,
                confirmText: L('ContextMenuStripMainGridDeleteSelectedAction') || 'Delete',
                cancelText: L('SettingsFormButtonCancel') || 'Cancel',
                onConfirm: function () {
                    Bridge.send('deleteAction', { index: index });
                }
            });
            return;
        }

        if (!window.confirm(confirmMessage)) {
            return;
        }

        Bridge.send('deleteAction', { index: index });
    },

    // Render the form HTML (used inside modal)
    renderForm(options) {
        options = options || {};
        var L = Bridge.lang.bind(Bridge);
        var isEdit = options.mode === 'edit';
        var actionOptions = this._renderOptionList(this._buildActionOptions());
        var triggerOptions = this._renderOptionList(this._buildTriggerOptions());
        return '' +
            '<div class="form-row">' +
                '<span class="form-label">' + L('MainLabelActionType') + '</span>' +
                '<select id="sel-action" class="form-select">' +
                    '<option value="0">' + L('MainCboxActionTypeItemChooseAction') + '</option>' +
                    actionOptions +
                '</select>' +
            '</div>' +
            '<div class="form-row">' +
                '<span class="form-label">' + L('MainLabelTrigger') + '</span>' +
                '<select id="sel-trigger" class="form-select">' +
                    '<option value="0">' + L('MainCboxTriggerTypeItemChooseTrigger') + '</option>' +
                    triggerOptions +
                '</select>' +
            '</div>' +
            '<div class="form-row" id="row-value">' +
                '<span class="form-label" id="lbl-value">' + L('MainLabelValue') + '</span>' +
                '<span class="form-hint" id="hint-trigger">' + L('LabelFirstlyChooseATrigger') + '</span>' +
                '<input type="number" id="inp-value" class="form-input form-input-small" min="1" max="99999" value="1" style="display:none">' +
                '<select id="sel-unit" class="form-select form-input-small" style="display:none">' +
                    '<option value="0">' + (L('MainTimeUnitSeconds') || 'Seconds') + '</option>' +
                    '<option value="1" selected>' + (L('MainTimeUnitMinutes') || 'Minutes') + '</option>' +
                    '<option value="2">' + (L('MainTimeUnitHours') || 'Hours') + '</option>' +
                '</select>' +
                '<input type="time" id="inp-time" class="form-input form-input-small" style="display:none">' +
            '</div>' +
            '<button class="btn btn-primary" id="btn-submit">' +
                (isEdit ? (L('MainButtonEditAction') || 'Update Action') : L('MainButtonAddAction')) +
            '</button>';
    },

    // Bind form events (called after modal body is populated)
    afterRenderForm(container, options) {
        options = options || {};
        var isEdit = options.mode === 'edit';
        var initialData = options.initialData || null;

        function updateTriggerInputs() {
            var v = container.querySelector('#sel-trigger').value;
            var hint = container.querySelector('#hint-trigger');
            var inp = container.querySelector('#inp-value');
            var unit = container.querySelector('#sel-unit');
            var time = container.querySelector('#inp-time');
            var lbl = container.querySelector('#lbl-value');
            var L = Bridge.lang.bind(Bridge);

            if (v === '0') {
                hint.style.display = '';
                inp.style.display = 'none';
                unit.style.display = 'none';
                time.style.display = 'none';
            } else if (v === 'SystemIdle' || v === 'FromNow') {
                hint.style.display = 'none';
                inp.style.display = '';
                unit.style.display = '';
                time.style.display = 'none';
                lbl.textContent = L('MainLabelValueDuration') || L('MainLabelValue');
            } else if (v === 'CertainTime') {
                hint.style.display = 'none';
                inp.style.display = 'none';
                unit.style.display = 'none';
                time.style.display = '';
                lbl.textContent = L('MainLabelValueTime') || L('MainLabelValue');
            }
        }

        // Trigger change -> toggle value inputs
        container.querySelector('#sel-trigger').addEventListener('change', function () {
            updateTriggerInputs();
        });

        if (initialData) {
            var selAction = container.querySelector('#sel-action');
            var selTrigger = container.querySelector('#sel-trigger');
            var inpValue = container.querySelector('#inp-value');
            var selUnit = container.querySelector('#sel-unit');
            var inpTime = container.querySelector('#inp-time');

            if (selAction) selAction.value = initialData.actionType || '0';
            if (selTrigger) selTrigger.value = initialData.triggerType || '0';
            updateTriggerInputs();

            if (inpValue) inpValue.value = initialData.value || '1';
            if (selUnit) selUnit.value = initialData.timeUnit || '1';
            if (inpTime && initialData.time) inpTime.value = initialData.time;
        } else {
            updateTriggerInputs();
        }

        // Submit action (add / edit)
        container.querySelector('#btn-submit').addEventListener('click', function () {
            var action = container.querySelector('#sel-action').value;
            var trigger = container.querySelector('#sel-trigger').value;
            var value = container.querySelector('#inp-value').value;
            var unit = container.querySelector('#sel-unit').value;
            var time = container.querySelector('#inp-time').value;

            var payload = {
                actionType: action,
                triggerType: trigger,
                value: value,
                timeUnit: unit,
                time: time
            };

            if (isEdit) {
                payload.index = options.index;
                Bridge.send('updateAction', payload);
                return;
            }

            Bridge.send('addAction', payload);
        });
    },

    // Page render - only table + context menu
    render() {
        var L = Bridge.lang.bind(Bridge);
        return '' +
        '<div class="card">' +
            '<div class="card-title">' + L('MainGroupBoxActionList') + '</div>' +
            '<div id="action-table-wrap"></div>' +
        '</div>' +
        '<div class="context-menu" id="ctx-menu">' +
            '<div class="context-menu-item" id="ctx-delete">' + L('ContextMenuStripMainGridDeleteSelectedAction') + '</div>' +
            '<div class="context-menu-item danger" id="ctx-clear">' + L('ContextMenuStripMainGridDeleteAllAction') + '</div>' +
            '<div class="context-menu-item" id="ctx-help">' + L('ContextMenuStripMainGridOpenHelp') + '</div>' +
        '</div>';
    },

    beforeLeave() {
        this._disposeHandlers();
    },

    _hideContextMenu() {
        var ctx = document.getElementById('ctx-menu');
        if (ctx) {
            ctx.classList.remove('show');
        }
    },

    _showContextMenu(x, y) {
        var ctx = document.getElementById('ctx-menu');
        if (!ctx) {
            return;
        }

        ctx.style.left = x + 'px';
        ctx.style.top = y + 'px';
        ctx.classList.add('show');
    },

    _setSelectedRow(wrap, row, index) {
        var prev = wrap.querySelector('tr.selected');
        if (prev && prev !== row) {
            prev.classList.remove('selected');
        }

        row.classList.add('selected');
        this._selectedRow = index;
    },

    _handleTableClick(e) {
        var wrap = document.getElementById('action-table-wrap');
        if (!wrap) return;

        var row = e.target.closest('tr[data-idx]');
        if (!row || !wrap.contains(row)) return;

        var idx = parseInt(row.getAttribute('data-idx'), 10);
        if (isNaN(idx) || idx < 0 || idx >= this._rawActions.length) return;

        this._setSelectedRow(wrap, row, idx);

        var actionBtn = e.target.closest('.row-action-btn[data-row-action]');
        if (!actionBtn) return;

        e.preventDefault();
        e.stopPropagation();

        var rowAction = actionBtn.getAttribute('data-row-action');
        if (rowAction === 'delete') {
            this.requestDeleteAction(idx);
            return;
        }

        if (rowAction === 'edit') {
            if (!window.App || typeof window.App.openEditActionModal !== 'function') return;
            var editable = this._toEditableAction(idx, this._rawActions[idx]);
            window.App.openEditActionModal(editable);
            return;
        }

        if (rowAction === 'toggle') {
            Bridge.send('toggleAction', { index: idx });
        }
    },

    _handleTableContextMenu(e) {
        var wrap = document.getElementById('action-table-wrap');
        if (!wrap) return;

        var row = e.target.closest('tr[data-idx]');
        if (!row || !wrap.contains(row)) return;

        var idx = parseInt(row.getAttribute('data-idx'), 10);
        if (isNaN(idx) || idx < 0 || idx >= this._rawActions.length) return;

        e.preventDefault();
        this._setSelectedRow(wrap, row, idx);
        this._showContextMenu(e.clientX, e.clientY);
    },

    afterRender() {
        var self = this;
        self._disposeHandlers();
        self._selectedRow = -1;
        self._resetLabelCaches();

        var wrap = document.getElementById('action-table-wrap');
        if (!wrap) return;

        var onDocumentClick = function (e) {
            var ctx = document.getElementById('ctx-menu');
            if (!ctx || !ctx.classList.contains('show')) return;
            if (!ctx.contains(e.target)) {
                self._hideContextMenu();
            }
        };
        document.addEventListener('click', onDocumentClick);
        self._registerCleanup(function () {
            document.removeEventListener('click', onDocumentClick);
        });

        var onWrapClick = function (e) { self._handleTableClick(e); };
        wrap.addEventListener('click', onWrapClick);
        self._registerCleanup(function () {
            wrap.removeEventListener('click', onWrapClick);
        });

        var onWrapContextMenu = function (e) { self._handleTableContextMenu(e); };
        wrap.addEventListener('contextmenu', onWrapContextMenu);
        self._registerCleanup(function () {
            wrap.removeEventListener('contextmenu', onWrapContextMenu);
        });

        var ctxDelete = document.getElementById('ctx-delete');
        var ctxClear = document.getElementById('ctx-clear');
        var ctxHelp = document.getElementById('ctx-help');

        if (ctxDelete) {
            var onCtxDelete = function () {
                if (self._selectedRow >= 0) {
                    self.requestDeleteAction(self._selectedRow);
                }
            };
            ctxDelete.addEventListener('click', onCtxDelete);
            self._registerCleanup(function () {
                ctxDelete.removeEventListener('click', onCtxDelete);
            });
        }

        if (ctxClear) {
            var onCtxClear = function () {
                Bridge.send('clearAllActions', {});
            };
            ctxClear.addEventListener('click', onCtxClear);
            self._registerCleanup(function () {
                ctxClear.removeEventListener('click', onCtxClear);
            });
        }

        if (ctxHelp) {
            var onCtxHelp = function () {
                self._hideContextMenu();
                Bridge.send('openWindow', { page: 'help' });
            };
            ctxHelp.addEventListener('click', onCtxHelp);
            self._registerCleanup(function () {
                ctxHelp.removeEventListener('click', onCtxHelp);
            });
        }

        this.renderTable(Bridge._actions);

        var offRefresh = Bridge.on('refreshActions', function (actions) {
            self.renderTable(actions);
        });
        self._registerCleanup(offRefresh);
    },

    renderTable(actions) {
        var perfStart = (window.performance && typeof window.performance.now === 'function')
            ? window.performance.now()
            : 0;
        var self = this;
        var wrap = document.getElementById('action-table-wrap');
        if (!wrap) return;

        var L = Bridge.lang.bind(Bridge);
        self._hideContextMenu();

        self._rawActions = actions || [];
        var entries = new Array(self._rawActions.length);
        for (var i = 0; i < self._rawActions.length; i++) {
            entries[i] = { idx: i, action: self._rawActions[i] };
        }

        var filtered = entries;
        if (self._currentFilter && self._currentFilter !== 'all') {
            var filterMap = {
                'ShutdownComputer': L('MainCboxActionTypeItemShutdownComputer'),
                'RestartComputer': L('MainCboxActionTypeItemRestartComputer'),
                'SleepComputer': L('MainCboxActionTypeItemSleepComputer'),
                'LockComputer': L('MainCboxActionTypeItemLockComputer'),
                'TurnOffMonitor': L('MainCboxActionTypeItemTurnOffMonitor'),
                'LogOffWindows': L('MainCboxActionTypeItemLogOffWindows')
            };
            var filterText = filterMap[self._currentFilter] || self._currentFilter;
            filtered = filtered.filter(function (entry) {
                var action = entry.action || {};
                var rawType = self._normalizeActionRaw(action.actionTypeRaw || action.actionType);
                return action.actionType === filterText ||
                    action.actionType === self._currentFilter ||
                    rawType === self._currentFilter;
            });
        }

        if (self._searchQuery) {
            var q = self._searchQuery;
            filtered = filtered.filter(function (entry) {
                var action = entry.action || {};
                return (action.triggerType || '').toLowerCase().indexOf(q) >= 0 ||
                       (action.actionType || '').toLowerCase().indexOf(q) >= 0 ||
                       (action.value || '').toLowerCase().indexOf(q) >= 0 ||
                       (action.valueUnit || '').toLowerCase().indexOf(q) >= 0 ||
                       (action.createdDate || '').toLowerCase().indexOf(q) >= 0;
            });
        }

        if (!filtered || filtered.length === 0) {
            wrap.innerHTML = '<div class="table-empty">' + (L('MessageContentNoLog') || 'No actions yet') + '</div>';
            if (perfStart > 0) {
                self._recordRenderPerf(window.performance.now() - perfStart);
            }
            return;
        }

        var html = '<table class="data-table"><thead><tr>' +
            '<th>' + L('MainDatagridMainTriggerType') + '</th>' +
            '<th>' + L('MainDatagridMainActionType') + '</th>' +
            '<th>' + L('MainDatagridMainValue') + '</th>' +
            '<th>' + (L('MainDatagridMainValueUnit') || 'Unit') + '</th>' +
            '<th>' + L('MainDatagridMainCreatedDate') + '</th>' +
            '<th class="row-actions-col"></th>' +
            '</tr></thead><tbody>';

        var canEdit = !!(window.App && typeof window.App.openEditActionModal === 'function');
        var editTitle = L('ContextMenuStripMainGridEditSelectedAction') || 'Edit action';
        var deleteTitle = L('ContextMenuStripMainGridDeleteSelectedAction') || 'Delete selected action';
        var enableTitle = L('ActionStart') || 'Start';
        var disableTitle = L('ActionStop') || 'Stop';

        for (var rowIndex = 0; rowIndex < filtered.length; rowIndex++) {
            var entry = filtered[rowIndex];
            var a = entry.action || {};
            var isEnabled = a.isEnabled !== false;
            var rowClasses = [];
            if (entry.idx === self._selectedRow) rowClasses.push('selected');
            if (!isEnabled) rowClasses.push('row-disabled');
            var rowClassAttr = rowClasses.length ? ' class="' + rowClasses.join(' ') + '"' : '';
            var displayValue = a.value || '';
            var displayUnit = a.valueUnit || '';
            var toggleIcon = isEnabled ? 'pause' : 'play_arrow';
            var toggleTitle = isEnabled ? disableTitle : enableTitle;
            var toggleClass = isEnabled ? 'row-action-stop' : 'row-action-start';
            html += '<tr data-idx="' + entry.idx + '"' + rowClassAttr + '>' +
                '<td>' + (a.triggerType || '') + '</td>' +
                '<td>' + (a.actionType || '') + '</td>' +
                '<td>' + displayValue + '</td>' +
                '<td>' + displayUnit + '</td>' +
                '<td>' + (a.createdDate || '') + '</td>' +
                '<td class="row-actions-cell">' +
                    '<button class="row-action-btn ' + toggleClass + '" data-row-action="toggle" title="' + toggleTitle + '"><span class="mi">' + toggleIcon + '</span></button>' +
                    (canEdit
                        ? '<button class="row-action-btn row-action-edit" data-row-action="edit" title="' + editTitle + '"><span class="mi">edit</span></button>'
                        : '') +
                    '<button class="row-action-btn row-action-delete" data-row-action="delete" title="' + deleteTitle + '">' +
                        '<span class="mi">delete</span>' +
                    '</button>' +
                '</td>' +
                '</tr>';
        }

        html += '</tbody></table>';
        wrap.innerHTML = html;

        if (perfStart > 0) {
            self._recordRenderPerf(window.performance.now() - perfStart);
        }
    }
};
