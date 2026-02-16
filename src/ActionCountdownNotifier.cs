using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using WindowsShutdownHelper.Functions;

namespace WindowsShutdownHelper
{
    public partial class ActionCountdownNotifier : Form
    {
        public static Language Language = LanguageSelector.LanguageFile();
        public ActionModel Action = new ActionModel();
        public string MessageTitle;
        public string MessageContentCountdownNotify, MessageContentCountdownNotify2;
        public string MessageContentActionType;
        public string MessageContentYouCanThat;
        public int ShowTimeSecond;
        public Timer Timer = new Timer();

        private bool _webViewReady;
        private bool _webViewInitStarted;
        private bool _pageReady;
        private bool _hasPendingNotification;
        private bool _showAfterInitialized;
        private bool _isPrewarmedHidden;
        private bool _initSent;
        private bool _timerStarted;
        private Panel _loadingOverlay;
        private Label _loadingLabel;
        private Timer _loadingDelayTimer;
        private const int LoadingOverlayDelayMs = 350;
        private Point dragStartCursor;
        private Point dragStartForm;
        private bool dragging;

        public ActionCountdownNotifier()
        {
            InitializeComponent();
            InitializeLoadingOverlay();
        }

        public ActionCountdownNotifier(string _messageTitle, string _messageContentCountdownNotify,
            string _messageContentCountdownNotify_2,
            string _messageContentActionType,
            string _messageContentYouCanThat, int _showTimeSecond,
            ActionModel _action)
            : this()
        {
            ConfigureNotification(
                _messageTitle,
                _messageContentCountdownNotify,
                _messageContentCountdownNotify_2,
                _messageContentActionType,
                _messageContentYouCanThat,
                _showTimeSecond,
                _action);
        }

        private async void actionCountdownNotifier_Load(object sender, EventArgs e)
        {
            try
            {
                await EnsureWebViewInitializedAsync(showLoading: false);
                TrySendInitData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Arayuz acilamadi.\r\n\r\nDetay: " + ex.Message,
                    Language?.MessageTitleError ?? "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
            }
        }

        public void ConfigureAndShow(
            string _messageTitle,
            string _messageContentCountdownNotify,
            string _messageContentCountdownNotify_2,
            string _messageContentActionType,
            string _messageContentYouCanThat,
            int _showTimeSecond,
            ActionModel _action)
        {
            ConfigureNotification(
                _messageTitle,
                _messageContentCountdownNotify,
                _messageContentCountdownNotify_2,
                _messageContentActionType,
                _messageContentYouCanThat,
                _showTimeSecond,
                _action);

            _showAfterInitialized = true;
            EnsureHiddenHostVisible();
            _ = EnsureWebViewInitializedSafeAsync(showLoading: false);
            TrySendInitData();
        }

        public void PrewarmInBackground()
        {
            if (_webViewReady || _webViewInitStarted || IsDisposed) return;

            EnsureHiddenHostVisible();
            _ = EnsureWebViewInitializedSafeAsync(showLoading: false);
        }

        private void ConfigureNotification(
            string _messageTitle,
            string _messageContentCountdownNotify,
            string _messageContentCountdownNotify_2,
            string _messageContentActionType,
            string _messageContentYouCanThat,
            int _showTimeSecond,
            ActionModel _action)
        {
            Action = _action ?? new ActionModel();
            ShowTimeSecond = Math.Max(0, _showTimeSecond);
            MessageTitle = _messageTitle;
            MessageContentCountdownNotify = _messageContentCountdownNotify;
            MessageContentCountdownNotify2 = _messageContentCountdownNotify_2;
            MessageContentActionType = _messageContentActionType;
            MessageContentYouCanThat = _messageContentYouCanThat;

            _hasPendingNotification = true;
            _showAfterInitialized = false;
            _initSent = false;
            _timerStarted = false;
            Timer.Stop();
        }

        private async Task EnsureWebViewInitializedSafeAsync(bool showLoading)
        {
            try
            {
                await EnsureWebViewInitializedAsync(showLoading);
                TrySendInitData();
            }
            catch
            {
                // If prewarm/init fails transiently, keep app running and allow retry.
            }
        }

        private async Task EnsureWebViewInitializedAsync(bool showLoading)
        {
            if (_webViewReady) return;
            if (_webViewInitStarted) return;

            _webViewInitStarted = true;
            if (showLoading)
            {
                ShowLoadingOverlay();
            }

            try
            {
                await InitializeWebView();
            }
            catch
            {
                _webViewInitStarted = false;
                throw;
            }
        }

        private void EnsureHiddenHostVisible()
        {
            if (!IsHandleCreated)
            {
                var _ = Handle;
            }

            if (!webView.IsHandleCreated)
            {
                webView.CreateControl();
            }

            if (Visible) return;

            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-32000, -32000);
            Opacity = 0;
            _isPrewarmedHidden = true;
            Show();
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            var env = await WebViewEnvironmentProvider.GetAsync();
            await webView.EnsureCoreWebView2Async(env);

            string webViewPath = Path.Combine(AppContext.BaseDirectory, "WebView");
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.local", webViewPath,
                CoreWebView2HostResourceAccessKind.Allow);

            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            webView.CoreWebView2.DOMContentLoaded += OnDomContentLoaded;

            webView.CoreWebView2.Navigate("https://app.local/Countdown.html");
        }

        private void OnDomContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (_webViewReady) return;
            _webViewReady = true;
            TrySendInitData();
        }

        private void TrySendInitData()
        {
            if (_initSent || !_hasPendingNotification || !_webViewReady || !_pageReady) return;
            _initSent = true;

            SendInitData();

            // Fallback: if page is already ready, show immediately.
            // This avoids missing popups when "initialized" message is delayed.
            if (_showAfterInitialized)
            {
                ShowForUser();
                StartCountdownTimer();
            }
        }

        private void SendInitData()
        {
            bool enableIgnore = true;
            bool enableDelete = Action.TriggerType == Config.TriggerTypes.FromNow ||
                                Action.TriggerType == Config.TriggerTypes.CertainTime;
            bool enableSkip = Action.TriggerType == Config.TriggerTypes.CertainTime;

            var initData = new
            {
                title = MessageTitle,
                countdownText1 = MessageContentCountdownNotify,
                countdownText2 = MessageContentCountdownNotify2,
                actionType = MessageContentActionType,
                infoText = MessageContentYouCanThat,
                seconds = ShowTimeSecond,
                btnDelete = Language.ActionCountdownNotifierButtonDelete ?? "Delete",
                btnIgnore = Language.ActionCountdownNotifierButtonIgnore ?? "Ignore",
                btnSkip = Language.ActionCountdownNotifierButtonSkip ?? "Skip",
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

            Timer.Interval = 1000;
            Timer.Tick -= TimerTick;
            Timer.Tick += TimerTick;
            Timer.Start();
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
                Text = Language?.CommonLoading ?? "Yükleniyor..."
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
            _loadingLabel.Text = Language?.CommonLoading ?? "Yükleniyor...";
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

        private void ShowForUser()
        {
            _showAfterInitialized = false;
            ShowInTaskbar = false;
            if (_isPrewarmedHidden)
            {
                StartPosition = FormStartPosition.Manual;
                var area = Screen.PrimaryScreen.WorkingArea;
                int x = area.Left + Math.Max(0, (area.Width - Width) / 2);
                int y = area.Top + Math.Max(0, (area.Height - Height) / 2);
                Location = new Point(x, y);
            }
            Opacity = 1;

            if (!Visible)
            {
                Show();
            }
            else
            {
                BringToFront();
            }

            Activate();
            Focus();
            _isPrewarmedHidden = false;
        }

        private void HideAndReset()
        {
            Timer.Stop();
            _timerStarted = false;
            _hasPendingNotification = false;
            _showAfterInitialized = false;
            _initSent = false;
            ShowTimeSecond = 0;

            if (!IsDisposed)
            {
                Hide();
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                Location = new Point(-32000, -32000);
                Opacity = 0;
                _isPrewarmedHidden = true;
            }
        }

        private void PostMessage(string type, object data)
        {
            if (!_webViewReady || webView.CoreWebView2 == null) return;
            var msg = JsonSerializer.Serialize(new { type, data });
            webView.CoreWebView2.PostWebMessageAsJson(msg);
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (ShowTimeSecond == 0)
            {
                HideAndReset();
                return;
            }

            if (Action.TriggerType == Config.TriggerTypes.SystemIdle)
            {
                uint idleTimeMin = SystemIdleDetector.GetLastInputTime();
                if (idleTimeMin == 0)
                {
                    HideAndReset();
                    return;
                }
            }

            --ShowTimeSecond;
            PostMessage("updateCountdown", new { seconds = ShowTimeSecond });
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
                    if (_showAfterInitialized)
                    {
                        ShowForUser();
                    }
                    StartCountdownTimer();
                    break;
                case "ignore":
                    HideAndReset();
                    break;
                case "delete":
                    MainForm.ActionList.Remove(Action);
                    MainForm.IsDeletedFromNotifier = true;
                    JsonWriter.WriteJson(AppContext.BaseDirectory + "\\ActionList.json", true,
                        MainForm.ActionList);
                    HideAndReset();
                    break;
                case "skip":
                    MainForm.IsSkippedCertainTimeAction = true;
                    HideAndReset();
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
