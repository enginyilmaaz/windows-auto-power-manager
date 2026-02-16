namespace WindowsShutdownHelper
{
    public class Settings
    {
        public bool logsEnabled { get; set; }
        public bool startWithWindows { get; set; }
        public bool runInTaskbarWhenClosed { get; set; }
        public bool isCountdownNotifierEnabled { get; set; }
        public int countdownNotifierSeconds { get; set; }
        public string language { get; set; }
        public string theme { get; set; }
    }
}