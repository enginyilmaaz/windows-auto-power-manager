using System;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Formats the remaining pause time the way the web view's pause banner does
    ///     ("1h 59m 12s"), so the tray menu and the main window never show two spellings of
    ///     the same countdown.
    /// </summary>
    internal static class PauseCountdown
    {
        public static string Format(double remainingSeconds)
        {
            int secs = (int)Math.Floor(Math.Max(0, remainingSeconds));
            int h = secs / 3600;
            int m = (secs % 3600) / 60;
            int s = secs % 60;

            string text = string.Empty;
            if (h > 0) text += h + "h ";
            if (m > 0 || h > 0) text += m + "m ";
            return text + s + "s";
        }
    }
}
