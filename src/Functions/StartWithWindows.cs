using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace WindowsShutdownHelper.Functions
{
    public class StartWithWindows
    {
        public static string KeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static string PathWithArguments = Application.ExecutablePath + " -runInTaskBar";

        public static RegistryKey StartupKey;


        public static void Is64BitOS()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                StartupKey = RegistryKey
                    .OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                    .OpenSubKey(KeyName, true);
            }
            else
            {
                StartupKey = RegistryKey
                    .OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                    .OpenSubKey(KeyName, true);
            }
        }

        public static void AddStartup(string appTitle)
        {
            Is64BitOS();

            if (StartupKey.GetValue(appTitle) == null)
            {
                StartupKey.SetValue(appTitle, PathWithArguments, RegistryValueKind.String);
            }

            StartupKey.Close();
        }


        public static void DeleteStartup(string appTitle)
        {
            Is64BitOS();
            if (StartupKey.GetValue(appTitle) != null)
            {
                StartupKey.DeleteValue(appTitle);
            }

            StartupKey.Close();
        }
    }
}