namespace WindowsAutoPowerManager.Config
{
    public static class Constants
    {
        public const string AppName = "Windows Auto Power Manager";

        /// <summary>Switch name without its prefix, so both -x and /x forms can be recognised.</summary>
        public const string RunInTaskBarSwitch = "runInTaskBar";

        /// <summary>Written to the startup entry so the app knows to stay in the tray.</summary>
        public const string RunInTaskBarArgument = "-" + RunInTaskBarSwitch;

        /// <summary>
        ///     Public repository the update check reads releases from. Named here rather than
        ///     inline because the check, the download and the release link must all agree on it.
        /// </summary>
        public const string UpdateRepository = "enginyilmaaz/windows-auto-power-manager";
    }
}
