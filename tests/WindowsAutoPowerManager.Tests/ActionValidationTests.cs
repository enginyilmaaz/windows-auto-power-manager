using System.Collections.Generic;
using WindowsAutoPowerManager.Config;
using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    /// <summary>
    ///     Characterization tests: these describe what conflict validation does today, so the
    ///     behaviour is pinned before the scheduling code is moved out of MainForm. A failure here
    ///     means behaviour changed, not necessarily that the new behaviour is wrong.
    /// </summary>
    public class ActionValidationTests
    {
        private static ActionModel Idle(string actionType, string value, string unit = "seconds", bool enabled = true)
        {
            return new ActionModel
            {
                TriggerType = TriggerTypes.SystemIdle,
                ActionType = actionType,
                Value = value,
                ValueUnit = unit,
                IsEnabled = enabled
            };
        }

        private static bool Validate(ActionModel candidate, params ActionModel[] existing)
        {
            return ActionValidation.TryValidateActionForAdd(
                candidate,
                new List<ActionModel>(existing),
                null,
                out _);
        }

        [Fact]
        public void MonitorOffBeforeLock_IsAccepted()
        {
            // The configuration actually in use: display off at 70s, lock at 90s. Neither is a
            // blocking behaviour, so the earlier one does not prevent the later one.
            Assert.True(Validate(
                Idle(ActionTypes.LockComputer, "90"),
                Idle(ActionTypes.TurnOffMonitor, "70")));
        }

        [Fact]
        public void TwoIdleActionsOfTheSameType_Conflict()
        {
            // Same action on the same trigger is rejected regardless of the two idle values.
            Assert.False(Validate(
                Idle(ActionTypes.TurnOffMonitor, "120"),
                Idle(ActionTypes.TurnOffMonitor, "70")));
        }

        [Fact]
        public void BlockingActionScheduledEarlier_BlocksTheLaterAction()
        {
            // Sleep suspends the system, so nothing scheduled after it could ever run.
            Assert.False(Validate(
                Idle(ActionTypes.LockComputer, "90"),
                Idle(ActionTypes.SleepComputer, "50")));
        }

        [Fact]
        public void BlockingActionScheduledLater_DoesNotBlockTheEarlierAction()
        {
            // Reversed order is fine: the lock runs first, then sleep.
            Assert.True(Validate(
                Idle(ActionTypes.SleepComputer, "90"),
                Idle(ActionTypes.LockComputer, "50")));
        }

        [Fact]
        public void SameIdleValue_NonBlockingDifferentTypes_IsAccepted()
        {
            // Two non-blocking actions are allowed to share an execution point.
            Assert.True(Validate(
                Idle(ActionTypes.LockComputer, "70"),
                Idle(ActionTypes.TurnOffMonitor, "70")));
        }

        [Fact]
        public void SameIdleValue_WithSessionEndingAction_Conflicts()
        {
            // Shutdown ends the session, so sharing an execution point with it is rejected.
            Assert.False(Validate(
                Idle(ActionTypes.LockComputer, "70"),
                Idle(ActionTypes.ShutdownComputer, "70")));
        }

        [Fact]
        public void DisabledExistingActions_AreIgnored()
        {
            Assert.True(Validate(
                Idle(ActionTypes.LockComputer, "70"),
                Idle(ActionTypes.LockComputer, "70", enabled: false)));
        }

        [Fact]
        public void CandidateThatWillNotBeEnabled_SkipsValidationEntirely()
        {
            bool result = ActionValidation.TryValidateActionForAdd(
                Idle(ActionTypes.TurnOffMonitor, "70"),
                new List<ActionModel> { Idle(ActionTypes.TurnOffMonitor, "70") },
                null,
                out _,
                candidateWillBeEnabled: false);

            Assert.True(result);
        }

        [Fact]
        public void IdleValueWithoutUnit_IsInterpretedAsMinutes()
        {
            // Sleep "5" with no unit is 300s, which lands after the 120s lock and so does not
            // block it. Were the value read as 5 seconds, sleep would come first and conflict.
            Assert.True(Validate(
                Idle(ActionTypes.LockComputer, "120"),
                Idle(ActionTypes.SleepComputer, "5", unit: null)));
        }

        [Fact]
        public void NullCandidate_IsRejected()
        {
            Assert.False(Validate(null));
        }

        [Fact]
        public void UnparsableIdleValue_IsRejected()
        {
            Assert.False(Validate(Idle(ActionTypes.LockComputer, "abc")));
        }

        [Fact]
        public void ZeroIdleValue_IsRejected()
        {
            Assert.False(Validate(Idle(ActionTypes.LockComputer, "0")));
        }

        [Fact]
        public void IdleAndDailyTimeTriggers_AreNeverCompared()
        {
            // Idle and daily-time schedules have no common ordering, so validation lets them
            // coexist even though sleep would in practice pre-empt the later lock.
            var existing = new ActionModel
            {
                TriggerType = TriggerTypes.SystemIdle,
                ActionType = ActionTypes.SleepComputer,
                Value = "60",
                ValueUnit = "seconds",
                IsEnabled = true
            };

            var candidate = new ActionModel
            {
                TriggerType = TriggerTypes.CertainTime,
                ActionType = ActionTypes.LockComputer,
                Value = "12:00:00",
                IsEnabled = true
            };

            Assert.True(Validate(candidate, existing));
        }

        [Fact]
        public void AbsoluteAndDailyTimeTriggers_AreComparedOnTheAbsoluteDate()
        {
            // A one-off sleep at 10:00 blocks a daily lock at 12:00, because the daily time is
            // projected onto the date of the absolute schedule before comparing.
            var existing = new ActionModel
            {
                TriggerType = TriggerTypes.FromNow,
                ActionType = ActionTypes.SleepComputer,
                Value = "01.01.2030 10:00:00",
                IsEnabled = true
            };

            var candidate = new ActionModel
            {
                TriggerType = TriggerTypes.CertainTime,
                ActionType = ActionTypes.LockComputer,
                Value = "12:00:00",
                IsEnabled = true
            };

            Assert.False(Validate(candidate, existing));
        }
    }
}
