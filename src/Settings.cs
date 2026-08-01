using System;

namespace WindowsAutoPowerManager
{
    public class Settings
    {
        public bool LogsEnabled { get; set; }
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
    }
}
