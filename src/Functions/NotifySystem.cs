using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsAutoPowerManager.Functions
{
    internal class NotifySystem
    {
        public static Language Language = LanguageSelector.LanguageFile();
        public static string ActionTypeName;
        private static readonly HashSet<string> _notifiedIdleActions = new HashSet<string>();
        private static readonly HashSet<string> _notifiedFromNowActions = new HashSet<string>();
        private static readonly Dictionary<string, DateTime> _notifiedCertainTimeDates = new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, DateTime> _fromNowExecutionCache = new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, TimeSpan> _certainTimeCache = new Dictionary<string, TimeSpan>();
        private static readonly Dictionary<string, int> _idleSecondsCache = new Dictionary<string, int>();
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
            return _sharedCountdownNotifier != null &&
                   !_sharedCountdownNotifier.IsDisposed &&
                   _sharedCountdownNotifier.Visible &&
                   _sharedCountdownNotifier.Opacity > 0;
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

        public static void ShowNotification(ActionModel action, uint idleTimeSec, Settings settings, DateTime now)
        {
            if (action == null || settings == null || !settings.IsCountdownNotifierEnabled)
            {
                return;
            }

            ActionTypeLocalization(action);
            string actionKey = BuildActionKey(action);

            if (action.TriggerType == Config.TriggerTypes.SystemIdle)
            {
                if (!TryGetCachedSystemIdleSeconds(action, actionKey, out int actionValue))
                {
                    return;
                }

                string idleNotifyKey = (action.CreatedDate ?? string.Empty) + "_" + (action.ActionType ?? string.Empty);

                if (idleTimeSec >= actionValue - settings.CountdownNotifierSeconds &&
                    !_notifiedIdleActions.Contains(idleNotifyKey))
                {
                    bool shown = ShowCountdownNotification(
                        Language.MessageContentCancelForSystemIdle,
                        settings.CountdownNotifierSeconds,
                        action);

                    if (shown)
                    {
                        _notifiedIdleActions.Add(idleNotifyKey);
                    }
                }
                return;
            }

            if (action.TriggerType == Config.TriggerTypes.FromNow)
            {
                if (!TryGetCachedFromNowExecution(action, actionKey, out DateTime actionExecuteDate))
                {
                    return;
                }

                DateTime notificationTime = actionExecuteDate.AddSeconds(-settings.CountdownNotifierSeconds);
                if (now >= actionExecuteDate)
                {
                    return;
                }

                if (now >= notificationTime && !_notifiedFromNowActions.Contains(actionKey))
                {
                    bool shown = ShowCountdownNotification(
                        Language.MessageContentYouCanThat,
                        settings.CountdownNotifierSeconds,
                        action);
                    if (shown)
                    {
                        _notifiedFromNowActions.Add(actionKey);
                    }
                }
                return;
            }

            if (action.TriggerType == Config.TriggerTypes.CertainTime)
            {
                if (!TryGetCachedCertainTime(action, actionKey, out TimeSpan executionTime))
                {
                    return;
                }

                DateTime executionToday = now.Date.Add(executionTime);
                DateTime notificationTime = executionToday.AddSeconds(-settings.CountdownNotifierSeconds);
                bool isInNotificationWindow = now >= notificationTime && now < executionToday;
                bool isNotifiedToday = _notifiedCertainTimeDates.TryGetValue(actionKey, out DateTime notifiedDate) &&
                                       notifiedDate.Date == now.Date;

                if (isInNotificationWindow && !isNotifiedToday)
                {
                    bool shown = ShowCountdownNotification(
                        Language.MessageContentYouCanThat,
                        settings.CountdownNotifierSeconds,
                        action);

                    if (shown)
                    {
                        _notifiedCertainTimeDates[actionKey] = now.Date;
                    }
                }
            }
        }

        public static void CleanupState(IEnumerable<ActionModel> actions)
        {
            var validKeys = new HashSet<string>(actions.Select(BuildActionKey));
            var validIdleKeys = new HashSet<string>(actions.Select(a =>
                (a?.CreatedDate ?? string.Empty) + "_" + (a?.ActionType ?? string.Empty)));

            _notifiedIdleActions.RemoveWhere(key => !validIdleKeys.Contains(key));
            _notifiedFromNowActions.RemoveWhere(key => !validKeys.Contains(key));

            foreach (string key in _notifiedCertainTimeDates.Keys.ToList())
            {
                if (!validKeys.Contains(key))
                {
                    _notifiedCertainTimeDates.Remove(key);
                }
            }

            foreach (string key in _fromNowExecutionCache.Keys.ToList())
            {
                if (!validKeys.Contains(key))
                {
                    _fromNowExecutionCache.Remove(key);
                }
            }

            foreach (string key in _certainTimeCache.Keys.ToList())
            {
                if (!validKeys.Contains(key))
                {
                    _certainTimeCache.Remove(key);
                }
            }

            foreach (string key in _idleSecondsCache.Keys.ToList())
            {
                if (!validKeys.Contains(key))
                {
                    _idleSecondsCache.Remove(key);
                }
            }
        }

        private static string BuildActionKey(ActionModel action)
        {
            if (action == null)
            {
                return string.Empty;
            }

            return (action.CreatedDate ?? string.Empty) + "|" +
                   (action.TriggerType ?? string.Empty) + "|" +
                   (action.ActionType ?? string.Empty) + "|" +
                   (action.Value ?? string.Empty);
        }

        private static bool TryGetCachedFromNowExecution(ActionModel action, string actionKey, out DateTime executionDate)
        {
            if (_fromNowExecutionCache.TryGetValue(actionKey, out executionDate))
            {
                return true;
            }

            if (action == null || string.IsNullOrWhiteSpace(action.Value))
            {
                executionDate = default;
                return false;
            }

            if (!DateTime.TryParseExact(
                    action.Value,
                    "dd.MM.yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out executionDate))
            {
                return false;
            }

            _fromNowExecutionCache[actionKey] = executionDate;
            return true;
        }

        private static bool TryGetCachedCertainTime(ActionModel action, string actionKey, out TimeSpan executionTime)
        {
            if (_certainTimeCache.TryGetValue(actionKey, out executionTime))
            {
                return true;
            }

            if (action == null || string.IsNullOrWhiteSpace(action.Value))
            {
                executionTime = default;
                return false;
            }

            if (!DateTime.TryParseExact(
                    action.Value,
                    "HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsed))
            {
                executionTime = default;
                return false;
            }

            executionTime = parsed.TimeOfDay;
            _certainTimeCache[actionKey] = executionTime;
            return true;
        }

        private static bool TryGetCachedSystemIdleSeconds(ActionModel action, string actionKey, out int seconds)
        {
            if (_idleSecondsCache.TryGetValue(actionKey, out seconds))
            {
                return true;
            }

            if (!TryGetSystemIdleSeconds(action, out seconds))
            {
                return false;
            }

            _idleSecondsCache[actionKey] = seconds;
            return true;
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
            if (action == null)
            {
                ActionTypeName = string.Empty;
                return;
            }

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
