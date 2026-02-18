using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using WindowsAutoPowerManager.Config;
using WindowsAutoPowerManager.Functions;

namespace WindowsAutoPowerManager
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
                        MainForm.Language?.MessageTitleError ?? "Error",
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

            string webViewPath = Path.Combine(AppContext.BaseDirectory, "WebView");
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.local", webViewPath,
                CoreWebView2HostResourceAccessKind.Allow);

            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            webView.CoreWebView2.DOMContentLoaded += OnDomContentLoaded;

            webView.CoreWebView2.Navigate("https://app.local/SubWindow.html?page=" + _pageName);
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
                Text = MainForm.Language?.CommonLoading ?? "Yükleniyor..."
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
            _loadingLabel.Text = MainForm.Language?.CommonLoading ?? "Yükleniyor...";
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

            // Always restore the assigned page when reopening a hidden sub-window.
            if (_webViewReady)
            {
                PostMessage("navigate", _pageName);
            }

            BringToFront();
            Activate();
        }

        protected override bool ShowWithoutActivation => true;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose && !MainForm.IsApplicationExiting)
            {
                e.Cancel = true;
                Hide();

                var main = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                if (main != null && !main.Visible)
                {
                    main.ShowMain();
                }

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

            var langDict = LanguagePayloadCache.Get(MainForm.Language);
            var displayActions = GetTranslatedActions();

            var main = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            var settingsObj = main?.GetCachedSettingsOrDefault() ?? LoadSettings();
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
                    var mainWindow = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                    if (mainWindow != null)
                    {
                        mainWindow.OpenSubWindow(page);
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
                case "closeWindow":
                    Close();
                    break;
            }
        }

        // =============== Action Handlers ===============

        private void HandleAddAction(JsonElement data)
        {
            if (MainForm.ActionList.Count >= 5)
            {
                PostMessage("showToast", new
                {
                    title = MainForm.Language.MessageTitleWarn,
                    message = MainForm.Language.MessageContentMaxActionWarn,
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
                    title = MainForm.Language.MessageTitleWarn,
                    message = MainForm.Language.MessageContentActionChoose,
                    type = "warn",
                    duration = 2000
                });
                PostMessage("addActionResult", new { success = false });
                return;
            }

            if (!ActionValidation.TryValidateActionForAdd(newAction, MainForm.ActionList, MainForm.Language, out string validationMessage))
            {
                PostMessage("showToast", new
                {
                    title = MainForm.Language.MessageTitleWarn,
                    message = validationMessage,
                    type = "warn",
                    duration = 3000
                });
                PostMessage("addActionResult", new { success = false });
                return;
            }

            MainForm.ActionList.Add(newAction);
            WriteActionList();

            PostMessage("showToast", new
            {
                title = MainForm.Language.MessageTitleSuccess,
                message = MainForm.Language.MessageContentActionCreated,
                type = "success",
                duration = 2000
            });
            PostMessage("addActionResult", new { success = true });
        }

        private void HandleUpdateAction(JsonElement data)
        {
            int index = data.GetProperty("index").GetInt32();
            if (index < 0 || index >= MainForm.ActionList.Count)
            {
                PostMessage("updateActionResult", new { success = false });
                return;
            }

            string createdDate = MainForm.ActionList[index]?.CreatedDate;
            if (string.IsNullOrWhiteSpace(createdDate))
            {
                createdDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            }

            if (!TryCreateActionModel(data, createdDate, out ActionModel updatedAction))
            {
                PostMessage("showToast", new
                {
                    title = MainForm.Language.MessageTitleWarn,
                    message = MainForm.Language.MessageContentActionChoose,
                    type = "warn",
                    duration = 2000
                });
                PostMessage("updateActionResult", new { success = false });
                return;
            }

            if (!ActionValidation.TryValidateActionForAdd(
                    updatedAction,
                    MainForm.ActionList.Where((_, actionIndex) => actionIndex != index),
                    MainForm.Language,
                    out string validationMessage))
            {
                PostMessage("showToast", new
                {
                    title = MainForm.Language.MessageTitleWarn,
                    message = validationMessage,
                    type = "warn",
                    duration = 3000
                });
                PostMessage("updateActionResult", new { success = false });
                return;
            }

            MainForm.ActionList[index] = updatedAction;
            WriteActionList();

            PostMessage("showToast", new
            {
                title = MainForm.Language.MessageTitleSuccess,
                message = MainForm.Language.MessageContentActionUpdated ?? MainForm.Language.MessageContentActionCreated,
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
            if (index >= 0 && index < MainForm.ActionList.Count)
            {
                MainForm.ActionList.RemoveAt(index);
                WriteActionList();

                PostMessage("showToast", new
                {
                    title = MainForm.Language.MessageTitleSuccess,
                    message = MainForm.Language.MessageContentActionDeleted,
                    type = "success",
                    duration = 2000
                });
            }
        }

        private void HandleClearAllActions()
        {
            MainForm.ActionList.Clear();
            WriteActionList();

            PostMessage("showToast", new
            {
                title = MainForm.Language.MessageTitleSuccess,
                message = MainForm.Language.MessageContentActionAllDeleted,
                type = "success",
                duration = 2000
            });
        }

        private void WriteActionList()
        {
            var main = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (main != null)
            {
                main.WriteJsonToActionList();
                return;
            }

            JsonWriter.WriteJson(AppContext.BaseDirectory + "\\ActionList.json", true, MainForm.ActionList);
            PostMessage("refreshActions", GetTranslatedActions());
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

            var main = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            string currentLang = main?.GetCachedSettingsOrDefault()?.Language ?? LoadSettings().Language;
            SettingsStorage.Save(newSettings);
            Logger.UpdateSettings(newSettings);
            main?.UpdateCachedSettings(newSettings);

            if (newSettings.StartWithWindows)
                StartWithWindows.AddStartup(MainForm.Language.SettingsFormAddStartupAppName ?? Constants.AppName);
            else
                StartWithWindows.DeleteStartup(MainForm.Language.SettingsFormAddStartupAppName ?? Constants.AppName);

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
                    title = MainForm.Language.MessageTitleSuccess,
                    message = MainForm.Language.MessageContentSettingSavedWithLangChanged,
                    type = "info",
                    duration = 4000
                });
            }
            else
            {
                PostMessage("showToast", new
                {
                    title = MainForm.Language.MessageTitleSuccess,
                    message = MainForm.Language.MessageContentSettingsSaved,
                    type = "success",
                    duration = 2000
                });
            }
        }

        private void HandleLoadSettings()
        {
            var main = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            var settingsObj = main?.GetCachedSettingsOrDefault() ?? LoadSettings();
            PostMessage("settingsLoaded", BuildSettingsPayload(settingsObj));
        }

        private void HandleLoadLogs()
        {
            var rawLogs = Logger.GetRecentLogs(250);
            var logs = rawLogs.Select(l => new
            {
                actionExecutedDate = l.ActionExecutedDate,
                actionType = TranslateLogAction(l.ActionType),
                actionTypeRaw = l.ActionType
            }).ToList();
            PostMessage("logsLoaded", logs);
        }

        private void HandleClearLogs()
        {
            Logger.Clear();

            PostMessage("showToast", new
            {
                title = MainForm.Language.MessageTitleSuccess,
                message = MainForm.Language.MessageContentClearedLogs,
                type = "success",
                duration = 2000
            });
        }

        private void HandleGetLanguageList()
        {
            var list = new List<object>();
            list.Add(new { LangCode = "auto", langName = (MainForm.Language.SettingsFormComboboxAutoLang ?? "Auto") });

            foreach (var entry in LanguageSelector.GetLanguageNames())
            {
                list.Add(new { LangCode = entry.LangCode, langName = entry.LangName });
            }

            PostMessage("languageList", list);
        }

        // =============== Helpers ===============

        private Settings LoadSettings()
        {
            return SettingsStorage.LoadOrDefault();
        }

        private List<Dictionary<string, string>> GetTranslatedActions()
        {
            var list = new List<Dictionary<string, string>>();
            foreach (var act in MainForm.ActionList)
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
            if (raw == Config.ActionTypes.LockComputer) return MainForm.Language.MainCboxActionTypeItemLockComputer;
            if (raw == Config.ActionTypes.ShutdownComputer) return MainForm.Language.MainCboxActionTypeItemShutdownComputer;
            if (raw == Config.ActionTypes.RestartComputer) return MainForm.Language.MainCboxActionTypeItemRestartComputer;
            if (raw == Config.ActionTypes.LogOffWindows) return MainForm.Language.MainCboxActionTypeItemLogOffWindows;
            if (raw == Config.ActionTypes.SleepComputer) return MainForm.Language.MainCboxActionTypeItemSleepComputer;
            if (raw == Config.ActionTypes.TurnOffMonitor) return MainForm.Language.MainCboxActionTypeItemTurnOffMonitor;
            return raw;
        }

        private string TranslateTrigger(string raw)
        {
            if (raw == Config.TriggerTypes.SystemIdle) return MainForm.Language.MainCboxTriggerTypeItemSystemIdle;
            if (raw == Config.TriggerTypes.CertainTime) return MainForm.Language.MainCboxTriggerTypeItemCertainTime;
            if (raw == Config.TriggerTypes.FromNow) return MainForm.Language.MainCboxTriggerTypeItemFromNow;
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
            if (raw == "seconds") return MainForm.Language.MainTimeUnitSeconds ?? "Seconds";
            if (string.IsNullOrEmpty(raw)) return MainForm.Language.MainTimeUnitMinutes ?? "Minutes";
            return raw;
        }

        private string TranslateLogAction(string raw)
        {
            if (raw == Config.ActionTypes.LockComputer) return MainForm.Language.LogViewerFormLockComputer;
            if (raw == Config.ActionTypes.LockComputerManually) return MainForm.Language.LogViewerFormLockComputerManually;
            if (raw == Config.ActionTypes.UnlockComputer) return MainForm.Language.LogViewerFormUnlockComputer;
            if (raw == Config.ActionTypes.ShutdownComputer) return MainForm.Language.LogViewerFormShutdownComputer;
            if (raw == Config.ActionTypes.RestartComputer) return MainForm.Language.LogViewerFormRestartComputer;
            if (raw == Config.ActionTypes.LogOffWindows) return MainForm.Language.LogViewerFormLogOffWindows;
            if (raw == Config.ActionTypes.SleepComputer) return MainForm.Language.LogViewerFormSleepComputer;
            if (raw == Config.ActionTypes.TurnOffMonitor) return MainForm.Language.LogViewerFormTurnOffMonitor;
            if (raw == Config.ActionTypes.AppStarted) return MainForm.Language.LogViewerFormAppStarted;
            if (raw == Config.ActionTypes.AppTerminated) return MainForm.Language.LogViewerFormAppTerminated;
            return raw;
        }
    }
}
