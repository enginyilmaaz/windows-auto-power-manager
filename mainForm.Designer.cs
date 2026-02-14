namespace WindowsShutdownHelper
{
    partial class mainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(mainForm));
            this.numericUpDown_value = new System.Windows.Forms.NumericUpDown();
            this.comboBox_timeUnit = new System.Windows.Forms.ComboBox();
            this.comboBox_actionType = new System.Windows.Forms.ComboBox();
            this.label_actionType = new System.Windows.Forms.Label();
            this.statusStrip_default = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel_CurrentTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.button_AddAction = new System.Windows.Forms.Button();
            this.label_trigger = new System.Windows.Forms.Label();
            this.comboBox_triggerType = new System.Windows.Forms.ComboBox();
            this.label_value = new System.Windows.Forms.Label();
            this.dateTimePicker_time = new System.Windows.Forms.DateTimePicker();
            this.label_firstly_choose_a_trigger = new System.Windows.Forms.Label();
            this.contextMenuStrip_mainGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteSelectedTaskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearAllActionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox_newAction = new System.Windows.Forms.GroupBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dataGridView_taskList = new System.Windows.Forms.DataGridView();
            this.groupBox_actionList = new System.Windows.Forms.GroupBox();
            this.notifyIcon_main = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip_notifyIcon = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addNewActionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showTheLogsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitTheProgramToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pictureBox_settings = new System.Windows.Forms.PictureBox();
            this.pictureBox_logs = new System.Windows.Forms.PictureBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.contextMenuStrip_MainForm = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem_showSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_showLog = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_exit = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_value)).BeginInit();
            this.statusStrip_default.SuspendLayout();
            this.contextMenuStrip_mainGrid.SuspendLayout();
            this.groupBox_newAction.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_taskList)).BeginInit();
            this.groupBox_actionList.SuspendLayout();
            this.contextMenuStrip_notifyIcon.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_settings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_logs)).BeginInit();
            this.contextMenuStrip_MainForm.SuspendLayout();
            this.SuspendLayout();
            // 
            // numericUpDown_value
            // 
            this.numericUpDown_value.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.numericUpDown_value.Location = new System.Drawing.Point(170, 126);
            this.numericUpDown_value.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numericUpDown_value.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_value.Name = "numericUpDown_value";
            this.numericUpDown_value.Size = new System.Drawing.Size(155, 25);
            this.numericUpDown_value.TabIndex = 3;
            this.numericUpDown_value.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_value.Visible = false;
            //
            // comboBox_timeUnit
            //
            this.comboBox_timeUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_timeUnit.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.comboBox_timeUnit.Location = new System.Drawing.Point(331, 125);
            this.comboBox_timeUnit.Name = "comboBox_timeUnit";
            this.comboBox_timeUnit.Size = new System.Drawing.Size(96, 28);
            this.comboBox_timeUnit.TabIndex = 30;
            this.comboBox_timeUnit.Visible = false;
            //
            // comboBox_actionType
            // 
            this.comboBox_actionType.BackColor = System.Drawing.Color.White;
            this.comboBox_actionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_actionType.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.comboBox_actionType.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.comboBox_actionType.ForeColor = System.Drawing.SystemColors.WindowText;
            this.comboBox_actionType.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.comboBox_actionType.Location = new System.Drawing.Point(170, 38);
            this.comboBox_actionType.Name = "comboBox_actionType";
            this.comboBox_actionType.Size = new System.Drawing.Size(257, 28);
            this.comboBox_actionType.TabIndex = 1;
            this.comboBox_actionType.SelectedIndexChanged += new System.EventHandler(this.comboBox_actionType_SelectedIndexChanged);
            // 
            // label_actionType
            // 
            this.label_actionType.AutoSize = true;
            this.label_actionType.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label_actionType.Location = new System.Drawing.Point(26, 41);
            this.label_actionType.Name = "label_actionType";
            this.label_actionType.Size = new System.Drawing.Size(92, 20);
            this.label_actionType.TabIndex = 0;
            this.label_actionType.Text = "actionType:";
            // 
            // statusStrip_default
            // 
            this.statusStrip_default.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel_CurrentTime});
            this.statusStrip_default.Location = new System.Drawing.Point(0, 462);
            this.statusStrip_default.Name = "statusStrip_default";
            this.statusStrip_default.Size = new System.Drawing.Size(529, 22);
            this.statusStrip_default.SizingGrip = false;
            this.statusStrip_default.TabIndex = 8;
            this.statusStrip_default.Text = "statusStrip1";
            // 
            // toolStripStatusLabel_CurrentTime
            // 
            this.toolStripStatusLabel_CurrentTime.Name = "toolStripStatusLabel_CurrentTime";
            this.toolStripStatusLabel_CurrentTime.Size = new System.Drawing.Size(0, 17);
            // 
            // button_AddAction
            // 
            this.button_AddAction.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.button_AddAction.Location = new System.Drawing.Point(40, 193);
            this.button_AddAction.Name = "button_AddAction";
            this.button_AddAction.Size = new System.Drawing.Size(448, 28);
            this.button_AddAction.TabIndex = 4;
            this.button_AddAction.Text = "Add Action";
            this.button_AddAction.UseVisualStyleBackColor = true;
            this.button_AddAction.Click += new System.EventHandler(this.button_AddToList_Click);
            // 
            // label_trigger
            // 
            this.label_trigger.AutoSize = true;
            this.label_trigger.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label_trigger.Location = new System.Drawing.Point(26, 85);
            this.label_trigger.Name = "label_trigger";
            this.label_trigger.Size = new System.Drawing.Size(67, 20);
            this.label_trigger.TabIndex = 0;
            this.label_trigger.Text = "trigger: ";
            // 
            // comboBox_triggerType
            // 
            this.comboBox_triggerType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_triggerType.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.comboBox_triggerType.FormattingEnabled = true;
            this.comboBox_triggerType.Location = new System.Drawing.Point(170, 82);
            this.comboBox_triggerType.Name = "comboBox_triggerType";
            this.comboBox_triggerType.Size = new System.Drawing.Size(257, 28);
            this.comboBox_triggerType.TabIndex = 2;
            this.comboBox_triggerType.SelectedIndexChanged += new System.EventHandler(this.comboBox_trigger_SelectedIndexChanged);
            // 
            // label_value
            // 
            this.label_value.AutoSize = true;
            this.label_value.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label_value.Location = new System.Drawing.Point(26, 128);
            this.label_value.Name = "label_value";
            this.label_value.Size = new System.Drawing.Size(59, 20);
            this.label_value.TabIndex = 0;
            this.label_value.Text = "value: ";
            // 
            // dateTimePicker_time
            // 
            this.dateTimePicker_time.CalendarFont = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.dateTimePicker_time.CustomFormat = "                    HH:mm";
            this.dateTimePicker_time.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.dateTimePicker_time.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker_time.Location = new System.Drawing.Point(170, 126);
            this.dateTimePicker_time.Name = "dateTimePicker_time";
            this.dateTimePicker_time.ShowUpDown = true;
            this.dateTimePicker_time.Size = new System.Drawing.Size(257, 25);
            this.dateTimePicker_time.TabIndex = 3;
            this.dateTimePicker_time.Visible = false;
            // 
            // label_firstly_choose_a_trigger
            // 
            this.label_firstly_choose_a_trigger.AutoSize = true;
            this.label_firstly_choose_a_trigger.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label_firstly_choose_a_trigger.Location = new System.Drawing.Point(166, 128);
            this.label_firstly_choose_a_trigger.Name = "label_firstly_choose_a_trigger";
            this.label_firstly_choose_a_trigger.Size = new System.Drawing.Size(188, 20);
            this.label_firstly_choose_a_trigger.TabIndex = 0;
            this.label_firstly_choose_a_trigger.Text = "Firstly, choose a trigger... ";
            // 
            // contextMenuStrip_mainGrid
            // 
            this.contextMenuStrip_mainGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteSelectedTaskToolStripMenuItem,
            this.clearAllActionToolStripMenuItem});
            this.contextMenuStrip_mainGrid.Name = "contextMenuStrip_mainGrid";
            this.contextMenuStrip_mainGrid.Size = new System.Drawing.Size(193, 48);
            // 
            // deleteSelectedTaskToolStripMenuItem
            // 
            this.deleteSelectedTaskToolStripMenuItem.Image = global::WindowsShutdownHelper.Properties.Resources.delete;
            this.deleteSelectedTaskToolStripMenuItem.Name = "deleteSelectedTaskToolStripMenuItem";
            this.deleteSelectedTaskToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.deleteSelectedTaskToolStripMenuItem.Text = "Delete Selected Action";
            this.deleteSelectedTaskToolStripMenuItem.Click += new System.EventHandler(this.deleteSelectedTaskToolStripMenuItem_Click);
            // 
            // clearAllActionToolStripMenuItem
            // 
            this.clearAllActionToolStripMenuItem.Image = global::WindowsShutdownHelper.Properties.Resources.clear;
            this.clearAllActionToolStripMenuItem.Name = "clearAllActionToolStripMenuItem";
            this.clearAllActionToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.clearAllActionToolStripMenuItem.Text = "Clear All Action";
            this.clearAllActionToolStripMenuItem.Click += new System.EventHandler(this.clearAllActionToolStripMenuItem_Click);
            // 
            // groupBox_newAction
            // 
            this.groupBox_newAction.Controls.Add(this.comboBox_timeUnit);
            this.groupBox_newAction.Controls.Add(this.comboBox_actionType);
            this.groupBox_newAction.Controls.Add(this.label_actionType);
            this.groupBox_newAction.Controls.Add(this.label_firstly_choose_a_trigger);
            this.groupBox_newAction.Controls.Add(this.label_trigger);
            this.groupBox_newAction.Controls.Add(this.comboBox_triggerType);
            this.groupBox_newAction.Controls.Add(this.dateTimePicker_time);
            this.groupBox_newAction.Controls.Add(this.label_value);
            this.groupBox_newAction.Controls.Add(this.numericUpDown_value);
            this.groupBox_newAction.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.groupBox_newAction.Location = new System.Drawing.Point(40, 12);
            this.groupBox_newAction.Name = "groupBox_newAction";
            this.groupBox_newAction.Size = new System.Drawing.Size(448, 175);
            this.groupBox_newAction.TabIndex = 0;
            this.groupBox_newAction.TabStop = false;
            this.groupBox_newAction.Text = "New Action";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dataGridView_taskList);
            this.panel2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.panel2.Location = new System.Drawing.Point(0, 27);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(448, 174);
            this.panel2.TabIndex = 19;
            // 
            // dataGridView_taskList
            // 
            this.dataGridView_taskList.AllowUserToAddRows = false;
            this.dataGridView_taskList.AllowUserToDeleteRows = false;
            this.dataGridView_taskList.AllowUserToResizeColumns = false;
            this.dataGridView_taskList.AllowUserToResizeRows = false;
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle10.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            dataGridViewCellStyle10.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView_taskList.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle10;
            this.dataGridView_taskList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            dataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView_taskList.DefaultCellStyle = dataGridViewCellStyle11;
            this.dataGridView_taskList.Location = new System.Drawing.Point(0, 0);
            this.dataGridView_taskList.MultiSelect = false;
            this.dataGridView_taskList.Name = "dataGridView_taskList";
            this.dataGridView_taskList.ReadOnly = true;
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            dataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView_taskList.RowHeadersDefaultCellStyle = dataGridViewCellStyle12;
            this.dataGridView_taskList.RowHeadersVisible = false;
            this.dataGridView_taskList.ShowCellErrors = false;
            this.dataGridView_taskList.Size = new System.Drawing.Size(448, 174);
            this.dataGridView_taskList.TabIndex = 5;
            this.dataGridView_taskList.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_taskList_CellContentClick);
            this.dataGridView_taskList.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dataGridView_taskList_RowsAdded);
            this.dataGridView_taskList.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.dataGridView_taskList_RowsRemoved);
            // 
            // groupBox_actionList
            // 
            this.groupBox_actionList.Controls.Add(this.panel2);
            this.groupBox_actionList.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.groupBox_actionList.Location = new System.Drawing.Point(40, 245);
            this.groupBox_actionList.Name = "groupBox_actionList";
            this.groupBox_actionList.Size = new System.Drawing.Size(448, 201);
            this.groupBox_actionList.TabIndex = 0;
            this.groupBox_actionList.TabStop = false;
            this.groupBox_actionList.Text = "Action List";
            // 
            // notifyIcon_main
            // 
            this.notifyIcon_main.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIcon_main.ContextMenuStrip = this.contextMenuStrip_notifyIcon;
            this.notifyIcon_main.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon_main.Icon")));
            this.notifyIcon_main.Text = "notifyIcon1";
            this.notifyIcon_main.Visible = true;
            this.notifyIcon_main.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_main_MouseDoubleClick);
            // 
            // contextMenuStrip_notifyIcon
            // 
            this.contextMenuStrip_notifyIcon.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addNewActionToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.showTheLogsToolStripMenuItem,
            this.exitTheProgramToolStripMenuItem});
            this.contextMenuStrip_notifyIcon.Name = "contextMenuStrip_notifyIcon";
            this.contextMenuStrip_notifyIcon.Size = new System.Drawing.Size(163, 92);
            this.contextMenuStrip_notifyIcon.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_notifyIcon_Opening);
            // 
            // addNewActionToolStripMenuItem
            // 
            this.addNewActionToolStripMenuItem.Image = global::WindowsShutdownHelper.Properties.Resources.add;
            this.addNewActionToolStripMenuItem.Name = "addNewActionToolStripMenuItem";
            this.addNewActionToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.addNewActionToolStripMenuItem.Text = "Add new action";
            this.addNewActionToolStripMenuItem.Click += new System.EventHandler(this.addNewActionToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Image = global::WindowsShutdownHelper.Properties.Resources.settings;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // showTheLogsToolStripMenuItem
            // 
            this.showTheLogsToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("showTheLogsToolStripMenuItem.Image")));
            this.showTheLogsToolStripMenuItem.Name = "showTheLogsToolStripMenuItem";
            this.showTheLogsToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.showTheLogsToolStripMenuItem.Text = "Show logs";
            this.showTheLogsToolStripMenuItem.Click += new System.EventHandler(this.showTheLogsToolStripMenuItem_Click);
            // 
            // exitTheProgramToolStripMenuItem
            // 
            this.exitTheProgramToolStripMenuItem.Image = global::WindowsShutdownHelper.Properties.Resources.exit;
            this.exitTheProgramToolStripMenuItem.Name = "exitTheProgramToolStripMenuItem";
            this.exitTheProgramToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.exitTheProgramToolStripMenuItem.Text = "Exit the program";
            this.exitTheProgramToolStripMenuItem.Click += new System.EventHandler(this.exitTheProgramToolStripMenuItem_Click);
            // 
            // pictureBox_settings
            // 
            this.pictureBox_settings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pictureBox_settings.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox_settings.Image")));
            this.pictureBox_settings.Location = new System.Drawing.Point(502, 464);
            this.pictureBox_settings.Name = "pictureBox_settings";
            this.pictureBox_settings.Size = new System.Drawing.Size(20, 20);
            this.pictureBox_settings.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox_settings.TabIndex = 23;
            this.pictureBox_settings.TabStop = false;
            this.pictureBox_settings.Click += new System.EventHandler(this.pictureBox_settings_Click);
            // 
            // pictureBox_logs
            // 
            this.pictureBox_logs.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pictureBox_logs.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox_logs.Image")));
            this.pictureBox_logs.Location = new System.Drawing.Point(476, 465);
            this.pictureBox_logs.Name = "pictureBox_logs";
            this.pictureBox_logs.Size = new System.Drawing.Size(17, 17);
            this.pictureBox_logs.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox_logs.TabIndex = 22;
            this.pictureBox_logs.TabStop = false;
            this.pictureBox_logs.Click += new System.EventHandler(this.pictureBox_logs_Click);
            // 
            // toolTip
            // 
            this.toolTip.IsBalloon = true;
            // 
            // contextMenuStrip_MainForm
            // 
            this.contextMenuStrip_MainForm.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_showSetting,
            this.toolStripMenuItem_showLog,
            this.toolStripMenuItem_exit});
            this.contextMenuStrip_MainForm.Name = "contextMenuStrip_notifyIcon";
            this.contextMenuStrip_MainForm.Size = new System.Drawing.Size(163, 70);
            // 
            // toolStripMenuItem_showSetting
            // 
            this.toolStripMenuItem_showSetting.Image = global::WindowsShutdownHelper.Properties.Resources.settings;
            this.toolStripMenuItem_showSetting.Name = "toolStripMenuItem_showSetting";
            this.toolStripMenuItem_showSetting.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem_showSetting.Text = "Settings";
            this.toolStripMenuItem_showSetting.Click += new System.EventHandler(this.toolStripMenuItem_showSetting_Click);
            // 
            // toolStripMenuItem_showLog
            // 
            this.toolStripMenuItem_showLog.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItem_showLog.Image")));
            this.toolStripMenuItem_showLog.Name = "toolStripMenuItem_showLog";
            this.toolStripMenuItem_showLog.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem_showLog.Text = "Show logs";
            this.toolStripMenuItem_showLog.Click += new System.EventHandler(this.toolStripMenuItem_showLog_Click);
            // 
            // toolStripMenuItem_exit
            // 
            this.toolStripMenuItem_exit.Image = global::WindowsShutdownHelper.Properties.Resources.exit;
            this.toolStripMenuItem_exit.Name = "toolStripMenuItem_exit";
            this.toolStripMenuItem_exit.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem_exit.Text = "Exit the program";
            this.toolStripMenuItem_exit.Click += new System.EventHandler(this.toolStripMenuItem_exit_Click);
            // 
            // mainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(529, 484);
            this.ContextMenuStrip = this.contextMenuStrip_MainForm;
            this.Controls.Add(this.pictureBox_settings);
            this.Controls.Add(this.pictureBox_logs);
            this.Controls.Add(this.groupBox_newAction);
            this.Controls.Add(this.groupBox_actionList);
            this.Controls.Add(this.statusStrip_default);
            this.Controls.Add(this.button_AddAction);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "mainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Windows Shutdown Helper";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.mainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.mainForm_FormClosed);
            this.Load += new System.EventHandler(this.mainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_value)).EndInit();
            this.statusStrip_default.ResumeLayout(false);
            this.statusStrip_default.PerformLayout();
            this.contextMenuStrip_mainGrid.ResumeLayout(false);
            this.groupBox_newAction.ResumeLayout(false);
            this.groupBox_newAction.PerformLayout();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_taskList)).EndInit();
            this.groupBox_actionList.ResumeLayout(false);
            this.contextMenuStrip_notifyIcon.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_settings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_logs)).EndInit();
            this.contextMenuStrip_MainForm.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.NumericUpDown numericUpDown_value;
        private System.Windows.Forms.ComboBox comboBox_timeUnit;
        private System.Windows.Forms.ComboBox comboBox_actionType;
        private System.Windows.Forms.Label label_actionType;
        private System.Windows.Forms.StatusStrip statusStrip_default;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel_CurrentTime;
        private System.Windows.Forms.Button button_AddAction;
        private System.Windows.Forms.Label label_trigger;
        private System.Windows.Forms.ComboBox comboBox_triggerType;
        private System.Windows.Forms.Label label_value;
        private System.Windows.Forms.DateTimePicker dateTimePicker_time;
        private System.Windows.Forms.Label label_firstly_choose_a_trigger;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip_mainGrid;
        private System.Windows.Forms.ToolStripMenuItem deleteSelectedTaskToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBox_newAction;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView dataGridView_taskList;
        private System.Windows.Forms.GroupBox groupBox_actionList;
        private System.Windows.Forms.PictureBox pictureBox_logs;
        private System.Windows.Forms.PictureBox pictureBox_settings;
        private System.Windows.Forms.NotifyIcon notifyIcon_main;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip_notifyIcon;
        private System.Windows.Forms.ToolStripMenuItem exitTheProgramToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addNewActionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showTheLogsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearAllActionToolStripMenuItem;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip_MainForm;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_showSetting;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_showLog;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_exit;
    }
}