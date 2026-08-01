using System;
using System.Collections.Generic;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     The decision half of the updater: which release counts as newer, which asset is the
    ///     installer, and whether a scheduled check is due. Kept free of network and settings I/O
    ///     so every rule can be tested directly.
    /// </summary>
    internal static class UpdatePolicy
    {
        private const string InstallerAssetPrefix = "WindowsAutoPowerManager_Setup_";
        private const string InstallerAssetExtension = ".exe";

        /// <summary>Turns "v1.0.105" or "1.0.105+abc" into its numeric components.</summary>
        public static int[] ParseVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new int[0];
            }

            string trimmed = value.Trim();
            if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(1);
            }

            // Build metadata after '+' is not part of the version ordering.
            int metadataIndex = trimmed.IndexOf('+');
            if (metadataIndex >= 0)
            {
                trimmed = trimmed.Substring(0, metadataIndex);
            }

            string[] parts = trimmed.Split('.');
            var components = new int[parts.Length];
            for (int index = 0; index < parts.Length; ++index)
            {
                components[index] = int.TryParse(parts[index], out int parsed) ? parsed : 0;
            }

            return components;
        }

        /// <summary>
        ///     Compares component by component rather than as text, so 1.0.10 ranks above 1.0.9.
        ///     Missing components count as zero, making 1.1 equal to 1.1.0.
        /// </summary>
        public static bool IsNewer(string candidate, string current)
        {
            int[] candidateParts = ParseVersion(candidate);
            int[] currentParts = ParseVersion(current);
            int length = Math.Max(candidateParts.Length, currentParts.Length);

            for (int index = 0; index < length; ++index)
            {
                int candidatePart = index < candidateParts.Length ? candidateParts[index] : 0;
                int currentPart = index < currentParts.Length ? currentParts[index] : 0;

                if (candidatePart > currentPart) return true;
                if (candidatePart < currentPart) return false;
            }

            return false;
        }

        /// <summary>
        ///     Picks the installer out of a release's assets. The portable executable is published
        ///     alongside it, so matching on the setup prefix avoids offering the wrong download.
        /// </summary>
        public static bool IsInstallerAsset(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return false;
            }

            return assetName.StartsWith(InstallerAssetPrefix, StringComparison.OrdinalIgnoreCase) &&
                   assetName.EndsWith(InstallerAssetExtension, StringComparison.OrdinalIgnoreCase);
        }

        public static TimeSpan ResolveInterval(string interval)
        {
            if (string.Equals(interval, Config.UpdateCheckIntervals.Hourly, StringComparison.OrdinalIgnoreCase))
            {
                return TimeSpan.FromHours(1);
            }

            if (string.Equals(interval, Config.UpdateCheckIntervals.Weekly, StringComparison.OrdinalIgnoreCase))
            {
                return TimeSpan.FromDays(7);
            }

            // Daily is also the fallback for an unrecognised value, so a hand-edited settings file
            // keeps checking rather than silently never checking again.
            return TimeSpan.FromDays(1);
        }

        /// <summary>
        ///     A check is due when none was ever recorded, when the interval has elapsed, or when
        ///     the stored timestamp lies in the future, which happens after a clock correction and
        ///     would otherwise postpone checks indefinitely.
        /// </summary>
        public static bool IsCheckDue(DateTime? lastCheckUtc, DateTime nowUtc, string interval)
        {
            if (!lastCheckUtc.HasValue)
            {
                return true;
            }

            if (lastCheckUtc.Value > nowUtc)
            {
                return true;
            }

            return nowUtc - lastCheckUtc.Value >= ResolveInterval(interval);
        }

        public static string NormalizeInterval(string interval)
        {
            foreach (string known in KnownIntervals())
            {
                if (string.Equals(interval, known, StringComparison.OrdinalIgnoreCase))
                {
                    return known;
                }
            }

            return Config.UpdateCheckIntervals.Default;
        }

        public static IEnumerable<string> KnownIntervals()
        {
            yield return Config.UpdateCheckIntervals.Hourly;
            yield return Config.UpdateCheckIntervals.Daily;
            yield return Config.UpdateCheckIntervals.Weekly;
        }
    }
}
