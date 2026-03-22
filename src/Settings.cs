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

    }
}
