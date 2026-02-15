using System;
using System.Collections.Generic;
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

        public mainForm()
        {
            InitializeComponent();
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
            foreach (ActionModel action in actionList.ToList())
            {
                if (action.triggerType == config.triggerTypes.fromNow)
                {
                    DateTime actionDate = DateTime.Parse(action.value);
                    if (DateTime.Now > actionDate)
                    {
                        actionList.Remove(action);
                        writeJsonToActionList();
                    }
                }
            }
        }

        private async void mainForm_Load(object sender, EventArgs e)
        {
            Logger.doLog(config.actionTypes.appStarted);
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

            // Initialize WebView2
            await InitializeWebView();
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            var env = await CoreWebView2Environment.CreateAsync(null, Path.GetTempPath());
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
            SendInitData();
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

            var initData = new
            {
                language = langDict,
                actions = displayActions,
                settings = LoadSettings()
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
                language = data.GetProperty("language").GetString()
            };

            string currentLang = LoadSettings().language;
            jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json", true, newSettings);

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

            string langDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang");
            if (Directory.Exists(langDir))
            {
                foreach (string file in Directory.GetFiles(langDir, "lang_*.json"))
                {
                    string code = Path.GetFileNameWithoutExtension(file).Replace("lang_", "");
                    try
                    {
                        var langObj = JsonSerializer.Deserialize<language>(File.ReadAllText(file));
                        string name = langObj?.main_FormName ?? code.ToUpper();
                        list.Add(new { langCode = code, langName = code.ToUpper() + " - " + name });
                    }
                    catch
                    {
                        list.Add(new { langCode = code, langName = code.ToUpper() });
                    }
                }
            }

            PostMessage("languageList", list);
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
        }

        // =============== Timer & Action Execution ===============

        private void doAction(ActionModel action, uint idleTimeMin)
        {
            uint actionValueSeconds = string.IsNullOrEmpty(action.valueUnit)
                ? Convert.ToUInt32(action.value) * 60
                : Convert.ToUInt32(action.value);

            if (action.triggerType == config.triggerTypes.systemIdle && idleTimeMin == actionValueSeconds)
            {
                Actions.doActionByTypes(action);
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

        private void timerTick(object sender, EventArgs e)
        {
            // Update time in UI
            string timeText = (language.main_statusBar_currentTime ?? "Time") + " : " + DateTime.Now + "  |  Build Id: " + BuildInfo.CommitId;
            PostMessage("updateTime", timeText);

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
            showMain();
            PostMessage("navigate", "settings");
        }

        private void showTheLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showMain();
            PostMessage("navigate", "logs");
        }
    }
}
