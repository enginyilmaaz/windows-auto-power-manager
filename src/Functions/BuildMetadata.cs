using System;
using System.Reflection;

namespace WindowsAutoPowerManager
{
    internal static class BuildMetadata
    {
        private static readonly Lazy<(string version, string commitId)> Cache =
            new Lazy<(string version, string commitId)>(Resolve);

        public static string Version => Cache.Value.version;
        public static string CommitId => Cache.Value.commitId;

        private static (string version, string commitId) Resolve()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetName().Version?.ToString();
            string commitId = BuildInfo.CommitId;

            string infoVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(infoVersion))
            {
                string raw = infoVersion.Trim();
                int plusIndex = raw.IndexOf('+');
                if (plusIndex >= 0)
                {
                    string infoPart = raw.Substring(0, plusIndex).Trim();
                    string commitPart = raw.Substring(plusIndex + 1).Trim();
                    if (!string.IsNullOrWhiteSpace(infoPart))
                    {
                        version = infoPart;
                    }
                    if (!string.IsNullOrWhiteSpace(commitPart))
                    {
                        int dotIndex = commitPart.IndexOf('.');
                        if (dotIndex > 0)
                        {
                            commitPart = commitPart.Substring(0, dotIndex);
                        }
                        commitId = commitPart;
                    }
                }
                else
                {
                    version = raw;
                }
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                version = BuildInfo.Version;
            }

            if (string.IsNullOrWhiteSpace(commitId))
            {
                commitId = BuildInfo.CommitId;
            }

            return (version, commitId);
        }
    }
}
