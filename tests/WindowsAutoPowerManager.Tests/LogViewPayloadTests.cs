using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WindowsAutoPowerManager.Config;
using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    /// <summary>
    ///     Covers the contract between the history page and the store: what the page sends, and what
    ///     it gets back. Both windows serve the same page through this one place, and a mismatch
    ///     here shows up as an empty viewer over a full file — which is indistinguishable, from the
    ///     outside, from the store itself being empty.
    /// </summary>
    public class LogViewPayloadTests
    {
        private static readonly DateTimeOffset Now =
            new DateTimeOffset(2026, 10, 18, 18, 0, 0, TimeSpan.FromHours(3));

        private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement;

        private static LogSystem Entry(string type, DateTimeOffset at, string outcome = LogOutcomes.Ok) =>
            new LogSystem
            {
                Id = LogSystem.NewId(),
                Type = type,
                At = LogSystem.Timestamp(at),
                Outcome = outcome,
                Source = LogSources.Idle(70)
            };

        [Fact]
        public void EmptyRequest_AsksForEverything()
        {
            LogQueryRequest request = LogViewPayload.ParseRequest(Json("{}"));

            Assert.Equal(LogWindows.All, request.Window);
            Assert.Equal(LogSorts.Newest, request.Sort);
            Assert.Null(request.Types);
            Assert.False(request.OnlyErrors);
            Assert.Equal(0, request.Offset);
        }

        [Fact]
        public void FacetsFromThePage_BecomeActionTypes()
        {
            LogQueryRequest request = LogViewPayload.ParseRequest(
                Json("{\"facets\":[\"locks\",\"monitor\"]}"));

            Assert.NotNull(request.Types);
            Assert.Contains(ActionTypes.LockComputer, request.Types);
            Assert.Contains(ActionTypes.LockComputerManually, request.Types);
            Assert.Contains(ActionTypes.TurnOffMonitor, request.Types);

            // The failed monitor off belongs to the same facet; the browser's old map omitted it.
            Assert.Contains(ActionTypes.TurnOffMonitorFailed, request.Types);
            Assert.DoesNotContain(ActionTypes.UnlockComputer, request.Types);
        }

        [Fact]
        public void CustomWindow_ReadsBothBoundsAndCoversTheWholeEndDay()
        {
            LogQueryRequest request = LogViewPayload.ParseRequest(
                Json("{\"window\":\"custom\",\"from\":\"2026-10-01\",\"to\":\"2026-10-02\"}"));

            Assert.Equal(LogWindows.Custom, request.Window);
            Assert.NotNull(request.From);
            Assert.NotNull(request.To);

            // An entry late on the end day must still be inside the range.
            var entries = new List<LogSystem>
            {
                Entry("LockComputer", new DateTimeOffset(2026, 10, 2, 23, 30, 0, Now.Offset))
            };

            LogQueryResult result = LogQuery.Evaluate(entries, request, Now);
            Assert.Equal(1, result.Total);
        }

        [Fact]
        public void Offset_IsCarriedSoLoadMoreDoesNotRepeatThePage()
        {
            LogQueryRequest request = LogViewPayload.ParseRequest(
                Json("{\"window\":\"all\",\"offset\":4,\"limit\":2}"));

            Assert.Equal(4, request.Offset);
            Assert.Equal(2, request.Limit);
        }

        [Fact]
        public void BuiltPayload_ReportsTheStoredTotalNotThePageSize()
        {
            var entries = new List<LogSystem>();
            for (int i = 0; i < 40; ++i)
            {
                entries.Add(Entry("LockComputer", Now.AddMinutes(-i)));
            }

            LogQueryRequest request = LogViewPayload.ParseRequest(Json("{\"window\":\"all\",\"limit\":10}"));
            LogQueryResult result = LogQuery.Evaluate(entries, request, Now);

            object payload = LogViewPayload.Build(result, type => type, source => source, 90);
            string json = JsonSerializer.Serialize(payload);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            Assert.Equal(10, root.GetProperty("rows").GetArrayLength());
            Assert.Equal(40, root.GetProperty("total").GetInt32());
            Assert.Equal(40, root.GetProperty("grandTotal").GetInt32());
            Assert.True(root.GetProperty("hasMore").GetBoolean());
            Assert.Equal(90, root.GetProperty("retentionDays").GetInt32());
        }

        [Fact]
        public void BuiltRow_CarriesWhatTheTableDraws()
        {
            var entries = new List<LogSystem> { Entry("TurnOffMonitor", Now.AddHours(-1)) };
            LogQueryResult result = LogQuery.Evaluate(
                entries, new LogQueryRequest { Window = LogWindows.All }, Now);

            object payload = LogViewPayload.Build(
                result, type => "Ekran otomatik olarak kapatıldı", source => "Boşta · 70 Saniye", 90);

            using JsonDocument doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));
            JsonElement row = doc.RootElement.GetProperty("rows")[0];

            Assert.Equal("Ekran otomatik olarak kapatıldı", row.GetProperty("label").GetString());
            Assert.Equal("Boşta · 70 Saniye", row.GetProperty("sourceLabel").GetString());
            Assert.Equal(LogFacets.Monitor, row.GetProperty("facet").GetString());
            Assert.False(row.GetProperty("isError").GetBoolean());

            // Split here so the page groups by day without parsing a timestamp itself.
            Assert.Equal("2026-10-18", row.GetProperty("day").GetString());
            Assert.Equal("17:00:00", row.GetProperty("time").GetString());
        }

        [Fact]
        public void FacetCounts_AreReportedPerFacetNotPerActionType()
        {
            var entries = new List<LogSystem>
            {
                Entry("LockComputer", Now.AddMinutes(-1)),
                Entry("LockComputerManually", Now.AddMinutes(-2)),
                Entry("TurnOffMonitor", Now.AddMinutes(-3))
            };

            LogQueryResult result = LogQuery.Evaluate(
                entries, new LogQueryRequest { Window = LogWindows.All }, Now);

            object payload = LogViewPayload.Build(result, t => t, s => s, 90);
            using JsonDocument doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));
            JsonElement counts = doc.RootElement.GetProperty("facetCounts");

            // Both lock types roll up into the one facet the sidebar shows.
            Assert.Equal(2, counts.GetProperty(LogFacets.Locks).GetInt32());
            Assert.Equal(1, counts.GetProperty(LogFacets.Monitor).GetInt32());
            Assert.Equal(0, counts.GetProperty(LogFacets.Sleeps).GetInt32());
        }

        [Fact]
        public void EveryFacetTheSidebarListsIsPresentInTheCounts()
        {
            LogQueryResult result = LogQuery.Evaluate(
                Array.Empty<LogSystem>(), new LogQueryRequest(), Now);

            object payload = LogViewPayload.Build(result, t => t, s => s, 90);
            using JsonDocument doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));
            JsonElement counts = doc.RootElement.GetProperty("facetCounts");

            // A facet missing here renders as an empty count rather than a zero.
            Assert.All(LogFacets.Keys, key => Assert.True(counts.TryGetProperty(key, out _), key));
        }

        [Fact]
        public void MalformedRequest_DoesNotThrow()
        {
            // Whatever the page sends, the store answers: a failure here leaves the viewer with no
            // reply at all, which reads on screen as an empty history.
            foreach (string raw in new[]
                     {
                         "{\"window\":123}", "{\"facets\":\"locks\"}", "{\"offset\":\"abc\"}",
                         "{\"limit\":-5}", "{\"window\":\"custom\",\"from\":\"nonsense\"}",
                         "{\"onlyErrors\":\"yes\"}", "[]", "null"
                     })
            {
                LogQueryRequest request = LogViewPayload.ParseRequest(Json(raw));
                LogQueryResult result = LogQuery.Evaluate(
                    new[] { Entry("LockComputer", Now.AddMinutes(-1)) }, request, Now);

                Assert.NotNull(result);
                Assert.Equal(1, result.GrandTotal);
            }
        }
    }
}
