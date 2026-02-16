using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace WindowsShutdownHelper.Functions
{
    internal class NotifySystem
    {
        public static Language Language = LanguageSelector.LanguageFile();
        public static string ActionTypeName;
        private static HashSet<string> _notifiedIdleActions = new HashSet<string>();
        private static ActionCountdownNotifier _sharedCountdownNotifier;

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
                _sharedCountdownNotifier = new ActionCountdownNotifier();
            }
        }

        private static bool IsCountdownNotifierVisible()
        {
            return Application.OpenForms.OfType<ActionCountdownNotifier>()
                .Any(form => form.Visible && form.Opacity > 0);
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
                Language.MessageTitleInfo,
                Language.MessageContentCountdownNotify,
                Language.MessageContentCountdownNotify2,
                ActionTypeName,
                infoText,
                countdownSeconds,
                action);
            return true;
        }

        public static void ShowNotification(ActionModel action, uint idleTimeMin)
        {
            Settings settings = new Settings();

            if (File.Exists(AppContext.BaseDirectory + "\\settings.json"))
            {
                settings = JsonSerializer.Deserialize<Settings>(
                    File.ReadAllText(AppContext.BaseDirectory + "\\settings.json"));
            }
            else
            {
                settings.IsCountdownNotifierEnabled = false;
            }

            if (settings.IsCountdownNotifierEnabled)
            {
                ActionTypeLocalization(action);

                if (action.TriggerType == Config.TriggerTypes.SystemIdle)
                {
                    if (!TryGetSystemIdleSeconds(action, out int actionValue)) return;
                    string actionKey = action.CreatedDate + "_" + action.ActionType;

                    if (idleTimeMin >= actionValue - settings.CountdownNotifierSeconds
                        && !_notifiedIdleActions.Contains(actionKey))
                    {
                        bool shown = ShowCountdownNotification(
                            Language.MessageContentCancelForSystemIdle,
                            settings.CountdownNotifierSeconds,
                            action);
                        if (shown)
                        {
                            _notifiedIdleActions.Add(actionKey);
                        }
                    }
                }

                else if (action.TriggerType == Config.TriggerTypes.FromNow)
                {
                    if (!DateTime.TryParseExact(
                        action.Value,
                        "dd.MM.yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime actionExecuteDate))
                    {
                        return;
                    }

                    actionExecuteDate = actionExecuteDate.AddSeconds(-settings.CountdownNotifierSeconds);
                    string nowDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    string executionDate = actionExecuteDate.ToString("dd.MM.yyyy HH:mm:ss");

                    if (executionDate == nowDate)
                    {
                        ShowCountdownNotification(
                            Language.MessageContentYouCanThat,
                            settings.CountdownNotifierSeconds,
                            action);
                    }
                }


                else if (action.TriggerType == Config.TriggerTypes.CertainTime)
                {
                    if (!DateTime.TryParseExact(
                        action.Value,
                        "HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime actionExecuteDate))
                    {
                        return;
                    }

                    actionExecuteDate = actionExecuteDate.AddSeconds(-settings.CountdownNotifierSeconds);
                    string nowDate = DateTime.Now.ToString("HH:mm:ss");
                    string executionDate = actionExecuteDate.ToString("HH:mm:ss");

                    if (executionDate == nowDate)
                    {
                        ShowCountdownNotification(
                            Language.MessageContentYouCanThat,
                            settings.CountdownNotifierSeconds,
                            action);
                    }
                }
            }
        }

        private static bool TryGetSystemIdleSeconds(ActionModel action, out int seconds)
        {
            seconds = 0;
            if (action == null || string.IsNullOrWhiteSpace(action.Value))
            {
                return false;
            }

            if (!int.TryParse(action.Value, out int parsed))
            {
                return false;
            }

            if (parsed <= 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(action.ValueUnit))
            {
                if (parsed > int.MaxValue / 60)
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


        public static void ActionTypeLocalization(ActionModel action)
        {
            if (action.ActionType == Config.ActionTypes.LockComputer)
            {
                ActionTypeName = Language.MainCboxActionTypeItemLockComputer;
            }
            else if (action.ActionType == Config.ActionTypes.ShutdownComputer)
            {
                ActionTypeName = Language.MainCboxActionTypeItemShutdownComputer;
            }
            else if (action.ActionType == Config.ActionTypes.RestartComputer)
            {
                ActionTypeName = Language.MainCboxActionTypeItemRestartComputer;
            }
            else if (action.ActionType == Config.ActionTypes.LogOffWindows)
            {
                ActionTypeName = Language.MainCboxActionTypeItemLogOffWindows;
            }
            else if (action.ActionType == Config.ActionTypes.SleepComputer)
            {
                ActionTypeName = Language.MainCboxActionTypeItemSleepComputer;
            }
            else if (action.ActionType == Config.ActionTypes.TurnOffMonitor)
            {
                ActionTypeName = Language.MainCboxActionTypeItemTurnOffMonitor;
            }
        }
    }
}
