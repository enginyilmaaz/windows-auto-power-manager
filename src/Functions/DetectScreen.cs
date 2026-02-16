using Microsoft.Win32;

namespace WindowsShutdownHelper.functions
{
    public static class detectScreen
    {
        public static bool isLocked;

        public static void main()
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                if (Actions.Lock.isLockedManually())
                {
                    Logger.doLog(config.actionTypes.lockComputerManually); 
                    isLocked = true;
                }

             
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Logger.doLog(config.actionTypes.unlockComputer);
                isLocked = false;
            }
        }


        public static bool isLockedWorkstation()
        {
            return isLocked;
        }


        public static void manuelLockingActionLogger()
        {
            main();
        }
    }
}