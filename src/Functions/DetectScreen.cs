using Microsoft.Win32;

namespace WindowsShutdownHelper.functions
{
    public static class DetectScreen
    {
        public static bool IsLocked;

        public static void main()
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                if (Actions.Lock.IsLockedManually())
                {
                    Logger.DoLog(config.ActionTypes.lockComputerManually); 
                    IsLocked = true;
                }

             
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Logger.DoLog(config.ActionTypes.unlockComputer);
                IsLocked = false;
            }
        }


        public static bool IsLockedWorkstation()
        {
            return IsLocked;
        }


        public static void ManuelLockingActionLogger()
        {
            main();
        }
    }
}