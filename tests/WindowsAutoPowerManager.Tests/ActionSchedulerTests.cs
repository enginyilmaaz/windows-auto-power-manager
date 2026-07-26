using System;
using WindowsAutoPowerManager.Config;
using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    /// <summary>
    ///     Covers the rules moved out of MainForm. Several cases exist specifically to keep a
    ///     display off/on loop from returning: the scheduler used to re-arm an idle action on any
    ///     decrease of the idle counter, so the action fired again while the system was still past
    ///     its threshold.
    /// </summary>
    public class ActionSchedulerTests
    {
        private static readonly DateTime Noon = new DateTime(2030, 5, 5, 12, 0, 0);

        private static ActionModel Idle(string actionType, string value, string unit = "seconds")
        {
            return new ActionModel
            {
                CreatedDate = "01.01.2030 00:00:00",
                TriggerType = TriggerTypes.SystemIdle,
                ActionType = actionType,
                Value = value,
                ValueUnit = unit,
                IsEnabled = true
            };
        }

        private static ActionExecutionResult Decide(
            ActionScheduler scheduler,
            ActionModel action,
            uint idleSeconds,
            DateTime? now = null)
        {
            bool skip = false;
            return scheduler.Decide(action, idleSeconds, now ?? Noon, ref skip);
        }

        private static bool Executed(ActionExecutionResult result)
        {
            return (result & ActionExecutionResult.Executed) != 0;
        }

        [Fact]
        public void IdleAction_RunsWhenTheThresholdIsReached()
        {
            var scheduler = new ActionScheduler();
            ActionModel action = Idle(ActionTypes.TurnOffMonitor, "70");

            Assert.False(Executed(Decide(scheduler, action, 69)));
            Assert.True(Executed(Decide(scheduler, action, 70)));
        }

        [Fact]
        public void IdleAction_DoesNotRunAgainWhileStillIdle()
        {
            var scheduler = new ActionScheduler();
            ActionModel action = Idle(ActionTypes.TurnOffMonitor, "70");

            Assert.True(Executed(Decide(scheduler, action, 70)));

            for (uint idle = 71; idle < 200; idle += 10)
            {
                Assert.False(Executed(Decide(scheduler, action, idle)));
            }
        }

        [Fact]
        public void IdleCounterDropsButStaysAboveThreshold_DoesNotRunAgain()
        {
            // The display off/on loop: the idle counter fell without reaching the threshold, the
            // action re-armed anyway, and the display was switched off again seconds later.
            var scheduler = new ActionScheduler();
            ActionModel action = Idle(ActionTypes.TurnOffMonitor, "70");

            Assert.True(Executed(Decide(scheduler, action, 900)));
            Assert.False(Executed(Decide(scheduler, action, 400)));
            Assert.False(Executed(Decide(scheduler, action, 71)));
        }

        [Fact]
        public void IdleCounterFallsBelowThreshold_ArmsTheActionAgain()
        {
            var scheduler = new ActionScheduler();
            ActionModel action = Idle(ActionTypes.TurnOffMonitor, "70");

            Assert.True(Executed(Decide(scheduler, action, 900)));
            Assert.False(Executed(Decide(scheduler, action, 10)));
            Assert.True(Executed(Decide(scheduler, action, 70)));
        }

        [Fact]
        public void IdleActions_AreTrackedIndependently()
        {
            // The configuration in use here: display off at 70s, lock at 90s.
            var scheduler = new ActionScheduler();
            ActionModel monitor = Idle(ActionTypes.TurnOffMonitor, "70");
            ActionModel lockAction = Idle(ActionTypes.LockComputer, "90");

            Assert.True(Executed(Decide(scheduler, monitor, 70)));
            Assert.False(Executed(Decide(scheduler, lockAction, 70)));

            Assert.False(Executed(Decide(scheduler, monitor, 90)));
            Assert.True(Executed(Decide(scheduler, lockAction, 90)));
        }

        [Fact]
        public void IdleValueWithoutUnit_IsInterpretedAsMinutes()
        {
            var scheduler = new ActionScheduler();
            ActionModel action = Idle(ActionTypes.TurnOffMonitor, "2", unit: null);

            Assert.False(Executed(Decide(scheduler, action, 119)));
            Assert.True(Executed(Decide(scheduler, action, 120)));
        }

        [Fact]
        public void IdleActionWithUnparsableValue_NeverRuns()
        {
            var scheduler = new ActionScheduler();
            ActionModel action = Idle(ActionTypes.TurnOffMonitor, "abc");

            Assert.Equal(ActionExecutionResult.None, Decide(scheduler, action, 100000));
        }

        [Fact]
        public void FromNowAction_RunsOnceItsTargetIsReachedAndIsThenRemoved()
        {
            var scheduler = new ActionScheduler();
            var action = new ActionModel
            {
                CreatedDate = "01.01.2030 00:00:00",
                TriggerType = TriggerTypes.FromNow,
                ActionType = ActionTypes.ShutdownComputer,
                Value = "05.05.2030 12:00:00",
                IsEnabled = true
            };

            Assert.Equal(
                ActionExecutionResult.None,
                Decide(scheduler, action, 0, Noon.AddSeconds(-1)));

            ActionExecutionResult due = Decide(scheduler, action, 0, Noon);
            Assert.True(Executed(due));
            Assert.True((due & ActionExecutionResult.RemoveAction) != 0);
            Assert.True((due & ActionExecutionResult.NeedsPersist) != 0);
        }

        [Fact]
        public void CertainTimeAction_RunsOncePerDay()
        {
            var scheduler = new ActionScheduler();
            var action = new ActionModel
            {
                CreatedDate = "01.01.2030 00:00:00",
                TriggerType = TriggerTypes.CertainTime,
                ActionType = ActionTypes.LockComputer,
                Value = "12:00:00",
                IsEnabled = true
            };

            Assert.False(Executed(Decide(scheduler, action, 0, Noon.AddMinutes(-1))));
            Assert.True(Executed(Decide(scheduler, action, 0, Noon)));
            Assert.False(Executed(Decide(scheduler, action, 0, Noon.AddHours(3))));
            Assert.True(Executed(Decide(scheduler, action, 0, Noon.AddDays(1))));
        }

        [Fact]
        public void SkippedCertainTimeAction_DoesNotRunAndConsumesTheFlag()
        {
            var scheduler = new ActionScheduler();
            var action = new ActionModel
            {
                CreatedDate = "01.01.2030 00:00:00",
                TriggerType = TriggerTypes.CertainTime,
                ActionType = ActionTypes.LockComputer,
                Value = "12:00:00",
                IsEnabled = true
            };

            bool skip = true;
            ActionExecutionResult result = scheduler.Decide(action, 0, Noon, ref skip);

            Assert.False(Executed(result));
            Assert.False(skip);

            // The skipped occurrence still counts as handled, so it is not retried later today.
            Assert.False(Executed(Decide(scheduler, action, 0, Noon.AddHours(3))));
        }

        [Fact]
        public void CleanupKeepsBookkeepingForActionsThatStillExist()
        {
            var scheduler = new ActionScheduler();
            ActionModel action = Idle(ActionTypes.TurnOffMonitor, "70");

            Assert.True(Executed(Decide(scheduler, action, 900)));

            scheduler.CleanupState(new[] { action });

            // Saving the action list must not make an action that already ran run again.
            Assert.False(Executed(Decide(scheduler, action, 900)));
        }

        [Fact]
        public void CleanupDropsBookkeepingForActionsThatAreGone()
        {
            var scheduler = new ActionScheduler();
            ActionModel action = Idle(ActionTypes.TurnOffMonitor, "70");

            Assert.True(Executed(Decide(scheduler, action, 900)));

            scheduler.CleanupState(Array.Empty<ActionModel>());

            Assert.True(Executed(Decide(scheduler, action, 900)));
        }

        [Fact]
        public void NullAction_IsIgnored()
        {
            var scheduler = new ActionScheduler();
            Assert.Equal(ActionExecutionResult.None, Decide(scheduler, null, 900));
        }
    }
}
