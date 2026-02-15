using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace WindowsShutdownHelper.functions
{
    public class startWithWindows
    {
        public static string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static string pathwithArguments = Application.ExecutablePath + " -runInTaskBar";

        public static RegistryKey startupKey;


        public static void Is64BitOS()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                startupKey = RegistryKey
                    .OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                    .OpenSubKey(keyName, true);
            }
            else
            {
                startupKey = RegistryKey
                    .OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                    .OpenSubKey(keyName, true);
            }
        }

        public static void AddStartup(string appTitle)
        {
            Is64BitOS();

            if (startupKey.GetValue(appTitle) == null)
            {
                startupKey.SetValue(appTitle, pathwithArguments, RegistryValueKind.String);
            }

            startupKey.Close();
        }


        public static void DeleteStartup(string appTitle)
        {
            Is64BitOS();
            if (startupKey.GetValue(appTitle) != null)
            {
                startupKey.DeleteValue(appTitle);
            }

            startupKey.Close();
        }
    }
}