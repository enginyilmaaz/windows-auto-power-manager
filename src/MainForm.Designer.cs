namespace WindowsAutoPowerManager
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));

            this.webViewHost = new System.Windows.Forms.Panel();
            this.NotifyIconMain = new System.Windows.Forms.NotifyIcon(this.components);
            this.ContextMenuStripNotifyIcon = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.trayQuickActions = new WindowsAutoPowerManager.Functions.TrayQuickActionHost();
            this.trayQuickActionsSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.showTheLogsToolStripMenuItem = new WindowsAutoPowerManager.Functions.TrayMenuItem();
            this.helpToolStripMenuItem = new WindowsAutoPowerManager.Functions.TrayMenuItem();
            this.aboutToolStripMenuItem = new WindowsAutoPowerManager.Functions.TrayMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitTheProgramToolStripMenuItem = new WindowsAutoPowerManager.Functions.TrayMenuItem();

            this.ContextMenuStripNotifyIcon.SuspendLayout();
            this.SuspendLayout();

            // webViewHost
            this.webViewHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webViewHost.Location = new System.Drawing.Point(0, 0);
            this.webViewHost.Name = "webViewHost";
            this.webViewHost.Size = new System.Drawing.Size(629, 484);
            this.webViewHost.TabIndex = 0;

            // ContextMenuStripNotifyIcon
            // The item order is mirrored by EnumCmStripNotifyIcon.
            this.ContextMenuStripNotifyIcon.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.trayQuickActions,
                this.trayQuickActionsSeparator,
                this.showTheLogsToolStripMenuItem,
                this.helpToolStripMenuItem,
                this.aboutToolStripMenuItem,
                this.toolStripSeparator1,
                this.exitTheProgramToolStripMenuItem
            });
            this.ContextMenuStripNotifyIcon.Name = "ContextMenuStripNotifyIcon";
            // Rows draw their own glyphs; an image margin would push the quick action strip
            // away from the left edge.
            this.ContextMenuStripNotifyIcon.ShowImageMargin = false;
            this.ContextMenuStripNotifyIcon.ShowCheckMargin = false;
            this.ContextMenuStripNotifyIcon.ImageScalingSize = new System.Drawing.Size(16, 16);
            this.ContextMenuStripNotifyIcon.Padding = new System.Windows.Forms.Padding(6);
            this.ContextMenuStripNotifyIcon.Size = new System.Drawing.Size(236, 220);
            this.ContextMenuStripNotifyIcon.Opening += new System.ComponentModel.CancelEventHandler(this.ContextMenuStripNotifyIcon_Opening);
            this.ContextMenuStripNotifyIcon.Opened += new System.EventHandler(this.ContextMenuStripNotifyIcon_Opened);

            // trayQuickActions
            this.trayQuickActions.Name = "trayQuickActions";
            this.trayQuickActions.Strip.NewActionRequested += new System.EventHandler(this.trayQuickActions_NewActionRequested);
            this.trayQuickActions.Strip.PauseToggleRequested += new System.EventHandler(this.trayQuickActions_PauseToggleRequested);
            this.trayQuickActions.Strip.SettingsRequested += new System.EventHandler(this.trayQuickActions_SettingsRequested);

            // trayQuickActionsSeparator
            this.trayQuickActionsSeparator.Name = "trayQuickActionsSeparator";

            // showTheLogsToolStripMenuItem
            this.showTheLogsToolStripMenuItem.Glyph = WindowsAutoPowerManager.Functions.TrayMenuGlyphs.List;
            this.showTheLogsToolStripMenuItem.Name = "showTheLogsToolStripMenuItem";
            this.showTheLogsToolStripMenuItem.Text = "Show logs";
            this.showTheLogsToolStripMenuItem.Click += new System.EventHandler(this.showTheLogsToolStripMenuItem_Click);

            // helpToolStripMenuItem
            this.helpToolStripMenuItem.Glyph = WindowsAutoPowerManager.Functions.TrayMenuGlyphs.Help;
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Text = "Help";
            this.helpToolStripMenuItem.Click += new System.EventHandler(this.helpToolStripMenuItem_Click);

            // aboutToolStripMenuItem
            this.aboutToolStripMenuItem.Glyph = WindowsAutoPowerManager.Functions.TrayMenuGlyphs.Info;
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);

            // toolStripSeparator1
            this.toolStripSeparator1.Name = "toolStripSeparator1";

            // exitTheProgramToolStripMenuItem
            this.exitTheProgramToolStripMenuItem.Glyph = WindowsAutoPowerManager.Functions.TrayMenuGlyphs.Power;
            this.exitTheProgramToolStripMenuItem.IsDanger = true;
            this.exitTheProgramToolStripMenuItem.Name = "exitTheProgramToolStripMenuItem";
            this.exitTheProgramToolStripMenuItem.Text = "Exit the program";
            this.exitTheProgramToolStripMenuItem.Click += new System.EventHandler(this.exitTheProgramToolStripMenuItem_Click);

            // NotifyIconMain
            this.NotifyIconMain.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.NotifyIconMain.ContextMenuStrip = this.ContextMenuStripNotifyIcon;
            this.NotifyIconMain.Icon = ((System.Drawing.Icon)(resources.GetObject("NotifyIconMain.Icon")));
            this.NotifyIconMain.Text = "Windows Auto Power Manager";
            this.NotifyIconMain.Visible = true;
            this.NotifyIconMain.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.NotifyIconMain_MouseDoubleClick);

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(680, 520);
            this.MinimumSize = new System.Drawing.Size(480, 400);
            this.Controls.Add(this.webViewHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.BackColor = System.Drawing.Color.FromArgb(26, 27, 46);
            this.Text = Config.Constants.AppName;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.mainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.mainForm_FormClosed);
            this.Load += new System.EventHandler(this.mainForm_Load);

            this.ContextMenuStripNotifyIcon.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel webViewHost;
        private System.Windows.Forms.NotifyIcon NotifyIconMain;
        private System.Windows.Forms.ContextMenuStrip ContextMenuStripNotifyIcon;
        private WindowsAutoPowerManager.Functions.TrayQuickActionHost trayQuickActions;
        private System.Windows.Forms.ToolStripSeparator trayQuickActionsSeparator;
        private WindowsAutoPowerManager.Functions.TrayMenuItem showTheLogsToolStripMenuItem;
        private WindowsAutoPowerManager.Functions.TrayMenuItem helpToolStripMenuItem;
        private WindowsAutoPowerManager.Functions.TrayMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private WindowsAutoPowerManager.Functions.TrayMenuItem exitTheProgramToolStripMenuItem;
    }
}
