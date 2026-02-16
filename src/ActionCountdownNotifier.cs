using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using WindowsShutdownHelper.functions;

namespace WindowsShutdownHelper
{
    public partial class actionCountdownNotifier : Form
    {
        public static Language language = languageSelector.languageFile();
        public ActionModel action = new ActionModel();
        public string messageTitle;
        public string messageContentCountdownNotify, messageContentCountdownNotify_2;
        public string messageContentActionType;
        public string messageContentYouCanThat;
        public int showTimeSecond;
        public Timer timer = new Timer();

        private bool _webViewReady;
        private bool _pageReady;
        private bool _initSent;
        private bool _timerStarted;
        private Panel _loadingOverlay;
        private Label _loadingLabel;
        private Timer _loadingDelayTimer;
        private const int LoadingOverlayDelayMs = 350;
        private Point dragStartCursor;
        private Point dragStartForm;
        private bool dragging;

        public actionCountdownNotifier(string _messageTitle, string _messageContentCountdownNotify,
            string _messageContentCountdownNotify_2,
            string _messageContentActionType,
            string _messageContentYouCanThat, int _showTimeSecond,
            ActionModel _action)
        {
            InitializeComponent();
            InitializeLoadingOverlay();
            action = _action;
            showTimeSecond = _showTimeSecond;
            messageTitle = _messageTitle;
            messageContentCountdownNotify = _messageContentCountdownNotify;
            messageContentCountdownNotify_2 = _messageContentCountdownNotify_2;
            messageContentActionType = _messageContentActionType;
            messageContentYouCanThat = _messageContentYouCanThat;
        }

        private async void actionCountdownNotifier_Load(object sender, EventArgs e)
        {
            try
            {
                ShowLoadingOverlay();
                await InitializeWebView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Arayuz acilamadi.\r\n\r\nDetay: " + ex.Message,
                    language?.messageTitle_error ?? "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
            }
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            var env = await WebViewEnvironmentProvider.GetAsync();
            await webView.EnsureCoreWebView2Async(env);

            string wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.local", wwwrootPath,
                CoreWebView2HostResourceAccessKind.Allow);

            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            webView.CoreWebView2.DOMContentLoaded += OnDomContentLoaded;

            webView.CoreWebView2.Navigate("https://app.local/countdown.html");
        }

        private void OnDomContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (_webViewReady) return;
            _webViewReady = true;
            TrySendInitData();
        }

        private void TrySendInitData()
        {
            if (_initSent || !_webViewReady || !_pageReady) return;
            _initSent = true;

            SendInitData();
        }

        private void SendInitData()
        {
            bool enableIgnore = true;
            bool enableDelete = action.triggerType == config.triggerTypes.fromNow ||
                                action.triggerType == config.triggerTypes.certainTime;
            bool enableSkip = action.triggerType == config.triggerTypes.certainTime;

            var initData = new
            {
                title = messageTitle,
                countdownText1 = messageContentCountdownNotify,
                countdownText2 = messageContentCountdownNotify_2,
                actionType = messageContentActionType,
                infoText = messageContentYouCanThat,
                seconds = showTimeSecond,
                btnDelete = language.actionCountdownNotifier_button_delete ?? "Delete",
                btnIgnore = language.actionCountdownNotifier_button_ignore ?? "Ignore",
                btnSkip = language.actionCountdownNotifier_button_skip ?? "Skip",
                enableIgnore = enableIgnore,
                enableDelete = enableDelete,
                enableSkip = enableSkip
            };

            PostMessage("initCountdown", initData);
        }

        private void StartCountdownTimer()
        {
            if (_timerStarted) return;
            _timerStarted = true;

            timer.Interval = 1000;
            timer.Tick -= timerTick;
            timer.Tick += timerTick;
            timer.Start();
        }

        private void InitializeLoadingOverlay()
        {
            _loadingDelayTimer = new Timer
            {
                Interval = LoadingOverlayDelayMs
            };
            _loadingDelayTimer.Tick += (s, e) =>
            {
                _loadingDelayTimer.Stop();
                if (!_timerStarted && _loadingOverlay != null)
                {
                    _loadingOverlay.Visible = true;
                    _loadingOverlay.BringToFront();
                }
            };

            _loadingLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(97, 106, 124),
                Text = language?.common_loading ?? "Yükleniyor..."
            };

            _loadingOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(241, 245, 249)
            };

            _loadingOverlay.Controls.Add(_loadingLabel);
            Controls.Add(_loadingOverlay);
            _loadingOverlay.Visible = false;
            _loadingOverlay.BringToFront();
        }

        private void ShowLoadingOverlay()
        {
            if (_loadingOverlay == null) return;
            _loadingLabel.Text = language?.common_loading ?? "Yükleniyor...";
            _loadingOverlay.Visible = false;
            _loadingDelayTimer?.Stop();
            _loadingDelayTimer?.Start();
        }

        private void HideLoadingOverlay()
        {
            if (_loadingOverlay == null) return;
            _loadingDelayTimer?.Stop();
            _loadingOverlay.Visible = false;
        }

        private void PostMessage(string type, object data)
        {
            if (!_webViewReady || webView.CoreWebView2 == null) return;
            var msg = JsonSerializer.Serialize(new { type, data });
            webView.CoreWebView2.PostWebMessageAsJson(msg);
        }

        private void timerTick(object sender, EventArgs e)
        {
            if (showTimeSecond == 0)
            {
                timer.Stop();
                Close();
                return;
            }

            if (action.triggerType == config.triggerTypes.systemIdle)
            {
                uint idleTimeMin = systemIdleDetector.GetLastInputTime();
                if (idleTimeMin == 0)
                {
                    timer.Stop();
                    Close();
                    return;
                }
            }

            --showTimeSecond;
            PostMessage("updateCountdown", new { seconds = showTimeSecond });
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string json = e.WebMessageAsJson;
            var doc = JsonDocument.Parse(json);
            string msgJson = doc.RootElement.GetString();
            var msg = JsonDocument.Parse(msgJson);
            string type = msg.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "ready":
                    _pageReady = true;
                    TrySendInitData();
                    break;
                case "initialized":
                    HideLoadingOverlay();
                    StartCountdownTimer();
                    break;
                case "ignore":
                    timer.Stop();
                    Close();
                    break;
                case "delete":
                    timer.Stop();
                    mainForm.actionList.Remove(action);
                    mainForm.isDeletedFromNotifier = true;
                    jsonWriter.WriteJson(AppContext.BaseDirectory + "\\actionList.json", true,
                        mainForm.actionList);
                    Close();
                    break;
                case "skip":
                    timer.Stop();
                    mainForm.isSkippedCertainTimeAction = true;
                    Close();
                    break;
                case "dragStart":
                    var data = msg.RootElement.GetProperty("data");
                    int sx = data.GetProperty("x").GetInt32();
                    int sy = data.GetProperty("y").GetInt32();
                    StartDrag(sx, sy);
                    break;
            }
        }

        private void StartDrag(int screenX, int screenY)
        {
            dragging = true;
            dragStartCursor = new Point(screenX, screenY);
            dragStartForm = Location;

            this.MouseMove += DragMouseMove;
            this.MouseUp += DragMouseUp;
        }

        private void DragMouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(dragStartCursor));
                Location = Point.Add(dragStartForm, new Size(dif));
            }
        }

        private void DragMouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
            this.MouseMove -= DragMouseMove;
            this.MouseUp -= DragMouseUp;
        }
    }
}
