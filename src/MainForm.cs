using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private Timer _loadingDelayTimer;
        private const int LoadingOverlayDelayMs = 350;
        private readonly string[] _subWindowPrewarmPages = { "settings", "logs", "help", "about" };
        private readonly HashSet<string> _executedIdleActionKeys = new HashSet<string>();
        private readonly Dictionary<string, bool> _bluetoothReachabilityByActionKey =
            new Dictionary<string, bool>();
        private readonly Dictionary<string, DateTime> _certainTimeLastExecutionDates = new Dictionary<string, DateTime>();
        private bool _subWindowPrewarmStarted;
        private bool _startupErrorShown;
        private readonly Dictionary<string, ActionRuntimeState> _actionRuntimeStates =
            new Dictionary<string, ActionRuntimeState>();
        private int _lastBluetoothDiscoveryVersion = -1;

        [Flags]
        private enum ActionExecutionResult
        {
            None = 0,
            Executed = 1,
            RemoveAction = 2,
            NeedsPersist = 4
        }

        private sealed class ActionRuntimeState
        {
            public string ExecutionKey;
            public uint IdleSeconds;
            public bool HasIdleSeconds;
            public DateTime FromNowTarget;
            public bool HasFromNowTarget;
            public TimeSpan CertainTimeOfDay;
            public bool HasCertainTime;
            public ulong BluetoothAddress;
        }

        public MainForm()
        {
            InitializeComponent();
            ApplyExecutableIcon();
            InitializeLoadingOverlay();
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

        protected override void OnLoad(EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();

            foreach (string arg in args)
            {
                if (arg == "-runInTaskBar" && RunInTaskbarCounter <= 0)
                {
                    ++RunInTaskbarCounter;
                    Hide();
                    ShowInTaskbar = false;
                }
            }

            base.OnLoad(e);
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
                    if (!TryParseFromNowValue(action, out DateTime actionDate) || now > actionDate)
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
                RebuildActionRuntimeStates();
            }
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            ShowLoadingOverlay();

            // First paint as fast as possible, then initialize heavy components.
            BeginInvoke(new Action(() =>
            {
                CreateWebViewControl();
                _ = InitializeWebViewSafeAsync();
            }));

            Text = Language.MainFormName;
            NotifyIconMain.Text = Language.MainFormName + " " + Language.NotifyIconMain;
            BeginInvoke(new Action(InitializeRuntimeState));
        }

        private void CreateWebViewControl()
        {
            if (webView != null) return;

            webView = new WebView2
            {
                AllowExternalDrop = false,
                Dock = DockStyle.Fill,
                Name = "webView",
                ZoomFactor = 1D,
                TabIndex = 0
            };

            webViewHost.Controls.Add(webView);
        }

        private void InitializeRuntimeState()
        {
            try
            {
                DetectScreen.ManuelLockingActionLogger();
                ActionList = LoadActionList();
                RebuildActionRuntimeStates();

                DeleteExpriedAction();
                EnsureBluetoothMonitoring();

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

        private void InitializeLoadingOverlay()
        {
            _loadingDelayTimer = new Timer
            {
                Interval = LoadingOverlayDelayMs
            };
            _loadingDelayTimer.Tick += (s, e) =>
            {
                _loadingDelayTimer.Stop();
                if (!_initSent && _loadingOverlay != null)
                {
                    _loadingOverlay.Visible = true;
                    _loadingOverlay.BringToFront();
                }
            };

            _loadingLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(95, 99, 112),
                Text = Language?.CommonLoading ?? "Yükleniyor..."
            };

            _loadingOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(240, 242, 245)
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

        private void TrySendInitData()
        {
            if (_initSent || !_webViewReady || !_bootDataReady) return;
            _initSent = true;
            SendInitData();
            HideLoadingOverlay();
            StartSubWindowPrewarm();
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
                isCountdownNotifierEnabled = resolved.IsCountdownNotifierEnabled,
                countdownNotifierSeconds = resolved.CountdownNotifierSeconds,
                language = resolved.Language,
                theme = resolved.Theme,
                bluetoothThresholdSeconds = resolved.BluetoothThresholdSeconds > 0 ? resolved.BluetoothThresholdSeconds : 5,
                appVersion = BuildInfo.Version,
                buildId = BuildInfo.CommitId
            };
        }

        private List<Dictionary<string, string>> GetTranslatedActions()
        {
            var list = new List<Dictionary<string, string>>();
            foreach (var act in ActionList)
            {
                var d = new Dictionary<string, string>
                {
                    ["triggerType"] = TranslateTrigger(act.TriggerType),
                    ["triggerTypeRaw"] = NormalizeTriggerTypeRaw(act.TriggerType),
                    ["actionType"] = TranslateAction(act.ActionType),
                    ["actionTypeRaw"] = act.ActionType ?? "",
                    ["value"] = act.Value ?? "",
                    ["valueUnit"] = TranslateUnit(act.ValueUnit),
                    ["valueUnitRaw"] = act.ValueUnit ?? "",
                    ["createdDate"] = act.CreatedDate ?? ""
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
            if (raw == Config.TriggerTypes.BluetoothNotReachable) return Language.MainCboxTriggerTypeItemBluetoothNotReachable;
            return raw;
        }

        private static string NormalizeTriggerTypeRaw(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string normalized = raw.Trim();
            if (normalized == "FromNow") return "FromNow";
            if (normalized == "SystemIdle") return "SystemIdle";
            if (normalized == "CertainTime") return "CertainTime";
            if (normalized == "BluetoothNotReachable") return "BluetoothNotReachable";
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

        public void UpdateCachedSettings(Settings settings)
        {
            if (settings == null)
            {
                return;
            }

            _cachedSettings = settings;
            Logger.UpdateSettings(settings);
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

            if (_loadingDelayTimer != null)
            {
                _loadingDelayTimer.Stop();
            }

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
                case "clearAllActions":
                    HandleClearAllActions();
                    break;
                case "saveSettings":
                    HandleSaveSettings(data);
                    break;
                case "loadSettings":
                    HandleLoadSettings();
                    break;
                case "loadLogs":
                    HandleLoadLogs();
                    break;
                case "clearLogs":
                    HandleClearLogs();
                    break;
                case "getLanguageList":
                    HandleGetLanguageList();
                    break;
                case "openWindow":
                    string page = data.GetProperty("page").GetString();
                    OpenSubWindow(page);
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
                case "startBluetoothScan":
                    HandleStartBluetoothScan();
                    break;
                case "stopBluetoothScan":
                    HandleStopBluetoothScan();
                    break;
                case "exitApp":
                    IsApplicationExiting = true;
                    StopSubWindowPrewarm();
                    CloseAllSubWindows();
                    BluetoothScanner.StopMonitoring();
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
                    out string validationMessage))
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
                else if (triggerType == "BluetoothNotReachable")
                {
                    parsedAction.TriggerType = Config.TriggerTypes.BluetoothNotReachable;

                    string btMac = data.GetProperty("bluetoothMac").GetString();
                    string btName = data.GetProperty("bluetoothName").GetString();

                    if (string.IsNullOrWhiteSpace(btMac))
                    {
                        return false;
                    }

                    parsedAction.Value = btMac;
                    parsedAction.ValueUnit = btName ?? "";

                    // Prime monitor state when the selected device was just discovered in UI scan.
                    BluetoothScanner.SeedMonitoredDeviceFromDiscovery(btMac);
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
            var newSettings = new Settings
            {
                LogsEnabled = data.GetProperty("logsEnabled").GetBoolean(),
                StartWithWindows = data.GetProperty("startWithWindows").GetBoolean(),
                RunInTaskbarWhenClosed = data.GetProperty("runInTaskbarWhenClosed").GetBoolean(),
                IsCountdownNotifierEnabled = data.GetProperty("isCountdownNotifierEnabled").GetBoolean(),
                CountdownNotifierSeconds = data.GetProperty("countdownNotifierSeconds").GetInt32(),
                Language = data.GetProperty("language").GetString(),
                Theme = data.GetProperty("theme").GetString(),
                BluetoothThresholdSeconds = data.GetProperty("bluetoothThresholdSeconds").GetInt32()
            };

            string currentLang = _cachedSettings?.Language ?? "auto";
            bool previousStartWithWindows = _cachedSettings?.StartWithWindows ?? false;
            SettingsStorage.Save(newSettings);
            _cachedSettings = newSettings;
            Logger.UpdateSettings(newSettings);

            // Update tray menu renderer and form BackColor based on theme
            bool isDark = DetermineIfDark(newSettings.Theme);
            ContextMenuStripNotifyIcon.Renderer = new WindowsAutoPowerManager.Functions.ModernMenuRenderer(isDark);
            BackColor = isDark
                ? System.Drawing.Color.FromArgb(26, 27, 46)
                : System.Drawing.Color.FromArgb(240, 242, 245);

            if (newSettings.StartWithWindows != previousStartWithWindows)
            {
                if (newSettings.StartWithWindows)
                    StartWithWindows.AddStartup(Language.SettingsFormAddStartupAppName ?? Constants.AppName);
                else
                    StartWithWindows.DeleteStartup(Language.SettingsFormAddStartupAppName ?? Constants.AppName);
            }

            if (newSettings.IsCountdownNotifierEnabled)
            {
                NotifySystem.PrewarmCountdownNotifier();
            }

            if (!string.Equals(currentLang, newSettings.Language, StringComparison.Ordinal))
            {
                LanguagePayloadCache.Invalidate();
            }

            if (currentLang != newSettings.Language)
            {
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleSuccess,
                    message = Language.MessageContentSettingSavedWithLangChanged,
                    type = "info",
                    duration = 4000
                });
            }
            else
            {
                PostMessage("showToast", new
                {
                    title = Language.MessageTitleSuccess,
                    message = Language.MessageContentSettingsSaved,
                    type = "success",
                    duration = 2000
                });
            }
        }

        private void HandleLoadSettings()
        {
            var settingsObj = _cachedSettings ?? LoadSettings();
            PostMessage("settingsLoaded", BuildSettingsPayload(settingsObj));
        }

        private void HandleLoadLogs()
        {
#if DEBUG
            long perfStart = DebugPerformanceTracker.Start();
#endif
            var rawLogs = Logger.GetRecentLogs(250);
            var logs = rawLogs.Select(l => new
            {
                actionExecutedDate = l.ActionExecutedDate,
                actionType = TranslateLogAction(l.ActionType),
                actionTypeRaw = l.ActionType
            }).ToList();

            PostMessage("logsLoaded", logs);
#if DEBUG
            DebugPerformanceTracker.Record("MainForm.HandleLoadLogs", perfStart);
#endif
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

        // =============== Bluetooth Scanning ===============

        private Timer _bluetoothScanTimer;

        private void HandleStartBluetoothScan()
        {
            BluetoothScanner.StartDiscoveryScan();
            if (_bluetoothScanTimer != null)
            {
                return;
            }

            _lastBluetoothDiscoveryVersion = -1;
            _bluetoothScanTimer = new Timer { Interval = 1000 };
            _bluetoothScanTimer.Tick += BluetoothScanTimerTick;
            _bluetoothScanTimer.Start();
        }

        private void BluetoothScanTimerTick(object sender, EventArgs e)
        {
            if (!BluetoothScanner.TryGetDiscoveredDevicesIfChanged(
                    ref _lastBluetoothDiscoveryVersion,
                    out List<BleDeviceInfo> devices))
            {
                return;
            }

            var list = new List<object>(devices.Count);
            foreach (BleDeviceInfo device in devices)
            {
                list.Add(new
                {
                    mac = device.MacAddress,
                    name = device.LocalName ?? string.Empty,
                    rssi = device.RssiDbm
                });
            }

            PostMessage("bluetoothScanResult", list);
        }

        private void HandleStopBluetoothScan()
        {
            _bluetoothScanTimer?.Stop();
            _bluetoothScanTimer?.Dispose();
            _bluetoothScanTimer = null;
            _lastBluetoothDiscoveryVersion = -1;
            BluetoothScanner.StopDiscoveryScan();
        }

        private void EnsureBluetoothMonitoring()
        {
            bool hasBluetoothTrigger = ActionList.Any(a =>
                a.TriggerType == Config.TriggerTypes.BluetoothNotReachable);

            if (hasBluetoothTrigger && !BluetoothScanner.IsMonitoring)
            {
                BluetoothScanner.StartMonitoring();
            }
            else if (!hasBluetoothTrigger && BluetoothScanner.IsMonitoring)
            {
                BluetoothScanner.StopMonitoring();
                _bluetoothReachabilityByActionKey.Clear();
            }
        }

        // =============== Action List Persistence ===============

        public void WriteJsonToActionList()
        {
#if DEBUG
            long perfStart = DebugPerformanceTracker.Start();
#endif
            JsonWriter.WriteJson(AppContext.BaseDirectory + "\\ActionList.json", true, ActionList);
            RebuildActionRuntimeStates();
            CleanupActionExecutionState();
            EnsureBluetoothMonitoring();
            RefreshActionsInUI();
#if DEBUG
            DebugPerformanceTracker.Record("MainForm.WriteJsonToActionList", perfStart);
#endif
        }

        private void CleanupActionExecutionState()
        {
            var validKeys = new HashSet<string>(ActionList.Select(BuildActionExecutionKey));
            _executedIdleActionKeys.RemoveWhere(key => !validKeys.Contains(key));
            foreach (string key in _bluetoothReachabilityByActionKey.Keys.ToList())
            {
                if (!validKeys.Contains(key))
                {
                    _bluetoothReachabilityByActionKey.Remove(key);
                }
            }

            foreach (string key in _certainTimeLastExecutionDates.Keys.ToList())
            {
                if (!validKeys.Contains(key))
                {
                    _certainTimeLastExecutionDates.Remove(key);
                }
            }

            NotifySystem.CleanupState(ActionList);
        }

        private void RebuildActionRuntimeStates()
        {
            _actionRuntimeStates.Clear();
            foreach (ActionModel action in ActionList)
            {
                string key = BuildActionExecutionKey(action);
                _actionRuntimeStates[key] = BuildActionRuntimeState(action, key);
            }
        }

        private ActionRuntimeState BuildActionRuntimeState(ActionModel action, string actionKey)
        {
            var state = new ActionRuntimeState
            {
                ExecutionKey = actionKey ?? string.Empty
            };

            if (TryGetSystemIdleSeconds(action, out uint idleSeconds))
            {
                state.IdleSeconds = idleSeconds;
                state.HasIdleSeconds = true;
            }

            if (TryParseFromNowValue(action, out DateTime fromNowTarget))
            {
                state.FromNowTarget = fromNowTarget;
                state.HasFromNowTarget = true;
            }

            if (action != null &&
                !string.IsNullOrWhiteSpace(action.Value) &&
                action.TriggerType == Config.TriggerTypes.CertainTime &&
                DateTime.TryParseExact(
                    action.Value,
                    "HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedTime))
            {
                state.CertainTimeOfDay = parsedTime.TimeOfDay;
                state.HasCertainTime = true;
            }

            if (action != null &&
                action.TriggerType == Config.TriggerTypes.BluetoothNotReachable &&
                !string.IsNullOrWhiteSpace(action.Value))
            {
                state.BluetoothAddress = BluetoothScanner.MacStringToUlong(action.Value);
            }

            return state;
        }

        private ActionRuntimeState GetOrCreateActionRuntimeState(ActionModel action)
        {
            string key = BuildActionExecutionKey(action);
            if (_actionRuntimeStates.TryGetValue(key, out ActionRuntimeState state))
            {
                return state;
            }

            state = BuildActionRuntimeState(action, key);
            _actionRuntimeStates[key] = state;
            return state;
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
            _subWindows[pageName] = win;

            win.FormClosed += (s, args) =>
            {
                _subWindows.Remove(pageName);
            };

            return win;
        }

        // =============== Timer & Action Execution ===============

        private ActionExecutionResult DoAction(
            ActionModel action,
            ActionRuntimeState state,
            uint idleTimeSec,
            DateTime now)
        {
            if (action == null || state == null)
            {
                return ActionExecutionResult.None;
            }

            string actionKey = state.ExecutionKey;
            ActionExecutionResult result = ActionExecutionResult.None;

            if (action.TriggerType == Config.TriggerTypes.SystemIdle)
            {
                if (!state.HasIdleSeconds)
                {
                    return ActionExecutionResult.None;
                }

                if (idleTimeSec >= state.IdleSeconds && _executedIdleActionKeys.Add(actionKey))
                {
                    Actions.DoActionByTypes(action);
                    return ActionExecutionResult.Executed;
                }

                return ActionExecutionResult.None;
            }

            if (action.TriggerType == Config.TriggerTypes.CertainTime &&
                ShouldExecuteCertainTimeAction(state, actionKey, now))
            {
                if (IsSkippedCertainTimeAction == false)
                {
                    Actions.DoActionByTypes(action);
                    result |= ActionExecutionResult.Executed;
                }
                else
                {
                    IsSkippedCertainTimeAction = false;
                }

                _certainTimeLastExecutionDates[actionKey] = now.Date;
            }

            if (action.TriggerType == Config.TriggerTypes.FromNow &&
                state.HasFromNowTarget &&
                now >= state.FromNowTarget)
            {
                Actions.DoActionByTypes(action);
                result |= ActionExecutionResult.Executed |
                          ActionExecutionResult.RemoveAction |
                          ActionExecutionResult.NeedsPersist;
            }

            if (action.TriggerType == Config.TriggerTypes.BluetoothNotReachable)
            {
                if (state.BluetoothAddress == 0)
                {
                    return result;
                }

                int threshold = (_cachedSettings?.BluetoothThresholdSeconds > 0) ? _cachedSettings.BluetoothThresholdSeconds : 5;
                bool reachable = BluetoothScanner.IsDeviceReachable(state.BluetoothAddress, threshold, now);
                bool hasEverBeenSeen = BluetoothScanner.HasDeviceEverBeenSeen(state.BluetoothAddress);
                bool wasReachable = _bluetoothReachabilityByActionKey.TryGetValue(actionKey, out bool previousReachable)
                    && previousReachable;

                if (reachable)
                {
                    _bluetoothReachabilityByActionKey[actionKey] = true;
                }
                else if (hasEverBeenSeen && wasReachable)
                {
                    _bluetoothReachabilityByActionKey[actionKey] = false;
                    Actions.DoActionByTypes(action);
                    result |= ActionExecutionResult.Executed;
                }
                else if (!_bluetoothReachabilityByActionKey.ContainsKey(actionKey))
                {
                    _bluetoothReachabilityByActionKey[actionKey] = false;
                }
            }

            return result;
        }

        private static string BuildActionExecutionKey(ActionModel action)
        {
            if (action == null) return string.Empty;

            return (action.CreatedDate ?? string.Empty) + "|" +
                   (action.TriggerType ?? string.Empty) + "|" +
                   (action.ActionType ?? string.Empty) + "|" +
                   (action.Value ?? string.Empty);
        }

        private bool ShouldExecuteCertainTimeAction(ActionRuntimeState state, string actionKey, DateTime now)
        {
            if (state == null || !state.HasCertainTime)
            {
                return false;
            }

            DateTime scheduledTime = now.Date.Add(state.CertainTimeOfDay);
            if (now < scheduledTime)
            {
                return false;
            }

            if (_certainTimeLastExecutionDates.TryGetValue(actionKey, out DateTime lastExecutionDate) &&
                lastExecutionDate.Date == now.Date)
            {
                return false;
            }

            return true;
        }

        private static bool TryParseFromNowValue(ActionModel action, out DateTime targetTime)
        {
            targetTime = default;
            if (action == null || string.IsNullOrWhiteSpace(action.Value))
            {
                return false;
            }

            return DateTime.TryParseExact(
                action.Value,
                "dd.MM.yyyy HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out targetTime);
        }

        private static bool TryGetSystemIdleSeconds(ActionModel action, out uint seconds)
        {
            seconds = 0;
            if (action == null || string.IsNullOrWhiteSpace(action.Value))
            {
                return false;
            }

            if (!uint.TryParse(action.Value, out uint parsed))
            {
                return false;
            }

            if (parsed == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(action.ValueUnit))
            {
                if (parsed > uint.MaxValue / 60)
                {
                    return false;
                }

                seconds = parsed * 60;
            }
            else
            {
                seconds = parsed;
            }

            return true;
        }

        private void TimerTick(object sender, EventArgs e)
        {
#if DEBUG
            long perfStart = DebugPerformanceTracker.Start();
#endif
            DateTime now = DateTime.Now;

            // Update time in UI
            string timeText = (Language.MainStatusBarCurrentTime ?? "Time") + " : " + now + "  |  Build Id: " + BuildInfo.CommitId;
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

            if (idleTimeSec == 0)
            {
                NotifySystem.ResetIdleNotifications();
                _executedIdleActionKeys.Clear();
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
                ActionRuntimeState state = GetOrCreateActionRuntimeState(action);
                ActionExecutionResult actionResult = DoAction(action, state, idleTimeSec, now);

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

#if DEBUG
            DebugPerformanceTracker.Record("MainForm.TimerTick", perfStart);
#endif
        }

        // =============== System Tray & Window Events ===============

        public void ShowMain()
        {
            ShowInTaskbar = true;
            Show();

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            BringToFront();
            Activate();
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
            BluetoothScanner.StopMonitoring();
            Logger.DoLog(Config.ActionTypes.AppTerminated);
        }

        private void exitTheProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IsApplicationExiting = true;
            StopSubWindowPrewarm();
            CloseAllSubWindows();
            BluetoothScanner.StopMonitoring();
            Logger.DoLog(Config.ActionTypes.AppTerminated);
            Application.ExitThread();
        }

        private void addNewActionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowMain();
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

        // =============== Theme Helpers ===============

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
