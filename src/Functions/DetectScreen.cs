using Microsoft.Win32;

namespace WindowsShutdownHelper.Functions
{
    public static class DetectScreen
    {
        public static bool IsLocked;

        public static void Main()
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                if (Actions.Lock.IsLockedManually())
                {
                    Logger.DoLog(Config.ActionTypes.LockComputerManually); 
                    IsLocked = true;
                }

             
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Logger.DoLog(Config.ActionTypes.UnlockComputer);
                IsLocked = false;
            }
        }


        public static bool IsLockedWorkstation()
        {
            return IsLocked;
        }


        public static void ManuelLockingActionLogger()
        {
            Main();
        }
    }
}