using WindowsAutoPowerManager.Config;
using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    /// <summary>
    ///     The startup entry passes a switch telling the app to stay in the tray. A missing or
    ///     unrecognised switch makes the window open on every sign-in, which is how a startup entry
    ///     written before the switch existed kept showing the window.
    /// </summary>
    public class StartWithWindowsTests
    {
        private const string ExePath = @"C:\Apps\Windows Auto Power Manager.exe";

        [Fact]
        public void StartupArgument_IsRecognised()
        {
            Assert.True(StartWithWindows.IsRunInTaskBarRequested(
                new[] { ExePath, Constants.RunInTaskBarArgument }));
        }

        [Theory]
        [InlineData("-runInTaskBar")]
        [InlineData("/runInTaskBar")]
        [InlineData("--runInTaskBar")]
        [InlineData("-runintaskbar")]
        [InlineData("-RUNINTASKBAR")]
        public void SwitchPrefixAndCasing_AreIgnored(string argument)
        {
            Assert.True(StartWithWindows.IsRunInTaskBarRequested(new[] { ExePath, argument }));
        }

        [Fact]
        public void NoArguments_MeansTheWindowIsShown()
        {
            // A startup entry with empty arguments is exactly the case that opened the window.
            Assert.False(StartWithWindows.IsRunInTaskBarRequested(new[] { ExePath }));
        }

        [Theory]
        [InlineData("-runInTaskBarExtra")]
        [InlineData("runInTask")]
        [InlineData("-silent")]
        public void UnrelatedArguments_AreNotMistakenForTheSwitch(string argument)
        {
            Assert.False(StartWithWindows.IsRunInTaskBarRequested(new[] { ExePath, argument }));
        }

        [Fact]
        public void NullArgumentsAndEntries_AreTolerated()
        {
            Assert.False(StartWithWindows.IsRunInTaskBarRequested(null));
            Assert.False(StartWithWindows.IsRunInTaskBarRequested(new[] { ExePath, null }));
        }

        [Fact]
        public void StartupArgumentConstant_CarriesTheSwitchName()
        {
            // The entry writer and the reader must not drift apart.
            Assert.Equal("-" + Constants.RunInTaskBarSwitch, Constants.RunInTaskBarArgument);
        }
    }
}
