using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using WindowsShutdownHelper.Enums;
using WindowsShutdownHelper.functions;

namespace WindowsShutdownHelper
{
    public partial class mainForm : Form
    {
        public static language language = languageSelector.languageFile();
        public static List<ActionModel> actionList = new List<ActionModel>();
        public static settings settings = new settings();
        public static bool isDeletedFromNotifier;
        public static bool isSkippedCertainTimeAction;
        public static Timer timer = new Timer();
        public static int runInTaskbarCounter;

        private bool _webViewReady;
        private bool _isPaused;
        private DateTime? _pauseUntilTime;
        private settings _cachedSettings;
        private Dictionary<string, SubWindow> _subWindows = new Dictionary<string, SubWindow>();
        private Panel _loadingOverlay;
        private Label _loadingLabel;

        public mainForm()
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

        public void deleteExpriedAction()
        {
            bool changed = false;
            foreach (ActionModel action in actionList.ToList())
            {
                if (action.triggerType == config.triggerTypes.fromNow)
                {
                    if (DateTime.TryParseExact(
                        action.value,
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
            if (changed) writeJsonToActionList();
        }

        private async void mainForm_Load(object sender, EventArgs e)
        {
            ShowLoadingOverlay();

            // Initialize WebView2 first - this is the slowest operation
            await InitializeWebView();

            Text = language.main_FormName;
            notifyIcon_main.Text = language.main_FormName + " " + language.notifyIcon_main;

            detectScreen.manuelLockingActionLogger();
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\actionList.json"))
            {
                actionList =
                    JsonSerializer.Deserialize<List<ActionModel>>(
                        File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\actionList.json"));
            }

            deleteExpriedAction();

            // Setup timer
            timer.Interval = 1000;
            timer.Tick += timerTick;
            timer.Start();

            // Setup notify icon context menu text
            contextMenuStrip_notifyIcon.Items[(int)enum_cmStrip_notifyIcon.AddNewAction].Text =
                language.contextMenuStrip_notifyIcon_addNewAction;
            contextMenuStrip_notifyIcon.Items[(int)enum_cmStrip_notifyIcon.ExitTheProgram].Text =
                language.contextMenuStrip_notifyIcon_exitProgram;
            contextMenuStrip_notifyIcon.Items[(int)enum_cmStrip_notifyIcon.Settings].Text =
                language.contextMenuStrip_notifyIcon_showSettings;
            contextMenuStrip_notifyIcon.Items[(int)enum_cmStrip_notifyIcon.ShowLogs].Text =
                language.contextMenuStrip_notifyIcon_showLogs;
            contextMenuStrip_notifyIcon.Items[(int)enum_cmStrip_notifyIcon.About].Text =
                language.about_menuItem ?? "About";

            // Apply modern tray menu renderer based on theme
            _cachedSettings = LoadSettings();
            bool isDark = DetermineIfDark(_cachedSettings.theme);
            contextMenuStrip_notifyIcon.Renderer = new WindowsShutdownHelper.functions.ModernMenuRenderer(isDark);
            contextMenuStrip_notifyIcon.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular);
            BackColor = isDark
                ? System.Drawing.Color.FromArgb(26, 27, 46)
                : System.Drawing.Color.FromArgb(240, 242, 245);

            // Log app started in background
            _ = System.Threading.Tasks.Task.Run(() => Logger.doLog(config.actionTypes.appStarted, _cachedSettings));
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            var env = await WebViewEnvironmentProvider.GetAsync();
            await webView.EnsureCoreWebView2Async(env);

            string wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.local", wwwrootPath,
                CoreWebView2HostResourceAccessKind.Allow);

            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            webView.CoreWebView2.Navigate("https://app.local/index.html");
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _webViewReady = true;
            HideLoadingOverlay();
            SendInitData();
        }

        private void InitializeLoadingOverlay()
        {
            _loadingLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(95, 99, 112),
                Text = language?.common_loading ?? "Yükleniyor..."
            };

            _loadingOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(240, 242, 245)
            };

            _loadingOverlay.Controls.Add(_loadingLabel);
            Controls.Add(_loadingOverlay);
            _loadingOverlay.BringToFront();
        }

        private void ShowLoadingOverlay()
        {
            if (_loadingOverlay == null) return;
            _loadingLabel.Text = language?.common_loading ?? "Yükleniyor...";
            _loadingOverlay.Visible = true;
            _loadingOverlay.BringToFront();
        }

        private void HideLoadingOverlay()
        {
            if (_loadingOverlay == null) return;
            _loadingOverlay.Visible = false;
        }

        private void SendInitData()
        {
            if (!_webViewReady) return;

            // Serialize language object via reflection
            var langDict = new Dictionary<string, string>();
            foreach (PropertyInfo prop in typeof(language).GetProperties())
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
                    settingsObj.logsEnabled,
                    settingsObj.startWithWindows,
                    settingsObj.runInTaskbarWhenClosed,
                    settingsObj.isCountdownNotifierEnabled,
                    settingsObj.countdownNotifierSeconds,
                    settingsObj.language,
                    settingsObj.theme,
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
                    ["triggerType"] = TranslateTrigger(act.triggerType),
                    ["actionType"] = TranslateAction(act.actionType),
                    ["value"] = act.value ?? "",
                    ["valueUnit"] = TranslateUnit(act.valueUnit),
                    ["createdDate"] = act.createdDate ?? ""
                };
                list.Add(d);
            }
            return list;
        }

        private string TranslateAction(string raw)
        {
            if (raw == config.actionTypes.lockComputer) return language.main_cbox_ActionType_Item_lockComputer;
            if (raw == config.actionTypes.shutdownComputer) return language.main_cbox_ActionType_Item_shutdownComputer;
            if (raw == config.actionTypes.restartComputer) return language.main_cbox_ActionType_Item_restartComputer;
            if (raw == config.actionTypes.logOffWindows) return language.main_cbox_ActionType_Item_logOffWindows;
            if (raw == config.actionTypes.sleepComputer) return language.main_cbox_ActionType_Item_sleepComputer;
            if (raw == config.actionTypes.turnOffMonitor) return language.main_cbox_ActionType_Item_turnOffMonitor;
            return raw;
        }

        private string TranslateTrigger(string raw)
        {
            if (raw == config.triggerTypes.systemIdle) return language.main_cbox_TriggerType_Item_systemIdle;
            if (raw == config.triggerTypes.certainTime) return language.main_cbox_TriggerType_Item_certainTime;
            if (raw == config.triggerTypes.fromNow) return language.main_cbox_TriggerType_Item_fromNow;
            return raw;
        }

        private string TranslateUnit(string raw)
        {
            if (raw == "seconds") return language.main_timeUnit_seconds ?? "Seconds";
            if (string.IsNullOrEmpty(raw)) return language.main_timeUnit_minutes ?? "Minutes";
            return raw;
        }

        private settings LoadSettings()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"))
            {
                return JsonSerializer.Deserialize<settings>(
                    File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"));
            }
            return config.settingsINI.defaulSettingFile();
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
                    Logger.doLog(config.actionTypes.appTerminated);
                    Application.ExitThread();
                    break;
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
                    title = language.messageTitle_warn,
                    message = language.messageContent_actionChoose,
                    type = "warn",
                    duration = 2000
                });
                return;
            }

            if (actionList.Count >= 5)
            {
                PostMessage("showToast", new
                {
                    title = language.messageTitle_warn,
                    message = language.messageContent_maxActionWarn,
                    type = "warn",
                    duration = 2000
                });
                return;
            }

            var newAction = new ActionModel
            {
                createdDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                actionType = actionType
            };

            if (triggerType == "fromNow")
            {
                newAction.triggerType = config.triggerTypes.fromNow;
                double inputValue = Convert.ToDouble(data.GetProperty("value").GetString());
                int unitIdx = Convert.ToInt32(data.GetProperty("timeUnit").GetString());
                DateTime targetTime;
                if (unitIdx == 0) targetTime = DateTime.Now.AddSeconds(inputValue);
                else if (unitIdx == 2) targetTime = DateTime.Now.AddHours(inputValue);
                else targetTime = DateTime.Now.AddMinutes(inputValue);
                newAction.value = targetTime.ToString("dd.MM.yyyy HH:mm:ss");
            }
            else if (triggerType == "systemIdle")
            {
                newAction.triggerType = config.triggerTypes.systemIdle;
                int inputValue = Convert.ToInt32(data.GetProperty("value").GetString());
                int unitIdx = Convert.ToInt32(data.GetProperty("timeUnit").GetString());
                int valueInSeconds;
                if (unitIdx == 0) valueInSeconds = inputValue;
                else if (unitIdx == 2) valueInSeconds = inputValue * 3600;
                else valueInSeconds = inputValue * 60;
                newAction.value = valueInSeconds.ToString();
                newAction.valueUnit = "seconds";
            }
            else if (triggerType == "certainTime")
            {
                newAction.triggerType = config.triggerTypes.certainTime;
                string timeStr = data.GetProperty("time").GetString();
                if (!string.IsNullOrEmpty(timeStr))
                {
                    newAction.value = timeStr + ":00";
                }
                else
                {
                    newAction.value = DateTime.Now.AddMinutes(1).ToString("HH:mm:00");
                }
            }

            actionList.Add(newAction);
            writeJsonToActionList();

            PostMessage("showToast", new
            {
                title = language.messageTitle_success,
                message = language.messageContent_actionCreated,
                type = "success",
                duration = 2000
            });
        }

        private void HandleDeleteAction(JsonElement data)
        {
            int index = data.GetProperty("index").GetInt32();
            if (index >= 0 && index < actionList.Count)
            {
                actionList.RemoveAt(index);
                writeJsonToActionList();

                PostMessage("showToast", new
                {
                    title = language.messageTitle_success,
                    message = language.messageContent_actionDeleted,
                    type = "success",
                    duration = 2000
                });
            }
        }

        private void HandleClearAllActions()
        {
            actionList.Clear();
            writeJsonToActionList();

            PostMessage("showToast", new
            {
                title = language.messageTitle_success,
                message = language.messageContent_actionAllDeleted,
                type = "success",
                duration = 2000
            });
        }

        private void HandleSaveSettings(JsonElement data)
        {
            var newSettings = new settings
            {
                logsEnabled = data.GetProperty("logsEnabled").GetBoolean(),
                startWithWindows = data.GetProperty("startWithWindows").GetBoolean(),
                runInTaskbarWhenClosed = data.GetProperty("runInTaskbarWhenClosed").GetBoolean(),
                isCountdownNotifierEnabled = data.GetProperty("isCountdownNotifierEnabled").GetBoolean(),
                countdownNotifierSeconds = data.GetProperty("countdownNotifierSeconds").GetInt32(),
                language = data.GetProperty("language").GetString(),
                theme = data.GetProperty("theme").GetString()
            };

            string currentLang = LoadSettings().language;
            jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json", true, newSettings);

            // Update tray menu renderer and form BackColor based on theme
            bool isDark = DetermineIfDark(newSettings.theme);
            contextMenuStrip_notifyIcon.Renderer = new WindowsShutdownHelper.functions.ModernMenuRenderer(isDark);
            BackColor = isDark
                ? System.Drawing.Color.FromArgb(26, 27, 46)
                : System.Drawing.Color.FromArgb(240, 242, 245);

            if (newSettings.startWithWindows)
                startWithWindows.AddStartup(language.settingsForm_addStartupAppName ?? "Windows Shutdown Helper");
            else
                startWithWindows.DeleteStartup(language.settingsForm_addStartupAppName ?? "Windows Shutdown Helper");

            if (currentLang != newSettings.language)
            {
                PostMessage("showToast", new
                {
                    title = language.messageTitle_success,
                    message = language.messageContent_settingSavedWithLangChanged,
                    type = "info",
                    duration = 4000
                });
            }
            else
            {
                PostMessage("showToast", new
                {
                    title = language.messageTitle_success,
                    message = language.messageContent_settingsSaved,
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
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\logs.json";
            if (File.Exists(logPath))
            {
                var rawLogs = JsonSerializer.Deserialize<List<logSystem>>(File.ReadAllText(logPath));
                var logs = rawLogs.OrderByDescending(a => a.actionExecutedDate).Take(250)
                    .Select(l => new
                    {
                        actionExecutedDate = l.actionExecutedDate,
                        actionType = TranslateLogAction(l.actionType),
                        actionTypeRaw = l.actionType
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
            if (raw == config.actionTypes.lockComputer) return language.logViewerForm_lockComputer;
            if (raw == config.actionTypes.lockComputerManually) return language.logViewerForm_lockComputerManually;
            if (raw == config.actionTypes.unlockComputer) return language.logViewerForm_unlockComputer;
            if (raw == config.actionTypes.shutdownComputer) return language.logViewerForm_shutdownComputer;
            if (raw == config.actionTypes.restartComputer) return language.logViewerForm_restartComputer;
            if (raw == config.actionTypes.logOffWindows) return language.logViewerForm_logOffWindows;
            if (raw == config.actionTypes.sleepComputer) return language.logViewerForm_sleepComputer;
            if (raw == config.actionTypes.turnOffMonitor) return language.logViewerForm_turnOffMonitor;
            if (raw == config.actionTypes.appStarted) return language.logViewerForm_appStarted;
            if (raw == config.actionTypes.appTerminated) return language.logViewerForm_appTerminated;
            return raw;
        }

        private void HandleClearLogs()
        {
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\logs.json";
            if (File.Exists(logPath)) File.Delete(logPath);

            PostMessage("showToast", new
            {
                title = language.messageTitle_success,
                message = language.messageContent_clearedLogs,
                type = "success",
                duration = 2000
            });
        }

        private void HandleGetLanguageList()
        {
            var list = new List<object>();
            list.Add(new { langCode = "auto", langName = (language.settingsForm_combobox_auto_lang ?? "Auto") });

            foreach (var entry in languageSelector.GetLanguageNames())
            {
                list.Add(new { langCode = entry.langCode, langName = entry.LangName });
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
                title = language.messageTitle_success,
                message = language.pause_paused ?? "Actions paused successfully",
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
                title = language.messageTitle_success,
                message = language.pause_resumed ?? "Actions resumed",
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

        public void writeJsonToActionList()
        {
            jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "\\actionList.json", true,
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
            // If already open, focus it
            if (_subWindows.ContainsKey(pageName) && !_subWindows[pageName].IsDisposed)
            {
                _subWindows[pageName].Show();
                _subWindows[pageName].Focus();
                return;
            }

            // Determine title
            string title;
            switch (pageName)
            {
                case "main":
                    title = language.main_groupbox_newAction ?? "Actions";
                    break;
                case "settings":
                    title = language.settingsForm_Name ?? "Settings";
                    break;
                case "logs":
                    title = language.logViewerForm_Name ?? "Logs";
                    break;
                case "about":
                    title = language.about_menuItem ?? "About";
                    break;
                default:
                    title = language.main_FormName ?? "Windows Shutdown Helper";
                    break;
            }

            var win = new SubWindow(pageName, title);
            _subWindows[pageName] = win;

            win.FormClosed += (s, args) =>
            {
                _subWindows.Remove(pageName);
            };

            win.Show();
            win.Focus();
        }

        // =============== Timer & Action Execution ===============

        private void doAction(ActionModel action, uint idleTimeMin)
        {
            if (action == null) return;

            if (action.triggerType == config.triggerTypes.systemIdle)
            {
                if (!TryGetSystemIdleSeconds(action, out uint actionValueSeconds)) return;
                if (idleTimeMin == actionValueSeconds)
                {
                    Actions.doActionByTypes(action);
                }
                return;
            }

            if (action.triggerType == config.triggerTypes.certainTime && action.value == DateTime.Now.ToString("HH:mm:ss"))
            {
                if (isSkippedCertainTimeAction == false)
                {
                    Actions.doActionByTypes(action);
                }
                else
                {
                    isSkippedCertainTimeAction = false;
                }
            }

            if (action.triggerType == config.triggerTypes.fromNow && action.value == DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"))
            {
                Actions.doActionByTypes(action);
                actionList.Remove(action);
                writeJsonToActionList();
            }
        }

        private static bool TryGetSystemIdleSeconds(ActionModel action, out uint seconds)
        {
            seconds = 0;
            if (action == null || string.IsNullOrWhiteSpace(action.value))
            {
                return false;
            }

            if (!uint.TryParse(action.value, out uint parsed))
            {
                return false;
            }

            if (string.IsNullOrEmpty(action.valueUnit))
            {
                seconds = parsed * 60;
            }
            else
            {
                seconds = parsed;
            }

            return true;
        }

        private void timerTick(object sender, EventArgs e)
        {
            // Update time in UI
            string timeText = (language.main_statusBar_currentTime ?? "Time") + " : " + DateTime.Now + "  |  Build Id: " + BuildInfo.CommitId;
            PostMessage("updateTime", timeText);

            // Check pause expiration
            if (_isPaused && _pauseUntilTime.HasValue && DateTime.Now >= _pauseUntilTime.Value)
            {
                _isPaused = false;
                _pauseUntilTime = null;
                SendPauseStatus();
                PostMessage("showToast", new
                {
                    title = language.messageTitle_info ?? "Info",
                    message = language.pause_resumed ?? "Actions resumed",
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

            uint idleTimeMin = systemIdleDetector.GetLastInputTime();

            if (idleTimeMin == 0)
            {
                notifySystem.ResetIdleNotifications();
                timer.Stop();
                timer.Start();
            }

            if (isDeletedFromNotifier)
            {
                writeJsonToActionList();
                isDeletedFromNotifier = false;
            }

            foreach (ActionModel action in actionList.ToList())
            {
                doAction(action, idleTimeMin);
                notifySystem.showNotification(action, idleTimeMin);
            }
        }

        // =============== System Tray & Window Events ===============

        public void showMain()
        {
            Show();
            Focus();
            ShowInTaskbar = true;
        }

        private void notifyIcon_main_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            showMain();
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"))
            {
                settings = JsonSerializer.Deserialize<settings>(
                    File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"));

                if (settings.runInTaskbarWhenClosed)
                {
                    e.Cancel = true;
                    Hide();
                }
            }
        }

        private void mainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logger.doLog(config.actionTypes.appTerminated);
        }

        private void exitTheProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.doLog(config.actionTypes.appTerminated);
            Application.ExitThread();
        }

        private void addNewActionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showMain();
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
