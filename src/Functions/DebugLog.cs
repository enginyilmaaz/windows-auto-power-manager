using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Opt-in diagnostic trace for problems the action log cannot explain, such as the UI
    ///     thread stalling: the action log only records actions that ran, so a freeze leaves no
    ///     trace in it at all.
    ///     Every line is flushed immediately, because a process that hangs or is killed never gets
    ///     to flush a buffer, and those are exactly the runs worth reading.
    /// </summary>
    internal static class DebugLog
    {
        /// <summary>Ceiling across both files, so the trace can never fill the disk.</summary>
        public const long MaxTotalBytes = 5L * 1024 * 1024;

        private const long MaxCurrentBytes = MaxTotalBytes / 2;
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

        private static readonly object SyncRoot = new object();
        private static readonly string CurrentPath = Path.Combine(AppContext.BaseDirectory, "Debug.log");
        private static readonly string PreviousPath = Path.Combine(AppContext.BaseDirectory, "Debug.prev.log");

        private static bool _enabled;

        public static bool IsEnabled => _enabled;

        public static void Configure(Settings settings)
        {
            _enabled = settings?.DebugLogEnabled ?? false;
        }

        public static void Write(string category, string message)
        {
            if (!_enabled)
            {
                return;
            }

            string line = string.Concat(
                DateTime.Now.ToString(TimestampFormat, CultureInfo.InvariantCulture),
                "  [", category, "] ", message, Environment.NewLine);

            try
            {
                lock (SyncRoot)
                {
                    RollIfOversizedLocked();
                    File.AppendAllText(CurrentPath, line, Encoding.UTF8);
                }
            }
            catch
            {
                // Diagnostics must never take the app down.
            }
        }

        /// <summary>
        ///     Rolls the current file aside once it reaches half the ceiling, keeping one older
        ///     file. Truncating a single file instead would discard the whole history at the exact
        ///     moment it wrapped, which is usually the part worth reading.
        /// </summary>
        private static void RollIfOversizedLocked()
        {
            var current = new FileInfo(CurrentPath);
            if (!current.Exists || current.Length < MaxCurrentBytes)
            {
                return;
            }

            if (File.Exists(PreviousPath))
            {
                File.Delete(PreviousPath);
            }

            File.Move(CurrentPath, PreviousPath);
        }

        public static void Clear()
        {
            lock (SyncRoot)
            {
                try
                {
                    if (File.Exists(CurrentPath)) File.Delete(CurrentPath);
                    if (File.Exists(PreviousPath)) File.Delete(PreviousPath);
                }
                catch
                {
                }
            }
        }
    }
}
