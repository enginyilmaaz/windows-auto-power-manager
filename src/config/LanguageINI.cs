namespace WindowsShutdownHelper
{
    public class languageNames

    {
        public string langCode { get; set; }
        public string LangName { get; set; }
    }

    public class Language
    {
        public string main_FormName { get; set; }
        public string langNativeName { get; set; }
        public string main_groupbox_newAction { get; set; }
        public string main_label_actionType { get; set; }
        public string main_label_trigger { get; set; }
        public string main_label_value { get; set; }
        public string main_label_value_duration { get; set; }
        public string main_label_value_time { get; set; }
        public string main_button_addAction { get; set; }
        public string main_groupBox_actionList { get; set; }
        public string main_statusBar_currentTime { get; set; }

        public string main_cbox_ActionType_Item_chooseAction { get; set; }
        public string main_cbox_ActionType_Item_lockComputer { get; set; }
        public string main_cbox_ActionType_Item_sleepComputer { get; set; }
        public string main_cbox_ActionType_Item_turnOffMonitor { get; set; }
        public string main_cbox_ActionType_Item_shutdownComputer { get; set; }
        public string main_cbox_ActionType_Item_restartComputer { get; set; }
        public string main_cbox_ActionType_Item_logOffWindows { get; set; }

        public string main_cbox_TriggerType_Item_chooseTrigger { get; set; }
        public string main_cbox_TriggerType_Item_systemIdle { get; set; }
        public string main_cbox_TriggerType_Item_certainTime { get; set; }
        public string main_cbox_TriggerType_Item_fromNow { get; set; }

        public string main_datagrid_main_triggerType { get; set; }
        public string main_datagrid_main_actionType { get; set; }
        public string main_datagrid_main_value { get; set; }
        public string main_datagrid_main_valueUnit { get; set; }
        public string main_datagrid_main_createdDate { get; set; }


        public string notifyIcon_main { get; set; }
        public string messageTitle_success { get; set; }
        public string messageTitle_info { get; set; }
        public string messageTitle_warn { get; set; }
        public string messageTitle_error { get; set; }
        public string messageContent_actionDeleted { get; set; }
        public string messageContent_datagridMain_actionChoose { get; set; }
        public string messageContent_actionChoose { get; set; }
        public string messageContent_actionCreated { get; set; }
        public string messageContent_maxActionWarn { get; set; }
        public string messageContent_idleActionConflict { get; set; }
        public string messageContent_another { get; set; }
        public string messageContent_windowAlredyOpen { get; set; }
        public string settingsForm_Name { get; set; }
        public string logViewerForm_Name { get; set; }
        public string messageContent_actionAllDeleted { get; set; }
        public string messageContent_noLog { get; set; }

        public string contextMenuStrip_mainGrid_deleteSelectedAction { get; set; }
        public string contextMenuStrip_mainGrid_deleteAllAction { get; set; }
        public string contextMenuStrip_notifyIcon_addNewAction { get; set; }
        public string contextMenuStrip_notifyIcon_showSettings { get; set; }
        public string contextMenuStrip_notifyIcon_showLogs { get; set; }
        public string contextMenuStrip_notifyIcon_exitProgram { get; set; }
        public string label_firstly_choose_a_trigger { get; set; }

        public string toolTip_showLogs { get; set; }
        public string toolTip_settings { get; set; }
        public string logViewerForm_actionType { get; set; }
        public string logViewerForm_actionExecutionTime { get; set; }
        public string logViewerForm_lockComputer { get; set; }
        public string logViewerForm_lockComputerManually { get; set; }
        public string logViewerForm_unlockComputer { get; set; }
        public string logViewerForm_sleepComputer { get; set; }
        public string logViewerForm_turnOffMonitor { get; set; }
        public string logViewerForm_shutdownComputer { get; set; }
        public string logViewerForm_restartComputer { get; set; }
        public string logViewerForm_logOffWindows { get; set; }
        public string logViewerForm_appStarted { get; set; }
        public string logViewerForm_appTerminated { get; set; }
        public string logViewerForm_button_clearLogs { get; set; }
        public string logViewerForm_button_cancel { get; set; }
        public string messageContent_clearedLogs { get; set; }
        public string messageContent_thisWillAutoClose { get; set; }
        public string logViewerForm_sorting_choose { get; set; }
        public string logViewerForm_sorting_newestToOld { get; set; }
        public string logViewerForm_sorting_OldestToNewest { get; set; }
        public string logViewerForm_label_filtering { get; set; }
        public string logViewerForm_label_sorting { get; set; }
        public string logViewerForm_filter_choose { get; set; }
        public string logViewerForm_filter_locks { get; set; }
        public string logViewerForm_filter_unlocks { get; set; }
        public string logViewerForm_filter_turnOffsMonitor { get; set; }
        public string logViewerForm_filter_restarts { get; set; }
        public string logViewerForm_filter_shutdowns { get; set; }
        public string logViewerForm_filter_logOffs { get; set; }
        public string logViewerForm_filter_sleeps { get; set; }
        public string logViewerForm_filter_appStarts { get; set; }
        public string logViewerForm_filter_appTerminates { get; set; }

        public string messageContent_CountdownNotify { get; set; }
        public string messageContent_CountdownNotify_2 { get; set; }
        public string messageContent_youCanThat { get; set; }
        public string messageContent_cancelForSystemIdle { get; set; }
        public string actionCountdownNotifier_button_skip { get; set; }
        public string actionCountdownNotifier_button_delete { get; set; }
        public string actionCountdownNotifier_button_ignore { get; set; }
        public string popupViewer_button_ok { get; set; }
        public string settingsForm_label_language { get; set; }
        public string settingsForm_combobox_auto_lang { get; set; }
        public string settingsForm_label_logs { get; set; }
        public string settingsForm_label_startWithWindows { get; set; }
        public string settingsForm_label_runInTaskbarWhenClosed { get; set; }
        public string settingsForm_label_countdownNotifierSeconds { get; set; }
        public string settingsForm_label_isCountdownNotifierEnabled { get; set; }
        public string settingsForm_checkbox_enabled { get; set; }
        public string settingsForm_button_save { get; set; }
        public string settingsForm_button_cancel { get; set; }
        public string settingsForm_addStartupAppName { get; set; }
        public string messageContent_settingsSaved { get; set; }
        public string messageContent_settingSavedWithLangChanged { get; set; }

        public string main_timeUnit_seconds { get; set; }
        public string main_timeUnit_minutes { get; set; }
        public string main_timeUnit_hours { get; set; }

        public string settingsForm_label_theme { get; set; }
        public string settingsForm_theme_dark { get; set; }
        public string settingsForm_theme_light { get; set; }
        public string settingsForm_theme_system { get; set; }
        public string common_loading { get; set; }

        public string about_menuItem { get; set; }
        public string about_label_version { get; set; }
        public string about_label_buildId { get; set; }
        public string about_label_author { get; set; }

        // Toolbar
        public string toolbar_addAction { get; set; }
        public string toolbar_pause { get; set; }
        public string toolbar_resume { get; set; }
        public string toolbar_search { get; set; }

        // Tabs
        public string tab_all { get; set; }
        public string tab_shutdown { get; set; }
        public string tab_restart { get; set; }
        public string tab_sleep { get; set; }
        public string tab_lock { get; set; }
        public string tab_monitor { get; set; }
        public string tab_logoff { get; set; }

        // Pause
        public string pause_30min { get; set; }
        public string pause_1hour { get; set; }
        public string pause_2hours { get; set; }
        public string pause_4hours { get; set; }
        public string pause_untilEndOfDay { get; set; }
        public string pause_custom { get; set; }
        public string pause_banner { get; set; }
        public string pause_remaining { get; set; }
        public string pause_customTitle { get; set; }
        public string pause_customPlaceholder { get; set; }
        public string pause_paused { get; set; }
        public string pause_resumed { get; set; }

        // Modal
        public string modal_title_newAction { get; set; }

    }
}
