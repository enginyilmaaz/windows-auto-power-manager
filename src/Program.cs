using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using WindowsAutoPowerManager.Config;
using WindowsAutoPowerManager.Functions;

namespace WindowsAutoPowerManager
{
    internal static class Program
    {
        public static Process PriorProcess()
        // Returns a System.Diagnostics.Process pointing to
        // a pre-existing process with the same name as the
        // current one, if any; or null if the current process
        // is unique.
        {
            Process curr = Process.GetCurrentProcess();
            Process[] procs = Process.GetProcessesByName(curr.ProcessName);
            foreach (Process p in procs)
            {
                if (p.Id != curr.Id &&
                    p.MainModule.FileName == curr.MainModule.FileName)
                {
                    return p;
                }
            }

            return null;
        }


        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            try
            {
                if (!File.Exists(SettingsStorage.SettingsPath))
                {
                    SettingsStorage.Save(SettingsINI.DefaulSettingFile());
                }

                if (PriorProcess() != null)
                {
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                WebViewEnvironmentProvider.Prewarm();
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
