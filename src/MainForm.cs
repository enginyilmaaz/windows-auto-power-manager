using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using WindowsAutoPowerManager.Config;
using WindowsAutoPowerManager.Enums;
using WindowsAutoPowerManager.Functions;

namespace WindowsAutoPowerManager
{
    public partial class MainForm : Form
    {
        public static Language Language = LanguageSelector.LanguageFile();
        public static List<ActionModel> ActionList = new List<ActionModel>();
        public static Settings Settings = new Settings();
        public static bool IsSkippedCertainTimeAction;
        public static bool IsApplicationExiting;
        public static Timer Timer = new Timer();
        public static int RunInTaskbarCounter;
        private bool _startMinimizedToTray;

        private bool _webViewReady;
        private bool _bootDataReady;
        private bool _initSent;
        private bool _isPaused;
        private DateTime? _pauseUntilTime;
        private Settings _cachedSettings;
        private Dictionary<string, SubWindow> _subWindows = new Dictionary<string, SubWindow>();
        private WebView2 webView;
        private Panel _loadingOverlay;
        private Label _loadingLabel;
        private readonly string[] _subWindowPrewarmPages = { "settings", "logs", "help", "about" };
        private bool _subWindowPrewarmStarted;
        private bool _startupErrorShown;
        private bool _pendingOpenNewActionModal;
        private uint? _lastObservedIdleTimeSec;
        private bool _updateCheckRunning;
        private bool _updateDownloadRunning;
        private bool _startupUpdateCheckStarted;
        private UpdateInfo _pendingUpdate;
        private readonly ActionScheduler _scheduler = new ActionScheduler();
        private const int WM_POWERBROADCAST = 0x0218;
        private const int PBT_APMRESUMESUSPEND = 0x0007;
        private const int PBT_APMRESUMEAUTOMATIC = 0x0012;
        private const int PBT_POWERSETTINGCHANGE = 0x8013;
        private const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        private static readonly Guid GuidConsoleDisplayState =
            new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");
        private IntPtr _displayStateNotificationHandle = IntPtr.Zero;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr RegisterPowerSettingNotification(
            IntPtr recipient,
            ref Guid powerSettingGuid,
            uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterPowerSettingNotification(IntPtr handle);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }

        public MainForm()
        {
            InitializeComponent();
            // Main window: widen by 30px, then lock resize (min=max) and disable maximize.
            var lockedMainSize = new System.Drawing.Size(Size.Width + 30, Size.Height);
            Size = lockedMainSize;
            MinimumSize = lockedMainSize;
            MaximumSize = lockedMainSize;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ApplyExecutableIcon();

            _startMinimizedToTray = StartWithWindows.IsRunInTaskBarRequested(Environment.GetCommandLineArgs());

            bool isDark = ReadCachedThemeIsDark();
            CreateWebViewControl(isDark);
            InitializeLoadingOverlay(isDark);
        }

        private void ApplyExecutableIcon()
        {
            System.Drawing.Icon appIcon =
                System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (appIcon == null)
            {
                return;
            }

            Icon = appIcon;
            NotifyIconMain.Icon = appIcon;
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!IsHandleCreated && _startMinimizedToTray)
            {
                CreateHandle();
                value = false;
            }

            base.SetVisibleCore(value);
        }

        protected override void OnLoad(EventArgs e)
        {
            if (_startMinimizedToTray && RunInTaskbarCounter <= 0)
            {
                ++RunInTaskbarCounter;
                ShowInTaskbar = false;
            }

            base.OnLoad(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (_displayStateNotificationHandle == IntPtr.Zero)
            {
                Guid displayStateGuid = GuidConsoleDisplayState;
                _displayStateNotificationHandle = RegisterPowerSettingNotification(
                    Handle,
                    ref displayStateGuid,
                    DEVICE_NOTIFY_WINDOW_HANDLE);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_displayStateNotificationHandle != IntPtr.Zero)
            {
                UnregisterPowerSettingNotification(_displayStateNotificationHandle);
                _displayStateNotificationHandle = IntPtr.Zero;
            }

            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_POWERBROADCAST)
            {
                int powerEvent = m.WParam.ToInt32();
                if (powerEvent == PBT_APMRESUMESUSPEND || powerEvent == PBT_APMRESUMEAUTOMATIC)
                {
                    HandleSystemResume();
                }
                else if (powerEvent == PBT_POWERSETTINGCHANGE)
                {
                    HandleDisplayStateChange(m.LParam);
                }
            }

            base.WndProc(ref m);
        }

        private void HandleDisplayStateChange(IntPtr lParam)
        {
            if (lParam == IntPtr.Zero)
            {
                return;
            }

            POWERBROADCAST_SETTING setting;
            try
            {
                setting = Marshal.PtrToStructure<POWERBROADCAST_SETTING>(lParam);
            }
            catch
            {
                return;
            }

            if (setting.PowerSetting != GuidConsoleDisplayState || setting.DataLength < 1)
            {
                return;
            }

            // 0 = off, 1 = on, 2 = dimmed. Anything other than off means the screen is visible.
            Actions.TurnOff.NotifyDisplayStateChanged(setting.Data != 0);
        }

        public void DeleteExpriedAction()
        {
            bool changed = false;
            DateTime now = DateTime.Now;

            for (int index = ActionList.Count - 1; index >= 0; --index)
            {
                ActionModel action = ActionList[index];
                if (action.TriggerType == Config.TriggerTypes.FromNow)
                {
                    if (!ActionScheduler.TryParseFromNowValue(action, out DateTime actionDate) || now > actionDate)
                    {
                        ActionList.RemoveAt(index);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                WriteJsonToActionList();
            }
            else
            {
                _scheduler.RebuildRuntimeStates(ActionList);
            }
        }

        private async void mainForm_Load(object sender, EventArgs e)
        {
            Text = Language.MainFormName;
            NotifyIconMain.Text = Language.MainFormName + " " + Language.NotifyIconMain;

            // Start WebView initialization directly -- no BeginInvoke round-trip.
            // The first await inside yields to the message pump, allowing
            // InitializeRuntimeState to run while EnsureCoreWebView2Async proceeds.
            var webViewTask = InitializeWebViewSafeAsync();

            // Queue runtime state initialization -- runs when the pump is free
            // (immediately after the first await in webViewTask yields).
            BeginInvoke(new Action(InitializeRuntimeState));

            await webViewTask;
        }

        private void CreateWebViewControl(bool isDark)
        {
            if (webView != null) return;

            webView = new WebView2
            {
                AllowExternalDrop = false,
                Dock = DockStyle.Fill,
                Name = "webView",
                ZoomFactor = 1D,
                TabIndex = 0,
                DefaultBackgroundColor = isDark
                    ? System.Drawing.Color.FromArgb(26, 27, 46)
                    : System.Drawing.Color.FromArgb(240, 242, 245)
            };

            webViewHost.Controls.Add(webView);
        }

        private void InitializeRuntimeState()
        {
            try
            {
                DetectScreen.ManuelLockingActionLogger();
                ActionList = LoadActionList();
                DeleteExpriedAction();
                ResolveConflictingEnabledActions();
                _scheduler.RebuildRuntimeStates(ActionList);

                // Setup timer
                Timer.Interval = 1000;
                Timer.Tick += TimerTick;
                Timer.Start();

                // Setup notify icon context menu text
                ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.AddNewAction].Text =
                    Language.ContextMenuStripNotifyIconAddNewAction;
                ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.ExitTheProgram].Text =
                    Language.ContextMenuStripNotifyIconExitProgram;
                ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.Settings].Text =
                    Language.ContextMenuStripNotifyIconShowSettings;
                ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.ShowLogs].Text =
                    Language.ContextMenuStripNotifyIconShowLogs;
                ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.Help].Text =
                    Language.ContextMenuStripNotifyIconShowHelp ?? "Help";
                ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.About].Text =
                    Language.AboutMenuItem ?? "About";

                // Apply modern tray menu renderer based on theme
                _cachedSettings = LoadSettings();
                Logger.Initialize(_cachedSettings);
                RepairStartupRegistration(_cachedSettings);
                bool isDark = DetermineIfDark(_cachedSettings.Theme);
                ContextMenuStripNotifyIcon.Renderer = new WindowsAutoPowerManager.Functions.ModernMenuRenderer(isDark);
                ContextMenuStripNotifyIcon.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular);
                BackColor = isDark
                    ? System.Drawing.Color.FromArgb(26, 27, 46)
                    : System.Drawing.Color.FromArgb(240, 242, 245);
            }
            catch (Exception ex)
            {
                ActionList = new List<ActionModel>();
                _cachedSettings = Config.SettingsINI.DefaulSettingFile();
                Logger.Initialize(_cachedSettings);
                ReportStartupError("Baslangic verileri yuklenemedi", ex);
            }
            finally
            {
                _bootDataReady = true;
                TrySendInitData();

                Logger.DoLog(Config.ActionTypes.AppStarted, _cachedSettings);
            }
        }

        /// <summary>
        ///     Rewrites the startup entry on every launch while the setting is on. It was only ever
        ///     written when the setting was saved, so an entry created before the tray argument
        ///     existed kept launching the visible window forever, with the setting still reading as
        ///     enabled. Writing it again is cheap and repairs entries altered outside the app.
        /// </summary>
        private void RepairStartupRegistration(Settings settings)
        {
            if (settings == null || !settings.StartWithWindows)
            {
                return;
            }

            StartWithWindows.AddStartup(Language.SettingsFormAddStartupAppName ?? Constants.AppName);
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
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            webView.CoreWebView2.DOMContentLoaded += OnDomContentLoaded;

            webView.CoreWebView2.Navigate("https://app.local/Index.html");
        }

        private async System.Threading.Tasks.Task InitializeWebViewSafeAsync()
        {
            try
            {
                await InitializeWebView();
            }
            catch (Exception ex)
            {
                ReportStartupError("Web arayuzu yuklenemedi", ex);
            }
        }

        private void OnDomContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (_webViewReady) return;
            _webViewReady = true;
            TrySendInitData();
        }

        private void InitializeLoadingOverlay(bool isDark)
        {
            _loadingLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold),
                ForeColor = isDark
                    ? System.Drawing.Color.FromArgb(160, 163, 180)
                    : System.Drawing.Color.FromArgb(95, 99, 112),
                Text = Language?.CommonLoading ?? "Yükleniyor..."
            };

            _loadingOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = isDark
                    ? System.Drawing.Color.FromArgb(26, 27, 46)
                    : System.Drawing.Color.FromArgb(240, 242, 245)
            };

            _loadingOverlay.Controls.Add(_loadingLabel);
            Controls.Add(_loadingOverlay);
            _loadingOverlay.Visible = true;
            _loadingOverlay.BringToFront();
        }

        private void ShowLoadingOverlay()
        {
            if (_loadingOverlay == null) return;
            _loadingLabel.Text = Language?.CommonLoading ?? "Yükleniyor...";
            _loadingOverlay.Visible = true;
            _loadingOverlay.BringToFront();
        }

        private void HideLoadingOverlay()
        {
            if (_loadingOverlay == null) return;
            _loadingOverlay.Visible = false;
        }

        private void TrySendInitData()
        {
            if (_initSent || !_webViewReady || !_bootDataReady) return;
            _initSent = true;
            SendInitData();
            TryDispatchPendingOpenNewActionModal();
            HideLoadingOverlay();
            StartSubWindowPrewarm();
            StartStartupUpdateCheck();
        }

        /// <summary>
        ///     Runs once per launch regardless of the interval, so a long-running install still
        ///     learns about a release published while it was closed.
        /// </summary>
        private void StartStartupUpdateCheck()
        {
            if (_startupUpdateCheckStarted || _cachedSettings == null || !_cachedSettings.UpdateCheckEnabled)
            {
                return;
            }

            _startupUpdateCheckStarted = true;
            _ = RunUpdateCheckAsync(isManual: false);
        }

        private void TryDispatchPendingOpenNewActionModal()
        {
            if (!_pendingOpenNewActionModal || !_initSent || !_webViewReady)
            {
                return;
            }

            _pendingOpenNewActionModal = false;
            PostMessage("openNewAction", new { });
        }

        private void StartSubWindowPrewarm()
        {
            if (_subWindowPrewarmStarted || IsApplicationExiting || IsDisposed) return;
            _subWindowPrewarmStarted = true;

            BeginInvoke(new Action(() =>
            {
                foreach (var pageName in _subWindowPrewarmPages)
                {
                    if (IsApplicationExiting || IsDisposed) break;
                    var win = GetOrCreateSubWindow(pageName);
                    win.PrewarmInBackground();
                }

                if (_cachedSettings?.IsCountdownNotifierEnabled == true)
                {
                    NotifySystem.PrewarmCountdownNotifier();
                }
            }));
        }

        private void StopSubWindowPrewarm()
        {
            _subWindowPrewarmStarted = true;
        }

        private void SendInitData()
        {
            if (!_webViewReady) return;

            var langDict = LanguagePayloadCache.Get(Language);
            var displayActions = GetTranslatedActions();

            var settingsObj = _cachedSettings ?? LoadSettings();
            var initData = new
            {
                language = langDict,
                actions = displayActions,
                settings = BuildSettingsPayload(settingsObj)
            };

            PostMessage("init", initData);
        }

        private object BuildSettingsPayload(Settings settings)
        {
            var resolved = settings ?? Config.SettingsINI.DefaulSettingFile();
            return new
            {
                logsEnabled = resolved.LogsEnabled,
                startWithWindows = resolved.StartWithWindows,
                runInTaskbarWhenClosed = resolved.RunInTaskbarWhenClosed,
                confirmExitOnProgramExit = resolved.ConfirmExitOnProgramExit,
                isCountdownNotifierEnabled = resolved.IsCountdownNotifierEnabled,
                countdownNotifierSeconds = resolved.CountdownNotifierSeconds,
                language = resolved.Language,
                theme = resolved.Theme,
                updateCheckEnabled = resolved.UpdateCheckEnabled,
                updateCheckInterval = UpdatePolicy.NormalizeInterval(resolved.UpdateCheckInterval),
                appVersion = BuildMetadata.Version,
                buildId = BuildMetadata.CommitId
            };
        }

        private List<Dictionary<string, object>> GetTranslatedActions()
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var act in ActionList)
            {
                var d = new Dictionary<string, object>
                {
                    ["triggerType"] = TranslateTrigger(act.TriggerType),
                    ["triggerTypeRaw"] = NormalizeTriggerTypeRaw(act.TriggerType),
                    ["actionType"] = TranslateAction(act.ActionType),
                    ["actionTypeRaw"] = act.ActionType ?? "",
                    ["value"] = act.Value ?? "",
                    ["valueUnit"] = TranslateUnit(act.ValueUnit),
                    ["valueUnitRaw"] = act.ValueUnit ?? "",
                    ["createdDate"] = act.CreatedDate ?? "",
                    ["isEnabled"] = act.IsEnabled
                };
                list.Add(d);
            }
            return list;
        }

        private string TranslateAction(string raw)
        {
            if (raw == Config.ActionTypes.LockComputer) return Language.MainCboxActionTypeItemLockComputer;
            if (raw == Config.ActionTypes.ShutdownComputer) return Language.MainCboxActionTypeItemShutdownComputer;
            if (raw == Config.ActionTypes.RestartComputer) return Language.MainCboxActionTypeItemRestartComputer;
            if (raw == Config.ActionTypes.LogOffWindows) return Language.MainCboxActionTypeItemLogOffWindows;
            if (raw == Config.ActionTypes.SleepComputer) return Language.MainCboxActionTypeItemSleepComputer;
            if (raw == Config.ActionTypes.TurnOffMonitor) return Language.MainCboxActionTypeItemTurnOffMonitor;
            return raw;
        }

        private string TranslateTrigger(string raw)
        {
            if (raw == Config.TriggerTypes.SystemIdle) return Language.MainCboxTriggerTypeItemSystemIdle;
            if (raw == Config.TriggerTypes.CertainTime) return Language.MainCboxTriggerTypeItemCertainTime;
            if (raw == Config.TriggerTypes.FromNow) return Language.MainCboxTriggerTypeItemFromNow;

            return raw;
        }

        private static string NormalizeTriggerTypeRaw(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string normalized = raw.Trim();
            if (normalized == "FromNow") return "FromNow";
            if (normalized == "SystemIdle") return "SystemIdle";
            if (normalized == "CertainTime") return "CertainTime";

            return normalized;
        }

        private string TranslateUnit(string raw)
        {
            if (raw == "seconds") return Language.MainTimeUnitSeconds ?? "Seconds";
            if (string.IsNullOrEmpty(raw)) return Language.MainTimeUnitMinutes ?? "Minutes";
            return raw;
        }

        private Settings LoadSettings()
        {
            return SettingsStorage.LoadOrDefault();
        }

        private void HandleSystemResume()
        {
            // Executed idle actions are deliberately not cleared here. A resume triggered by the
            // power button leaves the idle counter above the threshold, so re-arming on resume
            // made the action fire again immediately. Each action re-arms itself in DoAction once
            // the idle counter drops back below its own threshold.
            _lastObservedIdleTimeSec = null;
            NotifySystem.ResetIdleNotifications();
            Actions.TurnOff.RecordSystemResume();
        }

        private void ResolveConflictingEnabledActions()
        {
            bool changed = false;
            var acceptedEnabledActions = new List<ActionModel>();

            for (int index = 0; index < ActionList.Count; ++index)
            {
                ActionModel action = ActionList[index];
                if (action == null || !action.IsEnabled)
                {
                    continue;
                }

                if (ActionValidation.TryValidateActionForAdd(
                        action,
                        acceptedEnabledActions,
                        Language,
                        out _))
                {
                    acceptedEnabledActions.Add(action);
                    continue;
                }

                action.IsEnabled = false;
                changed = true;
            }

            if (changed)
            {
                WriteJsonToActionList();
            }
        }

        public void UpdateCachedSettings(Settings settings)
        {
            if (settings == null)
            {
                return;
            }

            _cachedSettings = settings;
            Logger.UpdateSettings(settings);
        }

        public void SyncSettingsAcrossUi(Settings settings, string previousLanguage, bool previousStartWithWindows)
        {
            if (settings == null)
            {
                return;
            }

            _cachedSettings = settings;
            Logger.UpdateSettings(settings);

            bool isDark = DetermineIfDark(settings.Theme);
            ContextMenuStripNotifyIcon.Renderer = new WindowsAutoPowerManager.Functions.ModernMenuRenderer(isDark);
            BackColor = isDark
                ? System.Drawing.Color.FromArgb(26, 27, 46)
                : System.Drawing.Color.FromArgb(240, 242, 245);

            if (settings.StartWithWindows != previousStartWithWindows)
            {
                if (settings.StartWithWindows)
                {
                    StartWithWindows.AddStartup(Language.SettingsFormAddStartupAppName ?? Constants.AppName);
                }
                else
                {
                    StartWithWindows.DeleteStartup(Language.SettingsFormAddStartupAppName ?? Constants.AppName);
                }
            }

            if (settings.IsCountdownNotifierEnabled)
            {
                NotifySystem.PrewarmCountdownNotifier();
            }

            if (!string.Equals(previousLanguage, settings.Language, StringComparison.Ordinal))
            {
                RefreshLanguageUI();
                return;
            }

            object settingsPayload = BuildSettingsPayload(settings);
            PostMessage("settingsLoaded", settingsPayload);
            PostMessage("themeChanged", settings.Theme ?? "system");
            BroadcastSubWindowSettings();
        }

        public Settings GetCachedSettingsOrDefault()
        {
            return _cachedSettings ?? LoadSettings();
        }

        private List<ActionModel> LoadActionList()
        {
            string path = AppContext.BaseDirectory + "\\ActionList.json";
            return ReadJsonFileOrDefault(path, new List<ActionModel>());
        }

        private static T ReadJsonFileOrDefault<T>(string path, T fallback)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return fallback;
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return fallback;
                }

                var parsed = JsonSerializer.Deserialize<T>(json);
                return parsed == null ? fallback : parsed;
            }
            catch
            {
                return fallback;
            }
        }

        private void ReportStartupError(string title, Exception ex)
        {
            if (_startupErrorShown) return;
            _startupErrorShown = true;

            if (_loadingOverlay != null)
            {
                _loadingOverlay.Visible = true;
                _loadingOverlay.BringToFront();
                _loadingLabel.Text = Language?.MessageTitleError ?? "Error";
            }

            MessageBox.Show(
                this,
                title + ".\r\n\r\nDetay: " + ex.Message,
                Language?.MessageTitleError ?? "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void PostMessage(string type, object data)
        {
            if (!_webViewReady || webView.CoreWebView2 == null) return;
            var msg = JsonSerializer.Serialize(new { type, data });
            webView.CoreWebView2.PostWebMessageAsJson(msg);
        }

        // =============== WebMessage Handler ===============

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string msgJson;
            try
            {
                msgJson = e.TryGetWebMessageAsString();
            }
            catch
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(msgJson))
            {
                return;
            }

            using var msg = JsonDocument.Parse(msgJson);
            if (!msg.RootElement.TryGetProperty("type", out JsonElement typeElement))
            {
                return;
            }

            string type = typeElement.GetString();
            JsonElement data = msg.RootElement.TryGetProperty("data", out JsonElement payload)
                ? payload
                : default;

            switch (type)
            {
                case "addAction":
                    HandleAddAction(data);
                    break;
                case "updateAction":
                    HandleUpdateAction(data);
                    break;
                case "deleteAction":
                    HandleDeleteAction(data);
                    break;
                case "toggleAction":
                    HandleToggleAction(data);
                    break;
                case "clearAllActions":
                    HandleClearAllActions();
                    break;
                case "saveSettings":
                    HandleSaveSettings(data);
                    break;
                case "checkUpdate":
                    _ = RunUpdateCheckAsync(isManual: true);
                    break;
                case "startUpdateDownload":
                    _ = RunUpdateDownloadAsync();
                    break;
                case "loadSettings":
                    HandleLoadSettings();
                    break;
                case "loadLogs":
                    HandleLoadLogs(data);
                    break;
                case "clearLogs":
                    HandleClearLogs();
                    break;
                case "getLanguageList":
                    HandleGetLanguageList();
                    break;
                case "openWindow":
                    string page = data.GetProperty("page").GetString();
                    if (page == "main")
                    {
                        ShowMainAndOpenNewActionModal();
                    }
                    else
                    {
                        OpenSubWindow(page);
                    }
                    break;
                case "openUrl":
                    string url = data.GetProperty("url").GetString();
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    break;
                case "pauseActions":
                    HandlePauseActions(data);
                    break;
                case "resumeActions":
                    HandleResumeActions();
                    break;
                case "exportSettingsConfig":
                    HandleExportSettingsConfig();
                    break;
                case "importSettingsConfig":
                    HandleImportSettingsConfig();
                    break;
                case "exitApp":
                    IsApplicationExiting = true;
                    StopSubWindowPrewarm();
                    CloseAllSubWindows();
                    Logger.DoLog(Config.ActionTypes.AppTerminated);
                    Application.ExitThread();
                    break;
            }
        }

        private void CloseAllSubWindows()
        {
            foreach (var sw in _subWindows.Values.ToList())
            {
                if (!sw.IsDisposed)
                {
                    sw.ForceClose();
                }
            }
        }

        private void HandleAddAction(JsonElement data)
        {
            if (ActionList.Count >= 5)
            {
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleWarn,
                    message = Language.MessageContentMaxActionWarn,
                    type = "warn",
                    duration = 2000
                });
                PostMessage("addActionResult", new { success = false });
                return;
            }

            if (!TryCreateActionModel(data, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), out ActionModel newAction))
            {
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleWarn,
                    message = Language.MessageContentActionChoose,
                    type = "warn",
                    duration = 2000
                });
                PostMessage("addActionResult", new { success = false });
                return;
            }

            if (!ActionValidation.TryValidateActionForAdd(newAction, ActionList, Language, out string validationMessage))
            {
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleWarn,
                    message = validationMessage,
                    type = "warn",
                    duration = 3000
                });
                PostMessage("addActionResult", new { success = false });
                return;
            }

            ActionList.Add(newAction);
            WriteJsonToActionList();

            PostMessage("showToast", new
            {
                title = Language.MessageTitleSuccess,
                message = Language.MessageContentActionCreated,
                type = "success",
                duration = 2000
            });
            PostMessage("addActionResult", new { success = true });
        }

        private void HandleUpdateAction(JsonElement data)
        {
            int index = data.GetProperty("index").GetInt32();
            if (index < 0 || index >= ActionList.Count)
            {
                PostMessage("updateActionResult", new { success = false });
                return;
            }

            string createdDate = ActionList[index]?.CreatedDate;
            if (string.IsNullOrWhiteSpace(createdDate))
            {
                createdDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            }

            if (!TryCreateActionModel(data, createdDate, out ActionModel updatedAction))
            {
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleWarn,
                    message = Language.MessageContentActionChoose,
                    type = "warn",
                    duration = 2000
                });
                PostMessage("updateActionResult", new { success = false });
                return;
            }

            if (!ActionValidation.TryValidateActionForAdd(
                    updatedAction,
                    ActionList.Where((_, actionIndex) => actionIndex != index),
                    Language,
                    out string validationMessage,
                    ActionList[index].IsEnabled))
            {
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleWarn,
                    message = validationMessage,
                    type = "warn",
                    duration = 3000
                });
                PostMessage("updateActionResult", new { success = false });
                return;
            }

            updatedAction.IsEnabled = ActionList[index].IsEnabled;
            ActionList[index] = updatedAction;
            WriteJsonToActionList();

            PostMessage("showToast", new
            {
                title = Language.MessageTitleSuccess,
                message = Language.MessageContentActionUpdated ?? Language.MessageContentActionCreated,
                type = "success",
                duration = 2000
            });
            PostMessage("updateActionResult", new { success = true });
        }

        private void HandleToggleAction(JsonElement data)
        {
            int index = data.GetProperty("index").GetInt32();
            if (index < 0 || index >= ActionList.Count) return;

            bool willEnable = !ActionList[index].IsEnabled;
            if (willEnable &&
                !ActionValidation.TryValidateActionForAdd(
                    ActionList[index],
                    ActionList.Where((_, actionIndex) => actionIndex != index),
                    Language,
                    out string validationMessage))
            {
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleWarn,
                    message = validationMessage,
                    type = "warn",
                    duration = 3000
                });
                return;
            }

            ActionList[index].IsEnabled = willEnable;
            WriteJsonToActionList();
        }

        private static bool TryCreateActionModel(JsonElement data, string createdDate, out ActionModel action)
        {
            action = null;

            string actionType = data.GetProperty("actionType").GetString();
            string triggerType = data.GetProperty("triggerType").GetString();

            if (string.IsNullOrWhiteSpace(actionType) ||
                string.IsNullOrWhiteSpace(triggerType) ||
                actionType == "0" ||
                triggerType == "0")
            {
                return false;
            }

            var parsedAction = new ActionModel
            {
                CreatedDate = createdDate,
                ActionType = actionType
            };

            try
            {
                if (triggerType == "FromNow")
                {
                    parsedAction.TriggerType = Config.TriggerTypes.FromNow;

                    if (!double.TryParse(data.GetProperty("value").GetString(), out double inputValue) || inputValue <= 0)
                    {
                        return false;
                    }

                    if (!int.TryParse(data.GetProperty("timeUnit").GetString(), out int unitIdx))
                    {
                        unitIdx = 1;
                    }

                    DateTime targetTime;
                    if (unitIdx == 0) targetTime = DateTime.Now.AddSeconds(inputValue);
                    else if (unitIdx == 2) targetTime = DateTime.Now.AddHours(inputValue);
                    else targetTime = DateTime.Now.AddMinutes(inputValue);
                    parsedAction.Value = targetTime.ToString("dd.MM.yyyy HH:mm:ss");
                }
                else if (triggerType == "SystemIdle")
                {
                    parsedAction.TriggerType = Config.TriggerTypes.SystemIdle;

                    if (!int.TryParse(data.GetProperty("value").GetString(), out int inputValue) || inputValue <= 0)
                    {
                        return false;
                    }

                    if (!int.TryParse(data.GetProperty("timeUnit").GetString(), out int unitIdx))
                    {
                        unitIdx = 1;
                    }

                    int valueInSeconds;
                    if (unitIdx == 0) valueInSeconds = inputValue;
                    else if (unitIdx == 2) valueInSeconds = inputValue * 3600;
                    else valueInSeconds = inputValue * 60;

                    parsedAction.Value = valueInSeconds.ToString();
                    parsedAction.ValueUnit = "seconds";
                }
                else if (triggerType == "CertainTime")
                {
                    parsedAction.TriggerType = Config.TriggerTypes.CertainTime;
                    string timeStr = data.GetProperty("time").GetString();

                    parsedAction.Value = !string.IsNullOrEmpty(timeStr)
                        ? timeStr + ":00"
                        : DateTime.Now.AddMinutes(1).ToString("HH:mm:00");
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            action = parsedAction;
            return true;
        }

        private void HandleDeleteAction(JsonElement data)
        {
            int index = data.GetProperty("index").GetInt32();
            if (index >= 0 && index < ActionList.Count)
            {
                ActionList.RemoveAt(index);
                WriteJsonToActionList();

                PostMessage("showToast", new
                {
                    title = Language.MessageTitleSuccess,
                    message = Language.MessageContentActionDeleted,
                    type = "success",
                    duration = 2000
                });
            }
        }

        private void HandleClearAllActions()
        {
            ActionList.Clear();
            WriteJsonToActionList();

            PostMessage("showToast", new
            {
                title = Language.MessageTitleSuccess,
                message = Language.MessageContentActionAllDeleted,
                type = "success",
                duration = 2000
            });
        }

        private void HandleSaveSettings(JsonElement data)
        {
            bool confirmExitOnProgramExit = true;
            if (data.TryGetProperty("confirmExitOnProgramExit", out JsonElement confirmExitElement))
            {
                if (confirmExitElement.ValueKind == JsonValueKind.True ||
                    confirmExitElement.ValueKind == JsonValueKind.False)
                {
                    confirmExitOnProgramExit = confirmExitElement.GetBoolean();
                }
                else if (confirmExitElement.ValueKind == JsonValueKind.String &&
                         bool.TryParse(confirmExitElement.GetString(), out bool parsedConfirmExit))
                {
                    confirmExitOnProgramExit = parsedConfirmExit;
                }
            }

            var newSettings = new Settings
            {
                LogsEnabled = data.GetProperty("logsEnabled").GetBoolean(),
                StartWithWindows = data.GetProperty("startWithWindows").GetBoolean(),
                RunInTaskbarWhenClosed = data.GetProperty("runInTaskbarWhenClosed").GetBoolean(),
                ConfirmExitOnProgramExit = confirmExitOnProgramExit,
                IsCountdownNotifierEnabled = data.GetProperty("isCountdownNotifierEnabled").GetBoolean(),
                CountdownNotifierSeconds = data.GetProperty("countdownNotifierSeconds").GetInt32(),
                Language = data.GetProperty("language").GetString(),
                Theme = data.GetProperty("theme").GetString(),
                UpdateCheckEnabled = JsonPayload.ReadBoolean(data, "updateCheckEnabled", true),
                UpdateCheckInterval = UpdatePolicy.NormalizeInterval(JsonPayload.ReadString(data, "updateCheckInterval")),

                // Carried over deliberately: the payload has no such field, so rebuilding the
                // settings from it would reset the schedule on every save and the interval would
                // never elapse.
                LastUpdateCheckUtc = _cachedSettings?.LastUpdateCheckUtc
            };

            string currentLang = _cachedSettings?.Language ?? "auto";
            bool previousStartWithWindows = _cachedSettings?.StartWithWindows ?? false;
            SettingsStorage.Save(newSettings);
            SyncSettingsAcrossUi(newSettings, currentLang, previousStartWithWindows);

            PostMessage("showToast", new
            {
                title = Language.MessageTitleSuccess,
                message = Language.MessageContentSettingsSaved,
                type = "success",
                duration = 2000
            });
        }

        private void HandleLoadSettings()
        {
            var settingsObj = _cachedSettings ?? LoadSettings();
            PostMessage("settingsLoaded", BuildSettingsPayload(settingsObj));
        }

        // =============== Update Check ===============

        /// <summary>
        ///     Runs from the one-second tick. The schedule is stored in settings rather than kept in
        ///     memory, so closing the app does not restart the interval.
        /// </summary>
        private void MaybeRunScheduledUpdateCheck(DateTime now)
        {
            if (_updateCheckRunning || _cachedSettings == null || !_cachedSettings.UpdateCheckEnabled)
            {
                return;
            }

            if (!UpdatePolicy.IsCheckDue(
                    _cachedSettings.LastUpdateCheckUtc,
                    now.ToUniversalTime(),
                    _cachedSettings.UpdateCheckInterval))
            {
                return;
            }

            _ = RunUpdateCheckAsync(isManual: false);
        }

        private async System.Threading.Tasks.Task RunUpdateCheckAsync(bool isManual)
        {
            if (_updateCheckRunning)
            {
                return;
            }

            _updateCheckRunning = true;
            try
            {
                UpdateInfo info = await UpdateChecker.CheckAsync();
                RecordUpdateCheckTime();

                if (info.IsAvailable)
                {
                    _pendingUpdate = info;
                    PostMessage("updateAvailable", new
                    {
                        current = info.CurrentVersion,
                        latest = info.LatestVersion,
                        assetName = info.AssetName,
                        releaseUrl = info.ReleaseUrl
                    });
                    return;
                }

                // A scheduled check stays silent; only an explicit request reports the outcome.
                if (isManual)
                {
                    PostMessage("updateStatus", new
                    {
                        reason = info.Reason,
                        current = info.CurrentVersion,
                        latest = info.LatestVersion
                    });
                }
            }
            finally
            {
                _updateCheckRunning = false;
            }
        }

        private void RecordUpdateCheckTime()
        {
            if (_cachedSettings == null)
            {
                return;
            }

            _cachedSettings.LastUpdateCheckUtc = DateTime.UtcNow;
            SettingsStorage.Save(_cachedSettings);
        }

        private async System.Threading.Tasks.Task RunUpdateDownloadAsync()
        {
            UpdateInfo info = _pendingUpdate;
            if (info == null || _updateDownloadRunning)
            {
                return;
            }

            _updateDownloadRunning = true;
            try
            {
                var progress = new Progress<UpdateDownloadProgress>(value => PostMessage("updateProgress", new
                {
                    received = value.Received,
                    total = value.Total
                }));

                string installerPath = await UpdateChecker.DownloadInstallerAsync(
                    info, progress, System.Threading.CancellationToken.None);

                if (!UpdateChecker.StartInstaller(installerPath))
                {
                    PostMessage("updateStatus", new { reason = UpdateInfo.ReasonError });
                    return;
                }

                PostMessage("updateStatus", new { reason = "installing" });

                // The installer cannot replace a running executable, so the app has to step aside.
                // It relaunches the app once it is done.
                IsApplicationExiting = true;
                Logger.DoLog(Config.ActionTypes.AppTerminated, _cachedSettings);
                Logger.Flush();
                Application.Exit();
            }
            catch (Exception)
            {
                PostMessage("updateStatus", new { reason = UpdateInfo.ReasonError });
            }
            finally
            {
                _updateDownloadRunning = false;
            }
        }

        private void HandleLoadLogs()
        {
            HandleLoadLogs(default);
        }

        private void HandleLoadLogs(JsonElement data)
        {
#if DEBUG
            long perfStart = DebugPerformanceTracker.Start();
#endif
            int limit = ResolveLogsLimit(data);
            var rawLogs = Logger.GetRecentLogs(limit);
            var logs = rawLogs.Select(l => new
            {
                id = l.Id ?? "",
                actionExecutedDate = l.ActionExecutedDate,
                actionType = TranslateLogAction(l.ActionType),
                actionTypeRaw = l.ActionType
            }).ToList();

            PostMessage("logsLoaded", logs);
#if DEBUG
            DebugPerformanceTracker.Record("MainForm.HandleLoadLogs", perfStart);
#endif
        }

        private static int ResolveLogsLimit(JsonElement data)
        {
            const int defaultLimit = 250;
            const int maxLimit = Logger.MaxLogCount;
            int limit = defaultLimit;

            if (data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("limit", out JsonElement limitElement))
            {
                if (limitElement.ValueKind == JsonValueKind.Number &&
                    limitElement.TryGetInt32(out int numericLimit))
                {
                    limit = numericLimit;
                }
                else if (limitElement.ValueKind == JsonValueKind.String &&
                         int.TryParse(limitElement.GetString(), out int stringLimit))
                {
                    limit = stringLimit;
                }
            }

            if (limit < 1)
            {
                return 1;
            }

            if (limit > maxLimit)
            {
                return maxLimit;
            }

            return limit;
        }

        private string TranslateLogAction(string raw)
        {
            if (raw == Config.ActionTypes.LockComputer) return Language.LogViewerFormLockComputer;
            if (raw == Config.ActionTypes.LockComputerManually) return Language.LogViewerFormLockComputerManually;
            if (raw == Config.ActionTypes.UnlockComputer) return Language.LogViewerFormUnlockComputer;
            if (raw == Config.ActionTypes.ShutdownComputer) return Language.LogViewerFormShutdownComputer;
            if (raw == Config.ActionTypes.RestartComputer) return Language.LogViewerFormRestartComputer;
            if (raw == Config.ActionTypes.LogOffWindows) return Language.LogViewerFormLogOffWindows;
            if (raw == Config.ActionTypes.SleepComputer) return Language.LogViewerFormSleepComputer;
            if (raw == Config.ActionTypes.TurnOffMonitor) return Language.LogViewerFormTurnOffMonitor;
            // Languages whose stored file predates this key fall back to the raw type.
            if (raw == Config.ActionTypes.TurnOffMonitorFailed) return Language.LogViewerFormTurnOffMonitorFailed ?? raw;
            if (raw == Config.ActionTypes.AppStarted) return Language.LogViewerFormAppStarted;
            if (raw == Config.ActionTypes.AppTerminated) return Language.LogViewerFormAppTerminated;
            return raw;
        }

        private void HandleClearLogs()
        {
            Logger.Clear();

            PostMessage("showToast", new
            {
                title = Language.MessageTitleSuccess,
                message = Language.MessageContentClearedLogs,
                type = "success",
                duration = 2000
            });
        }

        private void HandleGetLanguageList()
        {
            var list = new List<object>();
            list.Add(new { LangCode = "auto", langName = (Language.SettingsFormComboboxAutoLang ?? "Auto") });

            foreach (var entry in LanguageSelector.GetLanguageNames())
            {
                list.Add(new { LangCode = entry.LangCode, langName = entry.LangName });
            }

            PostMessage("languageList", list);
        }

        private void HandleExportSettingsConfig()
        {
            using var dialog = new SaveFileDialog
            {
                Title = Language.SettingsFormDialogTitleExportConfig ?? "Export Configuration",
                Filter = GetConfigFileDialogFilter(),
                DefaultExt = "conf",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = AppDataTransfer.BuildDefaultFileName(),
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (!ExportDataToFile(dialog.FileName, out string errorMessage))
            {
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleError,
                    message = (Language.SettingsFormConfigExportFailedPrefix ?? "Export failed:") + " " + errorMessage,
                    type = "error",
                    duration = 3500
                });
                return;
            }

            PostMessage("showToast", new
            {
                title = Language.MessageTitleSuccess,
                message = Language.SettingsFormConfigExportSuccess ?? "Configuration exported successfully.",
                type = "success",
                duration = 2200
            });
        }

        private void HandleImportSettingsConfig()
        {
            using var dialog = new OpenFileDialog
            {
                Title = Language.SettingsFormDialogTitleImportConfig ?? "Import Configuration",
                Filter = GetConfigFileDialogFilter(),
                DefaultExt = "conf",
                CheckFileExists = true,
                Multiselect = false,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (!ImportDataFromFile(dialog.FileName, out string errorMessage))
            {
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleError,
                    message = (Language.SettingsFormConfigImportFailedPrefix ?? "Import failed:") + " " + errorMessage,
                    type = "error",
                    duration = 3500
                });
                return;
            }

            PostMessage("showToast", new
            {
                title = Language.MessageTitleSuccess,
                message = Language.SettingsFormConfigImportSuccess ?? "Configuration imported successfully.",
                type = "success",
                duration = 2200
            });
        }

        private string GetConfigFileDialogFilter()
        {
            string configFilesLabel = Language.SettingsFormDialogFilterConfigFiles ?? "Configuration Files";
            string allFilesLabel = Language.SettingsFormDialogFilterAllFiles ?? "All Files";
            return $"{configFilesLabel} (*.conf)|*.conf|{allFilesLabel} (*.*)|*.*";
        }

        public bool ExportDataToFile(string filePath, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                Settings settingsSnapshot = _cachedSettings ?? LoadSettings();
                List<ActionModel> actionsSnapshot = ActionList
                    .Where(action => action != null)
                    .ToList();
                List<LogSystem> logsSnapshot = Logger.GetRecentLogs(Logger.MaxLogCount);
                logsSnapshot.Reverse();

                AppDataTransfer.ExportToFile(
                    filePath,
                    settingsSnapshot,
                    actionsSnapshot,
                    logsSnapshot);

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public bool ImportDataFromFile(string filePath, out string errorMessage)
        {
            if (!AppDataTransfer.TryImportFromFile(
                    filePath,
                    out Settings importedSettings,
                    out List<ActionModel> importedActions,
                    out List<LogSystem> importedLogs,
                    out errorMessage))
            {
                return false;
            }

            try
            {
                ApplyImportedSettings(importedSettings);

                ActionList = (importedActions ?? new List<ActionModel>())
                    .Where(action => action != null)
                    .ToList();
                WriteJsonToActionList();

                Logger.ReplaceLogs(importedLogs ?? new List<LogSystem>());
                HandleLoadSettings();
                HandleLoadLogs();
                BroadcastSubWindowSettings();
                BroadcastSubWindowLogs();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private void ApplyImportedSettings(Settings importedSettings)
        {
            var resolved = importedSettings ?? Config.SettingsINI.DefaulSettingFile();

            string currentLang = _cachedSettings?.Language ?? "auto";
            bool previousStartWithWindows = _cachedSettings?.StartWithWindows ?? false;

            SettingsStorage.Save(resolved);
            resolved = SettingsStorage.LoadOrDefault();
            _cachedSettings = resolved;
            Logger.UpdateSettings(resolved);

            bool isDark = DetermineIfDark(resolved.Theme);
            ContextMenuStripNotifyIcon.Renderer = new WindowsAutoPowerManager.Functions.ModernMenuRenderer(isDark);
            BackColor = isDark
                ? System.Drawing.Color.FromArgb(26, 27, 46)
                : System.Drawing.Color.FromArgb(240, 242, 245);

            if (resolved.StartWithWindows != previousStartWithWindows)
            {
                if (resolved.StartWithWindows)
                {
                    StartWithWindows.AddStartup(Language.SettingsFormAddStartupAppName ?? Constants.AppName);
                }
                else
                {
                    StartWithWindows.DeleteStartup(Language.SettingsFormAddStartupAppName ?? Constants.AppName);
                }
            }

            if (resolved.IsCountdownNotifierEnabled)
            {
                NotifySystem.PrewarmCountdownNotifier();
            }

            if (!string.Equals(currentLang, resolved.Language, StringComparison.Ordinal))
            {
                RefreshLanguageUI();
            }
        }

        private void BroadcastSubWindowSettings()
        {
            Settings settingsObj = _cachedSettings ?? LoadSettings();
            foreach (SubWindow subWindow in _subWindows.Values.ToList())
            {
                if (!subWindow.IsDisposed)
                {
                    subWindow.BroadcastSettings(settingsObj);
                }
            }
        }

        private void BroadcastSubWindowLogs()
        {
            var rawLogs = Logger.GetRecentLogs(250);
            var logs = rawLogs.Select(l => new
            {
                id = l.Id ?? "",
                actionExecutedDate = l.ActionExecutedDate,
                actionType = TranslateLogAction(l.ActionType),
                actionTypeRaw = l.ActionType
            }).ToList();

            foreach (SubWindow subWindow in _subWindows.Values.ToList())
            {
                if (!subWindow.IsDisposed)
                {
                    subWindow.BroadcastLogs(logs);
                }
            }
        }

        // =============== Pause / Resume ===============

        private void HandlePauseActions(JsonElement data)
        {
            int minutes = data.GetProperty("minutes").GetInt32();
            _isPaused = true;
            _pauseUntilTime = DateTime.Now.AddMinutes(minutes);

            PostMessage("showToast", new
            {
                title = Language.MessageTitleSuccess,
                message = Language.PausePaused ?? "Actions paused successfully",
                type = "info",
                duration = 2000
            });

            SendPauseStatus();
        }

        private void HandleResumeActions()
        {
            _isPaused = false;
            _pauseUntilTime = null;

            PostMessage("showToast", new
            {
                title = Language.MessageTitleSuccess,
                message = Language.PauseResumed ?? "Actions resumed",
                type = "success",
                duration = 2000
            });

            SendPauseStatus();
        }

        private void SendPauseStatus()
        {
            var status = new
            {
                isPaused = _isPaused,
                pauseUntil = _pauseUntilTime?.ToString("dd.MM.yyyy HH:mm:ss") ?? "",
                remainingSeconds = _isPaused && _pauseUntilTime.HasValue
                    ? Math.Max(0, (_pauseUntilTime.Value - DateTime.Now).TotalSeconds)
                    : 0
            };
            PostMessage("pauseStatus", status);
        }


        // =============== Action List Persistence ===============

        public void WriteJsonToActionList()
        {
#if DEBUG
            long perfStart = DebugPerformanceTracker.Start();
#endif
            JsonWriter.WriteJson(AppContext.BaseDirectory + "\\ActionList.json", true, ActionList);
            _scheduler.RebuildRuntimeStates(ActionList);
            CleanupActionExecutionState();
            RefreshActionsInUI();
#if DEBUG
            DebugPerformanceTracker.Record("MainForm.WriteJsonToActionList", perfStart);
#endif
        }

        private void CleanupActionExecutionState()
        {
            _scheduler.CleanupState(ActionList);
            NotifySystem.CleanupState(ActionList);
        }

        public void RefreshUIAfterToggle()
        {
            WriteJsonToActionList();
            _scheduler.RebuildRuntimeStates(ActionList);
            RefreshActionsInUI();
        }

        private void RefreshActionsInUI()
        {
            PostMessage("refreshActions", GetTranslatedActions());

            // Broadcast to open sub-windows
            foreach (var sw in _subWindows.Values.ToList())
            {
                if (!sw.IsDisposed)
                    sw.BroadcastRefreshActions();
            }
        }

        public void OpenSubWindow(string pageName)
        {
            var win = GetOrCreateSubWindow(pageName);
            win.ShowForUser();
            win.Focus();
        }

        private string GetSubWindowTitle(string pageName)
        {
            switch (pageName)
            {
                case "main":
                    return Language.MainGroupboxNewAction ?? "Actions";
                case "settings":
                    return Language.SettingsFormName ?? "Settings";
                case "logs":
                    return Language.LogViewerFormName ?? "Logs";
                case "help":
                    return Language.HelpMenuItem ?? "Help";
                case "about":
                    return Language.AboutMenuItem ?? "About";
                default:
                    return Language.MainFormName ?? Constants.AppName;
            }
        }

        private SubWindow GetOrCreateSubWindow(string pageName)
        {
            if (_subWindows.ContainsKey(pageName) && !_subWindows[pageName].IsDisposed)
            {
                return _subWindows[pageName];
            }

            var win = new SubWindow(pageName, GetSubWindowTitle(pageName));
            win.FormBorderStyle = FormBorderStyle.FixedSingle;
            win.MaximizeBox = false;

            System.Drawing.Size targetClientSize;
            switch (pageName)
            {
                case "help":
                    // Keep help layout always side-by-side.
                    targetClientSize = new System.Drawing.Size(1180, 700);
                    break;
                case "settings":
                    // Requested: +80px width, +120px height for settings window.
                    targetClientSize = new System.Drawing.Size(690, 640);
                    break;
                case "logs":
                    // Keep logs window exactly the same size as the main window.
                    targetClientSize = this.ClientSize;
                    break;
                case "about":
                    // Requested: +90px height for about window.
                    targetClientSize = new System.Drawing.Size(610, 610);
                    break;
                default:
                    targetClientSize = win.ClientSize;
                    break;
            }

            win.ClientSize = targetClientSize;
            win.MinimumSize = targetClientSize;
            win.MaximumSize = targetClientSize;

            _subWindows[pageName] = win;

            win.FormClosed += (s, args) =>
            {
                _subWindows.Remove(pageName);
            };

            return win;
        }

        // =============== Timer & Action Execution ===============

        private void TimerTick(object sender, EventArgs e)
        {
#if DEBUG
            long perfStart = DebugPerformanceTracker.Start();
#endif
            DateTime now = DateTime.Now;

            // Update time in UI
            string timeText = (Language.MainStatusBarCurrentTime ?? "Time") + " : " + now + "  |  Build Id: " + BuildMetadata.CommitId;
            PostMessage("updateTime", timeText);

            // Check pause expiration
            if (_isPaused && _pauseUntilTime.HasValue && now >= _pauseUntilTime.Value)
            {
                _isPaused = false;
                _pauseUntilTime = null;
                SendPauseStatus();
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleInfo ?? "Info",
                    message = Language.PauseResumed ?? "Actions resumed",
                    type = "info",
                    duration = 2000
                });
            }

            // Send pause status every tick for countdown display
            if (_isPaused)
            {
                SendPauseStatus();
#if DEBUG
                DebugPerformanceTracker.Record("MainForm.TimerTick", perfStart);
#endif
                return;
            }

            uint idleTimeSec = SystemIdleDetector.GetLastInputTime();
            bool hadRecentUserActivity = _lastObservedIdleTimeSec.HasValue &&
                                         idleTimeSec < _lastObservedIdleTimeSec.Value;
            _lastObservedIdleTimeSec = idleTimeSec;

            if (hadRecentUserActivity)
            {
                Actions.TurnOff.RecordPotentialWakeInteraction();
            }

            if (idleTimeSec == 0 || hadRecentUserActivity)
            {
                NotifySystem.ResetIdleNotifications();
            }

            if (_cachedSettings == null)
            {
                _cachedSettings = Config.SettingsINI.DefaulSettingFile();
                Logger.UpdateSettings(_cachedSettings);
            }

            Settings runtimeSettings = _cachedSettings;
            bool requiresPersist = false;
            List<int> removeIndices = null;

            for (int index = 0; index < ActionList.Count; ++index)
            {
                ActionModel action = ActionList[index];
                if (!action.IsEnabled) continue;

                bool skipCertainTimeAction = IsSkippedCertainTimeAction;
                ActionExecutionResult actionResult = _scheduler.Decide(
                    action,
                    idleTimeSec,
                    now,
                    ref skipCertainTimeAction);
                IsSkippedCertainTimeAction = skipCertainTimeAction;

                // The scheduler decides and this loop performs, so the ordering against the
                // notification below stays exactly as it was when both lived in DoAction.
                if ((actionResult & ActionExecutionResult.Executed) != 0)
                {
                    Actions.DoActionByTypes(action);
                }

                if ((actionResult & ActionExecutionResult.RemoveAction) != 0)
                {
                    if (removeIndices == null)
                    {
                        removeIndices = new List<int>();
                    }

                    removeIndices.Add(index);
                }

                if ((actionResult & ActionExecutionResult.NeedsPersist) != 0)
                {
                    requiresPersist = true;
                }

                if ((actionResult & ActionExecutionResult.RemoveAction) == 0)
                {
                    NotifySystem.ShowNotification(action, idleTimeSec, runtimeSettings, now);
                }
            }

            if (removeIndices != null)
            {
                for (int i = removeIndices.Count - 1; i >= 0; --i)
                {
                    ActionList.RemoveAt(removeIndices[i]);
                }

                requiresPersist = true;
            }

            if (requiresPersist)
            {
                WriteJsonToActionList();
            }

            MaybeRunScheduledUpdateCheck(now);

#if DEBUG
            DebugPerformanceTracker.Record("MainForm.TimerTick", perfStart);
#endif
        }

        // =============== System Tray & Window Events ===============

        public void ShowMain()
        {
            _startMinimizedToTray = false;
            ShowInTaskbar = true;
            Show();

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            BringToFront();
            Activate();
        }

        private void ShowMainAndOpenNewActionModal()
        {
            ShowMain();
            _pendingOpenNewActionModal = true;
            TrySendInitData();
            TryDispatchPendingOpenNewActionModal();
        }

        private void NotifyIconMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowMain();
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopSubWindowPrewarm();

            Settings = _cachedSettings ?? LoadSettings();
            if (Settings.RunInTaskbarWhenClosed)
            {
                e.Cancel = true;
                Hide();
            }

            if (!e.Cancel)
            {
                IsApplicationExiting = true;
                CloseAllSubWindows();
            }
        }

        private void mainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logger.DoLog(Config.ActionTypes.AppTerminated);
        }

        private void exitTheProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var settingsObj = _cachedSettings ?? LoadSettings();
            bool askConfirmation = settingsObj?.ConfirmExitOnProgramExit ?? true;

            if (askConfirmation)
            {
                var result = MessageBox.Show(
                    this,
                    Language.MessageContentConfirmExitProgram ?? "Are you sure you want to exit the program?",
                    Language.MessageTitleWarn ?? "Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            IsApplicationExiting = true;
            StopSubWindowPrewarm();
            CloseAllSubWindows();
            Logger.DoLog(Config.ActionTypes.AppTerminated);
            Application.ExitThread();
        }

        private void addNewActionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowMainAndOpenNewActionModal();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenSubWindow("settings");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenSubWindow("about");
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenSubWindow("help");
        }

        private void showTheLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenSubWindow("logs");
        }

        // =============== Language Hot-Reload ===============

        public void RefreshLanguageUI()
        {
            Language = LanguageSelector.LanguageFile();
            LanguagePayloadCache.Invalidate();

            // Update native UI texts
            Text = Language.MainFormName;
            NotifyIconMain.Text = Language.MainFormName + " " + Language.NotifyIconMain;

            ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.AddNewAction].Text =
                Language.ContextMenuStripNotifyIconAddNewAction;
            ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.ExitTheProgram].Text =
                Language.ContextMenuStripNotifyIconExitProgram;
            ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.Settings].Text =
                Language.ContextMenuStripNotifyIconShowSettings;
            ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.ShowLogs].Text =
                Language.ContextMenuStripNotifyIconShowLogs;
            ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.Help].Text =
                Language.ContextMenuStripNotifyIconShowHelp ?? "Help";
            ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.About].Text =
                Language.AboutMenuItem ?? "About";

            // Update sub-window titles
            foreach (var kvp in _subWindows)
            {
                if (!kvp.Value.IsDisposed)
                {
                    kvp.Value.Text = GetSubWindowTitle(kvp.Key);
                }
            }

            // Re-send full init data to main WebView and all sub-windows
            SendInitData();
            BroadcastSubWindowFullRefresh();
        }

        private void BroadcastSubWindowFullRefresh()
        {
            var langDict = LanguagePayloadCache.Get(Language);
            var displayActions = GetTranslatedActions();
            var settingsObj = _cachedSettings ?? LoadSettings();

            foreach (SubWindow subWindow in _subWindows.Values.ToList())
            {
                if (!subWindow.IsDisposed)
                {
                    subWindow.BroadcastFullRefresh(langDict, displayActions, BuildSettingsPayload(settingsObj));
                }
            }
        }

        // =============== Theme Helpers ===============

        private static bool ReadCachedThemeIsDark()
        {
            try
            {
                string path = SettingsStorage.SettingsPath;
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("Theme", out JsonElement themeEl))
                        {
                            string theme = themeEl.GetString();
                            if (theme == "dark") return true;
                            if (theme == "light") return false;
                        }
                    }
                }
            }
            catch { }
            return IsSystemDarkTheme();
        }

        private static bool IsSystemDarkTheme()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("AppsUseLightTheme");
                        if (val != null) return (int)val == 0;
                    }
                }
            }
            catch { }
            return true;
        }

        private bool DetermineIfDark(string theme)
        {
            if (theme == "dark") return true;
            if (theme == "light") return false;
            return IsSystemDarkTheme();
        }
    }
}
