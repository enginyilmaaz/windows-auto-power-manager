using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace WindowsShutdownHelper.functions
{
    internal class notifySystem
    {
        public static language language = languageSelector.languageFile();
        public static string actionTypeName;
        private static HashSet<string> _notifiedIdleActions = new HashSet<string>();

        public static void ResetIdleNotifications()
        {
            _notifiedIdleActions.Clear();
        }

        public static void showNotification(ActionModel action, uint idleTimeMin)
        {
            settings settings = new settings();

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"))
            {
                settings = JsonSerializer.Deserialize<settings>(
                    File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\settings.json"));
            }
            else
            {
                settings.isCountdownNotifierEnabled = false;
            }

            if (settings.isCountdownNotifierEnabled)
            {
                actionTypeLocalization(action);

                if (action.triggerType == config.triggerTypes.systemIdle)
                {
                    int actionValue = string.IsNullOrEmpty(action.valueUnit)
                        ? Convert.ToInt32(action.value) * 60
                        : Convert.ToInt32(action.value);
                    string actionKey = action.createdDate + "_" + action.actionType;

                    if (idleTimeMin >= actionValue - settings.countdownNotifierSeconds
                        && !_notifiedIdleActions.Contains(actionKey))
                    {
                        if (Application.OpenForms.OfType<actionCountdownNotifier>().Any() == false)
                        {
                            _notifiedIdleActions.Add(actionKey);
                            actionCountdownNotifier actionCountdownNotifier = new actionCountdownNotifier(language.messageTitle_info,
                                language.messageContent_CountdownNotify, language.messageContent_CountdownNotify_2,
                                actionTypeName, language.messageContent_cancelForSystemIdle,
                                settings.countdownNotifierSeconds,
                                action);

                            actionCountdownNotifier.Show();
                            actionCountdownNotifier.Focus();
                        }
                    }
                }

                else if (action.triggerType == config.triggerTypes.fromNow)
                {
                    DateTime actionExecuteDate = DateTime.Parse(action.value)
                        .AddSeconds(Convert.ToDouble(-settings.countdownNotifierSeconds));
                    string nowDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    string executionDate = actionExecuteDate.ToString("dd.MM.yyyy HH:mm:ss");

                    if (executionDate == nowDate)
                    {
                        if (Application.OpenForms.OfType<actionCountdownNotifier>().Any() == false)
                        {
                            actionCountdownNotifier actionCountdownNotifier = new actionCountdownNotifier(language.messageTitle_info,
                                language.messageContent_CountdownNotify, language.messageContent_CountdownNotify_2,
                                actionTypeName, language.messageContent_youCanThat,
                                settings.countdownNotifierSeconds,
                                action);

                            actionCountdownNotifier.Show();
                            actionCountdownNotifier.Focus();
                        }
                    }
                }


                else if (action.triggerType == config.triggerTypes.certainTime)
                {
                    DateTime actionExecuteDate = DateTime.Parse(action.value)
                        .AddSeconds(Convert.ToDouble(-settings.countdownNotifierSeconds));
                    string nowDate = DateTime.Now.ToString("HH:mm:ss");
                    string executionDate = actionExecuteDate.ToString("HH:mm:ss");

                    if (executionDate == nowDate)
                    {
                        if (Application.OpenForms.OfType<actionCountdownNotifier>().Any() == false)
                        {
                            actionCountdownNotifier actionCountdownNotifier = new actionCountdownNotifier(language.messageTitle_info,
                                language.messageContent_CountdownNotify, language.messageContent_CountdownNotify_2,
                                actionTypeName, language.messageContent_youCanThat,
                                settings.countdownNotifierSeconds,
                                action);

                            actionCountdownNotifier.Show();
                            actionCountdownNotifier.Focus();
                        }
                    }
                }
            }
        }


        public static void actionTypeLocalization(ActionModel action)
        {
            if (action.actionType == config.actionTypes.lockComputer)
            {
                actionTypeName = language.main_cbox_ActionType_Item_lockComputer;
            }
            else if (action.actionType == config.actionTypes.shutdownComputer)
            {
                actionTypeName = language.main_cbox_ActionType_Item_shutdownComputer;
            }
            else if (action.actionType == config.actionTypes.restartComputer)
            {
                actionTypeName = language.main_cbox_ActionType_Item_restartComputer;
            }
            else if (action.actionType == config.actionTypes.logOffWindows)
            {
                actionTypeName = language.main_cbox_ActionType_Item_logOffWindows;
            }
            else if (action.actionType == config.actionTypes.sleepComputer)
            {
                actionTypeName = language.main_cbox_ActionType_Item_sleepComputer;
            }
            else if (action.actionType == config.actionTypes.turnOffMonitor)
            {
                actionTypeName = language.main_cbox_ActionType_Item_turnOffMonitor;
            }
        }
    }
}