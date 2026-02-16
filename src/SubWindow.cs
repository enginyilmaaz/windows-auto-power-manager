using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using WindowsShutdownHelper.functions;

namespace WindowsShutdownHelper
{
    public partial class SubWindow : Form
    {
        private readonly string _pageName;
        private bool _webViewReady;
        private bool _webViewInitStarted;
        private bool _isPrewarmedHidden;
        private bool _allowClose;
        private Panel _loadingOverlay;
        private Label _loadingLabel;
        private Timer _loadingDelayTimer;
        private const int LoadingOverlayDelayMs = 350;

        public SubWindow(string pageName, string title)
        {
            InitializeComponent();
            _pageName = pageName;
            Text = title;
            InitializeLoadingOverlay();
        }

        private async void SubWindow_Load(object sender, EventArgs e)
        {
            try
            {
                await EnsureWebViewInitializedAsync(true);
            }
            catch (Exception ex)
            {
                if (ShowInTaskbar || Opacity > 0)
                {
                    MessageBox.Show(
                        this,
                        "Arayuz acilamadi.\r\n\r\nDetay: " + ex.Message,
                        mainForm.language?.messageTitle_error ?? "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
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

            webView.CoreWebView2.Navigate("https://app.local/subwindow.html?page=" + _pageName);
        }

        private async System.Threading.Tasks.Task EnsureWebViewInitializedAsync(bool showLoading)
        {
            if (_webViewReady || _webViewInitStarted) return;
            _webViewInitStarted = true;
            if (showLoading)
            {
                ShowLoadingOverlay();
            }
            await InitializeWebView();
        }

        private void OnDomContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (_webViewReady) return;
            _webViewReady = true;
            HideLoadingOverlay();
            SendInitData();
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
                if (!_webViewReady && _loadingOverlay != null)
                {
                    _loadingOverlay.Visible = true;
                    _loadingOverlay.BringToFront();
                }
            };

            _loadingLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(95, 99, 112),
                Text = mainForm.language?.common_loading ?? "Yükleniyor..."
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
            _loadingLabel.Text = mainForm.language?.common_loading ?? "Yükleniyor...";
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

        // Warm up WebView while keeping window hidden to reduce first-open delay.
        public void PrewarmInBackground()
        {
            if (_webViewReady || _webViewInitStarted || IsDisposed) return;

            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new System.Drawing.Point(-32000, -32000);
            Opacity = 0;
            _isPrewarmedHidden = true;

            if (!IsHandleCreated)
            {
                var _ = Handle;
            }

            if (!webView.IsHandleCreated)
            {
                webView.CreateControl();
            }

            _ = EnsureWebViewInitializedAsync(false);
        }

        public void ShowForUser()
        {
            if (IsDisposed) return;

            ShowInTaskbar = true;
            Opacity = 1;

            if (_isPrewarmedHidden)
            {
                var area = Screen.PrimaryScreen.WorkingArea;
                Location = new System.Drawing.Point(
                    area.Left + Math.Max(0, (area.Width - Width) / 2),
                    area.Top + Math.Max(0, (area.Height - Height) / 2));
                _isPrewarmedHidden = false;
            }

            if (!Visible)
            {
                Show();
            }

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            BringToFront();
            Activate();
        }

        protected override bool ShowWithoutActivation => true;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose && !mainForm.isApplicationExiting)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        public void ForceClose()
        {
            _allowClose = true;
            Close();
        }

        private void SendInitData()
        {
            if (!_webViewReady) return;

            var langDict = new Dictionary<string, string>();
            foreach (PropertyInfo prop in typeof(Language).GetProperties())
            {
                var val = prop.GetValue(mainForm.language);
                if (val != null) langDict[prop.Name] = val.ToString();
            }

            var displayActions = GetTranslatedActions();

            var settingsObj = LoadSettings();
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

        private void PostMessage(string type, object data)
        {
            if (!_webViewReady || webView.CoreWebView2 == null) return;
            var msg = JsonSerializer.Serialize(new { type, data });
            webView.CoreWebView2.PostWebMessageAsJson(msg);
        }

        public void BroadcastRefreshActions()
        {
            if (!_webViewReady) return;
            PostMessage("refreshActions", GetTranslatedActions());
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
                case "openUrl":
                    string url = data.GetProperty("url").GetString();
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    break;
                case "closeWindow":
                    Close();
                    break;
            }
        }

        // =============== Action Handlers ===============

        private void HandleAddAction(JsonElement data)
        {
            string actionType = data.GetProperty("actionType").GetString();
            string triggerType = data.GetProperty("triggerType").GetString();

            if (actionType == "0" || triggerType == "0")
            {
                PostMessage("showToast", new
                {
                    title = mainForm.language.messageTitle_warn,
                    message = mainForm.language.messageContent_actionChoose,
                    type = "warn",
                    duration = 2000
                });
                PostMessage("addActionResult", new { success = false });
                return;
            }

            if (mainForm.actionList.Count >= 5)
            {
                PostMessage("showToast", new
                {
                    title = mainForm.language.messageTitle_warn,
                    message = mainForm.language.messageContent_maxActionWarn,
                    type = "warn",
                    duration = 2000
                });
                PostMessage("addActionResult", new { success = false });
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

            if (!actionValidation.TryValidateActionForAdd(newAction, mainForm.actionList, mainForm.language, out string validationMessage))
            {
                PostMessage("showToast", new
                {
                    title = mainForm.language.messageTitle_warn,
                    message = validationMessage,
                    type = "warn",
                    duration = 3000
                });
                PostMessage("addActionResult", new { success = false });
                return;
            }

            mainForm.actionList.Add(newAction);
            WriteActionList();

            PostMessage("showToast", new
            {
                title = mainForm.language.messageTitle_success,
                message = mainForm.language.messageContent_actionCreated,
                type = "success",
                duration = 2000
            });
            PostMessage("addActionResult", new { success = true });
        }

        private void HandleDeleteAction(JsonElement data)
        {
            int index = data.GetProperty("index").GetInt32();
            if (index >= 0 && index < mainForm.actionList.Count)
            {
                mainForm.actionList.RemoveAt(index);
                WriteActionList();

                PostMessage("showToast", new
                {
                    title = mainForm.language.messageTitle_success,
                    message = mainForm.language.messageContent_actionDeleted,
                    type = "success",
                    duration = 2000
                });
            }
        }

        private void HandleClearAllActions()
        {
            mainForm.actionList.Clear();
            WriteActionList();

            PostMessage("showToast", new
            {
                title = mainForm.language.messageTitle_success,
                message = mainForm.language.messageContent_actionAllDeleted,
                type = "success",
                duration = 2000
            });
        }

        private void WriteActionList()
        {
            jsonWriter.WriteJson(AppContext.BaseDirectory + "\\actionList.json", true,
                mainForm.actionList.ToList());

            // Refresh in this window
            PostMessage("refreshActions", GetTranslatedActions());

            // Broadcast to main form
            var main = Application.OpenForms.OfType<mainForm>().FirstOrDefault();
            if (main != null)
            {
                main.writeJsonToActionList();
            }
        }

        private void HandleSaveSettings(JsonElement data)
        {
            var newSettings = new Settings
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
            jsonWriter.WriteJson(AppContext.BaseDirectory + "\\settings.json", true, newSettings);

            if (newSettings.startWithWindows)
                startWithWindows.AddStartup(mainForm.language.settingsForm_addStartupAppName ?? "Windows Shutdown Helper");
            else
                startWithWindows.DeleteStartup(mainForm.language.settingsForm_addStartupAppName ?? "Windows Shutdown Helper");

            if (newSettings.isCountdownNotifierEnabled)
            {
                notifySystem.PrewarmCountdownNotifier();
            }

            if (currentLang != newSettings.language)
            {
                PostMessage("showToast", new
                {
                    title = mainForm.language.messageTitle_success,
                    message = mainForm.language.messageContent_settingSavedWithLangChanged,
                    type = "info",
                    duration = 4000
                });
            }
            else
            {
                PostMessage("showToast", new
                {
                    title = mainForm.language.messageTitle_success,
                    message = mainForm.language.messageContent_settingsSaved,
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

        private void HandleClearLogs()
        {
            string logPath = AppContext.BaseDirectory + "\\logs.json";
            if (File.Exists(logPath)) File.Delete(logPath);

            PostMessage("showToast", new
            {
                title = mainForm.language.messageTitle_success,
                message = mainForm.language.messageContent_clearedLogs,
                type = "success",
                duration = 2000
            });
        }

        private void HandleGetLanguageList()
        {
            var list = new List<object>();
            list.Add(new { langCode = "auto", langName = (mainForm.language.settingsForm_combobox_auto_lang ?? "Auto") });

            foreach (var entry in languageSelector.GetLanguageNames())
            {
                list.Add(new { langCode = entry.langCode, langName = entry.LangName });
            }

            PostMessage("languageList", list);
        }

        // =============== Helpers ===============

        private Settings LoadSettings()
        {
            if (File.Exists(AppContext.BaseDirectory + "\\settings.json"))
            {
                return JsonSerializer.Deserialize<Settings>(
                    File.ReadAllText(AppContext.BaseDirectory + "\\settings.json"));
            }
            return config.settingsINI.defaulSettingFile();
        }

        private List<Dictionary<string, string>> GetTranslatedActions()
        {
            var list = new List<Dictionary<string, string>>();
            foreach (var act in mainForm.actionList)
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
            if (raw == config.actionTypes.lockComputer) return mainForm.language.main_cbox_ActionType_Item_lockComputer;
            if (raw == config.actionTypes.shutdownComputer) return mainForm.language.main_cbox_ActionType_Item_shutdownComputer;
            if (raw == config.actionTypes.restartComputer) return mainForm.language.main_cbox_ActionType_Item_restartComputer;
            if (raw == config.actionTypes.logOffWindows) return mainForm.language.main_cbox_ActionType_Item_logOffWindows;
            if (raw == config.actionTypes.sleepComputer) return mainForm.language.main_cbox_ActionType_Item_sleepComputer;
            if (raw == config.actionTypes.turnOffMonitor) return mainForm.language.main_cbox_ActionType_Item_turnOffMonitor;
            return raw;
        }

        private string TranslateTrigger(string raw)
        {
            if (raw == config.triggerTypes.systemIdle) return mainForm.language.main_cbox_TriggerType_Item_systemIdle;
            if (raw == config.triggerTypes.certainTime) return mainForm.language.main_cbox_TriggerType_Item_certainTime;
            if (raw == config.triggerTypes.fromNow) return mainForm.language.main_cbox_TriggerType_Item_fromNow;
            return raw;
        }

        private string TranslateUnit(string raw)
        {
            if (raw == "seconds") return mainForm.language.main_timeUnit_seconds ?? "Seconds";
            if (string.IsNullOrEmpty(raw)) return mainForm.language.main_timeUnit_minutes ?? "Minutes";
            return raw;
        }

        private string TranslateLogAction(string raw)
        {
            if (raw == config.actionTypes.lockComputer) return mainForm.language.logViewerForm_lockComputer;
            if (raw == config.actionTypes.lockComputerManually) return mainForm.language.logViewerForm_lockComputerManually;
            if (raw == config.actionTypes.unlockComputer) return mainForm.language.logViewerForm_unlockComputer;
            if (raw == config.actionTypes.shutdownComputer) return mainForm.language.logViewerForm_shutdownComputer;
            if (raw == config.actionTypes.restartComputer) return mainForm.language.logViewerForm_restartComputer;
            if (raw == config.actionTypes.logOffWindows) return mainForm.language.logViewerForm_logOffWindows;
            if (raw == config.actionTypes.sleepComputer) return mainForm.language.logViewerForm_sleepComputer;
            if (raw == config.actionTypes.turnOffMonitor) return mainForm.language.logViewerForm_turnOffMonitor;
            if (raw == config.actionTypes.appStarted) return mainForm.language.logViewerForm_appStarted;
            if (raw == config.actionTypes.appTerminated) return mainForm.language.logViewerForm_appTerminated;
            return raw;
        }
    }
}
