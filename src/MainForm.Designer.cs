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
            this.addNewActionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showTheLogsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitTheProgramToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.ContextMenuStripNotifyIcon.SuspendLayout();
            this.SuspendLayout();

            // webViewHost
            this.webViewHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webViewHost.Location = new System.Drawing.Point(0, 0);
            this.webViewHost.Name = "webViewHost";
            this.webViewHost.Size = new System.Drawing.Size(529, 484);
            this.webViewHost.TabIndex = 0;

            // ContextMenuStripNotifyIcon
            this.ContextMenuStripNotifyIcon.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.addNewActionToolStripMenuItem,
                this.settingsToolStripMenuItem,
                this.showTheLogsToolStripMenuItem,
                this.helpToolStripMenuItem,
                this.aboutToolStripMenuItem,
                this.toolStripSeparator1,
                this.exitTheProgramToolStripMenuItem
            });
            this.ContextMenuStripNotifyIcon.Name = "ContextMenuStripNotifyIcon";
            this.ContextMenuStripNotifyIcon.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.ContextMenuStripNotifyIcon.Size = new System.Drawing.Size(200, 156);

            // addNewActionToolStripMenuItem
            this.addNewActionToolStripMenuItem.Image = global::WindowsAutoPowerManager.Properties.Resources.add;
            this.addNewActionToolStripMenuItem.Name = "addNewActionToolStripMenuItem";
            this.addNewActionToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.addNewActionToolStripMenuItem.Text = "Add new action";
            this.addNewActionToolStripMenuItem.Click += new System.EventHandler(this.addNewActionToolStripMenuItem_Click);

            // settingsToolStripMenuItem
            this.settingsToolStripMenuItem.Image = global::WindowsAutoPowerManager.Properties.Resources.settings;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);

            // showTheLogsToolStripMenuItem
            this.showTheLogsToolStripMenuItem.Image = global::WindowsAutoPowerManager.Properties.Resources.logs;
            this.showTheLogsToolStripMenuItem.Name = "showTheLogsToolStripMenuItem";
            this.showTheLogsToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.showTheLogsToolStripMenuItem.Text = "Show logs";
            this.showTheLogsToolStripMenuItem.Click += new System.EventHandler(this.showTheLogsToolStripMenuItem_Click);

            // helpToolStripMenuItem
            this.helpToolStripMenuItem.Image = global::WindowsAutoPowerManager.Properties.Resources.info;
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.helpToolStripMenuItem.Text = "Help";
            this.helpToolStripMenuItem.Click += new System.EventHandler(this.helpToolStripMenuItem_Click);

            // aboutToolStripMenuItem
            this.aboutToolStripMenuItem.Image = global::WindowsAutoPowerManager.Properties.Resources.about;
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);

            // exitTheProgramToolStripMenuItem
            this.exitTheProgramToolStripMenuItem.Image = global::WindowsAutoPowerManager.Properties.Resources.exit;
            this.exitTheProgramToolStripMenuItem.Name = "exitTheProgramToolStripMenuItem";
            this.exitTheProgramToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.exitTheProgramToolStripMenuItem.Text = "Exit the program";
            this.exitTheProgramToolStripMenuItem.Click += new System.EventHandler(this.exitTheProgramToolStripMenuItem_Click);

            // NotifyIconMain
            this.NotifyIconMain.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.NotifyIconMain.ContextMenuStrip = this.ContextMenuStripNotifyIcon;
            this.NotifyIconMain.Icon = ((System.Drawing.Icon)(resources.GetObject("NotifyIconMain.Icon")));
            this.NotifyIconMain.Text = "notifyIcon1";
            this.NotifyIconMain.Visible = true;
            this.NotifyIconMain.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.NotifyIconMain_MouseDoubleClick);

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(580, 520);
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
        private System.Windows.Forms.ToolStripMenuItem addNewActionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showTheLogsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitTheProgramToolStripMenuItem;
    }
}
