// Main Page - Action creation and list
const MainPage = {
    _selectedRow: -1,

    render() {
        const L = Bridge.lang.bind(Bridge);
        return '' +
        '<div class="section-box">' +
            '<div class="section-title">' + L('main_groupbox_newAction') + '</div>' +
            '<div class="form-row">' +
                '<span class="form-label">' + L('main_label_actionType') + '</span>' +
                '<select id="sel-action" class="form-select">' +
                    '<option value="0">' + L('main_cbox_ActionType_Item_chooseAction') + '</option>' +
                    '<option value="shutdownComputer">' + L('main_cbox_ActionType_Item_shutdownComputer') + '</option>' +
                    '<option value="restartComputer">' + L('main_cbox_ActionType_Item_restartComputer') + '</option>' +
                    '<option value="logOffWindows">' + L('main_cbox_ActionType_Item_logOffWindows') + '</option>' +
                    '<option value="sleepComputer">' + L('main_cbox_ActionType_Item_sleepComputer') + '</option>' +
                    '<option value="lockComputer">' + L('main_cbox_ActionType_Item_lockComputer') + '</option>' +
                    '<option value="turnOffMonitor">' + L('main_cbox_ActionType_Item_turnOffMonitor') + '</option>' +
                '</select>' +
            '</div>' +
            '<div class="form-row">' +
                '<span class="form-label">' + L('main_label_trigger') + '</span>' +
                '<select id="sel-trigger" class="form-select">' +
                    '<option value="0">' + L('main_cbox_TriggerType_Item_chooseTrigger') + '</option>' +
                    '<option value="systemIdle">' + L('main_cbox_TriggerType_Item_systemIdle') + '</option>' +
                    '<option value="fromNow">' + L('main_cbox_TriggerType_Item_fromNow') + '</option>' +
                    '<option value="certainTime">' + L('main_cbox_TriggerType_Item_certainTime') + '</option>' +
                '</select>' +
            '</div>' +
            '<div class="form-row" id="row-value">' +
                '<span class="form-label" id="lbl-value">' + L('main_label_value') + '</span>' +
                '<span class="form-hint" id="hint-trigger">' + L('label_firstly_choose_a_trigger') + '</span>' +
                '<input type="number" id="inp-value" class="form-input form-input-small" min="1" max="99999" value="1" style="display:none">' +
                '<select id="sel-unit" class="form-select form-input-small" style="display:none">' +
                    '<option value="0">' + (L('main_timeUnit_seconds') || 'Seconds') + '</option>' +
                    '<option value="1" selected>' + (L('main_timeUnit_minutes') || 'Minutes') + '</option>' +
                    '<option value="2">' + (L('main_timeUnit_hours') || 'Hours') + '</option>' +
                '</select>' +
                '<input type="time" id="inp-time" class="form-input form-input-small" style="display:none">' +
            '</div>' +
            '<button class="btn btn-primary" id="btn-add">' + L('main_button_addAction') + '</button>' +
        '</div>' +
        '<div class="section-box">' +
            '<div class="section-title">' + L('main_groupBox_actionList') + '</div>' +
            '<div id="action-table-wrap"></div>' +
        '</div>' +
        '<div class="context-menu" id="ctx-menu">' +
            '<div class="context-menu-item" id="ctx-delete">' + L('contextMenuStrip_mainGrid_deleteSelectedAction') + '</div>' +
            '<div class="context-menu-item danger" id="ctx-clear">' + L('contextMenuStrip_mainGrid_deleteAllAction') + '</div>' +
        '</div>';
    },

    afterRender() {
        var self = this;
        self._selectedRow = -1;

        // Trigger change -> toggle value inputs
        document.getElementById('sel-trigger').addEventListener('change', function () {
            var v = this.value;
            var hint = document.getElementById('hint-trigger');
            var inp = document.getElementById('inp-value');
            var unit = document.getElementById('sel-unit');
            var time = document.getElementById('inp-time');
            var lbl = document.getElementById('lbl-value');
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
                lbl.textContent = L('main_label_value_duration') || L('main_label_value');
            } else if (v === 'certainTime') {
                hint.style.display = 'none';
                inp.style.display = 'none';
                unit.style.display = 'none';
                time.style.display = '';
                lbl.textContent = L('main_label_value_time') || L('main_label_value');
            }
        });

        // Add action
        document.getElementById('btn-add').addEventListener('click', function () {
            var action = document.getElementById('sel-action').value;
            var trigger = document.getElementById('sel-trigger').value;
            var value = document.getElementById('inp-value').value;
            var unit = document.getElementById('sel-unit').value;
            var time = document.getElementById('inp-time').value;

            Bridge.send('addAction', {
                actionType: action,
                triggerType: trigger,
                value: value,
                timeUnit: unit,
                time: time
            });
        });

        // Context menu
        document.addEventListener('click', function () {
            var ctx = document.getElementById('ctx-menu');
            if (ctx) ctx.classList.remove('show');
        });

        document.getElementById('ctx-delete').addEventListener('click', function () {
            if (self._selectedRow >= 0) {
                Bridge.send('deleteAction', { index: self._selectedRow });
            }
        });

        document.getElementById('ctx-clear').addEventListener('click', function () {
            Bridge.send('clearAllActions', {});
        });

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

        if (!actions || actions.length === 0) {
            wrap.innerHTML = '<div class="table-empty">' + (L('messageContent_noLog') || 'No actions yet') + '</div>';
            return;
        }

        var html = '<table class="data-table"><thead><tr>' +
            '<th>' + L('main_datagrid_main_triggerType') + '</th>' +
            '<th>' + L('main_datagrid_main_actionType') + '</th>' +
            '<th>' + L('main_datagrid_main_value') + '</th>' +
            '<th>' + (L('main_datagrid_main_valueUnit') || 'Unit') + '</th>' +
            '<th>' + L('main_datagrid_main_createdDate') + '</th>' +
            '</tr></thead><tbody>';

        for (var i = 0; i < actions.length; i++) {
            var a = actions[i];
            html += '<tr data-idx="' + i + '">' +
                '<td>' + (a.triggerType || '') + '</td>' +
                '<td>' + (a.actionType || '') + '</td>' +
                '<td>' + (a.value || '') + '</td>' +
                '<td>' + (a.valueUnit || '') + '</td>' +
                '<td>' + (a.createdDate || '') + '</td>' +
                '</tr>';
        }

        html += '</tbody></table>';
        wrap.innerHTML = html;

        // Row selection
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
                ctx.style.left = e.clientX + 'px';
                ctx.style.top = e.clientY + 'px';
                ctx.classList.add('show');
            });
        });
    }
};
