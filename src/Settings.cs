using System;

namespace WindowsAutoPowerManager
{
    public class Settings
    {
        public bool LogsEnabled { get; set; }

        /// <summary>
        ///     How many days of action history to keep. Null means the setting predates this
        ///     version and the default applies; <c>0</c> means keep everything. Nullable because
        ///     those two cases are different and both arrive as zero otherwise.
        /// </summary>
        public int? LogRetentionDays { get; set; }
        public bool StartWithWindows { get; set; }
        public bool RunInTaskbarWhenClosed { get; set; }
        public bool ConfirmExitOnProgramExit { get; set; } = true;
        public bool IsCountdownNotifierEnabled { get; set; }
        public int CountdownNotifierSeconds { get; set; }
        public string Language { get; set; }
        public string Theme { get; set; }
        public bool UpdateCheckEnabled { get; set; } = true;
        public string UpdateCheckInterval { get; set; } = Config.UpdateCheckIntervals.Default;

        /// <summary>Persisted so the interval survives restarts instead of restarting each launch.</summary>
        public DateTime? LastUpdateCheckUtc { get; set; }

        /// <summary>Diagnostic trace, off by default: it is meant to be switched on while chasing a problem.</summary>
        public bool DebugLogEnabled { get; set; }
    }
}
