using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsShutdownHelper.functions
{
    internal static class actionValidation
    {
        private const string FromNowDateFormat = "dd.MM.yyyy HH:mm:ss";
        private const string CertainTimeFormat = "HH:mm:ss";

        private static readonly HashSet<string> SessionEndingActions = new HashSet<string>
        {
            config.actionTypes.shutdownComputer,
            config.actionTypes.restartComputer,
            config.actionTypes.logOffWindows
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
                errorMessage = language?.messageContent_actionChoose ?? "Invalid action.";
                return false;
            }

            if (!TryGetActionOrderValue(newAction, out long newActionOrderValue))
            {
                return true;
            }

            foreach (ActionModel existingAction in existingActions)
            {
                if (existingAction == null)
                {
                    continue;
                }

                if (!string.Equals(existingAction.triggerType, newAction.triggerType, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!TryGetActionOrderValue(existingAction, out long existingActionOrderValue))
                {
                    continue;
                }

                if (HasSameExecutionPoint(existingAction, newAction, existingActionOrderValue, newActionOrderValue) &&
                    string.Equals(existingAction.actionType, newAction.actionType, StringComparison.Ordinal))
                {
                    errorMessage = language?.messageContent_idleActionConflict
                        ?? "This action conflicts with existing actions.";
                    return false;
                }

                if (newAction.triggerType == config.triggerTypes.systemIdle &&
                    string.Equals(existingAction.actionType, newAction.actionType, StringComparison.Ordinal))
                {
                    errorMessage = language?.messageContent_idleActionConflict
                        ?? "This action conflicts with existing actions.";
                    return false;
                }

                if (HasGuaranteedConflict(existingAction, newAction, existingActionOrderValue, newActionOrderValue))
                {
                    errorMessage = language?.messageContent_idleActionConflict
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
            return string.Equals(first.triggerType, second.triggerType, StringComparison.Ordinal)
                && firstOrder == secondOrder;
        }

        private static bool HasGuaranteedConflict(
            ActionModel existingAction,
            ActionModel newAction,
            long existingActionOrderValue,
            long newActionOrderValue)
        {
            bool existingIsSessionEnding = IsSessionEndingAction(existingAction.actionType);
            bool newIsSessionEnding = IsSessionEndingAction(newAction.actionType);

            if (!existingIsSessionEnding && !newIsSessionEnding)
            {
                return false;
            }

            if (newAction.triggerType == config.triggerTypes.certainTime)
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
            if (action == null || string.IsNullOrWhiteSpace(action.triggerType))
            {
                return false;
            }

            if (action.triggerType == config.triggerTypes.systemIdle)
            {
                if (!TryGetSystemIdleSeconds(action, out int systemIdleSeconds) || systemIdleSeconds <= 0)
                {
                    return false;
                }

                orderValue = systemIdleSeconds;
                return true;
            }

            if (action.triggerType == config.triggerTypes.fromNow)
            {
                if (string.IsNullOrWhiteSpace(action.value))
                {
                    return false;
                }

                if (!DateTime.TryParseExact(
                    action.value,
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

            if (action.triggerType == config.triggerTypes.certainTime)
            {
                if (string.IsNullOrWhiteSpace(action.value))
                {
                    return false;
                }

                if (!DateTime.TryParseExact(
                    action.value,
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
    }
}
