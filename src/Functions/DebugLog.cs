using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Opt-in diagnostic trace for problems the action log cannot explain, such as the UI
    ///     thread stalling: the action log records only actions that ran, so a freeze leaves no
    ///     trace in it at all.
    ///
    ///     Kept deliberately cheap. While disabled every call returns on a bool test and touches
    ///     no file. While enabled the writer stays open and the size is tracked in memory, so a
    ///     line costs one buffered write rather than an open, a length query and a close. Nothing
    ///     accumulates in memory, so the trace cannot grow the process.
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

        private static volatile bool _enabled;
        private static StreamWriter _writer;
        private static long _currentBytes;

        public static bool IsEnabled => _enabled;

        public static void Configure(Settings settings)
        {
            bool enable = settings?.DebugLogEnabled ?? false;
            if (enable == _enabled)
            {
                return;
            }

            lock (SyncRoot)
            {
                _enabled = enable;
                if (!enable)
                {
                    CloseWriterLocked();
                }
            }
        }

        public static void Write(string category, string message)
        {
            // The common case is disabled: one volatile read, no allocation, no file access.
            if (!_enabled)
            {
                return;
            }

            try
            {
                lock (SyncRoot)
                {
                    if (!_enabled)
                    {
                        return;
                    }

                    if (_writer == null && !TryOpenWriterLocked())
                    {
                        return;
                    }

                    if (_currentBytes >= MaxCurrentBytes)
                    {
                        RollLocked();
                        if (_writer == null)
                        {
                            return;
                        }
                    }

                    string line = string.Concat(
                        DateTime.Now.ToString(TimestampFormat, CultureInfo.InvariantCulture),
                        "  [", category, "] ", message);

                    _writer.WriteLine(line);

                    // Counted rather than queried from disk: a length check per line would turn
                    // every trace entry into a filesystem metadata round trip.
                    _currentBytes += line.Length + Environment.NewLine.Length;
                }
            }
            catch
            {
                // Diagnostics must never take the app down.
            }
        }

        private static bool TryOpenWriterLocked()
        {
            try
            {
                var existing = new FileInfo(CurrentPath);
                _currentBytes = existing.Exists ? existing.Length : 0;

                // AutoFlush: a process that hangs or is killed never runs a flush, and those are
                // exactly the runs worth reading. FileShare.Read keeps the file openable while
                // the app is still running.
                var stream = new FileStream(CurrentPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                return true;
            }
            catch
            {
                _writer = null;
                return false;
            }
        }

        private static void CloseWriterLocked()
        {
            try
            {
                _writer?.Dispose();
            }
            catch
            {
            }

            _writer = null;
            _currentBytes = 0;
        }

        /// <summary>
        ///     Moves the full file aside and starts a new one, keeping a single older file.
        ///     Truncating one file instead would discard the whole history at the exact moment it
        ///     wrapped, which is usually the part worth reading.
        /// </summary>
        private static void RollLocked()
        {
            CloseWriterLocked();

            try
            {
                if (File.Exists(PreviousPath))
                {
                    File.Delete(PreviousPath);
                }

                if (File.Exists(CurrentPath))
                {
                    File.Move(CurrentPath, PreviousPath);
                }
            }
            catch
            {
                // If the roll fails the writer below simply appends to the existing file; the
                // ceiling is then enforced on the next attempt.
            }

            TryOpenWriterLocked();
        }

        public static void Clear()
        {
            lock (SyncRoot)
            {
                CloseWriterLocked();

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
