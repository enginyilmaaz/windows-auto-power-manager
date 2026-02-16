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
using WindowsShutdownHelper.functions;

namespace WindowsShutdownHelper
{
    public partial class MainForm : Form
    {
        public static Language language = LanguageSelector.LanguageFile();
        public static List<ActionModel> actionList = new List<ActionModel>();
        public static Settings settings = new Settings();
        public static bool isDeletedFromNotifier;
        public static bool isSkippedCertainTimeAction;
        public static bool isApplicationExiting;
        public static Timer timer = new Timer();
        public static int runInTaskbarCounter;

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
                if (arg == "-runInTaskBar" && runInTaskbarCounter <= 0)
                {
                    ++runInTaskbarCounter;
                    Hide();
                    ShowInTaskbar = false;
                }
            }

            base.OnLoad(e);
        }

        public void DeleteExpriedAction()
        {
            bool changed = false;
            foreach (ActionModel action in actionList.ToList())
            {
                if (action.TriggerType == config.TriggerTypes.fromNow)
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
                            actionList.Remove(action);
                            changed = true;
                        }
                    }
                    else
                    {
                        // Remove malformed scheduled entries to avoid runtime crashes.
                        actionList.Remove(action);
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

            Text = language.MainFormName;
            NotifyIconMain.Text = language.MainFormName + " " + language.NotifyIconMain;
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
                actionList = LoadActionList();

                DeleteExpriedAction();

                // Setup timer
                timer.Interval = 1000;
                timer.Tick += TimerTick;
                timer.Start();

                // Setup notify icon context menu text
                contextMenuStrip_notifyIcon.Items[(int)EnumCmStripNotifyIcon.AddNewAction].Text =
                    language.ContextMenuStripNotifyIconAddNewAction;
                contextMenuStrip_notifyIcon.Items[(int)EnumCmStripNotifyIcon.ExitTheProgram].Text =
                    language.ContextMenuStripNotifyIconExitProgram;
                contextMenuStrip_notifyIcon.Items[(int)EnumCmStripNotifyIcon.Settings].Text =
                    language.ContextMenuStripNotifyIconShowSettings;
                contextMenuStrip_notifyIcon.Items[(int)EnumCmStripNotifyIcon.ShowLogs].Text =
                    language.ContextMenuStripNotifyIconShowLogs;
                contextMenuStrip_notifyIcon.Items[(int)EnumCmStripNotifyIcon.About].Text =
                    language.AboutMenuItem ?? "About";

                // Apply modern tray menu renderer based on theme
                _cachedSettings = LoadSettings();
                bool isDark = DetermineIfDark(_cachedSettings.Theme);
                contextMenuStrip_notifyIcon.Renderer = new WindowsShutdownHelper.functions.ModernMenuRenderer(isDark);
                contextMenuStrip_notifyIcon.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular);
                BackColor = isDark
                    ? System.Drawing.Color.FromArgb(26, 27, 46)
                    : System.Drawing.Color.FromArgb(240, 242, 245);
            }
            catch (Exception ex)
            {
                actionList = new List<ActionModel>();
                _cachedSettings = config.SettingsINI.DefaulSettingFile();
                ReportStartupError("Baslangic verileri yuklenemedi", ex);
            }
            finally
            {
                _bootDataReady = true;
                TrySendInitData();

                // Log app started in background
                _ = System.Threading.Tasks.Task.Run(() => Logger.DoLog(config.ActionTypes.appStarted, _cachedSettings));
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
                Text = language?.CommonLoading ?? "Yükleniyor..."
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
            _loadingLabel.Text = language?.CommonLoading ?? "Yükleniyor...";
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
            if (_subWindowPrewarmStarted || isApplicationExiting || IsDisposed) return;
            _subWindowPrewarmStarted = true;

            BeginInvoke(new Action(() =>
            {
                foreach (var pageName in _subWindowPrewarmPages)
                {
                    if (isApplicationExiting || IsDisposed) break;
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
                var val = prop.GetValue(language);
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
                    appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                    buildId = BuildInfo.CommitId
                }
            };

            PostMessage("init", initData);
        }

        private List<Dictionary<string, string>> GetTranslatedActions()
        {
            var list = new List<Dictionary<string, string>>();
            foreach (var act in actionList)
            {
                var d = new Dictionary<string, string>
                {
                    ["triggerType"] = TranslateTrigger(act.TriggerType),
                    ["actionType"] = TranslateAction(act.ActionType),
                    ["value"] = act.Value ?? "",
                    ["valueUnit"] = TranslateUnit(act.ValueUnit),
                    ["createdDate"] = act.CreatedDate ?? ""
                };
                list.Add(d);
            }
            return list;
        }

        private string TranslateAction(string raw)
        {
            if (raw == config.ActionTypes.lockComputer) return language.MainCboxActionTypeItemLockComputer;
            if (raw == config.ActionTypes.shutdownComputer) return language.MainCboxActionTypeItemShutdownComputer;
            if (raw == config.ActionTypes.restartComputer) return language.MainCboxActionTypeItemRestartComputer;
            if (raw == config.ActionTypes.logOffWindows) return language.MainCboxActionTypeItemLogOffWindows;
            if (raw == config.ActionTypes.sleepComputer) return language.MainCboxActionTypeItemSleepComputer;
            if (raw == config.ActionTypes.turnOffMonitor) return language.MainCboxActionTypeItemTurnOffMonitor;
            return raw;
        }

        private string TranslateTrigger(string raw)
        {
            if (raw == config.TriggerTypes.systemIdle) return language.MainCboxTriggerTypeItemSystemIdle;
            if (raw == config.TriggerTypes.certainTime) return language.MainCboxTriggerTypeItemCertainTime;
            if (raw == config.TriggerTypes.fromNow) return language.MainCboxTriggerTypeItemFromNow;
            return raw;
        }

        private string TranslateUnit(string raw)
        {
            if (raw == "seconds") return language.MainTimeUnitSeconds ?? "Seconds";
            if (string.IsNullOrEmpty(raw)) return language.MainTimeUnitMinutes ?? "Minutes";
            return raw;
        }

        private Settings LoadSettings()
        {
            string path = AppContext.BaseDirectory + "\\settings.json";
            return ReadJsonFileOrDefault(path, config.SettingsINI.DefaulSettingFile());
        }

        private List<ActionModel> LoadActionList()
        {
            string path = AppContext.BaseDirectory + "\\actionList.json";
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
                _loadingLabel.Text = language?.MessageTitleError ?? "Error";
            }

            MessageBox.Show(
                this,
                title + ".\r\n\r\nDetay: " + ex.Message,
                language?.MessageTitleError ?? "Error",
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
                case "exitApp":
                    isApplicationExiting = true;
                    StopSubWindowPrewarm();
                    CloseAllSubWindows();
                    Logger.DoLog(config.ActionTypes.appTerminated);
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
            string actionType = data.GetProperty("actionType").GetString();
            string triggerType = data.GetProperty("triggerType").GetString();

            if (actionType == "0" || triggerType == "0")
            {
                PostMessage("showToast", new
                {
                    title = language.MessageTitleWarn,
                    message = language.MessageContentActionChoose,
                    type = "warn",
                    duration = 2000
                });
                PostMessage("addActionResult", new { success = false });
                return;
            }

            if (actionList.Count >= 5)
            {
                PostMessage("showToast", new
                {
                    title = language.MessageTitleWarn,
                    message = language.MessageContentMaxActionWarn,
                    type = "warn",
                    duration = 2000
                });
                PostMessage("addActionResult", new { success = false });
                return;
            }

            var newAction = new ActionModel
            {
                CreatedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                ActionType = actionType
            };

            if (triggerType == "fromNow")
            {
                newAction.TriggerType = config.TriggerTypes.fromNow;
                double inputValue = Convert.ToDouble(data.GetProperty("value").GetString());
                int unitIdx = Convert.ToInt32(data.GetProperty("timeUnit").GetString());
                DateTime targetTime;
                if (unitIdx == 0) targetTime = DateTime.Now.AddSeconds(inputValue);
                else if (unitIdx == 2) targetTime = DateTime.Now.AddHours(inputValue);
                else targetTime = DateTime.Now.AddMinutes(inputValue);
                newAction.Value = targetTime.ToString("dd.MM.yyyy HH:mm:ss");
            }
            else if (triggerType == "systemIdle")
            {
                newAction.TriggerType = config.TriggerTypes.systemIdle;
                int inputValue = Convert.ToInt32(data.GetProperty("value").GetString());
                int unitIdx = Convert.ToInt32(data.GetProperty("timeUnit").GetString());
                int valueInSeconds;
                if (unitIdx == 0) valueInSeconds = inputValue;
                else if (unitIdx == 2) valueInSeconds = inputValue * 3600;
                else valueInSeconds = inputValue * 60;
                newAction.Value = valueInSeconds.ToString();
                newAction.ValueUnit = "seconds";
            }
            else if (triggerType == "certainTime")
            {
                newAction.TriggerType = config.TriggerTypes.certainTime;
                string timeStr = data.GetProperty("time").GetString();
                if (!string.IsNullOrEmpty(timeStr))
                {
                    newAction.Value = timeStr + ":00";
                }
                else
                {
                    newAction.Value = DateTime.Now.AddMinutes(1).ToString("HH:mm:00");
                }
            }

            if (!ActionValidation.TryValidateActionForAdd(newAction, actionList, language, out string validationMessage))
            {
                PostMessage("showToast", new
                {
                    title = language.MessageTitleWarn,
                    message = validationMessage,
                    type = "warn",
                    duration = 3000
                });
                PostMessage("addActionResult", new { success = false });
                return;
            }

            actionList.Add(newAction);
            WriteJsonToActionList();

            PostMessage("showToast", new
            {
                title = language.MessageTitleSuccess,
                message = language.MessageContentActionCreated,
                type = "success",
                duration = 2000
            });
            PostMessage("addActionResult", new { success = true });
        }

        private void HandleDeleteAction(JsonElement data)
        {
            int index = data.GetProperty("index").GetInt32();
            if (index >= 0 && index < actionList.Count)
            {
                actionList.RemoveAt(index);
                WriteJsonToActionList();

                PostMessage("showToast", new
                {
                    title = language.MessageTitleSuccess,
                    message = language.MessageContentActionDeleted,
                    type = "success",
                    duration = 2000
                });
            }
        }

        private void HandleClearAllActions()
        {
            actionList.Clear();
            WriteJsonToActionList();

            PostMessage("showToast", new
            {
                title = language.MessageTitleSuccess,
                message = language.MessageContentActionAllDeleted,
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
                Theme = data.GetProperty("theme").GetString()
            };

            string currentLang = LoadSettings().Language;
            JsonWriter.WriteJson(AppContext.BaseDirectory + "\\settings.json", true, newSettings);

            // Update tray menu renderer and form BackColor based on theme
            bool isDark = DetermineIfDark(newSettings.Theme);
            contextMenuStrip_notifyIcon.Renderer = new WindowsShutdownHelper.functions.ModernMenuRenderer(isDark);
            BackColor = isDark
                ? System.Drawing.Color.FromArgb(26, 27, 46)
                : System.Drawing.Color.FromArgb(240, 242, 245);

            if (newSettings.StartWithWindows)
                StartWithWindows.AddStartup(language.SettingsFormAddStartupAppName ?? "Windows Shutdown Helper");
            else
                StartWithWindows.DeleteStartup(language.SettingsFormAddStartupAppName ?? "Windows Shutdown Helper");

            if (newSettings.IsCountdownNotifierEnabled)
            {
                NotifySystem.PrewarmCountdownNotifier();
            }

            if (currentLang != newSettings.Language)
            {
                PostMessage("showToast", new
                {
                    title = language.MessageTitleSuccess,
                    message = language.MessageContentSettingSavedWithLangChanged,
                    type = "info",
                    duration = 4000
                });
            }
            else
            {
                PostMessage("showToast", new
                {
                    title = language.MessageTitleSuccess,
                    message = language.MessageContentSettingsSaved,
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
            string logPath = AppContext.BaseDirectory + "\\logs.json";
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
            if (raw == config.ActionTypes.lockComputer) return language.LogViewerFormLockComputer;
            if (raw == config.ActionTypes.lockComputerManually) return language.LogViewerFormLockComputerManually;
            if (raw == config.ActionTypes.unlockComputer) return language.LogViewerFormUnlockComputer;
            if (raw == config.ActionTypes.shutdownComputer) return language.LogViewerFormShutdownComputer;
            if (raw == config.ActionTypes.restartComputer) return language.LogViewerFormRestartComputer;
            if (raw == config.ActionTypes.logOffWindows) return language.LogViewerFormLogOffWindows;
            if (raw == config.ActionTypes.sleepComputer) return language.LogViewerFormSleepComputer;
            if (raw == config.ActionTypes.turnOffMonitor) return language.LogViewerFormTurnOffMonitor;
            if (raw == config.ActionTypes.appStarted) return language.LogViewerFormAppStarted;
            if (raw == config.ActionTypes.appTerminated) return language.LogViewerFormAppTerminated;
            return raw;
        }

        private void HandleClearLogs()
        {
            string logPath = AppContext.BaseDirectory + "\\logs.json";
            if (File.Exists(logPath)) File.Delete(logPath);

            PostMessage("showToast", new
            {
                title = language.MessageTitleSuccess,
                message = language.MessageContentClearedLogs,
                type = "success",
                duration = 2000
            });
        }

        private void HandleGetLanguageList()
        {
            var list = new List<object>();
            list.Add(new { LangCode = "auto", langName = (language.SettingsFormComboboxAutoLang ?? "Auto") });

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
                title = language.MessageTitleSuccess,
                message = language.PausePaused ?? "Actions paused successfully",
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
                title = language.MessageTitleSuccess,
                message = language.PauseResumed ?? "Actions resumed",
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
            JsonWriter.WriteJson(AppContext.BaseDirectory + "\\actionList.json", true,
                actionList.ToList());
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
                    return language.MainGroupboxNewAction ?? "Actions";
                case "settings":
                    return language.SettingsFormName ?? "Settings";
                case "logs":
                    return language.LogViewerFormName ?? "Logs";
                case "about":
                    return language.AboutMenuItem ?? "About";
                default:
                    return language.MainFormName ?? "Windows Shutdown Helper";
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

            if (action.TriggerType == config.TriggerTypes.systemIdle)
            {
                if (!TryGetSystemIdleSeconds(action, out uint actionValueSeconds)) return;
                if (idleTimeMin == actionValueSeconds)
                {
                    Actions.DoActionByTypes(action);
                }
                return;
            }

            if (action.TriggerType == config.TriggerTypes.certainTime && action.Value == DateTime.Now.ToString("HH:mm:ss"))
            {
                if (isSkippedCertainTimeAction == false)
                {
                    Actions.DoActionByTypes(action);
                }
                else
                {
                    isSkippedCertainTimeAction = false;
                }
            }

            if (action.TriggerType == config.TriggerTypes.fromNow && action.Value == DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"))
            {
                Actions.DoActionByTypes(action);
                actionList.Remove(action);
                WriteJsonToActionList();
            }
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
            string timeText = (language.MainStatusBarCurrentTime ?? "Time") + " : " + DateTime.Now + "  |  Build Id: " + BuildInfo.CommitId;
            PostMessage("updateTime", timeText);

            // Check pause expiration
            if (_isPaused && _pauseUntilTime.HasValue && DateTime.Now >= _pauseUntilTime.Value)
            {
                _isPaused = false;
                _pauseUntilTime = null;
                SendPauseStatus();
                PostMessage("showToast", new
                {
                    title = language.MessageTitleInfo ?? "Info",
                    message = language.PauseResumed ?? "Actions resumed",
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
                timer.Stop();
                timer.Start();
            }

            if (isDeletedFromNotifier)
            {
                WriteJsonToActionList();
                isDeletedFromNotifier = false;
            }

            foreach (ActionModel action in actionList.ToList())
            {
                DoAction(action, idleTimeMin);
                NotifySystem.ShowNotification(action, idleTimeMin);
            }
        }

        // =============== System Tray & Window Events ===============

        public void ShowMain()
        {
            Show();
            Focus();
            ShowInTaskbar = true;
        }

        private void NotifyIconMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowMain();
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopSubWindowPrewarm();

            settings = LoadSettings();
            if (settings.RunInTaskbarWhenClosed)
            {
                e.Cancel = true;
                Hide();
            }

            if (!e.Cancel)
            {
                isApplicationExiting = true;
                CloseAllSubWindows();
            }
        }

        private void mainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logger.DoLog(config.ActionTypes.appTerminated);
        }

        private void exitTheProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isApplicationExiting = true;
            StopSubWindowPrewarm();
            CloseAllSubWindows();
            Logger.DoLog(config.ActionTypes.appTerminated);
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
