namespace WindowsAutoPowerManager.Functions
{
    /// <summary>Outcome of an update check, including why no update is offered.</summary>
    internal sealed class UpdateInfo
    {
        public const string ReasonUpToDate = "up-to-date";
        public const string ReasonNoAsset = "no-asset";
        public const string ReasonNoTag = "no-tag";
        public const string ReasonError = "error";

        public bool IsAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public string Reason { get; set; }
        public string AssetName { get; set; }
        public string AssetDownloadUrl { get; set; }
        public string ReleaseUrl { get; set; }
    }

    /// <summary>Bytes transferred so far. Total is zero when the server sends no length.</summary>
    internal readonly struct UpdateDownloadProgress
    {
        public UpdateDownloadProgress(long received, long total)
        {
            Received = received;
            Total = total;
        }

        public long Received { get; }
        public long Total { get; }
    }
}
