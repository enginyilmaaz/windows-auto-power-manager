namespace WindowsAutoPowerManager.Config
{
    public static class Constants
    {
        public const string AppName = "Windows Auto Power Manager";

        /// <summary>Switch name without its prefix, so both -x and /x forms can be recognised.</summary>
        public const string RunInTaskBarSwitch = "runInTaskBar";

        /// <summary>Written to the startup entry so the app knows to stay in the tray.</summary>
        public const string RunInTaskBarArgument = "-" + RunInTaskBarSwitch;
    }
}
