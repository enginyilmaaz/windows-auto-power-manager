using System;
using System.Collections.Generic;
using System.Linq;
using WindowsAutoPowerManager.Config;
using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    /// <summary>
    ///     Covers the rules the history viewer runs on. Several cases exist specifically because the
    ///     old viewer fetched the last 250 entries and filtered those in the browser: a date range or
    ///     a search pointing past them reported "no records" while the records sat in the file.
    /// </summary>
    public class LogQueryTests
    {
        private static readonly DateTimeOffset Now =
            new DateTimeOffset(2026, 10, 18, 18, 0, 0, TimeSpan.FromHours(3));

        private static LogSystem Entry(
            string type, DateTimeOffset at, string outcome = LogOutcomes.Ok,
            string source = LogSources.System, string error = null)
        {
            return new LogSystem
            {
                Id = LogSystem.NewId(),
                Type = type,
                At = LogSystem.Timestamp(at),
                Outcome = outcome,
                Source = source,
                Error = error
            };
        }

        /// <summary>One entry per day going back, newest last, as the store keeps them.</summary>
        private static List<LogSystem> DailyHistory(int days, string type = "LockComputer")
        {
            var entries = new List<LogSystem>();
            for (int i = days - 1; i >= 0; --i)
            {
                entries.Add(Entry(type, Now.AddDays(-i).AddHours(-2)));
            }

            return entries;
        }

        [Fact]
        public void DateWindow_ReachesEntriesPastTheFirstPage()
        {
            // 400 entries: more than the 250 the old viewer ever fetched.
            var entries = DailyHistory(400);

            var result = LogQuery.Evaluate(
                entries,
                new LogQueryRequest
                {
                    Window = LogWindows.Custom,
                    From = Now.AddDays(-300),
                    To = Now.AddDays(-299),
                    Limit = 50
                },
                Now);

            Assert.True(result.Total > 0, "An entry 300 days back must still be findable.");
            Assert.All(result.Rows, row =>
            {
                Assert.True(row.TryGetMoment(out DateTimeOffset moment));
                Assert.InRange(moment, Now.AddDays(-300), Now.AddDays(-299));
            });
        }

        [Fact]
        public void Search_ReachesEntriesPastTheFirstPage()
        {
            var entries = DailyHistory(400);
            entries.Insert(0, Entry("SleepComputer", Now.AddDays(-399)));

            var result = LogQuery.Evaluate(
                entries,
                new LogQueryRequest { Window = LogWindows.All, Query = "Sleep", Limit = 50 },
                Now);

            Assert.Equal(1, result.Total);
            Assert.Equal("SleepComputer", result.Rows.Single().Type);
        }

        [Fact]
        public void Search_MatchesTheTranslatedLabel()
        {
            var entries = new List<LogSystem> { Entry("LockComputer", Now.AddHours(-1)) };

            string Translate(string type) =>
                type == "LockComputer" ? "Bilgisayar otomatik olarak kilitlendi" : type;

            var result = LogQuery.Evaluate(
                entries,
                new LogQueryRequest { Window = LogWindows.All, Query = "kilitlendi" },
                Now,
                Translate);

            Assert.Equal(1, result.Total);
        }

        [Fact]
        public void TodayWindow_CoversFromLocalMidnight()
        {
            var entries = new List<LogSystem>
            {
                Entry("LockComputer", Now.AddHours(-1)),                                    // today
                Entry("LockComputer", new DateTimeOffset(2026, 10, 17, 23, 55, 0, Now.Offset)) // last night
            };

            var result = LogQuery.Evaluate(
                entries, new LogQueryRequest { Window = LogWindows.Today }, Now);

            Assert.Equal(1, result.Total);
        }

        [Fact]
        public void TypeCounts_IgnoreTheTypeFilterItself()
        {
            var entries = new List<LogSystem>
            {
                Entry("LockComputer", Now.AddHours(-1)),
                Entry("LockComputer", Now.AddHours(-2)),
                Entry("TurnOffMonitor", Now.AddHours(-3))
            };

            var result = LogQuery.Evaluate(
                entries,
                new LogQueryRequest
                {
                    Window = LogWindows.Today,
                    Types = new HashSet<string>(StringComparer.Ordinal) { "LockComputer" }
                },
                Now);

            Assert.Equal(2, result.Total);

            // The unselected type still reports what selecting it would add, rather than zero.
            Assert.Equal(1, result.TypeCounts["TurnOffMonitor"]);
            Assert.Equal(2, result.TypeCounts["LockComputer"]);
        }

        [Fact]
        public void WindowCounts_IgnoreTheWindowItself()
        {
            var entries = DailyHistory(40);

            var result = LogQuery.Evaluate(
                entries, new LogQueryRequest { Window = LogWindows.Today }, Now);

            Assert.Equal(1, result.WindowCounts[LogWindows.Today]);
            Assert.Equal(7, result.WindowCounts[LogWindows.Last7]);
            Assert.Equal(30, result.WindowCounts[LogWindows.Last30]);
            Assert.Equal(40, result.WindowCounts[LogWindows.All]);
        }

        [Fact]
        public void OnlyErrors_NarrowsRowsButNotItsOwnCount()
        {
            var entries = new List<LogSystem>
            {
                Entry("TurnOffMonitor", Now.AddHours(-1)),
                Entry(ActionTypes.TurnOffMonitorFailed, Now.AddHours(-2), LogOutcomes.Error, error: "driver refused"),
                Entry("LockComputer", Now.AddHours(-3))
            };

            var all = LogQuery.Evaluate(entries, new LogQueryRequest { Window = LogWindows.Today }, Now);
            Assert.Equal(3, all.Total);
            Assert.Equal(1, all.ErrorCount);

            var errorsOnly = LogQuery.Evaluate(
                entries, new LogQueryRequest { Window = LogWindows.Today, OnlyErrors = true }, Now);

            Assert.Equal(1, errorsOnly.Total);
            Assert.Equal(1, errorsOnly.ErrorCount);
            Assert.True(errorsOnly.Rows.Single().IsError);
        }

        [Fact]
        public void Paging_ReportsMoreAndKeepsOrder()
        {
            var entries = DailyHistory(10);

            var first = LogQuery.Evaluate(
                entries, new LogQueryRequest { Window = LogWindows.All, Limit = 4 }, Now);

            Assert.Equal(10, first.Total);
            Assert.Equal(4, first.Rows.Count);
            Assert.True(first.HasMore);

            var last = LogQuery.Evaluate(
                entries, new LogQueryRequest { Window = LogWindows.All, Limit = 4, Offset = 8 }, Now);

            Assert.Equal(2, last.Rows.Count);
            Assert.False(last.HasMore);

            // Newest first by default.
            first.Rows[0].TryGetMoment(out DateTimeOffset newest);
            first.Rows[1].TryGetMoment(out DateTimeOffset next);
            Assert.True(newest > next);
        }

        [Fact]
        public void Sort_OldestFirstReversesTheOrder()
        {
            var entries = DailyHistory(5);

            var result = LogQuery.Evaluate(
                entries,
                new LogQueryRequest { Window = LogWindows.All, Sort = LogSorts.Oldest, Limit = 5 },
                Now);

            result.Rows[0].TryGetMoment(out DateTimeOffset first);
            result.Rows[4].TryGetMoment(out DateTimeOffset last);
            Assert.True(first < last);
        }

        [Fact]
        public void LegacyTimestamps_StayFilterableAndSortable()
        {
            // What versions up to 1.0.119 wrote, before migration re-stamps it.
            var entries = new List<LogSystem>
            {
                new LogSystem { Id = "a", Type = "LockComputer", At = "17.10.2026 09:00:00", Outcome = LogOutcomes.Ok },
                Entry("LockComputer", Now.AddHours(-1))
            };

            var result = LogQuery.Evaluate(
                entries, new LogQueryRequest { Window = LogWindows.Last7, Limit = 10 }, Now);

            Assert.Equal(2, result.Total);
            Assert.Equal("LockComputer", result.Rows[0].Type);
        }

        [Fact]
        public void UnreadableTimestamp_DoesNotCrashTheQuery()
        {
            var entries = new List<LogSystem>
            {
                new LogSystem { Id = "a", Type = "LockComputer", At = "not a date", Outcome = LogOutcomes.Ok },
                Entry("LockComputer", Now.AddHours(-1))
            };

            var result = LogQuery.Evaluate(
                entries, new LogQueryRequest { Window = LogWindows.All, Limit = 10 }, Now);

            Assert.Equal(2, result.GrandTotal);
            Assert.Equal(2, result.Total);
        }

        [Fact]
        public void Limit_IsClampedRatherThanTrusted()
        {
            var entries = DailyHistory(20);

            var result = LogQuery.Evaluate(
                entries, new LogQueryRequest { Window = LogWindows.All, Limit = 999999 }, Now);

            Assert.Equal(20, result.Rows.Count);
            Assert.False(result.HasMore);
        }

        [Fact]
        public void Facets_CoverEveryActionTypeIncludingAFailedMonitorOff()
        {
            // The browser's old filter map omitted TurnOffMonitorFailed, so failures could not be
            // filtered for at all.
            var every = new[]
            {
                ActionTypes.LockComputer, ActionTypes.LockComputerManually, ActionTypes.UnlockComputer,
                ActionTypes.TurnOffMonitor, ActionTypes.TurnOffMonitorFailed, ActionTypes.SleepComputer,
                ActionTypes.ShutdownComputer, ActionTypes.RestartComputer, ActionTypes.LogOffWindows,
                ActionTypes.AppStarted, ActionTypes.AppTerminated
            };

            Assert.All(every, type => Assert.NotNull(LogFacets.FacetOf(type)));
        }

        [Fact]
        public void Timestamp_RoundTripsThroughTheStoredForm()
        {
            var moment = new DateTimeOffset(2026, 10, 18, 17, 29, 53, TimeSpan.FromHours(3));
            var entry = new LogSystem { At = LogSystem.Timestamp(moment) };

            Assert.True(entry.TryGetMoment(out DateTimeOffset parsed));
            Assert.Equal(moment, parsed);
            Assert.Equal("2026-10-18T17:29:53+03:00", entry.At);
        }
    }
}
