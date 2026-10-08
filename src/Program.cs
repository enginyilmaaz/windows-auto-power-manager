using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WindowsAutoPowerManager.Config;
using WindowsAutoPowerManager.Functions;

namespace WindowsAutoPowerManager
{
    internal static class Program
    {
        // Held for the lifetime of the process; releasing it would let a second instance start.
        private static Mutex _singleInstanceMutex;

        /// <summary>
        ///     Claims the single-instance lock, returning false when another instance holds it.
        /// </summary>
        private static bool TryClaimSingleInstance()
        {
            // Session-local, matching the previous same-user process scan. Enumerating processes
            // and reading MainModule threw for processes owned by other users or running elevated,
            // which surfaced as a startup error dialog. An abandoned mutex is released when the
            // owning process dies, so a crashed instance no longer blocks the next launch.
            _singleInstanceMutex = new Mutex(
                true,
                @"Local\WindowsAutoPowerManager.SingleInstance",
                out bool createdNew);

            if (createdNew)
            {
                return true;
            }

            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            return false;
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            try
            {
                StartupTrace.Mark("main");

                if (!File.Exists(SettingsStorage.SettingsPath))
                {
                    SettingsStorage.Save(SettingsINI.DefaulSettingFile());
                }

                if (!TryClaimSingleInstance())
                {
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // A tray start may never show the window; the browser process would then sit
                // idle for hours. The environment is created on the first show instead.
                bool startInTray = StartWithWindows.IsRunInTaskBarRequested(Environment.GetCommandLineArgs());
                if (!startInTray)
                {
                    WebViewEnvironmentProvider.Prewarm();
                }
                StartupTrace.Mark(startInTray ? "webview environment deferred (tray start)" : "webview environment requested");

                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    Constants.AppName + " - Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
