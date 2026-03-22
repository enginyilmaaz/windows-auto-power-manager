using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsAutoPowerManager.Functions
{
    internal static class ActionValidation
    {
        private const string FromNowDateFormat = "dd.MM.yyyy HH:mm:ss";
        private const string CertainTimeFormat = "HH:mm:ss";

        private enum ActionBehavior
        {
            DisplayOnly,
            LockSession,
            SuspendSystem,
            EndSession
        }

        private enum TriggerScheduleKind
        {
            SystemIdle,
            DailyTime,
            AbsoluteDateTime
        }

        private readonly struct TriggerSchedule
        {
            public TriggerSchedule(TriggerScheduleKind kind, long orderValue, DateTime absoluteTime, TimeSpan timeOfDay)
            {
                Kind = kind;
                OrderValue = orderValue;
                AbsoluteTime = absoluteTime;
                TimeOfDay = timeOfDay;
            }

            public TriggerScheduleKind Kind { get; }
            public long OrderValue { get; }
            public DateTime AbsoluteTime { get; }
            public TimeSpan TimeOfDay { get; }
        }

        private static readonly HashSet<string> SessionEndingActions = new HashSet<string>
        {
            Config.ActionTypes.ShutdownComputer,
            Config.ActionTypes.RestartComputer,
            Config.ActionTypes.LogOffWindows
        };

        public static bool TryValidateActionForAdd(
            ActionModel newAction,
            IEnumerable<ActionModel> existingActions,
            Language language,
            out string errorMessage,
            bool candidateWillBeEnabled = true)
        {
            errorMessage = null;

            if (newAction == null)
            {
                errorMessage = language?.MessageContentActionChoose ?? "Invalid action.";
                return false;
            }

            if (!candidateWillBeEnabled)
            {
                return true;
            }

            if (!TryGetTriggerSchedule(newAction, out TriggerSchedule newActionSchedule))
            {
                errorMessage = language?.MessageContentActionChoose ?? "Invalid action value.";
                return false;
            }

            foreach (ActionModel existingAction in existingActions)
            {
                if (existingAction == null)
                {
                    continue;
                }

                if (!existingAction.IsEnabled)
                {
                    continue;
                }

                if (!TryGetTriggerSchedule(existingAction, out TriggerSchedule existingActionSchedule))
                {
                    continue;
                }

                if (ActionsConflict(existingAction, existingActionSchedule, newAction, newActionSchedule))
                {
                    errorMessage = language?.MessageContentIdleActionConflict
                        ?? "This action conflicts with existing actions.";
                    return false;
                }
            }

            return true;
        }

        private static bool ActionsConflict(
            ActionModel existingAction,
            TriggerSchedule existingActionSchedule,
            ActionModel newAction,
            TriggerSchedule newActionSchedule)
        {
            if (existingActionSchedule.Kind == TriggerScheduleKind.SystemIdle &&
                newActionSchedule.Kind == TriggerScheduleKind.SystemIdle &&
                string.Equals(existingAction.ActionType, newAction.ActionType, StringComparison.Ordinal))
            {
                return true;
            }

            if (!TryGetComparableExecutionValues(
                    existingActionSchedule,
                    newActionSchedule,
                    out long existingComparableValue,
                    out long newComparableValue))
            {
                return false;
            }

            if (HasSameExecutionPoint(existingAction, newAction, existingComparableValue, newComparableValue))
            {
                return true;
            }

            if (EarlierActionBlocksLater(existingAction, existingComparableValue, newComparableValue))
            {
                return true;
            }

            if (EarlierActionBlocksLater(newAction, newComparableValue, existingComparableValue))
            {
                return true;
            }

            return false;
        }

        private static bool IsSessionEndingAction(string actionType)
        {
            return !string.IsNullOrWhiteSpace(actionType) && SessionEndingActions.Contains(actionType);
        }

        private static bool HasSameExecutionPoint(ActionModel first, ActionModel second, long firstOrder, long secondOrder)
        {
            if (firstOrder != secondOrder)
            {
                return false;
            }

            if (string.Equals(first.ActionType, second.ActionType, StringComparison.Ordinal))
            {
                return true;
            }

            ActionBehavior firstBehavior = GetActionBehavior(first.ActionType);
            ActionBehavior secondBehavior = GetActionBehavior(second.ActionType);
            return IsBlockingBehavior(firstBehavior) || IsBlockingBehavior(secondBehavior);
        }

        private static bool EarlierActionBlocksLater(ActionModel earlierAction, long earlierOrderValue, long laterOrderValue)
        {
            if (earlierOrderValue >= laterOrderValue)
            {
                return false;
            }

            return IsBlockingBehavior(GetActionBehavior(earlierAction.ActionType));
        }

        private static bool TryGetComparableExecutionValues(
            TriggerSchedule firstSchedule,
            TriggerSchedule secondSchedule,
            out long firstComparableValue,
            out long secondComparableValue)
        {
            firstComparableValue = 0;
            secondComparableValue = 0;

            if (firstSchedule.Kind == secondSchedule.Kind)
            {
                firstComparableValue = firstSchedule.OrderValue;
                secondComparableValue = secondSchedule.OrderValue;
                return true;
            }

            if (firstSchedule.Kind == TriggerScheduleKind.AbsoluteDateTime &&
                secondSchedule.Kind == TriggerScheduleKind.DailyTime)
            {
                firstComparableValue = firstSchedule.AbsoluteTime.Ticks;
                secondComparableValue = firstSchedule.AbsoluteTime.Date.Add(secondSchedule.TimeOfDay).Ticks;
                return true;
            }

            if (firstSchedule.Kind == TriggerScheduleKind.DailyTime &&
                secondSchedule.Kind == TriggerScheduleKind.AbsoluteDateTime)
            {
                firstComparableValue = secondSchedule.AbsoluteTime.Date.Add(firstSchedule.TimeOfDay).Ticks;
                secondComparableValue = secondSchedule.AbsoluteTime.Ticks;
                return true;
            }

            return false;
        }

        private static bool TryGetTriggerSchedule(ActionModel action, out TriggerSchedule schedule)
        {
            schedule = default;
            if (action == null || string.IsNullOrWhiteSpace(action.TriggerType))
            {
                return false;
            }

            if (action.TriggerType == Config.TriggerTypes.SystemIdle)
            {
                if (!TryGetSystemIdleSeconds(action, out int systemIdleSeconds) || systemIdleSeconds <= 0)
                {
                    return false;
                }

                schedule = new TriggerSchedule(
                    TriggerScheduleKind.SystemIdle,
                    systemIdleSeconds,
                    default,
                    default);
                return true;
            }

            if (action.TriggerType == Config.TriggerTypes.FromNow)
            {
                if (string.IsNullOrWhiteSpace(action.Value))
                {
                    return false;
                }

                if (!DateTime.TryParseExact(
                        action.Value,
                        FromNowDateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime scheduledDate))
                {
                    return false;
                }

                schedule = new TriggerSchedule(
                    TriggerScheduleKind.AbsoluteDateTime,
                    scheduledDate.Ticks,
                    scheduledDate,
                    default);
                return true;
            }

            if (action.TriggerType == Config.TriggerTypes.CertainTime)
            {
                if (string.IsNullOrWhiteSpace(action.Value))
                {
                    return false;
                }

                if (!DateTime.TryParseExact(
                        action.Value,
                        CertainTimeFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime dailyTime))
                {
                    return false;
                }

                schedule = new TriggerSchedule(
                    TriggerScheduleKind.DailyTime,
                    (long)dailyTime.TimeOfDay.TotalSeconds,
                    default,
                    dailyTime.TimeOfDay);
                return true;
            }

            return false;
        }

        private static ActionBehavior GetActionBehavior(string actionType)
        {
            if (string.Equals(actionType, Config.ActionTypes.LockComputer, StringComparison.Ordinal))
            {
                return ActionBehavior.LockSession;
            }

            if (string.Equals(actionType, Config.ActionTypes.SleepComputer, StringComparison.Ordinal))
            {
                return ActionBehavior.SuspendSystem;
            }

            if (IsSessionEndingAction(actionType))
            {
                return ActionBehavior.EndSession;
            }

            return ActionBehavior.DisplayOnly;
        }

        private static bool IsBlockingBehavior(ActionBehavior behavior)
        {
            return behavior == ActionBehavior.SuspendSystem || behavior == ActionBehavior.EndSession;
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
    }
}
