namespace WindowsAutoPowerManager.Config
{
    public static class SettingsINI
    {
        /// <summary>Roughly three months of history: enough to answer "what happened last month".</summary>
        public const int DefaultLogRetentionDays = 90;

        /// <summary>Keep everything. Pruning is skipped entirely at this value.</summary>
        public const int UnlimitedLogRetention = 0;

        /// <summary>
        ///     Resolves the stored value: absent means this version's default, zero means unlimited,
        ///     and a negative value is treated as absent rather than trusted.
        /// </summary>
        public static int ResolveLogRetentionDays(int? stored)
        {
            if (!stored.HasValue || stored.Value < 0)
            {
                return DefaultLogRetentionDays;
            }

            return stored.Value;
        }

        public static Settings DefaulSettingFile()
        {
            Settings setting = new Settings
            {
                LogsEnabled = true,
                LogRetentionDays = DefaultLogRetentionDays,
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
