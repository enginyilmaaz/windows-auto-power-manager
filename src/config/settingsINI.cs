namespace WindowsShutdownHelper.config
{
    public static class settingsINI
    {
        public static Settings defaulSettingFile()
        {
            Settings setting = new Settings
            {
                logsEnabled = true,
                startWithWindows = false,
                runInTaskbarWhenClosed = false,
                isCountdownNotifierEnabled = true,
                countdownNotifierSeconds = 5,
                language = "auto",
                theme = "system"
            };


            return setting;
        }
    }
}