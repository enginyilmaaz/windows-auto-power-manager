namespace WindowsAutoPowerManager
{
    public class LanguageNames

    {
        public string LangCode { get; set; }
        public string LangName { get; set; }
    }

    public class Language
    {
        public string MainFormName { get; set; }
        public string LangNativeName { get; set; }
        public string MainGroupboxNewAction { get; set; }
        public string MainLabelActionType { get; set; }
        public string MainLabelTrigger { get; set; }
        public string MainLabelValue { get; set; }
        public string MainLabelValueDuration { get; set; }
        public string MainLabelValueTime { get; set; }
        public string MainButtonAddAction { get; set; }
        public string MainGroupBoxActionList { get; set; }
        public string MainStatusBarCurrentTime { get; set; }

        public string MainCboxActionTypeItemChooseAction { get; set; }
        public string MainCboxActionTypeItemLockComputer { get; set; }
        public string MainCboxActionTypeItemSleepComputer { get; set; }
        public string MainCboxActionTypeItemTurnOffMonitor { get; set; }
        public string MainCboxActionTypeItemShutdownComputer { get; set; }
        public string MainCboxActionTypeItemRestartComputer { get; set; }
        public string MainCboxActionTypeItemLogOffWindows { get; set; }

        public string MainCboxTriggerTypeItemChooseTrigger { get; set; }
        public string MainCboxTriggerTypeItemSystemIdle { get; set; }
        public string MainCboxTriggerTypeItemCertainTime { get; set; }
        public string MainCboxTriggerTypeItemFromNow { get; set; }

        public string MainDatagridMainTriggerType { get; set; }
        public string MainDatagridMainActionType { get; set; }
        public string MainDatagridMainValue { get; set; }
        public string MainDatagridMainValueUnit { get; set; }
        public string MainDatagridMainCreatedDate { get; set; }


        public string NotifyIconMain { get; set; }
        public string MessageTitleSuccess { get; set; }
        public string MessageTitleInfo { get; set; }
        public string MessageTitleWarn { get; set; }
        public string MessageTitleError { get; set; }
        public string MessageContentActionDeleted { get; set; }
        public string MessageContentActionUpdated { get; set; }
        public string MessageContentConfirmDeleteAction { get; set; }
        public string MessageContentDatagridMainActionChoose { get; set; }
        public string MessageContentActionChoose { get; set; }
        public string MessageContentActionCreated { get; set; }
        public string MessageContentMaxActionWarn { get; set; }
        public string MessageContentIdleActionConflict { get; set; }
        public string MessageContentAnother { get; set; }
        public string MessageContentWindowAlredyOpen { get; set; }
        public string SettingsFormName { get; set; }
        public string LogViewerFormName { get; set; }
        public string MessageContentActionAllDeleted { get; set; }
        public string MessageContentNoLog { get; set; }

        public string ContextMenuStripMainGridDeleteSelectedAction { get; set; }
        public string ContextMenuStripMainGridEditSelectedAction { get; set; }
        public string ContextMenuStripMainGridDeleteAllAction { get; set; }
        public string ContextMenuStripMainGridOpenHelp { get; set; }
        public string ContextMenuStripNotifyIconAddNewAction { get; set; }
        public string ContextMenuStripNotifyIconShowSettings { get; set; }
        public string ContextMenuStripNotifyIconShowLogs { get; set; }
        public string ContextMenuStripNotifyIconShowHelp { get; set; }
        public string ContextMenuStripNotifyIconExitProgram { get; set; }
        public string LabelFirstlyChooseATrigger { get; set; }

        public string ToolTipShowLogs { get; set; }
        public string ToolTipSettings { get; set; }
        public string LogViewerFormActionType { get; set; }
        public string LogViewerFormActionExecutionTime { get; set; }
        public string LogViewerFormLockComputer { get; set; }
        public string LogViewerFormLockComputerManually { get; set; }
        public string LogViewerFormUnlockComputer { get; set; }
        public string LogViewerFormSleepComputer { get; set; }
        public string LogViewerFormTurnOffMonitor { get; set; }
        public string LogViewerFormShutdownComputer { get; set; }
        public string LogViewerFormRestartComputer { get; set; }
        public string LogViewerFormLogOffWindows { get; set; }
        public string LogViewerFormAppStarted { get; set; }
        public string LogViewerFormAppTerminated { get; set; }
        public string LogViewerFormButtonClearLogs { get; set; }
        public string LogViewerFormButtonCancel { get; set; }
        public string MessageContentClearedLogs { get; set; }
        public string MessageContentThisWillAutoClose { get; set; }
        public string LogViewerFormSortingChoose { get; set; }
        public string LogViewerFormSortingNewestToOld { get; set; }
        public string LogViewerFormSortingOldestToNewest { get; set; }
        public string LogViewerFormLabelFiltering { get; set; }
        public string LogViewerFormLabelSorting { get; set; }
        public string LogViewerFormFilterChoose { get; set; }
        public string LogViewerFormFilterLocks { get; set; }
        public string LogViewerFormFilterUnlocks { get; set; }
        public string LogViewerFormFilterTurnOffsMonitor { get; set; }
        public string LogViewerFormFilterRestarts { get; set; }
        public string LogViewerFormFilterShutdowns { get; set; }
        public string LogViewerFormFilterLogOffs { get; set; }
        public string LogViewerFormFilterSleeps { get; set; }
        public string LogViewerFormFilterAppStarts { get; set; }
        public string LogViewerFormFilterAppTerminates { get; set; }
        public string LogViewerFormDateFrom { get; set; }
        public string LogViewerFormDateTo { get; set; }
        public string LogViewerFormButtonResetFilters { get; set; }

        public string MessageContentCountdownNotify { get; set; }
        public string MessageContentCountdownNotify2 { get; set; }
        public string MessageContentYouCanThat { get; set; }
        public string MessageContentCancelForSystemIdle { get; set; }
        public string ActionCountdownNotifierButtonSkip { get; set; }
        public string ActionCountdownNotifierButtonDelete { get; set; }
        public string ActionCountdownNotifierButtonIgnore { get; set; }
        public string PopupViewerButtonOk { get; set; }
        public string SettingsFormLabelLanguage { get; set; }
        public string SettingsFormComboboxAutoLang { get; set; }
        public string SettingsFormLabelLogs { get; set; }
        public string SettingsFormLabelStartWithWindows { get; set; }
        public string SettingsFormLabelRunInTaskbarWhenClosed { get; set; }
        public string SettingsFormLabelCountdownNotifierSeconds { get; set; }
        public string SettingsFormLabelIsCountdownNotifierEnabled { get; set; }
        public string SettingsFormCheckboxEnabled { get; set; }
        public string SettingsFormButtonSave { get; set; }
        public string SettingsFormButtonCancel { get; set; }
        public string SettingsFormButtonImportConfig { get; set; }
        public string SettingsFormButtonExportConfig { get; set; }
        public string SettingsFormDialogTitleImportConfig { get; set; }
        public string SettingsFormDialogTitleExportConfig { get; set; }
        public string SettingsFormDialogFilterConfigFiles { get; set; }
        public string SettingsFormDialogFilterAllFiles { get; set; }
        public string SettingsFormConfigImportSuccess { get; set; }
        public string SettingsFormConfigExportSuccess { get; set; }
        public string SettingsFormConfigImportFailedPrefix { get; set; }
        public string SettingsFormConfigExportFailedPrefix { get; set; }
        public string SettingsFormAddStartupAppName { get; set; }
        public string MessageContentMainWindowUnavailable { get; set; }
        public string MessageContentSettingsSaved { get; set; }
        public string MessageContentSettingSavedWithLangChanged { get; set; }

        public string MainTimeUnitSeconds { get; set; }
        public string MainTimeUnitMinutes { get; set; }
        public string MainTimeUnitHours { get; set; }

        public string SettingsFormLabelTheme { get; set; }
        public string SettingsFormThemeDark { get; set; }
        public string SettingsFormThemeLight { get; set; }
        public string SettingsFormThemeSystem { get; set; }
        public string CommonLoading { get; set; }

        public string AboutMenuItem { get; set; }
        public string AboutLabelVersion { get; set; }
        public string AboutLabelBuildId { get; set; }
        public string AboutLabelAuthor { get; set; }
        public string HelpMenuItem { get; set; }

        // Toolbar
        public string ToolbarAddAction { get; set; }
        public string ToolbarPause { get; set; }
        public string ToolbarResume { get; set; }
        public string ToolbarSearch { get; set; }

        // Tabs
        public string TabAll { get; set; }
        public string TabShutdown { get; set; }
        public string TabRestart { get; set; }
        public string TabSleep { get; set; }
        public string TabLock { get; set; }
        public string TabMonitor { get; set; }
        public string TabLogoff { get; set; }

        // Pause
        public string Pause30min { get; set; }
        public string Pause1hour { get; set; }
        public string Pause2hours { get; set; }
        public string Pause4hours { get; set; }
        public string PauseUntilEndOfDay { get; set; }
        public string PauseCustom { get; set; }
        public string PauseBanner { get; set; }
        public string PauseRemaining { get; set; }
        public string PauseCustomTitle { get; set; }
        public string PauseCustomPlaceholder { get; set; }
        public string PausePaused { get; set; }
        public string PauseResumed { get; set; }

        // Modal
        public string ModalTitleNewAction { get; set; }
        public string ModalTitleEditAction { get; set; }
        public string MainButtonEditAction { get; set; }

        // Settings tooltips
        public string TooltipCountdownSeconds { get; set; }

        // Action toggle
        public string ActionStart { get; set; }
        public string ActionStop { get; set; }

        // Countdown
        public string CountdownPreparingText { get; set; }
        public string CountdownInfoText { get; set; }

        // Help page UI
        public string HelpSearchPlaceholder { get; set; }
        public string HelpTocTitle { get; set; }
        public string HelpNoResult { get; set; }
        public string HelpPrevPage { get; set; }
        public string HelpNextPage { get; set; }
        public string HelpSearchResultsTitle { get; set; }
        public string HelpSearchResultCount { get; set; }

        // Help Ch1 - Quick Start
        public string HelpCh1Title { get; set; }
        public string HelpCh1Intro { get; set; }
        public string HelpCh1Step1 { get; set; }
        public string HelpCh1Step2 { get; set; }
        public string HelpCh1Step3 { get; set; }
        public string HelpCh1Step4 { get; set; }
        public string HelpCh1Step5 { get; set; }
        public string HelpCh1Tip { get; set; }
        public string HelpCh1Tip2 { get; set; }

        // Help Ch2 - Action Types
        public string HelpCh2Title { get; set; }
        public string HelpCh2Intro { get; set; }
        public string HelpCh2ShutdownTitle { get; set; }
        public string HelpCh2Shutdown { get; set; }
        public string HelpCh2RestartTitle { get; set; }
        public string HelpCh2Restart { get; set; }
        public string HelpCh2SleepTitle { get; set; }
        public string HelpCh2Sleep { get; set; }
        public string HelpCh2LockTitle { get; set; }
        public string HelpCh2Lock { get; set; }
        public string HelpCh2MonitorTitle { get; set; }
        public string HelpCh2Monitor { get; set; }
        public string HelpCh2LogoffTitle { get; set; }
        public string HelpCh2Logoff { get; set; }

        // Help Ch3 - Triggers
        public string HelpCh3Title { get; set; }
        public string HelpCh3Intro { get; set; }
        public string HelpCh3Sub1 { get; set; }
        public string HelpCh3Sub1Desc { get; set; }
        public string HelpCh3Sub1Detail { get; set; }
        public string HelpCh3Sub1Example { get; set; }
        public string HelpCh3Sub2 { get; set; }
        public string HelpCh3Sub2Desc { get; set; }
        public string HelpCh3Sub2Detail { get; set; }
        public string HelpCh3Sub2Example { get; set; }
        public string HelpCh3Sub2Warn { get; set; }
        public string HelpCh3Sub3 { get; set; }
        public string HelpCh3Sub3Desc { get; set; }
        public string HelpCh3Sub3Detail { get; set; }
        public string HelpCh3Sub3Example { get; set; }

        // Help Ch5 - Settings
        public string HelpCh5Title { get; set; }
        public string HelpCh5Intro { get; set; }
        public string HelpCh5ThemeTitle { get; set; }
        public string HelpCh5Theme { get; set; }
        public string HelpCh5LangTitle { get; set; }
        public string HelpCh5Lang { get; set; }
        public string HelpCh5LogsTitle { get; set; }
        public string HelpCh5Logs { get; set; }
        public string HelpCh5StartupTitle { get; set; }
        public string HelpCh5Startup { get; set; }
        public string HelpCh5TaskbarTitle { get; set; }
        public string HelpCh5Taskbar { get; set; }
        public string HelpCh5CountdownTitle { get; set; }
        public string HelpCh5Countdown { get; set; }
        public string HelpCh5CountdownSecTitle { get; set; }
        public string HelpCh5CountdownSec { get; set; }
        public string HelpCh5ImportTitle { get; set; }
        public string HelpCh5Import { get; set; }

        // Help Ch6 - Menus and Toolbar
        public string HelpCh6Title { get; set; }
        public string HelpCh6Intro { get; set; }
        public string HelpCh6Sub1 { get; set; }
        public string HelpCh6Toolbar1 { get; set; }
        public string HelpCh6Toolbar2 { get; set; }
        public string HelpCh6Toolbar3 { get; set; }
        public string HelpCh6Toolbar4 { get; set; }
        public string HelpCh6Sub2 { get; set; }
        public string HelpCh6Menu1 { get; set; }
        public string HelpCh6Sub3 { get; set; }
        public string HelpCh6Menu2 { get; set; }
        public string HelpCh6Sub4 { get; set; }
        public string HelpCh6Menu3 { get; set; }
        public string HelpCh6Sub5 { get; set; }
        public string HelpCh6Menu4 { get; set; }

        // Help Ch7 - Logs
        public string HelpCh7Title { get; set; }
        public string HelpCh7Intro { get; set; }
        public string HelpCh7Desc1 { get; set; }
        public string HelpCh7FilterTitle { get; set; }
        public string HelpCh7Desc2 { get; set; }
        public string HelpCh7Desc3 { get; set; }

        // Help Ch8 - Tips & FAQ
        public string HelpCh8Title { get; set; }
        public string HelpCh8Tip1 { get; set; }
        public string HelpCh8Tip2 { get; set; }
        public string HelpCh8Tip3 { get; set; }
        public string HelpCh8Tip4 { get; set; }
        public string HelpCh8Tip6 { get; set; }
        public string HelpCh8Tip7 { get; set; }
        public string HelpCh8Faq1Q { get; set; }
        public string HelpCh8Faq1A { get; set; }
        public string HelpCh8Faq2Q { get; set; }
        public string HelpCh8Faq2A { get; set; }
        public string HelpCh8Faq3Q { get; set; }
        public string HelpCh8Faq3A { get; set; }

    }
}
