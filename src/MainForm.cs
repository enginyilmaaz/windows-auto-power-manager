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
using WindowsShutdownHelper.Enums;
using WindowsShutdownHelper.Functions;

namespace WindowsShutdownHelper
{
    public partial class MainForm : Form
    {
        public static Language Language = LanguageSelector.LanguageFile();
        public static List<ActionModel> ActionList = new List<ActionModel>();
        public static Settings Settings = new Settings();
        public static bool IsDeletedFromNotifier;
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
        private readonly string[] _subWindowPrewarmPages = { "settings", "logs", "about" };
        private readonly HashSet<string> _executedIdleActionKeys = new HashSet<string>();
        private readonly HashSet<string> _executedBluetoothActionKeys = new HashSet<string>();
        private readonly Dictionary<string, DateTime> _certainTimeLastExecutionDates = new Dictionary<string, DateTime>();
        private bool _subWindowPrewarmStarted;
        private bool _startupErrorShown;

        public MainForm()
        {
            InitializeComponent();
            InitializeLoadingOverlay();
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
            foreach (ActionModel action in ActionList.ToList())
            {
                if (action.TriggerType == Config.TriggerTypes.FromNow)
                {
                    if (DateTime.TryParseExact(
                        action.Value,
                        "dd.MM.yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime actionDate))
                    {
                        if (DateTime.Now > actionDate)
                        {
                            ActionList.Remove(action);
                            changed = true;
                        }
                    }
                    else
                    {
                        // Remove malformed scheduled entries to avoid runtime crashes.
                        ActionList.Remove(action);
                        changed = true;
                    }
                }
            }
            if (changed) WriteJsonToActionList();
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
                ContextMenuStripNotifyIcon.Items[(int)EnumCmStripNotifyIcon.About].Text =
                    Language.AboutMenuItem ?? "About";

                // Apply modern tray menu renderer based on theme
                _cachedSettings = LoadSettings();
                ApplySettingsOnStartup(_cachedSettings);
                bool isDark = DetermineIfDark(_cachedSettings.Theme);
                ContextMenuStripNotifyIcon.Renderer = new WindowsShutdownHelper.Functions.ModernMenuRenderer(isDark);
                ContextMenuStripNotifyIcon.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular);
                BackColor = isDark
                    ? System.Drawing.Color.FromArgb(26, 27, 46)
                    : System.Drawing.Color.FromArgb(240, 242, 245);
            }
            catch (Exception ex)
            {
                ActionList = new List<ActionModel>();
                _cachedSettings = Config.SettingsINI.DefaulSettingFile();
                ReportStartupError("Baslangic verileri yuklenemedi", ex);
            }
            finally
            {
                _bootDataReady = true;
                TrySendInitData();

                // Log app started in background
                _ = System.Threading.Tasks.Task.Run(() => Logger.DoLog(Config.ActionTypes.AppStarted, _cachedSettings));
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

            // Serialize language object via reflection
            var langDict = new Dictionary<string, string>();
            foreach (PropertyInfo prop in typeof(Language).GetProperties())
            {
                var val = prop.GetValue(Language);
                if (val != null) langDict[prop.Name] = val.ToString();
            }

            // Build translated actions for display
            var displayActions = GetTranslatedActions();

            var settingsObj = _cachedSettings ?? LoadSettings();
            var initData = new
            {
                language = langDict,
                actions = displayActions,
                settings = new
                {
                    logsEnabled = settingsObj.LogsEnabled,
                    startWithWindows = settingsObj.StartWithWindows,
                    runInTaskbarWhenClosed = settingsObj.RunInTaskbarWhenClosed,
                    isCountdownNotifierEnabled = settingsObj.IsCountdownNotifierEnabled,
                    countdownNotifierSeconds = settingsObj.CountdownNotifierSeconds,
                    language = settingsObj.Language,
                    theme = settingsObj.Theme,
                    bluetoothThresholdSeconds = settingsObj.BluetoothThresholdSeconds > 0 ? settingsObj.BluetoothThresholdSeconds : 5,
                    appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                    buildId = BuildInfo.CommitId
                }
            };

            PostMessage("init", initData);
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

        private void ApplySettingsOnStartup(Settings settingsObj)
        {
            if (settingsObj == null) return;

            try
            {
                string startupName = Language.SettingsFormAddStartupAppName ?? "Windows Shutdown Helper";
                if (settingsObj.StartWithWindows)
                {
                    StartWithWindows.AddStartup(startupName);
                }
                else
                {
                    StartWithWindows.DeleteStartup(startupName);
                }
            }
            catch
            {
                // Keep startup resilient if registry access fails.
            }
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
            string json = e.WebMessageAsJson;
            var doc = JsonDocument.Parse(json);
            string msgJson = doc.RootElement.GetString();
            var msg = JsonDocument.Parse(msgJson);
            string type = msg.RootElement.GetProperty("type").GetString();
            var data = msg.RootElement.GetProperty("data");

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

            string currentLang = LoadSettings().Language;
            SettingsStorage.Save(newSettings);
            _cachedSettings = newSettings;

            // Update tray menu renderer and form BackColor based on theme
            bool isDark = DetermineIfDark(newSettings.Theme);
            ContextMenuStripNotifyIcon.Renderer = new WindowsShutdownHelper.Functions.ModernMenuRenderer(isDark);
            BackColor = isDark
                ? System.Drawing.Color.FromArgb(26, 27, 46)
                : System.Drawing.Color.FromArgb(240, 242, 245);

            if (newSettings.StartWithWindows)
                StartWithWindows.AddStartup(Language.SettingsFormAddStartupAppName ?? "Windows Shutdown Helper");
            else
                StartWithWindows.DeleteStartup(Language.SettingsFormAddStartupAppName ?? "Windows Shutdown Helper");

            if (newSettings.IsCountdownNotifierEnabled)
            {
                NotifySystem.PrewarmCountdownNotifier();
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
            PostMessage("settingsLoaded", LoadSettings());
        }

        private void HandleLoadLogs()
        {
            string logPath = AppContext.BaseDirectory + "\\Logs.json";
            if (File.Exists(logPath))
            {
                var rawLogs = JsonSerializer.Deserialize<List<LogSystem>>(File.ReadAllText(logPath));
                var logs = rawLogs.OrderByDescending(a => a.ActionExecutedDate).Take(250)
                    .Select(l => new
                    {
                        actionExecutedDate = l.ActionExecutedDate,
                        actionType = TranslateLogAction(l.ActionType),
                        actionTypeRaw = l.ActionType
                    }).ToList();
                PostMessage("logsLoaded", logs);
            }
            else
            {
                PostMessage("logsLoaded", new List<object>());
            }
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
            string logPath = AppContext.BaseDirectory + "\\Logs.json";
            if (File.Exists(logPath)) File.Delete(logPath);

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

            _bluetoothScanTimer = new Timer { Interval = 1000 };
            _bluetoothScanTimer.Tick += (s, e) =>
            {
                var devices = BluetoothScanner.GetDiscoveredDevices();
                var list = devices.Select(d => new
                {
                    mac = d.MacAddress,
                    name = d.LocalName ?? "",
                    rssi = d.RssiDbm
                }).ToList();

                PostMessage("bluetoothScanResult", list);
            };
            _bluetoothScanTimer.Start();
        }

        private void HandleStopBluetoothScan()
        {
            _bluetoothScanTimer?.Stop();
            _bluetoothScanTimer?.Dispose();
            _bluetoothScanTimer = null;
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
                _executedBluetoothActionKeys.Clear();
            }
        }

        // =============== Action List Persistence ===============

        public void WriteJsonToActionList()
        {
            JsonWriter.WriteJson(AppContext.BaseDirectory + "\\ActionList.json", true,
                ActionList.ToList());
            CleanupActionExecutionState();
            EnsureBluetoothMonitoring();
            RefreshActionsInUI();
        }

        private void CleanupActionExecutionState()
        {
            var validKeys = new HashSet<string>(ActionList.Select(BuildActionExecutionKey));
            _executedIdleActionKeys.RemoveWhere(key => !validKeys.Contains(key));
            _executedBluetoothActionKeys.RemoveWhere(key => !validKeys.Contains(key));

            foreach (string key in _certainTimeLastExecutionDates.Keys.ToList())
            {
                if (!validKeys.Contains(key))
                {
                    _certainTimeLastExecutionDates.Remove(key);
                }
            }
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
                case "about":
                    return Language.AboutMenuItem ?? "About";
                default:
                    return Language.MainFormName ?? "Windows Shutdown Helper";
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

        private void DoAction(ActionModel action, uint idleTimeMin)
        {
            if (action == null) return;
            string actionKey = BuildActionExecutionKey(action);

            if (action.TriggerType == Config.TriggerTypes.SystemIdle)
            {
                if (!TryGetSystemIdleSeconds(action, out uint actionValueSeconds)) return;
                if (idleTimeMin >= actionValueSeconds && !_executedIdleActionKeys.Contains(actionKey))
                {
                    _executedIdleActionKeys.Add(actionKey);
                    Actions.DoActionByTypes(action);
                }
                return;
            }

            if (action.TriggerType == Config.TriggerTypes.CertainTime &&
                ShouldExecuteCertainTimeAction(action, actionKey, DateTime.Now))
            {
                if (IsSkippedCertainTimeAction == false)
                {
                    Actions.DoActionByTypes(action);
                }
                else
                {
                    IsSkippedCertainTimeAction = false;
                }

                _certainTimeLastExecutionDates[actionKey] = DateTime.Now.Date;
            }

            if (action.TriggerType == Config.TriggerTypes.FromNow &&
                TryParseFromNowValue(action, out DateTime targetTime) &&
                DateTime.Now >= targetTime)
            {
                Actions.DoActionByTypes(action);
                ActionList.Remove(action);
                WriteJsonToActionList();
            }

            if (action.TriggerType == Config.TriggerTypes.BluetoothNotReachable)
            {
                if (string.IsNullOrWhiteSpace(action.Value)) return;

                int threshold = (_cachedSettings?.BluetoothThresholdSeconds > 0) ? _cachedSettings.BluetoothThresholdSeconds : 5;
                bool reachable = BluetoothScanner.IsDeviceReachable(action.Value, threshold);

                if (reachable)
                {
                    _executedBluetoothActionKeys.Remove(actionKey);
                }
                else if (BluetoothScanner.HasDeviceEverBeenSeen(action.Value) &&
                         !_executedBluetoothActionKeys.Contains(actionKey))
                {
                    _executedBluetoothActionKeys.Add(actionKey);
                    Actions.DoActionByTypes(action);
                }
            }
        }

        private static string BuildActionExecutionKey(ActionModel action)
        {
            if (action == null) return string.Empty;

            return (action.CreatedDate ?? string.Empty) + "|" +
                   (action.TriggerType ?? string.Empty) + "|" +
                   (action.ActionType ?? string.Empty) + "|" +
                   (action.Value ?? string.Empty);
        }

        private bool ShouldExecuteCertainTimeAction(ActionModel action, string actionKey, DateTime now)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.Value))
            {
                return false;
            }

            if (!DateTime.TryParseExact(
                    action.Value,
                    "HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsed))
            {
                return false;
            }

            DateTime scheduledTime = now.Date.Add(parsed.TimeOfDay);
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
            // Update time in UI
            string timeText = (Language.MainStatusBarCurrentTime ?? "Time") + " : " + DateTime.Now + "  |  Build Id: " + BuildInfo.CommitId;
            PostMessage("updateTime", timeText);

            // Check pause expiration
            if (_isPaused && _pauseUntilTime.HasValue && DateTime.Now >= _pauseUntilTime.Value)
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
                return;
            }

            uint idleTimeMin = SystemIdleDetector.GetLastInputTime();

            if (idleTimeMin == 0)
            {
                NotifySystem.ResetIdleNotifications();
                _executedIdleActionKeys.Clear();
                Timer.Stop();
                Timer.Start();
            }

            if (IsDeletedFromNotifier)
            {
                WriteJsonToActionList();
                IsDeletedFromNotifier = false;
            }

            foreach (ActionModel action in ActionList.ToList())
            {
                DoAction(action, idleTimeMin);
                NotifySystem.ShowNotification(action, idleTimeMin);
            }
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

            Settings = LoadSettings();
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
