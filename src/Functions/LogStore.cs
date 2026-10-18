using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     The action history on disk, one JSON object per line.
    ///
    ///     The previous store kept the whole history in a single JSON array and rewrote the entire
    ///     file on every logged action, on a timer, one second after the fact. That cost the whole
    ///     file per action, dropped entries the timer had not reached when the session ended, and —
    ///     worst of it — a write torn by shutdown left the array unparseable, at which point the
    ///     reader returned an empty list and the next write committed that emptiness over the file.
    ///     A single interrupted write erased the entire history.
    ///
    ///     Appending one line removes all three problems at once: the cost per action no longer
    ///     depends on how much history exists, there is nothing pending to lose, and a torn line is
    ///     one skipped entry rather than a lost file.
    /// </summary>
    internal static class LogStore
    {
        private static readonly object SyncRoot = new object();

        private static readonly string JsonlPath = Path.Combine(AppContext.BaseDirectory, "Logs.jsonl");
        private static readonly string LegacyJsonPath = Path.Combine(AppContext.BaseDirectory, "Logs.json");
        private static readonly string LegacyBackupPath = Path.Combine(AppContext.BaseDirectory, "Logs.json.bak");

        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        private static List<LogSystem> _cache;

        /// <summary>Entries held in memory, oldest first. Loaded from disk on first use.</summary>
        public static List<LogSystem> ReadAll()
        {
            lock (SyncRoot)
            {
                EnsureLoadedLocked();
                return new List<LogSystem>(_cache);
            }
        }

        public static int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    EnsureLoadedLocked();
                    return _cache.Count;
                }
            }
        }

        /// <summary>Records one entry: appended to the file straight away, no pending state.</summary>
        public static void Append(LogSystem entry)
        {
            if (entry == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                EnsureLoadedLocked();
                _cache.Add(entry);

                try
                {
                    string line = JsonSerializer.Serialize(entry, WriteOptions);
                    using var stream = new FileStream(
                        JsonlPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                    writer.WriteLine(line);
                }
                catch
                {
                    // The history is a convenience, never a reason to take the app down. The entry
                    // stays in memory so the open viewer still shows it.
                }
            }
        }

        /// <summary>Replaces the stored history outright. Used by configuration import.</summary>
        public static void ReplaceAll(IEnumerable<LogSystem> entries)
        {
            lock (SyncRoot)
            {
                _cache = Normalize(entries);
                WriteWholeFileLocked(_cache);
            }
        }

        public static void Clear()
        {
            lock (SyncRoot)
            {
                _cache = new List<LogSystem>();

                try
                {
                    if (File.Exists(JsonlPath))
                    {
                        File.Delete(JsonlPath);
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        ///     Drops entries older than the retention window and returns how many went. Rewrites the
        ///     file only when something actually expired, so an ordinary launch does no writing.
        /// </summary>
        public static int Prune(int retentionDays)
        {
            if (retentionDays <= 0)
            {
                return 0;
            }

            lock (SyncRoot)
            {
                EnsureLoadedLocked();

                DateTimeOffset cutoff = DateTimeOffset.Now.AddDays(-retentionDays);
                var kept = new List<LogSystem>(_cache.Count);

                foreach (LogSystem entry in _cache)
                {
                    // An entry whose timestamp cannot be read is kept: discarding it would be
                    // deleting history over a parse failure.
                    if (!entry.TryGetMoment(out DateTimeOffset moment) || moment >= cutoff)
                    {
                        kept.Add(entry);
                    }
                }

                int removed = _cache.Count - kept.Count;
                if (removed > 0)
                {
                    _cache = kept;
                    WriteWholeFileLocked(kept);
                }

                return removed;
            }
        }

        /// <summary>
        ///     Converts a history written by an older version. The old file is kept alongside as
        ///     <c>Logs.json.bak</c> rather than deleted — it is the user's data, and a conversion
        ///     that goes wrong should be recoverable.
        /// </summary>
        public static int MigrateLegacyIfNeeded()
        {
            lock (SyncRoot)
            {
                if (File.Exists(JsonlPath) || !File.Exists(LegacyJsonPath))
                {
                    return 0;
                }

                List<LogSystem> converted;
                try
                {
                    string raw = File.ReadAllText(LegacyJsonPath);
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        return 0;
                    }

                    var legacy = JsonSerializer.Deserialize<List<LegacyEntry>>(raw, ReadOptions);
                    converted = (legacy ?? new List<LegacyEntry>())
                        .Where(e => e != null)
                        .Select(e => e.ToEntry())
                        .ToList();
                }
                catch
                {
                    return 0;
                }

                if (converted.Count == 0)
                {
                    return 0;
                }

                _cache = converted;
                WriteWholeFileLocked(converted);

                try
                {
                    if (File.Exists(LegacyBackupPath))
                    {
                        File.Delete(LegacyBackupPath);
                    }

                    File.Move(LegacyJsonPath, LegacyBackupPath);
                }
                catch
                {
                    // Leaving the old file in place is harmless: the guard above keys on the new
                    // file existing, so the conversion does not run twice.
                }

                DebugLog.Write("logs", "migrated " + converted.Count + " entries from Logs.json");
                return converted.Count;
            }
        }

        private static void EnsureLoadedLocked()
        {
            if (_cache != null)
            {
                return;
            }

            _cache = new List<LogSystem>();

            if (!File.Exists(JsonlPath))
            {
                return;
            }

            int skipped = 0;
            try
            {
                foreach (string line in File.ReadLines(JsonlPath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    LogSystem entry = null;
                    try
                    {
                        entry = JsonSerializer.Deserialize<LogSystem>(line, ReadOptions);
                    }
                    catch
                    {
                        // A line torn by a shutdown mid-write. One entry, not the file.
                    }

                    if (entry != null && !string.IsNullOrWhiteSpace(entry.Type))
                    {
                        _cache.Add(entry);
                    }
                    else
                    {
                        ++skipped;
                    }
                }
            }
            catch
            {
                // An unreadable file leaves the history empty for this run, but nothing is
                // overwritten: appends go to the end and the existing lines stay as they are.
            }

            // Logged unconditionally: "loaded 0" against a file that has lines is the symptom that
            // matters, and it leaves no other trace.
            DebugLog.Write("logs", "loaded " + _cache.Count + " entries, skipped " + skipped);
        }

        private static void WriteWholeFileLocked(List<LogSystem> entries)
        {
            // Written through a temporary file and moved into place, so an interruption leaves the
            // previous file intact instead of a half-written one.
            string tempPath = JsonlPath + ".tmp";

            try
            {
                using (var stream = new FileStream(
                           tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (LogSystem entry in entries)
                    {
                        writer.WriteLine(JsonSerializer.Serialize(entry, WriteOptions));
                    }
                }

                if (File.Exists(JsonlPath))
                {
                    File.Replace(tempPath, JsonlPath, null);
                }
                else
                {
                    File.Move(tempPath, JsonlPath);
                }
            }
            catch
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>Fills in anything an imported or hand-edited entry left out.</summary>
        private static List<LogSystem> Normalize(IEnumerable<LogSystem> entries)
        {
            return (entries ?? Enumerable.Empty<LogSystem>())
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Type))
                .Select(entry => new LogSystem
                {
                    Id = string.IsNullOrWhiteSpace(entry.Id) ? LogSystem.NewId() : entry.Id,
                    Type = entry.Type,
                    At = entry.At,
                    Outcome = string.IsNullOrWhiteSpace(entry.Outcome) ? LogOutcomes.Ok : entry.Outcome,
                    Source = string.IsNullOrWhiteSpace(entry.Source) ? LogSources.System : entry.Source,
                    Rule = entry.Rule,
                    Error = entry.Error
                })
                .ToList();
        }

        /// <summary>The shape written by versions up to 1.0.119.</summary>
        private sealed class LegacyEntry
        {
            public string Id { get; set; }
            public string ActionType { get; set; }
            public string ActionExecutedDate { get; set; }

            public LogSystem ToEntry()
            {
                string at = ActionExecutedDate;

                // Re-stamped as ISO where it parses, so migrated rows sort and filter like new ones.
                if (DateTime.TryParseExact(
                        ActionExecutedDate ?? string.Empty, "dd.MM.yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                {
                    at = LogSystem.Timestamp(
                        new DateTimeOffset(parsed, TimeZoneInfo.Local.GetUtcOffset(parsed)));
                }

                return new LogSystem
                {
                    Id = string.IsNullOrWhiteSpace(Id) ? LogSystem.NewId() : Id,
                    Type = ActionType,
                    At = at,
                    // The old format recorded neither, and a failed monitor off was its own type.
                    Outcome = string.Equals(ActionType, Config.ActionTypes.TurnOffMonitorFailed, StringComparison.Ordinal)
                        ? LogOutcomes.Error
                        : LogOutcomes.Ok,
                    Source = LogSources.System
                };
            }
        }
    }
}
