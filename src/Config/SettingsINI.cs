namespace WindowsAutoPowerManager.Config
{
    public static class SettingsINI
    {
        public static Settings DefaulSettingFile()
        {
            Settings setting = new Settings
            {
                LogsEnabled = true,
                StartWithWindows = false,
                RunInTaskbarWhenClosed = false,
                ConfirmExitOnProgramExit = true,
                IsCountdownNotifierEnabled = true,
                CountdownNotifierSeconds = 5,
                Language = "auto",
                Theme = "system",
                UpdateCheckEnabled = true,
                UpdateCheckInterval = UpdateCheckIntervals.Default,
                DebugLogEnabled = false,

            };


            return setting;
        }
    }
}
