namespace WindowsShutdownHelper
{
    public class Settings
    {
        public bool LogsEnabled { get; set; }
        public bool StartWithWindows { get; set; }
        public bool RunInTaskbarWhenClosed { get; set; }
        public bool IsCountdownNotifierEnabled { get; set; }
        public int CountdownNotifierSeconds { get; set; }
        public string Language { get; set; }
        public string Theme { get; set; }
    }
}