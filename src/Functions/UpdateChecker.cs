using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsAutoPowerManager.Functions
{
    /// <summary>
    ///     Reads the latest GitHub release, downloads its installer and hands over to it. The
    ///     repository is public, so no token or sign-in is involved anywhere in this flow.
    /// </summary>
    internal static class UpdateChecker
    {
        private const string ReleasesApiUrl =
            "https://api.github.com/repos/" + Config.Constants.UpdateRepository + "/releases/latest";

        private const string ReleasesPageUrl =
            "https://github.com/" + Config.Constants.UpdateRepository + "/releases/latest";

        private const int DownloadBufferSize = 81920;
        private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(20);

        private static readonly Lazy<HttpClient> Client = new Lazy<HttpClient>(CreateClient);

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            // GitHub rejects API requests without a User-Agent.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WindowsAutoPowerManager-Updater");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        public static async Task<UpdateInfo> CheckAsync()
        {
            string currentVersion = BuildMetadata.Version;
            var result = new UpdateInfo
            {
                CurrentVersion = currentVersion,
                ReleaseUrl = ReleasesPageUrl,
                Reason = UpdateInfo.ReasonError
            };

            try
            {
                using var timeout = new CancellationTokenSource(CheckTimeout);
                using HttpResponseMessage response =
                    await Client.Value.GetAsync(ReleasesApiUrl, timeout.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    result.Reason = "http-" + (int)response.StatusCode;
                    return result;
                }

                string payload = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                using JsonDocument document = JsonDocument.Parse(payload);
                JsonElement root = document.RootElement;

                string latestVersion = ReadString(root, "tag_name");
                if (string.IsNullOrWhiteSpace(latestVersion))
                {
                    result.Reason = UpdateInfo.ReasonNoTag;
                    return result;
                }

                result.LatestVersion = latestVersion.TrimStart('v', 'V');
                result.ReleaseUrl = ReadString(root, "html_url") ?? ReleasesPageUrl;
                FillInstallerAsset(root, result);

                if (!UpdatePolicy.IsNewer(result.LatestVersion, currentVersion))
                {
                    result.Reason = UpdateInfo.ReasonUpToDate;
                    return result;
                }

                if (string.IsNullOrWhiteSpace(result.AssetDownloadUrl))
                {
                    result.Reason = UpdateInfo.ReasonNoAsset;
                    return result;
                }

                result.IsAvailable = true;
                result.Reason = null;
                return result;
            }
            catch (Exception)
            {
                // A failed check must never interrupt the app; the caller only surfaces the reason
                // when the user asked for the check explicitly.
                result.Reason = UpdateInfo.ReasonError;
                return result;
            }
        }

        private static void FillInstallerAsset(JsonElement root, UpdateInfo result)
        {
            if (!root.TryGetProperty("assets", out JsonElement assets) ||
                assets.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (JsonElement asset in assets.EnumerateArray())
            {
                string name = ReadString(asset, "name");
                if (!UpdatePolicy.IsInstallerAsset(name))
                {
                    continue;
                }

                result.AssetName = name;
                result.AssetDownloadUrl = ReadString(asset, "browser_download_url");
                return;
            }
        }

        private static string ReadString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        /// <summary>Downloads the installer to a private temp folder and returns its path.</summary>
        public static async Task<string> DownloadInstallerAsync(
            UpdateInfo info,
            IProgress<UpdateDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.AssetDownloadUrl))
            {
                throw new InvalidOperationException("No installer asset to download.");
            }

            string targetDirectory = Path.Combine(Path.GetTempPath(), "WindowsAutoPowerManagerUpdate");
            Directory.CreateDirectory(targetDirectory);
            string targetPath = Path.Combine(targetDirectory, info.AssetName ?? "Setup.exe");

            using HttpResponseMessage response = await Client.Value
                .GetAsync(info.AssetDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            long total = response.Content.Headers.ContentLength ?? 0;
            long received = 0;
            progress?.Report(new UpdateDownloadProgress(0, total));

            using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var destination = new FileStream(
                targetPath, FileMode.Create, FileAccess.Write, FileShare.None, DownloadBufferSize, useAsync: true);

            var buffer = new byte[DownloadBufferSize];
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                received += read;
                progress?.Report(new UpdateDownloadProgress(received, total));
            }

            return targetPath;
        }

        /// <summary>
        ///     Starts the installer and reports whether it launched. The caller must exit right
        ///     afterwards: the running executable cannot be replaced while it is held open, and
        ///     the installer relaunches the app once it finishes.
        /// </summary>
        public static bool StartInstaller(string installerPath)
        {
            if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
            {
                return false;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    // Silent, but still shows its progress window so a long install is visible.
                    Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
