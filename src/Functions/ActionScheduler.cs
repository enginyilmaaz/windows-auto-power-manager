using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsAutoPowerManager.Functions
{
    [Flags]
    internal enum ActionExecutionResult
    {
        None = 0,
        Executed = 1,
        RemoveAction = 2,
        NeedsPersist = 4
    }

    internal sealed class ActionRuntimeState
    {
        public string ExecutionKey;
        public uint IdleSeconds;
        public bool HasIdleSeconds;
        public DateTime FromNowTarget;
        public bool HasFromNowTarget;
        public TimeSpan CertainTimeOfDay;
        public bool HasCertainTime;
    }

    /// <summary>
    ///     Decides which actions are due. Deliberately free of UI and of any side effect: it never
    ///     performs an action, it reports that one should run and the caller carries it out. That
    ///     is what makes the rules testable, which they were not while they lived in the form.
    /// </summary>
    internal sealed class ActionScheduler
    {
        private readonly HashSet<string> _executedIdleActionKeys = new HashSet<string>();
        private readonly Dictionary<string, DateTime> _certainTimeLastExecutionDates = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, ActionRuntimeState> _actionRuntimeStates = new Dictionary<string, ActionRuntimeState>();

        public ActionExecutionResult Decide(
            ActionModel action,
            uint idleTimeSec,
            DateTime now,
            ref bool skipCertainTimeAction)
        {
            if (action == null)
            {
                return ActionExecutionResult.None;
            }

            ActionRuntimeState state = GetOrCreateRuntimeState(action);
            string actionKey = state.ExecutionKey;
            ActionExecutionResult result = ActionExecutionResult.None;

            if (action.TriggerType == Config.TriggerTypes.SystemIdle)
            {
                if (!state.HasIdleSeconds)
                {
                    return ActionExecutionResult.None;
                }

                if (idleTimeSec < state.IdleSeconds)
                {
                    // Re-arm only once the idle counter genuinely falls back below this action's
                    // own threshold. Re-arming on any decrease of the idle counter let the action
                    // fire again while the system was still past the threshold, which turned the
                    // display off and on repeatedly.
                    _executedIdleActionKeys.Remove(actionKey);
                    return ActionExecutionResult.None;
                }

                if (_executedIdleActionKeys.Add(actionKey))
                {
                    return ActionExecutionResult.Executed;
                }

                return ActionExecutionResult.None;
            }

            if (action.TriggerType == Config.TriggerTypes.CertainTime &&
                ShouldExecuteCertainTimeAction(state, actionKey, now))
            {
                if (skipCertainTimeAction == false)
                {
                    result |= ActionExecutionResult.Executed;
                }
                else
                {
                    skipCertainTimeAction = false;
                }

                // Recorded in both branches: a skipped occurrence still counts as handled for
                // today, otherwise it would be reconsidered on the next tick.
                _certainTimeLastExecutionDates[actionKey] = now.Date;
            }

            if (action.TriggerType == Config.TriggerTypes.FromNow &&
                state.HasFromNowTarget &&
                now >= state.FromNowTarget)
            {
                result |= ActionExecutionResult.Executed |
                          ActionExecutionResult.RemoveAction |
                          ActionExecutionResult.NeedsPersist;
            }

            return result;
        }

        public void RebuildRuntimeStates(IEnumerable<ActionModel> actions)
        {
            _actionRuntimeStates.Clear();
            if (actions == null)
            {
                return;
            }

            foreach (ActionModel action in actions)
            {
                string key = BuildExecutionKey(action);
                _actionRuntimeStates[key] = BuildRuntimeState(action, key);
            }
        }

        /// <summary>
        ///     Drops execution bookkeeping for actions that no longer exist. Entries for surviving
        ///     actions are kept, so an action that already ran does not run again just because the
        ///     list was saved.
        /// </summary>
        public void CleanupState(IEnumerable<ActionModel> actions)
        {
            var validKeys = new HashSet<string>(
                (actions ?? Enumerable.Empty<ActionModel>()).Select(BuildExecutionKey));

            _executedIdleActionKeys.RemoveWhere(key => !validKeys.Contains(key));

            foreach (string key in _certainTimeLastExecutionDates.Keys.ToList())
            {
                if (!validKeys.Contains(key))
                {
                    _certainTimeLastExecutionDates.Remove(key);
                }
            }
        }

        public ActionRuntimeState GetOrCreateRuntimeState(ActionModel action)
        {
            string key = BuildExecutionKey(action);
            if (_actionRuntimeStates.TryGetValue(key, out ActionRuntimeState state))
            {
                return state;
            }

            state = BuildRuntimeState(action, key);
            _actionRuntimeStates[key] = state;
            return state;
        }

        public static string BuildExecutionKey(ActionModel action)
        {
            if (action == null) return string.Empty;

            return (action.CreatedDate ?? string.Empty) + "|" +
                   (action.TriggerType ?? string.Empty) + "|" +
                   (action.ActionType ?? string.Empty) + "|" +
                   (action.Value ?? string.Empty);
        }

        public static bool TryParseFromNowValue(ActionModel action, out DateTime targetTime)
        {
            targetTime = default;
            if (action == null || string.IsNullOrWhiteSpace(action.Value))
            {
                return false;
            }

            return DateTime.TryParseExact(
                action.Value,
                "dd.MM.yyyy HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out targetTime);
        }

        public static bool TryGetSystemIdleSeconds(ActionModel action, out uint seconds)
        {
            seconds = 0;
            if (action == null || string.IsNullOrWhiteSpace(action.Value))
            {
                return false;
            }

            if (!uint.TryParse(action.Value, out uint parsed))
            {
                return false;
            }

            if (parsed == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(action.ValueUnit))
            {
                if (parsed > uint.MaxValue / 60)
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

        private static ActionRuntimeState BuildRuntimeState(ActionModel action, string actionKey)
        {
            var state = new ActionRuntimeState
            {
                ExecutionKey = actionKey ?? string.Empty
            };

            if (TryGetSystemIdleSeconds(action, out uint idleSeconds))
            {
                state.IdleSeconds = idleSeconds;
                state.HasIdleSeconds = true;
            }

            if (TryParseFromNowValue(action, out DateTime fromNowTarget))
            {
                state.FromNowTarget = fromNowTarget;
                state.HasFromNowTarget = true;
            }

            if (action != null &&
                !string.IsNullOrWhiteSpace(action.Value) &&
                action.TriggerType == Config.TriggerTypes.CertainTime &&
                DateTime.TryParseExact(
                    action.Value,
                    "HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedTime))
            {
                state.CertainTimeOfDay = parsedTime.TimeOfDay;
                state.HasCertainTime = true;
            }

            return state;
        }

        private bool ShouldExecuteCertainTimeAction(ActionRuntimeState state, string actionKey, DateTime now)
        {
            if (state == null || !state.HasCertainTime)
            {
                return false;
            }

            DateTime scheduledTime = now.Date.Add(state.CertainTimeOfDay);
            if (now < scheduledTime)
            {
                return false;
            }

            if (_certainTimeLastExecutionDates.TryGetValue(actionKey, out DateTime lastExecutionDate) &&
                lastExecutionDate.Date == now.Date)
            {
                return false;
            }

            return true;
        }
    }
}
