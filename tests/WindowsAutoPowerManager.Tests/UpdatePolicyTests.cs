using System;
using WindowsAutoPowerManager.Config;
using WindowsAutoPowerManager.Functions;
using Xunit;

namespace WindowsAutoPowerManager.Tests
{
    public class UpdatePolicyTests
    {
        private static readonly DateTime Now = new DateTime(2030, 5, 5, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData("1.0.106", "1.0.105")]
        [InlineData("1.1.0", "1.0.999")]
        [InlineData("2.0.0", "1.9.9")]
        public void HigherVersion_IsNewer(string candidate, string current)
        {
            Assert.True(UpdatePolicy.IsNewer(candidate, current));
        }

        [Fact]
        public void VersionsCompareNumerically_NotAsText()
        {
            // Text ordering would place 1.0.9 above 1.0.10 and never offer the update.
            Assert.True(UpdatePolicy.IsNewer("1.0.10", "1.0.9"));
            Assert.False(UpdatePolicy.IsNewer("1.0.9", "1.0.10"));
        }

        [Theory]
        [InlineData("1.0.105", "1.0.105")]
        [InlineData("1.0.104", "1.0.105")]
        public void SameOrOlderVersion_IsNotNewer(string candidate, string current)
        {
            Assert.False(UpdatePolicy.IsNewer(candidate, current));
        }

        [Fact]
        public void TagPrefixAndBuildMetadata_AreIgnored()
        {
            // Releases are tagged "v1.0.106" while the app reports "1.0.105+c9e580".
            Assert.True(UpdatePolicy.IsNewer("v1.0.106", "1.0.105+c9e580"));
            Assert.False(UpdatePolicy.IsNewer("v1.0.105", "1.0.105+c9e580"));
        }

        [Fact]
        public void MissingComponents_CountAsZero()
        {
            Assert.False(UpdatePolicy.IsNewer("1.1", "1.1.0"));
            Assert.True(UpdatePolicy.IsNewer("1.1.1", "1.1"));
        }

        [Theory]
        [InlineData("WindowsAutoPowerManager_Setup_1.0.105_c9e580.exe")]
        [InlineData("windowsautopowermanager_setup_1.0.1_abc.EXE")]
        public void SetupExecutable_IsTheInstallerAsset(string assetName)
        {
            Assert.True(UpdatePolicy.IsInstallerAsset(assetName));
        }

        [Theory]
        [InlineData("Windows Auto Power Manager.exe")]
        [InlineData("WindowsAutoPowerManager_Setup_1.0.105.zip")]
        [InlineData("")]
        [InlineData(null)]
        public void OtherAssets_AreNotOfferedAsTheInstaller(string assetName)
        {
            // The portable executable ships in the same release and must not be launched as setup.
            Assert.False(UpdatePolicy.IsInstallerAsset(assetName));
        }

        [Theory]
        [InlineData("hourly", 1)]
        [InlineData("daily", 24)]
        [InlineData("weekly", 168)]
        public void IntervalsResolveToTheirDuration(string interval, int expectedHours)
        {
            Assert.Equal(TimeSpan.FromHours(expectedHours), UpdatePolicy.ResolveInterval(interval));
        }

        [Theory]
        [InlineData("nonsense")]
        [InlineData("")]
        [InlineData(null)]
        public void UnknownInterval_FallsBackToDaily(string interval)
        {
            // A hand-edited settings file must keep checking rather than stop silently.
            Assert.Equal(TimeSpan.FromDays(1), UpdatePolicy.ResolveInterval(interval));
            Assert.Equal(UpdateCheckIntervals.Default, UpdatePolicy.NormalizeInterval(interval));
        }

        [Fact]
        public void CheckIsDue_WhenNoneWasEverRecorded()
        {
            Assert.True(UpdatePolicy.IsCheckDue(null, Now, UpdateCheckIntervals.Daily));
        }

        [Fact]
        public void CheckIsDue_OnlyAfterTheIntervalElapses()
        {
            DateTime lastCheck = Now.AddHours(-23);
            Assert.False(UpdatePolicy.IsCheckDue(lastCheck, Now, UpdateCheckIntervals.Daily));
            Assert.True(UpdatePolicy.IsCheckDue(lastCheck.AddHours(-1), Now, UpdateCheckIntervals.Daily));
        }

        [Fact]
        public void HourlyAndWeeklyIntervals_AreHonoured()
        {
            Assert.True(UpdatePolicy.IsCheckDue(Now.AddMinutes(-61), Now, UpdateCheckIntervals.Hourly));
            Assert.False(UpdatePolicy.IsCheckDue(Now.AddMinutes(-59), Now, UpdateCheckIntervals.Hourly));

            Assert.True(UpdatePolicy.IsCheckDue(Now.AddDays(-8), Now, UpdateCheckIntervals.Weekly));
            Assert.False(UpdatePolicy.IsCheckDue(Now.AddDays(-6), Now, UpdateCheckIntervals.Weekly));
        }

        [Fact]
        public void TimestampInTheFuture_MakesTheCheckDue()
        {
            // After a clock correction a future timestamp would otherwise postpone checks forever.
            Assert.True(UpdatePolicy.IsCheckDue(Now.AddDays(30), Now, UpdateCheckIntervals.Weekly));
        }

        [Theory]
        [InlineData("HOURLY", "hourly")]
        [InlineData("Weekly", "weekly")]
        [InlineData("daily", "daily")]
        public void IntervalNormalization_IsCaseInsensitive(string stored, string expected)
        {
            Assert.Equal(expected, UpdatePolicy.NormalizeInterval(stored));
        }
    }
}
