using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsShutdownHelper.Functions
{
    internal static class ActionValidation
    {
        private const string FromNowDateFormat = "dd.MM.yyyy HH:mm:ss";
        private const string CertainTimeFormat = "HH:mm:ss";

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
            out string errorMessage)
        {
            errorMessage = null;

            if (newAction == null)
            {
                errorMessage = language?.MessageContentActionChoose ?? "Invalid action.";
                return false;
            }

            if (!TryGetActionOrderValue(newAction, out long newActionOrderValue))
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

                if (!string.Equals(existingAction.TriggerType, newAction.TriggerType, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!TryGetActionOrderValue(existingAction, out long existingActionOrderValue))
                {
                    continue;
                }

                if (HasSameExecutionPoint(existingAction, newAction, existingActionOrderValue, newActionOrderValue) &&
                    string.Equals(existingAction.ActionType, newAction.ActionType, StringComparison.Ordinal))
                {
                    errorMessage = language?.MessageContentIdleActionConflict
                        ?? "This action conflicts with existing actions.";
                    return false;
                }

                if (newAction.TriggerType == Config.TriggerTypes.SystemIdle &&
                    string.Equals(existingAction.ActionType, newAction.ActionType, StringComparison.Ordinal))
                {
                    errorMessage = language?.MessageContentIdleActionConflict
                        ?? "This action conflicts with existing actions.";
                    return false;
                }

                if (HasGuaranteedConflict(existingAction, newAction, existingActionOrderValue, newActionOrderValue))
                {
                    errorMessage = language?.MessageContentIdleActionConflict
                        ?? "This action conflicts with existing actions.";
                    return false;
                }
            }

            return true;
        }

        private static bool IsSessionEndingAction(string actionType)
        {
            return !string.IsNullOrWhiteSpace(actionType) && SessionEndingActions.Contains(actionType);
        }

        private static bool HasSameExecutionPoint(ActionModel first, ActionModel second, long firstOrder, long secondOrder)
        {
            return string.Equals(first.TriggerType, second.TriggerType, StringComparison.Ordinal)
                && firstOrder == secondOrder;
        }

        private static bool HasGuaranteedConflict(
            ActionModel existingAction,
            ActionModel newAction,
            long existingActionOrderValue,
            long newActionOrderValue)
        {
            bool existingIsSessionEnding = IsSessionEndingAction(existingAction.ActionType);
            bool newIsSessionEnding = IsSessionEndingAction(newAction.ActionType);

            if (!existingIsSessionEnding && !newIsSessionEnding)
            {
                return false;
            }

            if (newAction.TriggerType == Config.TriggerTypes.CertainTime)
            {
                return existingActionOrderValue == newActionOrderValue;
            }

            if (existingIsSessionEnding && existingActionOrderValue <= newActionOrderValue)
            {
                return true;
            }

            if (newIsSessionEnding && newActionOrderValue <= existingActionOrderValue)
            {
                return true;
            }

            return false;
        }

        private static bool TryGetActionOrderValue(ActionModel action, out long orderValue)
        {
            orderValue = 0;
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

                orderValue = systemIdleSeconds;
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

                orderValue = scheduledDate.Ticks;
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

                orderValue = (long)dailyTime.TimeOfDay.TotalSeconds;
                return true;
            }

            return false;
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
