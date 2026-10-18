using System.Collections.Generic;
using System.Text.Json;
using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    /// <summary>
    ///     Reads back lines exactly as they appear in a real store, including the ones v1.0.120
    ///     wrote with the derived "IsError" field before it was excluded. The viewer reported an
    ///     empty history against a file holding a thousand of them, so what a stored line does on
    ///     the way back in is checked here against the literal text rather than reasoned about.
    /// </summary>
    public class LogLineReadTests
    {
        private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        private static LogSystem Read(string line) =>
            JsonSerializer.Deserialize<LogSystem>(line, ReadOptions);

        [Fact]
        public void LineWithTheDerivedFlag_StillReadsBack()
        {
            // Copied verbatim from a store written by 1.0.120.
            const string line =
                "{\"id\":\"bf25e660\",\"type\":\"LockComputer\"," +
                "\"at\":\"2026-10-05T10:33:13+03:00\",\"outcome\":\"ok\"," +
                "\"source\":\"system\",\"IsError\":false}";

            LogSystem entry = Read(line);

            Assert.NotNull(entry);
            Assert.Equal("bf25e660", entry.Id);
            Assert.Equal("LockComputer", entry.Type);
            Assert.Equal("ok", entry.Outcome);
            Assert.False(entry.IsError);
            Assert.True(entry.TryGetMoment(out _));
        }

        [Fact]
        public void LineWithoutTheDerivedFlag_ReadsBack()
        {
            const string line =
                "{\"id\":\"c97c913b\",\"type\":\"AppStarted\"," +
                "\"at\":\"2026-10-18T19:24:24+03:00\",\"outcome\":\"ok\",\"source\":\"system\"}";

            LogSystem entry = Read(line);

            Assert.NotNull(entry);
            Assert.Equal("AppStarted", entry.Type);
            Assert.True(entry.TryGetMoment(out _));
        }

        [Fact]
        public void LineWithEveryField_ReadsBack()
        {
            const string line =
                "{\"id\":\"deadbeef\",\"type\":\"TurnOffMonitorFailed\"," +
                "\"at\":\"2026-10-17T15:53:41+03:00\",\"outcome\":\"error\"," +
                "\"source\":\"idle:70\",\"rule\":\"18.02.2026 14:50:05\"," +
                "\"error\":\"No window carried the monitor power request through.\"}";

            LogSystem entry = Read(line);

            Assert.NotNull(entry);
            Assert.True(entry.IsError);
            Assert.Equal("idle:70", entry.Source);
            Assert.Equal("18.02.2026 14:50:05", entry.Rule);
        }

        /// <summary>
        ///     A whole file's worth, the way the store walks it: every line must survive, because a
        ///     line silently dropped here is a record the viewer never shows.
        /// </summary>
        [Fact]
        public void EveryLineOfAMixedFile_Survives()
        {
            var lines = new List<string>
            {
                "{\"id\":\"a1\",\"type\":\"LockComputer\",\"at\":\"2026-10-05T10:33:13+03:00\",\"outcome\":\"ok\",\"source\":\"system\",\"IsError\":false}",
                "{\"id\":\"a2\",\"type\":\"UnlockComputer\",\"at\":\"2026-10-05T11:02:41+03:00\",\"outcome\":\"ok\",\"source\":\"manual\",\"IsError\":false}",
                "{\"id\":\"a3\",\"type\":\"TurnOffMonitor\",\"at\":\"2026-10-18T17:29:53+03:00\",\"outcome\":\"ok\",\"source\":\"idle:70\"}",
                "{\"id\":\"a4\",\"type\":\"AppStarted\",\"at\":\"2026-10-18T19:24:24+03:00\",\"outcome\":\"ok\",\"source\":\"system\"}"
            };

            var read = new List<LogSystem>();
            foreach (string line in lines)
            {
                LogSystem entry = Read(line);
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Type))
                {
                    read.Add(entry);
                }
            }

            Assert.Equal(lines.Count, read.Count);
        }

        [Fact]
        public void TornLastLine_IsTheOnlyOneLost()
        {
            // What a shutdown mid-append leaves behind.
            const string torn = "{\"id\":\"a5\",\"type\":\"Shutdown";

            LogSystem entry = null;
            bool threw = false;
            try
            {
                entry = Read(torn);
            }
            catch
            {
                threw = true;
            }

            Assert.True(threw || entry == null || string.IsNullOrWhiteSpace(entry.Type));
        }
    }
}
