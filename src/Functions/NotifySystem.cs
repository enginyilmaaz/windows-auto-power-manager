using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace WindowsShutdownHelper.functions
{
    internal class notifySystem
    {
        public static Language language = languageSelector.languageFile();
        public static string actionTypeName;
        private static HashSet<string> _notifiedIdleActions = new HashSet<string>();
        private static actionCountdownNotifier _sharedCountdownNotifier;

        public static void ResetIdleNotifications()
        {
            _notifiedIdleActions.Clear();
        }

        public static void PrewarmCountdownNotifier()
        {
            EnsureCountdownNotifier();
            _sharedCountdownNotifier.PrewarmInBackground();
        }

        private static void EnsureCountdownNotifier()
        {
            if (_sharedCountdownNotifier == null || _sharedCountdownNotifier.IsDisposed)
            {
                _sharedCountdownNotifier = new actionCountdownNotifier();
            }
        }

        private static bool IsCountdownNotifierVisible()
        {
            return Application.OpenForms.OfType<actionCountdownNotifier>().Any(form => form.Visible);
        }

        private static bool ShowCountdownNotification(
            string infoText,
            int countdownSeconds,
            ActionModel action)
        {
            if (IsCountdownNotifierVisible())
            {
                return false;
            }

            EnsureCountdownNotifier();
            _sharedCountdownNotifier.ConfigureAndShow(
                language.messageTitle_info,
                language.messageContent_CountdownNotify,
                language.messageContent_CountdownNotify_2,
                actionTypeName,
                infoText,
                countdownSeconds,
                action);
            return true;
        }

        public static void showNotification(ActionModel action, uint idleTimeMin)
        {
            Settings settings = new Settings();

            if (File.Exists(AppContext.BaseDirectory + "\\settings.json"))
            {
                settings = JsonSerializer.Deserialize<Settings>(
                    File.ReadAllText(AppContext.BaseDirectory + "\\settings.json"));
            }
            else
            {
                settings.isCountdownNotifierEnabled = false;
            }

            if (settings.isCountdownNotifierEnabled)
            {
                actionTypeLocalization(action);

                if (action.triggerType == config.triggerTypes.systemIdle)
                {
                    if (!TryGetSystemIdleSeconds(action, out int actionValue)) return;
                    string actionKey = action.createdDate + "_" + action.actionType;

                    if (idleTimeMin >= actionValue - settings.countdownNotifierSeconds
                        && !_notifiedIdleActions.Contains(actionKey))
                    {
                        bool shown = ShowCountdownNotification(
                            language.messageContent_cancelForSystemIdle,
                            settings.countdownNotifierSeconds,
                            action);
                        if (shown)
                        {
                            _notifiedIdleActions.Add(actionKey);
                        }
                    }
                }

                else if (action.triggerType == config.triggerTypes.fromNow)
                {
                    if (!DateTime.TryParseExact(
                        action.value,
                        "dd.MM.yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime actionExecuteDate))
                    {
                        return;
                    }

                    actionExecuteDate = actionExecuteDate.AddSeconds(-settings.countdownNotifierSeconds);
                    string nowDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    string executionDate = actionExecuteDate.ToString("dd.MM.yyyy HH:mm:ss");

                    if (executionDate == nowDate)
                    {
                        ShowCountdownNotification(
                            language.messageContent_youCanThat,
                            settings.countdownNotifierSeconds,
                            action);
                    }
                }


                else if (action.triggerType == config.triggerTypes.certainTime)
                {
                    if (!DateTime.TryParseExact(
                        action.value,
                        "HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime actionExecuteDate))
                    {
                        return;
                    }

                    actionExecuteDate = actionExecuteDate.AddSeconds(-settings.countdownNotifierSeconds);
                    string nowDate = DateTime.Now.ToString("HH:mm:ss");
                    string executionDate = actionExecuteDate.ToString("HH:mm:ss");

                    if (executionDate == nowDate)
                    {
                        ShowCountdownNotification(
                            language.messageContent_youCanThat,
                            settings.countdownNotifierSeconds,
                            action);
                    }
                }
            }
        }

        private static bool TryGetSystemIdleSeconds(ActionModel action, out int seconds)
        {
            seconds = 0;
            if (action == null || string.IsNullOrWhiteSpace(action.value))
            {
                return false;
            }

            if (!int.TryParse(action.value, out int parsed))
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


        public static void actionTypeLocalization(ActionModel action)
        {
            if (action.actionType == config.actionTypes.lockComputer)
            {
                actionTypeName = language.main_cbox_ActionType_Item_lockComputer;
            }
            else if (action.actionType == config.actionTypes.shutdownComputer)
            {
                actionTypeName = language.main_cbox_ActionType_Item_shutdownComputer;
            }
            else if (action.actionType == config.actionTypes.restartComputer)
            {
                actionTypeName = language.main_cbox_ActionType_Item_restartComputer;
            }
            else if (action.actionType == config.actionTypes.logOffWindows)
            {
                actionTypeName = language.main_cbox_ActionType_Item_logOffWindows;
            }
            else if (action.actionType == config.actionTypes.sleepComputer)
            {
                actionTypeName = language.main_cbox_ActionType_Item_sleepComputer;
            }
            else if (action.actionType == config.actionTypes.turnOffMonitor)
            {
                actionTypeName = language.main_cbox_ActionType_Item_turnOffMonitor;
            }
        }
    }
}
