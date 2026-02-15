using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace WindowsShutdownHelper.functions
{
    internal static class WebViewEnvironmentProvider
    {
        private static readonly object _syncRoot = new object();
        private static Task<CoreWebView2Environment> _sharedEnvironmentTask;

        public static Task<CoreWebView2Environment> GetAsync()
        {
            lock (_syncRoot)
            {
                if (_sharedEnvironmentTask == null ||
                    _sharedEnvironmentTask.IsFaulted ||
                    _sharedEnvironmentTask.IsCanceled)
                {
                    _sharedEnvironmentTask = CreateEnvironmentAsync();
                }

                return _sharedEnvironmentTask;
            }
        }

        public static void Prewarm()
        {
            // Keep initialization on the current (UI/STA) thread.
            // Running in Task.Run may force MTA and cause RPC_E_CHANGED_MODE.
            var task = GetAsync();
            task.ContinueWith(t =>
            {
                // Observe task exceptions to avoid UnobservedTaskException.
                var _ = t.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private static Task<CoreWebView2Environment> CreateEnvironmentAsync()
        {
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WindowsShutdownHelper",
                "WebView2");

            Directory.CreateDirectory(userDataFolder);
            return CoreWebView2Environment.CreateAsync(null, userDataFolder);
        }
    }
}
