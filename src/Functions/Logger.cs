using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace WindowsAutoPowerManager.Functions
{
    public class Logger
    {
        private static readonly object SyncRoot = new object();
        private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "Logs.json");
        private static readonly TimeSpan FlushDelay = TimeSpan.FromSeconds(1);
        private const int MaxLogCount = 5000;

        private static List<LogSystem> _logCache = new List<LogSystem>();
        private static Timer _flushTimer;
        private static bool _initialized;
        private static bool _logsEnabled = true;

        public static void Initialize(Settings settings)
        {
            EnsureInitialized(settings);
        }

        public static void UpdateSettings(Settings settings)
        {
            lock (SyncRoot)
            {
                _logsEnabled = settings?.LogsEnabled ?? true;
            }
        }

        public static void DoLog(string actionType, Settings cachedSettings = null)
        {
            if (string.IsNullOrWhiteSpace(actionType))
            {
                return;
            }

            EnsureInitialized(cachedSettings);

            bool flushImmediately = false;
            lock (SyncRoot)
            {
                if (!_logsEnabled)
                {
                    return;
                }

                _logCache.Add(new LogSystem
                {
                    ActionType = actionType,
                    ActionExecutedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
                });

                TrimToLimitLocked();
                ScheduleFlushLocked();
                flushImmediately =
                    string.Equals(actionType, Config.ActionTypes.AppTerminated, StringComparison.Ordinal) ||
                    string.Equals(actionType, Config.ActionTypes.ShutdownComputer, StringComparison.Ordinal) ||
                    string.Equals(actionType, Config.ActionTypes.RestartComputer, StringComparison.Ordinal) ||
                    string.Equals(actionType, Config.ActionTypes.LogOffWindows, StringComparison.Ordinal) ||
                    string.Equals(actionType, Config.ActionTypes.SleepComputer, StringComparison.Ordinal);
            }

            if (flushImmediately)
            {
                Flush();
            }
        }

        public static List<LogSystem> GetRecentLogs(int limit)
        {
            EnsureInitialized(null);
            if (limit <= 0)
            {
                return new List<LogSystem>();
            }

            lock (SyncRoot)
            {
                int startIndex = Math.Max(0, _logCache.Count - limit);
                var result = new List<LogSystem>(_logCache.Count - startIndex);

                for (int i = _logCache.Count - 1; i >= startIndex; --i)
                {
                    result.Add(_logCache[i]);
                }

                return result;
            }
        }

        public static void Clear()
        {
            EnsureInitialized(null);

            lock (SyncRoot)
            {
                _logCache.Clear();
                _flushTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }

            try
            {
                if (File.Exists(LogPath))
                {
                    File.Delete(LogPath);
                }
            }
            catch
            {
                // Ignore clear errors to keep UI responsive.
            }
        }

        public static void Flush()
        {
            List<LogSystem> snapshot;
            lock (SyncRoot)
            {
                if (!_initialized)
                {
                    return;
                }

                snapshot = new List<LogSystem>(_logCache);
            }

            JsonWriter.WriteJson(LogPath, true, snapshot);
        }

        private static void EnsureInitialized(Settings settings)
        {
            if (_initialized)
            {
                if (settings != null)
                {
                    UpdateSettings(settings);
                }

                return;
            }

            lock (SyncRoot)
            {
                if (_initialized)
                {
                    if (settings != null)
                    {
                        _logsEnabled = settings.LogsEnabled;
                    }

                    return;
                }

                Settings resolvedSettings = settings ?? SettingsStorage.LoadOrDefault();
                _logsEnabled = resolvedSettings?.LogsEnabled ?? true;
                _logCache = ReadLogsFromDisk();
                TrimToLimitLocked();
                _flushTimer = new Timer(_ => Flush(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                _initialized = true;
            }
        }

        private static List<LogSystem> ReadLogsFromDisk()
        {
            if (!File.Exists(LogPath))
            {
                return new List<LogSystem>();
            }

            try
            {
                string raw = File.ReadAllText(LogPath);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return new List<LogSystem>();
                }

                return JsonSerializer.Deserialize<List<LogSystem>>(raw) ?? new List<LogSystem>();
            }
            catch
            {
                return new List<LogSystem>();
            }
        }

        private static void TrimToLimitLocked()
        {
            int excess = _logCache.Count - MaxLogCount;
            if (excess > 0)
            {
                _logCache.RemoveRange(0, excess);
            }
        }

        private static void ScheduleFlushLocked()
        {
            if (_flushTimer == null)
            {
                _flushTimer = new Timer(_ => Flush(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }

            _flushTimer.Change(FlushDelay, Timeout.InfiniteTimeSpan);
        }
    }
}
