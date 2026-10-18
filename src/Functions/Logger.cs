using System;
using System.Collections.Generic;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Records executed actions. A thin front for <see cref="LogStore"/>: the entries go to disk
    ///     as they happen, so there is no cache to merge, no timer to flush and nothing pending when
    ///     the session ends.
    /// </summary>
    public class Logger
    {
        private static readonly object SyncRoot = new object();

        private static bool _initialized;
        private static bool _logsEnabled = true;
        private static int _retentionDays = Config.SettingsINI.DefaultLogRetentionDays;

        public static void Initialize(Settings settings)
        {
            EnsureInitialized(settings);
        }

        public static void UpdateSettings(Settings settings)
        {
            if (settings == null)
            {
                return;
            }

            bool retentionChanged;
            int retentionDays;

            lock (SyncRoot)
            {
                _logsEnabled = settings.LogsEnabled;
                retentionDays = NormalizeRetention(settings.LogRetentionDays);
                retentionChanged = retentionDays != _retentionDays;
                _retentionDays = retentionDays;
            }

            // A shortened window takes effect at once rather than at the next launch.
            if (retentionChanged)
            {
                Prune(retentionDays);
            }
        }

        /// <summary>
        ///     Records a successful action, taking what triggered it from the ambient
        ///     <see cref="LogContext"/>.
        /// </summary>
        public static void DoLog(string actionType, Settings cachedSettings = null)
        {
            Record(actionType, LogOutcomes.Ok, null, cachedSettings);
        }

        /// <summary>Records an action that was attempted and failed, with the reason.</summary>
        public static void DoLogFailure(string actionType, string error, Settings cachedSettings = null)
        {
            Record(actionType, LogOutcomes.Error, error, cachedSettings);
        }

        private static void Record(string actionType, string outcome, string error, Settings cachedSettings)
        {
            if (string.IsNullOrWhiteSpace(actionType))
            {
                return;
            }

            EnsureInitialized(cachedSettings);

            lock (SyncRoot)
            {
                if (!_logsEnabled)
                {
                    return;
                }
            }

            LogStore.Append(new LogSystem
            {
                Id = LogSystem.NewId(),
                Type = actionType,
                At = LogSystem.Timestamp(DateTimeOffset.Now),
                Outcome = outcome,
                Source = LogContext.Source,
                Rule = LogContext.Rule,
                Error = string.IsNullOrWhiteSpace(error) ? null : error
            });
        }

        /// <summary>Answers a viewer request against the whole stored history.</summary>
        internal static LogQueryResult Query(LogQueryRequest request, Func<string, string> translate = null)
        {
            EnsureInitialized(null);
            return LogQuery.Evaluate(LogStore.ReadAll(), request, DateTimeOffset.Now, translate);
        }

        /// <summary>Everything on disk, oldest first. Used by configuration export.</summary>
        public static List<LogSystem> GetAll()
        {
            EnsureInitialized(null);
            return LogStore.ReadAll();
        }

        public static int Count
        {
            get
            {
                EnsureInitialized(null);
                return LogStore.Count;
            }
        }

        public static void Clear()
        {
            EnsureInitialized(null);
            LogStore.Clear();
        }

        public static void ReplaceLogs(IEnumerable<LogSystem> logs)
        {
            EnsureInitialized(null);
            LogStore.ReplaceAll(logs);
            Prune(CurrentRetentionDays);
        }

        public static int CurrentRetentionDays
        {
            get
            {
                lock (SyncRoot)
                {
                    return _retentionDays;
                }
            }
        }

        private static void Prune(int retentionDays)
        {
            int removed = LogStore.Prune(retentionDays);
            if (removed > 0)
            {
                DebugLog.Write("logs", "pruned " + removed + " entries past " + retentionDays + " days");
            }
        }

        private static void EnsureInitialized(Settings settings)
        {
            bool runMaintenance = false;
            int retentionDays;

            lock (SyncRoot)
            {
                if (!_initialized)
                {
                    Settings resolved = settings ?? SettingsStorage.LoadOrDefault();
                    _logsEnabled = resolved?.LogsEnabled ?? true;
                    _retentionDays = NormalizeRetention(resolved?.LogRetentionDays);
                    _initialized = true;
                    runMaintenance = true;
                }
                else if (settings != null)
                {
                    _logsEnabled = settings.LogsEnabled;
                    _retentionDays = NormalizeRetention(settings.LogRetentionDays);
                }

                retentionDays = _retentionDays;
            }

            if (runMaintenance)
            {
                // Outside the lock: both touch the file, and LogStore has its own.
                LogStore.MigrateLegacyIfNeeded();
                Prune(retentionDays);
            }
        }

        private static int NormalizeRetention(int? days) =>
            Config.SettingsINI.ResolveLogRetentionDays(days);
    }
}
