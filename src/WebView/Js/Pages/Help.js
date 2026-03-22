// Help Page - Detailed usage guide (book-like, GitBook style with pagination)
window.HelpPage = {
    _currentPage: 0,
    _chapters: null,

    _copy() {
        var L = Bridge.lang.bind(Bridge);
        return {
            searchPlaceholder: L('HelpSearchPlaceholder') || 'Search help topics...',
            tocTitle: L('HelpTocTitle') || 'Table of Contents',
            noResult: L('HelpNoResult') || 'No results found for your search.',
            prevPage: L('HelpPrevPage') || 'Previous',
            nextPage: L('HelpNextPage') || 'Next',
            searchResultsTitle: L('HelpSearchResultsTitle') || 'Search Results',
            searchResultCount: L('HelpSearchResultCount') || ' results found',

            // Ch1
            ch1Title: L('HelpCh1Title') || 'Quick Start',
            ch1Icon: 'rocket_launch',
            ch1Intro: L('HelpCh1Intro') || 'Windows Auto Power Manager lets you automatically shut down, lock, sleep, and perform other power actions on your computer based on specific conditions.',
            ch1Step1: L('HelpCh1Step1') || 'Click the <b>+</b> button on the toolbar or select <b>New Action</b> from the hamburger menu.',
            ch1Step2: L('HelpCh1Step2') || 'From the <b>Action Type</b> dropdown, select the action you want to perform.',
            ch1Step3: L('HelpCh1Step3') || 'From the <b>Trigger</b> dropdown, select when the action should run.',
            ch1Step4: L('HelpCh1Step4') || 'Enter the required values based on the selected trigger.',
            ch1Step5: L('HelpCh1Step5') || 'Click <b>Add to Action List</b> to save the action.',
            ch1Tip: L('HelpCh1Tip') || 'To edit an existing action, click the row and use the <b>pencil</b> icon.',
            ch1Tip2: L('HelpCh1Tip2') || 'You can create up to <b>5 actions</b> at the same time.',

            // Ch2
            ch2Title: L('HelpCh2Title') || 'Action Types',
            ch2Icon: 'category',
            ch2Intro: L('HelpCh2Intro') || 'The application supports six different action types.',
            ch2ShutdownTitle: L('HelpCh2ShutdownTitle') || 'Shutdown Computer',
            ch2Shutdown: L('HelpCh2Shutdown') || 'Completely shuts down your computer.',
            ch2RestartTitle: L('HelpCh2RestartTitle') || 'Restart Computer',
            ch2Restart: L('HelpCh2Restart') || 'Restarts your computer.',
            ch2SleepTitle: L('HelpCh2SleepTitle') || 'Sleep Computer',
            ch2Sleep: L('HelpCh2Sleep') || 'Puts your computer into Sleep mode.',
            ch2LockTitle: L('HelpCh2LockTitle') || 'Lock Computer',
            ch2Lock: L('HelpCh2Lock') || 'Locks your Windows session.',
            ch2MonitorTitle: L('HelpCh2MonitorTitle') || 'Turn Off Monitor',
            ch2Monitor: L('HelpCh2Monitor') || 'Only turns off your monitor (screen).',
            ch2LogoffTitle: L('HelpCh2LogoffTitle') || 'Log Off Windows',
            ch2Logoff: L('HelpCh2Logoff') || 'Logs off your current Windows session.',

            // Ch3
            ch3Title: L('HelpCh3Title') || 'Triggers',
            ch3Icon: 'timer',
            ch3Intro: L('HelpCh3Intro') || 'Triggers determine under what conditions your created action will run.',
            ch3Sub1: L('HelpCh3Sub1') || 'Countdown',
            ch3Sub1Desc: L('HelpCh3Sub1Desc') || 'Runs the action once when the specified duration ends.',
            ch3Sub1Detail: L('HelpCh3Sub1Detail') || '<b>How it works:</b> The countdown starts when the action is created.',
            ch3Sub1Example: L('HelpCh3Sub1Example') || '<b>Example:</b> Set Action Type, Trigger, Value and Unit.',
            ch3Sub2: L('HelpCh3Sub2') || 'System Idle',
            ch3Sub2Desc: L('HelpCh3Sub2Desc') || 'Waits for the specified duration with no keyboard or mouse input.',
            ch3Sub2Detail: L('HelpCh3Sub2Detail') || '<b>How it works:</b> The system continuously monitors keyboard and mouse activity.',
            ch3Sub2Example: L('HelpCh3Sub2Example') || '<b>Example:</b> Set Action Type, Trigger, Value and Unit.',
            ch3Sub2Warn: L('HelpCh3Sub2Warn') || 'Creating multiple idle actions with the same duration may cause conflicts.',
            ch3Sub3: L('HelpCh3Sub3') || 'Every Day by Time',
            ch3Sub3Desc: L('HelpCh3Sub3Desc') || 'Runs the action once per day at the selected time.',
            ch3Sub3Detail: L('HelpCh3Sub3Detail') || '<b>How it works:</b> The application checks for the specified time every day.',
            ch3Sub3Example: L('HelpCh3Sub3Example') || '<b>Example:</b> Set Action Type, Trigger and Time.',

            // Ch5
            ch5Title: L('HelpCh5Title') || 'Settings',
            ch5Icon: 'settings',
            ch5Intro: L('HelpCh5Intro') || 'The Settings window allows you to customize the application\'s behavior.',
            ch5ThemeTitle: L('HelpCh5ThemeTitle') || 'Theme',
            ch5Theme: L('HelpCh5Theme') || 'Changes the application\'s appearance.',
            ch5LangTitle: L('HelpCh5LangTitle') || 'Language',
            ch5Lang: L('HelpCh5Lang') || 'Changes the application\'s interface language.',
            ch5LogsTitle: L('HelpCh5LogsTitle') || 'Record Logs',
            ch5Logs: L('HelpCh5Logs') || 'When enabled, all actions performed by the application are recorded.',
            ch5StartupTitle: L('HelpCh5StartupTitle') || 'Start with Windows',
            ch5Startup: L('HelpCh5Startup') || 'When enabled, the application starts automatically when Windows boots.',
            ch5TaskbarTitle: L('HelpCh5TaskbarTitle') || 'Run in Background When Closed',
            ch5Taskbar: L('HelpCh5Taskbar') || 'When enabled, closing the application window does not close the application.',
            ch5CountdownTitle: L('HelpCh5CountdownTitle') || 'Show Alert Before Action',
            ch5Countdown: L('HelpCh5Countdown') || 'When enabled, a countdown warning window is shown before any action runs.',
            ch5CountdownSecTitle: L('HelpCh5CountdownSecTitle') || 'Countdown Seconds',
            ch5CountdownSec: L('HelpCh5CountdownSec') || 'Determines how many seconds the warning window is displayed.',
            ch5ImportTitle: L('HelpCh5ImportTitle') || 'Import / Export',
            ch5Import: L('HelpCh5Import') || 'You can export your application settings as a <b>.conf</b> file.',

            // Ch6
            ch6Title: L('HelpCh6Title') || 'Menus and Toolbar',
            ch6Icon: 'menu',
            ch6Intro: L('HelpCh6Intro') || 'The application provides various menus and toolbar buttons for quick access.',
            ch6Sub1: L('HelpCh6Sub1') || 'Toolbar',
            ch6Toolbar1: L('HelpCh6Toolbar1') || '<b>+ (Plus) Button:</b> Opens the new action creation window.',
            ch6Toolbar2: L('HelpCh6Toolbar2') || '<b>Pause Button:</b> Temporarily pauses all actions.',
            ch6Toolbar3: L('HelpCh6Toolbar3') || '<b>Resume Button:</b> Resumes paused actions.',
            ch6Toolbar4: L('HelpCh6Toolbar4') || '<b>Search Box:</b> Instantly searches the action list by keyword.',
            ch6Sub2: L('HelpCh6Sub2') || 'Hamburger Menu (Top Right)',
            ch6Menu1: L('HelpCh6Menu1') || 'Opens by clicking the three-line icon in the top-right corner.',
            ch6Sub3: L('HelpCh6Sub3') || 'Right-Click Menu (Action List)',
            ch6Menu2: L('HelpCh6Menu2') || 'Right-click on any row in the action list to open the context menu.',
            ch6Sub4: L('HelpCh6Sub4') || 'Tab Filters',
            ch6Menu3: L('HelpCh6Menu3') || 'Use the tabs above the action list to filter actions by type.',
            ch6Sub5: L('HelpCh6Sub5') || 'System Tray Icon',
            ch6Menu4: L('HelpCh6Menu4') || 'When the application runs in the background, an icon appears in the system tray.',

            // Ch7
            ch7Title: L('HelpCh7Title') || 'Logs and History',
            ch7Icon: 'description',
            ch7Intro: L('HelpCh7Intro') || 'The Logs window shows a detailed history of all actions performed by the application.',
            ch7Desc1: L('HelpCh7Desc1') || 'Each record includes the type of action performed and the exact time it occurred.',
            ch7FilterTitle: L('HelpCh7FilterTitle') || 'Filtering and Sorting',
            ch7Desc2: L('HelpCh7Desc2') || 'You can filter records by various criteria.',
            ch7Desc3: L('HelpCh7Desc3') || 'The <b>Delete All Logs</b> button clears the entire log history.',

            // Ch8
            ch8Title: L('HelpCh8Title') || 'Tips & Frequently Asked Questions',
            ch8Icon: 'lightbulb',
            ch8Tip1: L('HelpCh8Tip1') || '<b>Countdown</b> actions are automatically removed from the list after execution.',
            ch8Tip2: L('HelpCh8Tip2') || '<b>Every day by time</b> trigger runs only once per day.',
            ch8Tip3: L('HelpCh8Tip3') || 'Use the <b>Pause/Resume</b> feature to temporarily stop all actions.',
            ch8Tip4: L('HelpCh8Tip4') || 'For the <b>System Idle</b> trigger, moving the mouse during the countdown warning cancels the action.',
            ch8Tip6: L('HelpCh8Tip6') || 'Enable <b>"Run in background when closed"</b> in Settings.',
            ch8Tip7: L('HelpCh8Tip7') || 'It is recommended to regularly <b>export your settings</b> as a backup.',
            ch8Faq1Q: L('HelpCh8Faq1Q') || '<b>Question:</b> Do actions run when the application is closed?',
            ch8Faq1A: L('HelpCh8Faq1A') || '<b>Answer:</b> Yes, if "Run in background when closed" is enabled.',
            ch8Faq2Q: L('HelpCh8Faq2Q') || '<b>Question:</b> Are actions lost when the computer restarts?',
            ch8Faq2A: L('HelpCh8Faq2A') || '<b>Answer:</b> No. Actions are saved and restored when the application opens again.',
            ch8Faq3Q: L('HelpCh8Faq3Q') || '<b>Question:</b> How many actions can I create at the same time?',
            ch8Faq3A: L('HelpCh8Faq3A') || '<b>Answer:</b> Up to 5 actions can be active at the same time.'
        };
    },

    _buildChapter(num, icon, title, bodyHtml, searchableText) {
        return {
            num: num,
            icon: icon,
            title: title,
            bodyHtml: bodyHtml,
            searchableText: (title + ' ' + searchableText).toLowerCase().replace(/<[^>]*>/g, '')
        };
    },

    _step(num, text) {
        return '<div class="help-step"><span class="help-step-num">' + num + '</span><span class="help-step-text">' + text + '</span></div>';
    },
    _tip(text) { return '<div class="help-tip-box tip"><span class="mi">lightbulb</span><span>' + text + '</span></div>'; },
    _warn(text) { return '<div class="help-tip-box warn"><span class="mi">warning</span><span>' + text + '</span></div>'; },
    _success(text) { return '<div class="help-tip-box success"><span class="mi">check_circle</span><span>' + text + '</span></div>'; },
    _desc(text) { return '<div class="help-desc-item">' + text + '</div>'; },
    _sub(text) { return '<div class="help-sub-title">' + text + '</div>'; },

    _buildChapters(t) {
        var chapters = [];
        var s = this;

        // Ch1
        var ch1 = s._desc(t.ch1Intro) +
            s._step(1, t.ch1Step1) + s._step(2, t.ch1Step2) + s._step(3, t.ch1Step3) + s._step(4, t.ch1Step4) + s._step(5, t.ch1Step5) +
            s._tip(t.ch1Tip) + s._tip(t.ch1Tip2);
        chapters.push(s._buildChapter(1, t.ch1Icon, t.ch1Title, ch1, t.ch1Intro + ' ' + t.ch1Step1 + ' ' + t.ch1Step2 + ' ' + t.ch1Step3 + ' ' + t.ch1Step4 + ' ' + t.ch1Step5 + ' ' + t.ch1Tip + ' ' + t.ch1Tip2));

        // Ch2
        var ch2 = s._desc(t.ch2Intro) +
            s._sub(t.ch2ShutdownTitle) + s._desc(t.ch2Shutdown) +
            s._sub(t.ch2RestartTitle) + s._desc(t.ch2Restart) +
            s._sub(t.ch2SleepTitle) + s._desc(t.ch2Sleep) +
            s._sub(t.ch2LockTitle) + s._desc(t.ch2Lock) +
            s._sub(t.ch2MonitorTitle) + s._desc(t.ch2Monitor) +
            s._sub(t.ch2LogoffTitle) + s._desc(t.ch2Logoff);
        chapters.push(s._buildChapter(2, t.ch2Icon, t.ch2Title, ch2, t.ch2Intro + ' ' + t.ch2Shutdown + ' ' + t.ch2Restart + ' ' + t.ch2Sleep + ' ' + t.ch2Lock + ' ' + t.ch2Monitor + ' ' + t.ch2Logoff));

        // Ch3
        var ch3 = s._desc(t.ch3Intro) +
            s._sub(t.ch3Sub1) + s._desc(t.ch3Sub1Desc) + s._desc(t.ch3Sub1Detail) + s._success(t.ch3Sub1Example) +
            s._sub(t.ch3Sub2) + s._desc(t.ch3Sub2Desc) + s._desc(t.ch3Sub2Detail) + s._success(t.ch3Sub2Example) + s._warn(t.ch3Sub2Warn) +
            s._sub(t.ch3Sub3) + s._desc(t.ch3Sub3Desc) + s._desc(t.ch3Sub3Detail) + s._success(t.ch3Sub3Example);
        chapters.push(s._buildChapter(3, t.ch3Icon, t.ch3Title, ch3, t.ch3Intro + ' ' + t.ch3Sub1Desc + ' ' + t.ch3Sub1Detail + ' ' + t.ch3Sub2Desc + ' ' + t.ch3Sub2Detail + ' ' + t.ch3Sub3Desc + ' ' + t.ch3Sub3Detail));

        // Ch4
        var ch4 = s._desc(t.ch5Intro) +
            s._sub(t.ch5ThemeTitle) + s._desc(t.ch5Theme) +
            s._sub(t.ch5LangTitle) + s._desc(t.ch5Lang) +
            s._sub(t.ch5LogsTitle) + s._desc(t.ch5Logs) +
            s._sub(t.ch5StartupTitle) + s._desc(t.ch5Startup) +
            s._sub(t.ch5TaskbarTitle) + s._desc(t.ch5Taskbar) +
            s._sub(t.ch5CountdownTitle) + s._desc(t.ch5Countdown) +
            s._sub(t.ch5CountdownSecTitle) + s._desc(t.ch5CountdownSec) +
            s._sub(t.ch5ImportTitle) + s._desc(t.ch5Import);
        chapters.push(s._buildChapter(4, t.ch5Icon, t.ch5Title, ch4, t.ch5Intro + ' ' + t.ch5Theme + ' ' + t.ch5Lang + ' ' + t.ch5Logs + ' ' + t.ch5Startup + ' ' + t.ch5Taskbar + ' ' + t.ch5Countdown + ' ' + t.ch5CountdownSec + ' ' + t.ch5Import));

        // Ch5
        var ch5 = s._desc(t.ch6Intro) +
            s._sub(t.ch6Sub1) + s._desc(t.ch6Toolbar1) + s._desc(t.ch6Toolbar2) + s._desc(t.ch6Toolbar3) + s._desc(t.ch6Toolbar4) +
            s._sub(t.ch6Sub2) + s._desc(t.ch6Menu1) +
            s._sub(t.ch6Sub3) + s._desc(t.ch6Menu2) +
            s._sub(t.ch6Sub4) + s._desc(t.ch6Menu3) +
            s._sub(t.ch6Sub5) + s._desc(t.ch6Menu4);
        chapters.push(s._buildChapter(5, t.ch6Icon, t.ch6Title, ch5, t.ch6Intro + ' ' + t.ch6Toolbar1 + ' ' + t.ch6Toolbar2 + ' ' + t.ch6Menu1 + ' ' + t.ch6Menu2 + ' ' + t.ch6Menu3 + ' ' + t.ch6Menu4));

        // Ch6
        var ch6 = s._desc(t.ch7Intro) + s._desc(t.ch7Desc1) +
            s._sub(t.ch7FilterTitle) + s._desc(t.ch7Desc2) +
            s._warn(t.ch7Desc3);
        chapters.push(s._buildChapter(6, t.ch7Icon, t.ch7Title, ch6, t.ch7Intro + ' ' + t.ch7Desc1 + ' ' + t.ch7Desc2 + ' ' + t.ch7Desc3));

        // Ch7
        var ch7 = s._tip(t.ch8Tip1) + s._tip(t.ch8Tip2) + s._tip(t.ch8Tip3) + s._tip(t.ch8Tip4) + s._tip(t.ch8Tip6) + s._tip(t.ch8Tip7) +
            s._sub('FAQ') +
            s._desc(t.ch8Faq1Q) + s._desc(t.ch8Faq1A) +
            s._desc(t.ch8Faq2Q) + s._desc(t.ch8Faq2A) +
            s._desc(t.ch8Faq3Q) + s._desc(t.ch8Faq3A);
        chapters.push(s._buildChapter(7, t.ch8Icon, t.ch8Title, ch7, t.ch8Tip1 + ' ' + t.ch8Tip2 + ' ' + t.ch8Tip3 + ' ' + t.ch8Tip4 + ' ' + t.ch8Faq1Q + ' ' + t.ch8Faq1A + ' ' + t.ch8Faq2Q + ' ' + t.ch8Faq2A + ' ' + t.ch8Faq3Q + ' ' + t.ch8Faq3A));

        return chapters;
    },

    _renderChapterHtml(c) {
        return '<div class="help-chapter" id="help-ch-' + c.num + '">' +
            '<div class="help-chapter-header">' +
                '<span class="help-chapter-number">' + c.num + '</span>' +
                '<span class="mi help-chapter-icon">' + c.icon + '</span>' +
                '<span class="help-chapter-title">' + c.title + '</span>' +
            '</div>' +
            '<div class="help-chapter-body">' + c.bodyHtml + '</div>' +
        '</div>';
    },

    render() {
        var L = Bridge.lang.bind(Bridge);
        var t = this._copy();
        this._chapters = this._buildChapters(t);
        this._currentPage = 0;
        this._t = t;

        // TOC sidebar (always visible)
        var tocHtml = '<div class="help-toc-sidebar" id="help-toc-sidebar">' +
            '<div class="help-toc-header">' +
                '<span class="help-toc-title">' + t.tocTitle + '</span>' +
            '</div>' +
            '<ul class="help-toc-list">';
        for (var i = 0; i < this._chapters.length; i++) {
            var ch = this._chapters[i];
            tocHtml += '<li><a class="help-toc-link" data-page="' + i + '">' +
                '<span class="help-toc-num">' + ch.num + '</span>' +
                '<span>' + ch.title + '</span></a></li>';
        }
        tocHtml += '</ul></div>';

        return '' +
        '<div class="card help-page-card">' +
            '<div class="card-title">' +
                '<span class="mi">help</span>' +
                (L('HelpMenuItem') || 'Help') +
            '</div>' +
            '<div class="help-content">' +
                tocHtml +
                '<div class="help-main">' +
                    '<div class="help-search-box">' +
                        '<span class="mi">search</span>' +
                        '<input type="text" class="help-search-input" id="help-search" placeholder="' + t.searchPlaceholder + '">' +
                    '</div>' +
                    '<div id="help-search-results" style="display:none"></div>' +
                    '<div id="help-chapter-view"></div>' +
                    '<div class="help-no-result" id="help-no-result" style="display:none">' + t.noResult + '</div>' +
                    '<div class="help-page-nav" id="help-page-nav">' +
                        '<button class="help-page-nav-btn" id="help-prev"><span class="mi">navigate_before</span>' + t.prevPage + '</button>' +
                        '<span class="help-page-indicator" id="help-page-indicator"></span>' +
                        '<button class="help-page-nav-btn" id="help-next">' + t.nextPage + '<span class="mi">navigate_next</span></button>' +
                    '</div>' +
                '</div>' +
            '</div>' +
        '</div>';
    },

    afterRender() {
        var self = this;
        var chapters = this._chapters;
        var t = this._t;
        if (!chapters || !chapters.length) return;

        var chapterView = document.getElementById('help-chapter-view');
        var searchResults = document.getElementById('help-search-results');
        var noResult = document.getElementById('help-no-result');
        var pageNav = document.getElementById('help-page-nav');
        var prevBtn = document.getElementById('help-prev');
        var nextBtn = document.getElementById('help-next');
        var indicator = document.getElementById('help-page-indicator');
        var searchInput = document.getElementById('help-search');
        var sidebar = document.getElementById('help-toc-sidebar');
        var tocLinks = document.querySelectorAll('.help-toc-link[data-page]');

        var currentPage = 0;
        var isSearchMode = false;

        function showPage(idx) {
            if (idx < 0 || idx >= chapters.length) return;
            currentPage = idx;
            isSearchMode = false;

            // Render chapter
            chapterView.innerHTML = self._renderChapterHtml(chapters[idx]);
            chapterView.style.display = '';
            searchResults.style.display = 'none';
            noResult.style.display = 'none';
            pageNav.style.display = '';

            // Update nav buttons
            prevBtn.disabled = (idx === 0);
            nextBtn.disabled = (idx === chapters.length - 1);
            indicator.textContent = (idx + 1) + ' / ' + chapters.length;

            // Update TOC active state
            for (var i = 0; i < tocLinks.length; i++) {
                tocLinks[i].classList.toggle('active', parseInt(tocLinks[i].getAttribute('data-page')) === idx);
            }

            // Scroll to top
            var pageContainer = document.getElementById('page-container');
            if (pageContainer) pageContainer.scrollTop = 0;
        }

        function showSearchResults(query) {
            var q = query.toLowerCase().trim();
            if (!q) {
                isSearchMode = false;
                chapterView.style.display = '';
                searchResults.style.display = 'none';
                noResult.style.display = 'none';
                pageNav.style.display = '';
                showPage(currentPage);
                return;
            }

            isSearchMode = true;
            var matches = [];
            for (var i = 0; i < chapters.length; i++) {
                if (chapters[i].searchableText.indexOf(q) >= 0) {
                    matches.push(i);
                }
            }

            chapterView.style.display = 'none';
            pageNav.style.display = 'none';

            if (matches.length === 0) {
                searchResults.style.display = 'none';
                noResult.style.display = '';
                return;
            }

            noResult.style.display = 'none';

            var html = '<div class="help-search-results-header">' +
                '<span class="mi">search</span>' +
                '<span>' + matches.length + t.searchResultCount + '</span>' +
            '</div>' +
            '<div class="help-search-results-list">';

            for (var j = 0; j < matches.length; j++) {
                var ch = chapters[matches[j]];
                // Find a text snippet around the match
                var plainText = ch.searchableText;
                var matchIdx = plainText.indexOf(q);
                var snippetStart = Math.max(0, matchIdx - 40);
                var snippetEnd = Math.min(plainText.length, matchIdx + q.length + 60);
                var snippet = (snippetStart > 0 ? '...' : '') +
                    plainText.substring(snippetStart, matchIdx) +
                    '<mark>' + plainText.substring(matchIdx, matchIdx + q.length) + '</mark>' +
                    plainText.substring(matchIdx + q.length, snippetEnd) +
                    (snippetEnd < plainText.length ? '...' : '');

                html += '<div class="help-search-result-item" data-page="' + matches[j] + '">' +
                    '<div class="help-search-result-item-header">' +
                        '<span class="help-chapter-number">' + ch.num + '</span>' +
                        '<span class="mi help-chapter-icon">' + ch.icon + '</span>' +
                        '<span class="help-search-result-title">' + ch.title + '</span>' +
                    '</div>' +
                    '<div class="help-search-result-snippet">' + snippet + '</div>' +
                '</div>';
            }

            html += '</div>';
            searchResults.innerHTML = html;
            searchResults.style.display = '';

            // Attach click handlers to search result items
            var items = searchResults.querySelectorAll('.help-search-result-item[data-page]');
            for (var k = 0; k < items.length; k++) {
                items[k].addEventListener('click', function () {
                    var pageIdx = parseInt(this.getAttribute('data-page'));
                    searchInput.value = '';
                    showPage(pageIdx);
                });
            }
        }

        // Initialize first page
        showPage(0);

        // Prev/Next
        if (prevBtn) {
            prevBtn.addEventListener('click', function () {
                if (currentPage > 0) showPage(currentPage - 1);
            });
        }
        if (nextBtn) {
            nextBtn.addEventListener('click', function () {
                if (currentPage < chapters.length - 1) showPage(currentPage + 1);
            });
        }

        // Search
        if (searchInput) {
            searchInput.addEventListener('input', function () {
                showSearchResults(this.value);
            });
        }

        if (sidebar) {
            sidebar.classList.add('open');
        }

        // TOC links navigate to page
        for (var k = 0; k < tocLinks.length; k++) {
            tocLinks[k].addEventListener('click', function () {
                var pageIdx = parseInt(this.getAttribute('data-page'));
                searchInput.value = '';
                showPage(pageIdx);
            });
        }
    }
};
