using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsAutoPowerManager.Functions
{
    public class StartWithWindows
    {
        private const string KeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private static string StartupCommand
        {
            get { return "\"" + Application.ExecutablePath + "\""; }
        }

        public static void AddStartup(string appTitle)
        {
            string entryName = ResolveEntryName(appTitle);

            if (TryCreateOrUpdateStartupShortcut(entryName))
            {
                DeleteStartupRegistryValue(entryName);
                return;
            }

            try
            {
                using (RegistryKey startupKey = OpenStartupKey())
                {
                    if (startupKey == null) return;

                    string currentValue = startupKey.GetValue(entryName) as string;
                    if (!string.Equals(currentValue, StartupCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        startupKey.SetValue(entryName, StartupCommand, RegistryValueKind.String);
                    }
                }
            }
            catch
            {
                // Keep startup toggle resilient against registry access issues.
            }
        }

        public static void DeleteStartup(string appTitle)
        {
            string entryName = ResolveEntryName(appTitle);
            DeleteStartupShortcut(entryName);
            DeleteStartupRegistryValue(entryName);
        }

        private static void DeleteStartupRegistryValue(string entryName)
        {
            try
            {
                using (RegistryKey startupKey = OpenStartupKey())
                {
                    if (startupKey == null) return;

                    if (startupKey.GetValue(entryName) != null)
                    {
                        startupKey.DeleteValue(entryName, false);
                    }
                }
            }
            catch
            {
                // Keep startup toggle resilient against registry access issues.
            }
        }

        private static RegistryKey OpenStartupKey()
        {
            RegistryView view = Environment.Is64BitOperatingSystem
                ? RegistryView.Registry64
                : RegistryView.Registry32;

            return RegistryKey
                .OpenBaseKey(RegistryHive.CurrentUser, view)
                .OpenSubKey(KeyName, true);
        }

        private static bool TryCreateOrUpdateStartupShortcut(string entryName)
        {
            object shell = null;
            object shortcut = null;

            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (string.IsNullOrWhiteSpace(startupFolder))
                {
                    return false;
                }

                Directory.CreateDirectory(startupFolder);
                string shortcutPath = Path.Combine(startupFolder, MakeSafeFileName(entryName) + ".lnk");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    return false;
                }

                shell = Activator.CreateInstance(shellType);
                shortcut = shellType.InvokeMember(
                    "CreateShortcut",
                    BindingFlags.InvokeMethod,
                    null,
                    shell,
                    new object[] { shortcutPath });

                if (shortcut == null)
                {
                    return false;
                }

                Type shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut,
                    new object[] { Application.ExecutablePath });
                shortcutType.InvokeMember("Arguments", BindingFlags.SetProperty, null, shortcut,
                    new object[] { string.Empty });
                shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut,
                    new object[] { AppContext.BaseDirectory });
                shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut,
                    new object[] { Config.Constants.AppName });
                shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                ReleaseComObject(shortcut);
                ReleaseComObject(shell);
            }
        }

        private static void DeleteStartupShortcut(string entryName)
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (string.IsNullOrWhiteSpace(startupFolder))
                {
                    return;
                }

                string shortcutPath = Path.Combine(startupFolder, MakeSafeFileName(entryName) + ".lnk");
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
            catch
            {
                // Keep startup toggle resilient against file-system access issues.
            }
        }

        private static string ResolveEntryName(string appTitle)
        {
            return string.IsNullOrWhiteSpace(appTitle)
                ? Config.Constants.AppName
                : appTitle.Trim();
        }

        private static string MakeSafeFileName(string value)
        {
            string fileName = string.IsNullOrWhiteSpace(value) ? "StartupApp" : value.Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }

        private static void ReleaseComObject(object comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
            {
                Marshal.FinalReleaseComObject(comObject);
            }
        }
    }
}
