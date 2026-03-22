using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WindowsAutoPowerManager.Functions
{
    public class ActionModel
    {
        public string TriggerType { get; set; }
        public string ActionType { get; set; }
        public string Value { get; set; }
        public string ValueUnit { get; set; }
        public string CreatedDate { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public class Actions
    {
        public static void DoActionByTypes(ActionModel action)
        {
            if (action.ActionType == Config.ActionTypes.LockComputer)
            {
                Lock.Computer();
            }

            if (action.ActionType == Config.ActionTypes.SleepComputer)
            {
                Sleep.Computer();
            }

            if (action.ActionType == Config.ActionTypes.TurnOffMonitor)
            {
                TurnOff.Monitor();
            }

            if (action.ActionType == Config.ActionTypes.ShutdownComputer)
            {
                ShutdownComputer();
            }

            if (action.ActionType == Config.ActionTypes.RestartComputer)
            {
                RestartComputer();
            }

            if (action.ActionType == Config.ActionTypes.LogOffWindows)
            {
                LogOff.Windows();
            }
        }

        public static void ShutdownComputer()
        {
            Logger.DoLog(Config.ActionTypes.ShutdownComputer);
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "shutdown",
                Arguments = "/s /t 0"
            };
            process.StartInfo = startInfo;
            process.Start();
        }


        public static void RestartComputer()
        {
            Logger.DoLog(Config.ActionTypes.RestartComputer);
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "shutdown",
                Arguments = "/r /t 0"
            };

            process.StartInfo = startInfo;
            process.Start();
        }


        public class Lock
        {
            public static bool ManualLocked = true;

            [DllImport("user32.dll")]
            public static extern void LockWorkStation();

            public static void Computer()
            {
                if (DetectScreen.IsLockedWorkstation() == false)
                {
                    ManualLocked = false;
                    Logger.DoLog(Config.ActionTypes.LockComputer);
                    LockWorkStation();
                }
            }


            public static bool IsLockedManually()
            {
                return ManualLocked;
            }
        }


        public class LogOff
        {
            [DllImport("user32")]
            public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

            public static void Windows()
            {
                Logger.DoLog(Config.ActionTypes.LogOffWindows);
                ExitWindowsEx(0, 0);
            }
        }

        public class Sleep
        {
            [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

            public static void Computer()
            {
                Logger.DoLog(Config.ActionTypes.SleepComputer);
                SetSuspendState(false, true, true);
            }
        }


        public class TurnOff
        {
            public enum MonitorState
            {
                OFF = 2
            }

            [Flags]
            private enum SendMessageTimeoutFlags : uint
            {
                Block = 0x0001,
                AbortIfHung = 0x0002,
                NoTimeoutIfNotHung = 0x0008
            }

            private const uint WM_SYSCOMMAND = 0x0112;
            private static readonly IntPtr SC_MONITORPOWER = new IntPtr(0xF170);
            private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
            private const uint MonitorPowerCallTimeoutMs = 1500;
            private const long MonitorOffCooldownMs = 8000;
            private const long MonitorWakeGuardMs = 15000;
            private const long MonitorWakeTrackingWindowMs = 30000;
            private static long _lastMonitorOffTick = -MonitorOffCooldownMs;
            private static long _lastWakeInteractionTick = -MonitorWakeGuardMs;

            [DllImport("user32.dll", SetLastError = true)]
            private static extern IntPtr SendMessageTimeout(
                IntPtr hWnd,
                uint msg,
                IntPtr wParam,
                IntPtr lParam,
                SendMessageTimeoutFlags fuFlags,
                uint uTimeout,
                out IntPtr lpdwResult);

            private static bool TrySendMonitorPowerMessage(IntPtr windowHandle, MonitorState state)
            {
                if (windowHandle == IntPtr.Zero)
                {
                    return false;
                }

                IntPtr lResult;
                IntPtr sendResult = SendMessageTimeout(
                    windowHandle,
                    WM_SYSCOMMAND,
                    SC_MONITORPOWER,
                    new IntPtr((int)state),
                    SendMessageTimeoutFlags.Block |
                    SendMessageTimeoutFlags.AbortIfHung |
                    SendMessageTimeoutFlags.NoTimeoutIfNotHung,
                    MonitorPowerCallTimeoutMs,
                    out lResult);

                return sendResult != IntPtr.Zero;
            }

            public static bool SetMonitorState(MonitorState state)
            {
                if (TrySendMonitorPowerMessage(HWND_BROADCAST, state))
                {
                    return true;
                }

                foreach (Form form in Application.OpenForms)
                {
                    if (form == null || !form.IsHandleCreated)
                    {
                        continue;
                    }

                    if (TrySendMonitorPowerMessage(form.Handle, state))
                    {
                        return true;
                    }
                }

                return false;
            }

            public static void RecordPotentialWakeInteraction()
            {
                long nowTick = Environment.TickCount64;
                long lastMonitorOffTick = Interlocked.Read(ref _lastMonitorOffTick);

                if (nowTick - lastMonitorOffTick > MonitorWakeTrackingWindowMs)
                {
                    return;
                }

                Interlocked.Exchange(ref _lastWakeInteractionTick, nowTick);
            }

            public static void RecordSystemResume()
            {
                Interlocked.Exchange(ref _lastWakeInteractionTick, Environment.TickCount64);
            }

            private static bool IsMonitorOffSuppressed()
            {
                long nowTick = Environment.TickCount64;
                long lastTick = Interlocked.Read(ref _lastMonitorOffTick);

                if (nowTick - lastTick < MonitorOffCooldownMs)
                {
                    return true;
                }

                long lastWakeInteractionTick = Interlocked.Read(ref _lastWakeInteractionTick);
                return nowTick - lastWakeInteractionTick < MonitorWakeGuardMs;
            }

            public static void Monitor()
            {
                if (IsMonitorOffSuppressed())
                {
                    return;
                }

                if (!SetMonitorState(MonitorState.OFF))
                {
                    return;
                }

                long nowTick = Environment.TickCount64;
                Interlocked.Exchange(ref _lastWakeInteractionTick, -MonitorWakeGuardMs);
                Interlocked.Exchange(ref _lastMonitorOffTick, nowTick);
                Logger.DoLog(Config.ActionTypes.TurnOffMonitor);
            }
        }


        /////////////
    }
}
