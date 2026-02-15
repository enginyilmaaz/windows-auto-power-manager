namespace WindowsShutdownHelper
{
    partial class mainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(mainForm));

            this.webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.notifyIcon_main = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip_notifyIcon = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addNewActionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showTheLogsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitTheProgramToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            ((System.ComponentModel.ISupportInitialize)(this.webView)).BeginInit();
            this.contextMenuStrip_notifyIcon.SuspendLayout();
            this.SuspendLayout();

            // webView
            this.webView.AllowExternalDrop = false;
            this.webView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webView.Location = new System.Drawing.Point(0, 0);
            this.webView.Name = "webView";
            this.webView.Size = new System.Drawing.Size(529, 484);
            this.webView.TabIndex = 0;
            this.webView.ZoomFactor = 1D;

            // contextMenuStrip_notifyIcon
            this.contextMenuStrip_notifyIcon.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.addNewActionToolStripMenuItem,
                this.settingsToolStripMenuItem,
                this.showTheLogsToolStripMenuItem,
                this.aboutToolStripMenuItem,
                this.toolStripSeparator1,
                this.exitTheProgramToolStripMenuItem
            });
            this.contextMenuStrip_notifyIcon.Name = "contextMenuStrip_notifyIcon";
            this.contextMenuStrip_notifyIcon.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip_notifyIcon.Size = new System.Drawing.Size(200, 120);

            // addNewActionToolStripMenuItem
            this.addNewActionToolStripMenuItem.Image = global::WindowsShutdownHelper.Properties.Resources.add;
            this.addNewActionToolStripMenuItem.Name = "addNewActionToolStripMenuItem";
            this.addNewActionToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.addNewActionToolStripMenuItem.Text = "Add new action";
            this.addNewActionToolStripMenuItem.Click += new System.EventHandler(this.addNewActionToolStripMenuItem_Click);

            // settingsToolStripMenuItem
            this.settingsToolStripMenuItem.Image = global::WindowsShutdownHelper.Properties.Resources.settings;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);

            // showTheLogsToolStripMenuItem
            this.showTheLogsToolStripMenuItem.Image = global::WindowsShutdownHelper.Properties.Resources.logs;
            this.showTheLogsToolStripMenuItem.Name = "showTheLogsToolStripMenuItem";
            this.showTheLogsToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.showTheLogsToolStripMenuItem.Text = "Show logs";
            this.showTheLogsToolStripMenuItem.Click += new System.EventHandler(this.showTheLogsToolStripMenuItem_Click);

            // aboutToolStripMenuItem
            this.aboutToolStripMenuItem.Image = global::WindowsShutdownHelper.Properties.Resources.about;
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);

            // exitTheProgramToolStripMenuItem
            this.exitTheProgramToolStripMenuItem.Image = global::WindowsShutdownHelper.Properties.Resources.exit;
            this.exitTheProgramToolStripMenuItem.Name = "exitTheProgramToolStripMenuItem";
            this.exitTheProgramToolStripMenuItem.Size = new System.Drawing.Size(200, 36);
            this.exitTheProgramToolStripMenuItem.Text = "Exit the program";
            this.exitTheProgramToolStripMenuItem.Click += new System.EventHandler(this.exitTheProgramToolStripMenuItem_Click);

            // notifyIcon_main
            this.notifyIcon_main.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIcon_main.ContextMenuStrip = this.contextMenuStrip_notifyIcon;
            this.notifyIcon_main.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon_main.Icon")));
            this.notifyIcon_main.Text = "notifyIcon1";
            this.notifyIcon_main.Visible = true;
            this.notifyIcon_main.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_main_MouseDoubleClick);

            // mainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(580, 520);
            this.MinimumSize = new System.Drawing.Size(480, 400);
            this.Controls.Add(this.webView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "mainForm";
            this.BackColor = System.Drawing.Color.FromArgb(26, 27, 46);
            this.Text = "Windows Shutdown Helper";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.mainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.mainForm_FormClosed);
            this.Load += new System.EventHandler(this.mainForm_Load);

            ((System.ComponentModel.ISupportInitialize)(this.webView)).EndInit();
            this.contextMenuStrip_notifyIcon.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private System.Windows.Forms.NotifyIcon notifyIcon_main;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip_notifyIcon;
        private System.Windows.Forms.ToolStripMenuItem addNewActionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showTheLogsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitTheProgramToolStripMenuItem;
    }
}
