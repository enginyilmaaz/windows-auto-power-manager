using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    /// <summary>
    ///     Guards the way the monitor off is sent. The flags once included SMTO_BLOCK and
    ///     SMTO_NOTIMEOUTIFNOTHUNG, and the message once went to HWND_BROADCAST: the timeout was
    ///     waived for every window that kept pumping, the wait repeated for each top-level window
    ///     on the desktop, and the calling thread could not service sent messages meanwhile. One
    ///     off held the UI thread for 61 seconds with the keyboard and mouse dead, traced as
    ///     "ui thread blocked for 63427 ms" on 2026-10-16.
    /// </summary>
    public class MonitorPowerSendTests
    {
        [Fact]
        public void SendFlags_DoNotWaiveTheTimeout()
        {
            Assert.False(
                Actions.TurnOff.MonitorPowerSendFlags.HasFlag(
                    Actions.TurnOff.SendMessageTimeoutFlags.NoTimeoutIfNotHung),
                "NoTimeoutIfNotHung lets the call run past its timeout for as long as the " +
                "receiving thread keeps pumping, which is what froze the UI thread.");
        }

        [Fact]
        public void SendFlags_DoNotBlockTheCallingThread()
        {
            Assert.False(
                Actions.TurnOff.MonitorPowerSendFlags.HasFlag(
                    Actions.TurnOff.SendMessageTimeoutFlags.Block),
                "Block stops the calling thread servicing sent messages while it waits, so the " +
                "window stops responding for the whole call.");
        }

        [Fact]
        public void SendFlags_StillAbortIfTheReceiverIsHung()
        {
            Assert.True(
                Actions.TurnOff.MonitorPowerSendFlags.HasFlag(
                    Actions.TurnOff.SendMessageTimeoutFlags.AbortIfHung),
                "A hung receiver must not hold the call for the full timeout.");
        }

        [Fact]
        public void CallTimeout_StaysShortEnoughToKeepTheTickResponsive()
        {
            // The tick runs every second; a send that can outlast a couple of ticks is a stall.
            Assert.InRange(Actions.TurnOff.MonitorPowerCallTimeoutMs, 1u, 2000u);
        }
    }
}
