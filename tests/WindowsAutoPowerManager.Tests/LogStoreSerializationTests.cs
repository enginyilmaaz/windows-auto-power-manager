using System.Text.Json;
using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    /// <summary>
    ///     Guards what a stored line contains. The first migrated file carried
    ///     <c>"IsError":false</c> on every line: a computed property with a public getter, which the
    ///     serializer picked up and wrote beside the value it is computed from.
    /// </summary>
    public class LogStoreSerializationTests
    {
        private static string Serialize(LogSystem entry) =>
            JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

        [Fact]
        public void StoredLine_DoesNotCarryTheDerivedErrorFlag()
        {
            string line = Serialize(new LogSystem
            {
                Id = "abcd1234",
                Type = "LockComputer",
                At = "2026-10-18T17:30:13+03:00",
                Outcome = LogOutcomes.Ok,
                Source = LogSources.Manual
            });

            Assert.DoesNotContain("IsError", line);
        }

        [Fact]
        public void StoredLine_OmitsTheOptionalFieldsWhenEmpty()
        {
            string line = Serialize(new LogSystem
            {
                Id = "abcd1234",
                Type = "LockComputer",
                At = "2026-10-18T17:30:13+03:00",
                Outcome = LogOutcomes.Ok,
                Source = LogSources.Manual
            });

            Assert.DoesNotContain("\"rule\"", line);
            Assert.DoesNotContain("\"error\"", line);
        }

        [Fact]
        public void StoredLine_KeepsTheFieldsTheViewerReadsBack()
        {
            var entry = new LogSystem
            {
                Id = "abcd1234",
                Type = "TurnOffMonitorFailed",
                At = "2026-10-18T17:30:13+03:00",
                Outcome = LogOutcomes.Error,
                Source = LogSources.Idle(70),
                Rule = "18.02.2026 14:50:05",
                Error = "driver refused"
            };

            string line = Serialize(entry);
            var round = JsonSerializer.Deserialize<LogSystem>(line);

            Assert.Equal(entry.Id, round.Id);
            Assert.Equal(entry.Type, round.Type);
            Assert.Equal(entry.At, round.At);
            Assert.Equal(entry.Outcome, round.Outcome);
            Assert.Equal("idle:70", round.Source);
            Assert.Equal(entry.Rule, round.Rule);
            Assert.Equal(entry.Error, round.Error);
            Assert.True(round.IsError);
        }

        [Fact]
        public void StoredLine_StaysOnOneLine()
        {
            // The store is read line by line; an indented object would break every entry.
            string line = Serialize(new LogSystem
            {
                Id = "abcd1234", Type = "LockComputer",
                At = "2026-10-18T17:30:13+03:00", Outcome = LogOutcomes.Ok, Source = LogSources.System
            });

            Assert.DoesNotContain("\n", line);
        }
    }
}
