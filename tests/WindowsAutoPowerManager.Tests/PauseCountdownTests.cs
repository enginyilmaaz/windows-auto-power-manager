using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    public class PauseCountdownTests
    {
        [Theory]
        [InlineData(0, "0s")]
        [InlineData(59, "59s")]
        [InlineData(60, "1m 0s")]
        [InlineData(3600, "1h 0m 0s")]
        [InlineData(7152, "1h 59m 12s")]
        public void Format_MatchesTheWebBanner(double seconds, string expected)
        {
            Assert.Equal(expected, PauseCountdown.Format(seconds));
        }

        [Fact]
        public void Format_TruncatesFractionsInsteadOfRounding()
        {
            // The banner floors as well; rounding up would show a second that has not arrived.
            Assert.Equal("1m 1s", PauseCountdown.Format(61.9));
        }

        [Fact]
        public void Format_ClampsNegativeToZero()
        {
            Assert.Equal("0s", PauseCountdown.Format(-5));
        }
    }
}
