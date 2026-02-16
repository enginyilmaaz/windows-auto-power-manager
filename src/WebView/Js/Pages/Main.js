// Main Page - Action list (table only), form moved to modal
window.MainPage = {
    _selectedRow: -1,
    _currentFilter: 'all',
    _searchQuery: '',
    _rawActions: [],

    // Render the form HTML (used inside modal)
    renderForm() {
        var L = Bridge.lang.bind(Bridge);
        return '' +
            '<div class="form-row">' +
                '<span class="form-label">' + L('MainLabelActionType') + '</span>' +
                '<select id="sel-action" class="form-select">' +
                    '<option value="0">' + L('MainCboxActionTypeItemChooseAction') + '</option>' +
                    '<option value="shutdownComputer">' + L('MainCboxActionTypeItemShutdownComputer') + '</option>' +
                    '<option value="restartComputer">' + L('MainCboxActionTypeItemRestartComputer') + '</option>' +
                    '<option value="logOffWindows">' + L('MainCboxActionTypeItemLogOffWindows') + '</option>' +
                    '<option value="sleepComputer">' + L('MainCboxActionTypeItemSleepComputer') + '</option>' +
                    '<option value="lockComputer">' + L('MainCboxActionTypeItemLockComputer') + '</option>' +
                    '<option value="turnOffMonitor">' + L('MainCboxActionTypeItemTurnOffMonitor') + '</option>' +
                '</select>' +
            '</div>' +
            '<div class="form-row">' +
                '<span class="form-label">' + L('MainLabelTrigger') + '</span>' +
                '<select id="sel-trigger" class="form-select">' +
                    '<option value="0">' + L('MainCboxTriggerTypeItemChooseTrigger') + '</option>' +
                    '<option value="systemIdle">' + L('MainCboxTriggerTypeItemSystemIdle') + '</option>' +
                    '<option value="fromNow">' + L('MainCboxTriggerTypeItemFromNow') + '</option>' +
                    '<option value="certainTime">' + L('MainCboxTriggerTypeItemCertainTime') + '</option>' +
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
            '<button class="btn btn-primary" id="btn-add">' + L('MainButtonAddAction') + '</button>';
    },

    // Bind form events (called after modal body is populated)
    afterRenderForm(container) {
        // Trigger change -> toggle value inputs
        container.querySelector('#sel-trigger').addEventListener('change', function () {
            var v = this.value;
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
            } else if (v === 'systemIdle' || v === 'fromNow') {
                hint.style.display = 'none';
                inp.style.display = '';
                unit.style.display = '';
                time.style.display = 'none';
                lbl.textContent = L('MainLabelValueDuration') || L('MainLabelValue');
            } else if (v === 'certainTime') {
                hint.style.display = 'none';
                inp.style.display = 'none';
                unit.style.display = 'none';
                time.style.display = '';
                lbl.textContent = L('MainLabelValueTime') || L('MainLabelValue');
            }
        });

        // Add action
        container.querySelector('#btn-add').addEventListener('click', function () {
            var action = container.querySelector('#sel-action').value;
            var trigger = container.querySelector('#sel-trigger').value;
            var value = container.querySelector('#inp-value').value;
            var unit = container.querySelector('#sel-unit').value;
            var time = container.querySelector('#inp-time').value;

            Bridge.send('addAction', {
                actionType: action,
                triggerType: trigger,
                value: value,
                timeUnit: unit,
                time: time
            });
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

        // Context menu
        document.addEventListener('click', function () {
            var ctx = document.getElementById('ctx-menu');
            if (ctx) ctx.classList.remove('show');
        });

        var ctxDelete = document.getElementById('ctx-delete');
        if (ctxDelete) {
            ctxDelete.addEventListener('click', function () {
                if (self._selectedRow >= 0) {
                    Bridge.send('deleteAction', { index: self._selectedRow });
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
                'shutdownComputer': L('MainCboxActionTypeItemShutdownComputer'),
                'restartComputer': L('MainCboxActionTypeItemRestartComputer'),
                'sleepComputer': L('MainCboxActionTypeItemSleepComputer'),
                'lockComputer': L('MainCboxActionTypeItemLockComputer'),
                'turnOffMonitor': L('MainCboxActionTypeItemTurnOffMonitor'),
                'logOffWindows': L('MainCboxActionTypeItemLogOffWindows')
            };
            var filterText = filterMap[self._currentFilter] || self._currentFilter;
            filtered = filtered.filter(function (a) {
                return a.actionType === filterText || a.actionType === self._currentFilter;
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
            '</tr></thead><tbody>';

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
    }
};
