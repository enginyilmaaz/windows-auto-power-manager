// Main Page - Action list (table only), form moved to modal
window.MainPage = {
    _selectedRow: -1,
    _currentFilter: 'all',
    _searchQuery: '',
    _rawActions: [],
    _actionTypeByLabel: null,
    _triggerTypeByLabel: null,

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
        return '' +
            '<div class="form-row">' +
                '<span class="form-label">' + L('MainLabelActionType') + '</span>' +
                '<select id="sel-action" class="form-select">' +
                    '<option value="0">' + L('MainCboxActionTypeItemChooseAction') + '</option>' +
                    '<option value="ShutdownComputer">' + L('MainCboxActionTypeItemShutdownComputer') + '</option>' +
                    '<option value="RestartComputer">' + L('MainCboxActionTypeItemRestartComputer') + '</option>' +
                    '<option value="LogOffWindows">' + L('MainCboxActionTypeItemLogOffWindows') + '</option>' +
                    '<option value="SleepComputer">' + L('MainCboxActionTypeItemSleepComputer') + '</option>' +
                    '<option value="LockComputer">' + L('MainCboxActionTypeItemLockComputer') + '</option>' +
                    '<option value="TurnOffMonitor">' + L('MainCboxActionTypeItemTurnOffMonitor') + '</option>' +
                '</select>' +
            '</div>' +
            '<div class="form-row">' +
                '<span class="form-label">' + L('MainLabelTrigger') + '</span>' +
                '<select id="sel-trigger" class="form-select">' +
                    '<option value="0">' + L('MainCboxTriggerTypeItemChooseTrigger') + '</option>' +
                    '<option value="SystemIdle">' + L('MainCboxTriggerTypeItemSystemIdle') + '</option>' +
                    '<option value="FromNow">' + L('MainCboxTriggerTypeItemFromNow') + '</option>' +
                    '<option value="CertainTime">' + L('MainCboxTriggerTypeItemCertainTime') + '</option>' +
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
        '</div>';
    },

    afterRender() {
        var self = this;
        self._selectedRow = -1;
        self._resetLabelCaches();

        // Context menu
        document.addEventListener('click', function () {
            var ctx = document.getElementById('ctx-menu');
            if (ctx) ctx.classList.remove('show');
        });

        var ctxDelete = document.getElementById('ctx-delete');
        if (ctxDelete) {
            ctxDelete.addEventListener('click', function () {
                if (self._selectedRow >= 0) {
                    self.requestDeleteAction(self._selectedRow);
                }
            });
        }

        var ctxClear = document.getElementById('ctx-clear');
        if (ctxClear) {
            ctxClear.addEventListener('click', function () {
                Bridge.send('clearAllActions', {});
            });
        }

        // Render table
        this.renderTable(Bridge._actions);

        // Listen for refresh
        Bridge.on('refreshActions', function (actions) {
            self.renderTable(actions);
        });
    },

    renderTable(actions) {
        var self = this;
        var wrap = document.getElementById('action-table-wrap');
        if (!wrap) return;

        var L = Bridge.lang.bind(Bridge);

        // Store raw actions for filtering
        self._rawActions = actions || [];

        // Apply filter
        var filtered = self._rawActions;
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
            filtered = filtered.filter(function (a) {
                var rawType = self._normalizeActionRaw(a.actionTypeRaw || a.actionType);
                return a.actionType === filterText ||
                    a.actionType === self._currentFilter ||
                    rawType === self._currentFilter;
            });
        }

        // Apply search
        if (self._searchQuery) {
            var q = self._searchQuery;
            filtered = filtered.filter(function (a) {
                return (a.triggerType || '').toLowerCase().indexOf(q) >= 0 ||
                       (a.actionType || '').toLowerCase().indexOf(q) >= 0 ||
                       (a.value || '').toLowerCase().indexOf(q) >= 0 ||
                       (a.valueUnit || '').toLowerCase().indexOf(q) >= 0 ||
                       (a.createdDate || '').toLowerCase().indexOf(q) >= 0;
            });
        }

        if (!filtered || filtered.length === 0) {
            wrap.innerHTML = '<div class="table-empty">' + (L('MessageContentNoLog') || 'No actions yet') + '</div>';
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

        for (var i = 0; i < filtered.length; i++) {
            var a = filtered[i];
            // Find original index for delete
            var origIdx = self._rawActions.indexOf(a);
            html += '<tr data-idx="' + origIdx + '">' +
                '<td>' + (a.triggerType || '') + '</td>' +
                '<td>' + (a.actionType || '') + '</td>' +
                '<td>' + (a.value || '') + '</td>' +
                '<td>' + (a.valueUnit || '') + '</td>' +
                '<td>' + (a.createdDate || '') + '</td>' +
                '<td class="row-actions-cell">' +
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

        // Row selection + context menu
        wrap.querySelectorAll('tr[data-idx]').forEach(function (row) {
            row.addEventListener('click', function () {
                wrap.querySelectorAll('tr.selected').forEach(function (r) { r.classList.remove('selected'); });
                row.classList.add('selected');
                self._selectedRow = parseInt(row.getAttribute('data-idx'));
            });

            row.addEventListener('contextmenu', function (e) {
                e.preventDefault();
                wrap.querySelectorAll('tr.selected').forEach(function (r) { r.classList.remove('selected'); });
                row.classList.add('selected');
                self._selectedRow = parseInt(row.getAttribute('data-idx'));
                var ctx = document.getElementById('ctx-menu');
                if (ctx) {
                    ctx.style.left = e.clientX + 'px';
                    ctx.style.top = e.clientY + 'px';
                    ctx.classList.add('show');
                }
            });
        });

        wrap.querySelectorAll('.row-action-btn[data-row-action="edit"]').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();

                var row = btn.closest('tr[data-idx]');
                if (!row) return;

                var idx = parseInt(row.getAttribute('data-idx'), 10);
                if (isNaN(idx) || idx < 0 || idx >= self._rawActions.length) return;

                wrap.querySelectorAll('tr.selected').forEach(function (r) { r.classList.remove('selected'); });
                row.classList.add('selected');
                self._selectedRow = idx;

                if (!window.App || typeof window.App.openEditActionModal !== 'function') return;
                var editable = self._toEditableAction(idx, self._rawActions[idx]);
                window.App.openEditActionModal(editable);
            });
        });

        wrap.querySelectorAll('.row-action-btn[data-row-action="delete"]').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();

                var row = btn.closest('tr[data-idx]');
                if (!row) return;

                var idx = parseInt(row.getAttribute('data-idx'), 10);
                if (isNaN(idx) || idx < 0) return;

                wrap.querySelectorAll('tr.selected').forEach(function (r) { r.classList.remove('selected'); });
                row.classList.add('selected');
                self._selectedRow = idx;
                self.requestDeleteAction(idx);
            });
        });
    }
};
