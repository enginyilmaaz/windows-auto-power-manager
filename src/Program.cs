using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using WindowsShutdownHelper.config;
using WindowsShutdownHelper.functions;

namespace WindowsShutdownHelper
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
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"))
            {
                jsonWriter.WriteJson(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json", true,
                    settingsINI.defaulSettingFile());
            }

            if (PriorProcess() != null)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            WebViewEnvironmentProvider.Prewarm();
            Application.Run(new mainForm());
        }
    }
}
