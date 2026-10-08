using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Records how long each startup stage took from process launch, so a slow start can be
    ///     attributed to a stage instead of guessed. Stages reached before the settings are read
    ///     (and with them the debug log switch) are held in memory and written out once the log
    ///     is configured; nothing is written while the debug log is off.
    /// </summary>
    internal static class StartupTrace
    {
        private const string Category = "startup";

        private static readonly object SyncRoot = new object();
        private static readonly List<string> Pending = new List<string>();
        private static DateTime? _processStart;
        private static bool _flushed;

        public static void Mark(string stage)
        {
            lock (SyncRoot)
            {
                string line = stage + " +" + ElapsedMs() + "ms";
                if (_flushed)
                {
                    DebugLog.Write(Category, line);
                }
                else
                {
                    Pending.Add(line);
                }
            }
        }

        /// <summary>Called once the debug log knows whether it is enabled.</summary>
        public static void Flush()
        {
            lock (SyncRoot)
            {
                _flushed = true;
                foreach (string line in Pending)
                {
                    DebugLog.Write(Category, line);
                }
                Pending.Clear();
            }
        }

        private static long ElapsedMs()
        {
            try
            {
                if (_processStart == null)
                {
                    // Measured from the process start rather than from Main, so the runtime's
                    // own startup is part of the picture.
                    using (Process process = Process.GetCurrentProcess())
                    {
                        _processStart = process.StartTime;
                    }
                }
                return (long)(DateTime.Now - _processStart.Value).TotalMilliseconds;
            }
            catch
            {
                return -1;
            }
        }
    }
}
