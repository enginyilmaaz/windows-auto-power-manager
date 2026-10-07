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
            private const int DisplayStateUnknown = -1;
            private const int DisplayStateOff = 0;
            private const int DisplayStateOn = 1;
            private static long _lastMonitorOffTick = -MonitorOffCooldownMs;
            private static long _lastWakeInteractionTick = -MonitorWakeGuardMs;

            // Last state reported by GUID_CONSOLE_DISPLAY_STATE, or DisplayStateUnknown until
            // the first notification arrives.
            private static int _displayState = DisplayStateUnknown;

            // 1 while a monitor off we issued has not been followed by the display coming back on.
            private static int _monitorOffInEffect;

            // A display-on within a second of our own off cannot be the user: reacting, reaching
            // for the mouse and moving it takes longer than that, while the driver blips in the
            // trace land at 0-0.9 s. Windows counts the blip as input, which resets the idle
            // counter and restarts the whole cycle one threshold later.
            //
            // A wider window (5 s) was tried and misfired: a person who woke the screen 1.5-5 s
            // after it went dark was taken for the driver and had it switched off again in their
            // face. Likewise the quiet period must be long: a person nudges the mouse once and
            // then waits for the picture, so a few seconds of silence proves nothing, whereas
            // nobody being there shows as no input at all for half a minute.
            private const long SpuriousWakeWindowMs = 1000;
            private const long SpuriousRetryDelayMs = 30000;
            private const int MaxSpuriousRetries = 2;
            private const uint SpuriousRetryQuietIdleSeconds = 25;
            private static long _spuriousWakeTick;
            private static int _spuriousRetryPending;
            private static int _spuriousRetryCount;

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

            // Called from the display power notification (GUID_CONSOLE_DISPLAY_STATE). The input
            // that wakes a display turned off through SC_MONITORPOWER is frequently consumed as
            // the wake event and never reaches GetLastInputInfo, so this is the only dependable
            // signal that the user is back in front of the screen.
            public static void NotifyDisplayStateChanged(bool isDisplayOn)
            {
                Interlocked.Exchange(ref _displayState, isDisplayOn ? DisplayStateOn : DisplayStateOff);

                if (!isDisplayOn)
                {
                    return;
                }

                long nowTick = Environment.TickCount64;
                long sinceOffMs = nowTick - Interlocked.Read(ref _lastMonitorOffTick);
                if (Interlocked.CompareExchange(ref _monitorOffInEffect, 1, 1) == 1 &&
                    sinceOffMs < SpuriousWakeWindowMs)
                {
                    // Not treated as a wake: the guard stays down so the retry is allowed, and the
                    // off stays in effect so the idle drop that follows is not read as the user.
                    Interlocked.Exchange(ref _spuriousWakeTick, nowTick);
                    Interlocked.Exchange(ref _spuriousRetryPending, 1);
                    DebugLog.Write("monitor", "display back on " + sinceOffMs + "ms after off, suspect spurious wake");
                    return;
                }

                Interlocked.Exchange(ref _spuriousRetryPending, 0);
                Interlocked.Exchange(ref _spuriousRetryCount, 0);
                Interlocked.Exchange(ref _monitorOffInEffect, 0);
                Interlocked.Exchange(ref _lastWakeInteractionTick, nowTick);
            }

            public static void RecordPotentialWakeInteraction()
            {
                // Observed input means the screen is visible. This also self-heals the tracked
                // state if a display on notification was missed while the recipient window handle
                // was being recreated, which would otherwise suppress monitor off permanently.
                Interlocked.CompareExchange(ref _displayState, DisplayStateOn, DisplayStateOff);

                // Input inside the spurious window is the same driver blip the display notification
                // reports. Treating it as the user would arm the guard and block the retry.
                if (Environment.TickCount64 - Interlocked.Read(ref _lastMonitorOffTick) < SpuriousWakeWindowMs)
                {
                    return;
                }

                // The wake guard is only meaningful while a monitor off we issued is still in
                // effect. Arming it on ordinary activity would delay every later monitor off by
                // the guard duration, so the flag is consumed on the first input that follows.
                if (Interlocked.CompareExchange(ref _monitorOffInEffect, 0, 1) != 1)
                {
                    return;
                }

                Interlocked.Exchange(ref _lastWakeInteractionTick, Environment.TickCount64);
            }

            public static void RecordSystemResume()
            {
                Interlocked.Exchange(ref _monitorOffInEffect, 0);
                Interlocked.Exchange(ref _displayState, DisplayStateUnknown);
                Interlocked.Exchange(ref _lastWakeInteractionTick, Environment.TickCount64);
            }

            private static bool IsMonitorOffSuppressed()
            {
                if (Interlocked.CompareExchange(ref _displayState, 0, 0) == DisplayStateOff)
                {
                    return true;
                }

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
                    // The reason matters when the display cycles off and on: it separates a guard
                    // doing its job from an attempt that reached the driver and failed.
                    DebugLog.Write("monitor", string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "off suppressed (displayState={0} sinceOff={1}ms sinceWake={2}ms)",
                        Interlocked.CompareExchange(ref _displayState, 0, 0),
                        Environment.TickCount64 - Interlocked.Read(ref _lastMonitorOffTick),
                        Environment.TickCount64 - Interlocked.Read(ref _lastWakeInteractionTick)));
                    return;
                }

                // A fresh idle cycle starts with a clean retry budget.
                Interlocked.Exchange(ref _spuriousRetryCount, 0);
                Interlocked.Exchange(ref _spuriousRetryPending, 0);
                SendMonitorOff("off sent", recordInActionLog: true);
            }

            private static void SendMonitorOff(string traceMessage, bool recordInActionLog)
            {
                if (!SetMonitorState(MonitorState.OFF))
                {
                    DebugLog.Write("monitor", "off FAILED (driver rejected the request)");
                    if (recordInActionLog)
                    {
                        // Without this the action leaves no trace at all when the display refuses
                        // to turn off, which makes the difference between "suppressed" and
                        // "attempted and failed" invisible in the log.
                        Logger.DoLog(Config.ActionTypes.TurnOffMonitorFailed);
                    }
                    return;
                }

                long nowTick = Environment.TickCount64;
                Interlocked.Exchange(ref _lastWakeInteractionTick, -MonitorWakeGuardMs);
                Interlocked.Exchange(ref _lastMonitorOffTick, nowTick);
                Interlocked.Exchange(ref _monitorOffInEffect, 1);
                DebugLog.Write("monitor", traceMessage);

                // Retries are the same action still being carried out, not a new one; the action
                // log records the attempt once, the trace records every send.
                if (recordInActionLog)
                {
                    Logger.DoLog(Config.ActionTypes.TurnOffMonitor);
                }
            }

            /// <summary>
            ///     Driven by the one-second tick. Re-sends the off after a suspected spurious wake,
            ///     but only after half a minute with no input at all: a person who woke the screen
            ///     touches something again well within that, a driver blip is followed by nothing.
            /// </summary>
            public static void TryRetryAfterSpuriousWake(uint idleSeconds)
            {
                if (Interlocked.CompareExchange(ref _spuriousRetryPending, 1, 1) != 1)
                {
                    return;
                }

                long nowTick = Environment.TickCount64;
                if (nowTick - Interlocked.Read(ref _spuriousWakeTick) < SpuriousRetryDelayMs)
                {
                    return;
                }

                if (idleSeconds < SpuriousRetryQuietIdleSeconds)
                {
                    Interlocked.Exchange(ref _spuriousRetryPending, 0);
                    Interlocked.Exchange(ref _spuriousRetryCount, 0);
                    Interlocked.Exchange(ref _monitorOffInEffect, 0);
                    Interlocked.Exchange(ref _lastWakeInteractionTick, nowTick);
                    DebugLog.Write("monitor", "spurious wake dismissed: input continued (idle=" + idleSeconds + "s)");
                    return;
                }

                Interlocked.Exchange(ref _spuriousRetryPending, 0);
                int attempt = Interlocked.Increment(ref _spuriousRetryCount);
                if (attempt > MaxSpuriousRetries)
                {
                    Interlocked.Exchange(ref _monitorOffInEffect, 0);
                    DebugLog.Write("monitor", "spurious wake: retry limit reached, leaving the display on");
                    return;
                }

                SendMonitorOff(
                    "off re-sent after spurious wake (retry " + attempt + "/" + MaxSpuriousRetries + ")",
                    recordInActionLog: false);
            }
        }


        /////////////
    }
}
