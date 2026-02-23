// Help Page - Detailed usage guide (book-like)
window.HelpPage = {
    _isTurkish() {
        var selected = Bridge && Bridge._settings ? Bridge._settings.language : '';
        if (!selected || selected === 'auto') {
            selected = (navigator.language || 'en');
        }
        return String(selected).toLowerCase().indexOf('tr') === 0;
    },

    _copy() {
        if (this._isTurkish()) {
            return {
                searchPlaceholder: 'Yardım konularında ara...',
                tocTitle: 'İçindekiler',
                noResult: 'Aramanızla eşleşen sonuç bulunamadı.',

                ch1Title: 'Hızlı Başlangıç',
                ch1Icon: 'rocket_launch',
                ch1Step1: 'Araç çubuğundaki <b>+</b> butonuna veya menüden <b>Yeni Görev</b> seçeneğine tıklayın.',
                ch1Step2: 'Açılan pencerede <b>Görev Türü</b> (ör. Bilgisayarı kilitle) ve <b>Tetikleyici</b> (ör. Geriye sayım) seçin.',
                ch1Step3: 'Tetikleyiciye göre süre, saat veya Bluetooth cihazı belirleyin ve <b>Görev Listesine Ekle</b> butonuna tıklayın.',
                ch1Tip: 'Görev listesindeki bir görevi düzenlemek için satıra tıklayıp <b>düzenle</b> ikonunu kullanabilirsiniz.',

                ch2Title: 'Görev Türleri',
                ch2Icon: 'category',
                ch2Shutdown: '<b>Bilgisayarı kapat:</b> Bilgisayarı tamamen kapatır. Kaydedilmemiş çalışmalarınızı kaydettiğinizden emin olun.',
                ch2Restart: '<b>Yeniden başlat:</b> Bilgisayarı yeniden başlatır. Güncellemeler sonrası veya sistem temizliği için kullanışlıdır.',
                ch2Sleep: '<b>Bilgisayarı uyut:</b> Bilgisayarı uyku moduna alır. Hızlı geri dönüş sağlar, düşük güç tüketir.',
                ch2Lock: '<b>Bilgisayarı kilitle:</b> Oturumu kilitler. Masaüstünden uzaklaştığınızda güvenlik için ideal.',
                ch2Monitor: '<b>Ekranı kapat:</b> Yalnızca monitörü kapatır. Bilgisayar çalışmaya devam eder.',
                ch2Logoff: '<b>Oturumu kapat:</b> Mevcut Windows oturumunu kapatır. Tüm açık programlar kapanır.',

                ch3Title: 'Tetikleyiciler',
                ch3Icon: 'timer',
                ch3Sub1: 'Geriye Sayım',
                ch3Sub1Desc: 'Belirtilen süre dolduğunda görev bir kez çalışır. Süreyi saniye, dakika veya saat olarak girebilirsiniz. Görev çalıştıktan sonra listeden otomatik olarak kaldırılır.',
                ch3Sub2: 'Boşta Kalınan Süre',
                ch3Sub2Desc: 'Klavye veya fare kullanımı olmadığında belirlenen süre kadar beklenir. Süre dolduğunda görev çalışır. Fare veya klavye ile etkileşim süreyi sıfırlar.',
                ch3Sub3: 'Saate Göre Her Gün',
                ch3Sub3Desc: 'Her gün belirlenen saatte görev bir kez çalışır. Aynı gün içinde tekrar çalışmaz, ertesi gün aynı saatte tekrar tetiklenir.',
                ch3Sub4: 'Bluetooth Kilidi',
                ch3Sub4Desc: 'Seçilen Bluetooth cihazı önce algılanır. Cihaz bağlı iken bağlantısı kesildiğinde görev çalışır. Cihaz tekrar bağlanıp tekrar koparsa görev yeniden tetiklenir.',

                ch4Title: 'Bluetooth Kilidi Kılavuzu',
                ch4Icon: 'bluetooth',
                ch4Intro: 'Bluetooth Kilidi, telefonunuz veya başka bir Bluetooth cihazınız bilgisayardan uzaklaştığında otomatik olarak bilgisayarı kilitler.',
                ch4Step1: 'Yeni görev oluştururken tetikleyici olarak <b>Bluetooth Kilidi</b> seçin.',
                ch4Step2: 'Cihaz listesine tıklayın; Bluetooth taraması otomatik başlar.',
                ch4Step3: 'Listeden cihazınızı seçin ve görevi kaydedin.',
                ch4Step4: 'Cihazınız algılandıktan sonra, bağlantı kesildiğinde seçtiğiniz görev otomatik çalışır.',
                ch4Warn: 'İlk tetikleme için cihazın en az bir kez algılanması gerekir. Cihazınızın Bluetooth\'unun açık ve eşleşmiş olduğundan emin olun.',
                ch4Sub1: 'Bluetooth Eşik Süresi',
                ch4Sub1Desc: 'Ayarlar ekranındaki <b>Bluetooth eşik süresi (sn)</b> değeri, cihazın koptu sayılması için beklenen süredir. Önerilen: 5-15 saniye. Düşük değerler yanlış tetiklenmelere, yüksek değerler geç tepkiye neden olabilir.',
                ch4Sub2: 'Bluetooth Sinyal Eşiği',
                ch4Sub2Desc: 'Ayarlar ekranındaki <b>Bluetooth sinyal eşiği (dBm)</b> değeri, cihazın bağlı sayılması için gereken minimum sinyal gücüdür. 0 = kontrolü devre dışı bırakır. Önerilen: -70 ile -50 arası.',

                ch5Title: 'Ayarlar',
                ch5Icon: 'settings',
                ch5Theme: '<b>Tema:</b> Koyu, Açık veya Sistem Varsayılanı arasında seçim yapabilirsiniz.',
                ch5Lang: '<b>Dil Seçimi:</b> Uygulama dilini değiştirir. Değişiklik için uygulamanın yeniden başlatılması gerekir.',
                ch5Logs: '<b>Kayıt Tut:</b> Yapılan işlemlerin geçmişini tutar.',
                ch5Startup: '<b>Sistem başlangıcında çalışsın:</b> Windows açıldığında uygulama otomatik başlar.',
                ch5Taskbar: '<b>Kapatılsa da arkaplanda çalışsın:</b> Pencere kapatıldığında uygulama sistem tepsisinde çalışmaya devam eder.',
                ch5Countdown: '<b>İşlem yapılmadan uyarı göster:</b> Görev çalışmadan önce bir geri sayım uyarısı gösterir.',
                ch5CountdownSec: '<b>Uyarı gösterilecek süre (sn):</b> Uyarının kaç saniye gösterileceğini belirler. Önerilen: 5-10 saniye.',
                ch5Import: '<b>İçe Aktar / Dışa Aktar:</b> Ayarlarınızı .conf dosyası olarak yedekleyebilir veya başka bir bilgisayara taşıyabilirsiniz.',

                ch6Title: 'Menüler ve Araç Çubuğu',
                ch6Icon: 'menu',
                ch6Sub1: 'Araç Çubuğu',
                ch6Toolbar1: '<b>+ Butonu:</b> Yeni görev oluşturma penceresini açar.',
                ch6Toolbar2: '<b>Duraklat/Devam:</b> Tüm görevleri geçici olarak duraklatır veya devam ettirir.',
                ch6Toolbar3: '<b>Arama:</b> Görev listesinde anahtar kelime ile arama yapar.',
                ch6Sub2: 'Hamburger Menü',
                ch6Menu1: 'Sağ üst köşedeki menü ikonuna tıklayarak Ayarlar, Kayıtlar, Yardım ve Hakkında pencerelerini açabilirsiniz.',
                ch6Sub3: 'Sağ Tık Menü',
                ch6Menu2: 'Görev listesinde bir satıra sağ tıklayarak seçili görevi silebilir, tüm görevleri temizleyebilir veya yardım sayfasını açabilirsiniz.',
                ch6Sub4: 'Sekme Filtreleri',
                ch6Menu3: 'Görev listesinin üstündeki sekmeler ile görevleri türlerine göre filtreleyebilirsiniz (Tümü, Kapat, Y. Başlat, Uyku, vb.).',

                ch7Title: 'İpuçları ve Sık Sorulan Sorular',
                ch7Icon: 'lightbulb',
                ch7Tip1: 'Geriye Sayım tetikleyicisi çalıştıktan sonra listeden otomatik kaldırılır.',
                ch7Tip2: 'Saate göre her gün tetikleyicisi her gün yalnızca bir kez çalışır.',
                ch7Tip3: 'Duraklat/Devam Et ile tüm görevleri geçici olarak kontrol edebilirsiniz.',
                ch7Tip4: 'Boşta kalma tetikleyicisinde, geri sayım uyarısı sırasında fare veya klavye ile etkileşim işlemi iptal eder.',
                ch7Tip5: 'En fazla 5 görev aynı anda aktif olabilir.',
                ch7Tip6: 'Bluetooth Kilidi kullanırken, cihazınızın Bluetooth\'unun sürekli açık olduğundan emin olun.'
            };
        }

        return {
            searchPlaceholder: 'Search help topics...',
            tocTitle: 'Table of Contents',
            noResult: 'No results found for your search.',

            ch1Title: 'Quick Start',
            ch1Icon: 'rocket_launch',
            ch1Step1: 'Click the <b>+</b> button on the toolbar or select <b>New Action</b> from the menu.',
            ch1Step2: 'Choose an <b>Action Type</b> (e.g. Lock computer) and a <b>Trigger</b> (e.g. Countdown).',
            ch1Step3: 'Set the duration, time, or Bluetooth device based on the trigger, then click <b>Add to Action List</b>.',
            ch1Tip: 'To edit an existing action, click the row and use the <b>edit</b> icon.',

            ch2Title: 'Action Types',
            ch2Icon: 'category',
            ch2Shutdown: '<b>Shutdown computer:</b> Completely shuts down the computer. Make sure to save your work first.',
            ch2Restart: '<b>Restart computer:</b> Restarts the computer. Useful after updates or system cleanup.',
            ch2Sleep: '<b>Sleep computer:</b> Puts the computer into sleep mode. Fast wake-up and low power consumption.',
            ch2Lock: '<b>Lock computer:</b> Locks the current session. Ideal for security when leaving your desk.',
            ch2Monitor: '<b>Turn off monitor:</b> Only turns off the monitor. The computer continues running.',
            ch2Logoff: '<b>Log off Windows:</b> Logs off the current Windows session. All open programs will close.',

            ch3Title: 'Triggers',
            ch3Icon: 'timer',
            ch3Sub1: 'Countdown',
            ch3Sub1Desc: 'Runs the action once when the specified duration ends. You can enter the time in seconds, minutes, or hours. The action is automatically removed from the list after execution.',
            ch3Sub2: 'System Idle',
            ch3Sub2Desc: 'Waits for the specified duration of no keyboard or mouse input. When the idle time is reached, the action runs. Any mouse or keyboard interaction resets the timer.',
            ch3Sub3: 'Every Day by Time',
            ch3Sub3Desc: 'Runs the action once per day at the selected time. It will not run again on the same day; it triggers again at the same time the next day.',
            ch3Sub4: 'Bluetooth Lock',
            ch3Sub4Desc: 'The selected Bluetooth device must be detected first. When the device disconnects after being reachable, the action runs. If the device reconnects and disconnects again, the action triggers again.',

            ch4Title: 'Bluetooth Lock Guide',
            ch4Icon: 'bluetooth',
            ch4Intro: 'Bluetooth Lock automatically locks your computer (or performs another action) when your phone or another Bluetooth device moves away.',
            ch4Step1: 'Select <b>Bluetooth Lock</b> as the trigger when creating a new action.',
            ch4Step2: 'Click on the device list; Bluetooth scanning starts automatically.',
            ch4Step3: 'Select your device from the list and save the action.',
            ch4Step4: 'Once your device is detected, the action will run automatically when the connection is lost.',
            ch4Warn: 'The device must be detected at least once for the first trigger. Make sure your device\'s Bluetooth is on and paired.',
            ch4Sub1: 'Bluetooth Threshold',
            ch4Sub1Desc: 'The <b>Bluetooth threshold (sec)</b> setting controls how long to wait before considering the device disconnected. Recommended: 5-15 seconds. Low values may cause false triggers; high values may delay the response.',
            ch4Sub2: 'Bluetooth Signal Threshold',
            ch4Sub2Desc: 'The <b>Bluetooth signal threshold (dBm)</b> is the minimum signal strength to consider the device connected. 0 = disables the check. Recommended: -70 to -50.',

            ch5Title: 'Settings',
            ch5Icon: 'settings',
            ch5Theme: '<b>Theme:</b> Choose between Dark, Light, or System Default.',
            ch5Lang: '<b>Language:</b> Changes the application language. Requires a restart to take effect.',
            ch5Logs: '<b>Record Logs:</b> Keeps a history of performed actions.',
            ch5Startup: '<b>Start with Windows:</b> Automatically starts the application when Windows boots.',
            ch5Taskbar: '<b>Run in background:</b> The application keeps running in the system tray when the window is closed.',
            ch5Countdown: '<b>Show alert before action:</b> Shows a countdown warning before the action executes.',
            ch5CountdownSec: '<b>Countdown seconds:</b> How long the warning is displayed. Recommended: 5-10 seconds.',
            ch5Import: '<b>Import / Export:</b> Backup your settings as a .conf file or transfer them to another computer.',

            ch6Title: 'Menus and Toolbar',
            ch6Icon: 'menu',
            ch6Sub1: 'Toolbar',
            ch6Toolbar1: '<b>+ Button:</b> Opens the new action creation window.',
            ch6Toolbar2: '<b>Pause/Resume:</b> Temporarily pauses or resumes all actions.',
            ch6Toolbar3: '<b>Search:</b> Searches the action list by keyword.',
            ch6Sub2: 'Hamburger Menu',
            ch6Menu1: 'Click the menu icon in the top-right corner to open Settings, Logs, Help, and About windows.',
            ch6Sub3: 'Right-Click Menu',
            ch6Menu2: 'Right-click on a row in the action list to delete the selected action, clear all actions, or open this help page.',
            ch6Sub4: 'Tab Filters',
            ch6Menu3: 'Use the tabs above the action list to filter actions by type (All, Shutdown, Restart, Sleep, etc.).',

            ch7Title: 'Tips & FAQ',
            ch7Icon: 'lightbulb',
            ch7Tip1: 'Countdown actions are removed automatically after execution.',
            ch7Tip2: 'Daily time trigger runs only once per day.',
            ch7Tip3: 'Use pause/resume to temporarily stop all actions.',
            ch7Tip4: 'For idle trigger, mouse or keyboard interaction during the countdown warning cancels the action.',
            ch7Tip5: 'Up to 5 actions can be active at the same time.',
            ch7Tip6: 'When using Bluetooth Lock, make sure your device\'s Bluetooth stays on at all times.'
        };
    },

    _buildChapter(num, icon, title, bodyHtml, searchableText) {
        return {
            num: num,
            icon: icon,
            title: title,
            bodyHtml: bodyHtml,
            searchableText: (title + ' ' + searchableText).toLowerCase()
        };
    },

    _buildChapters(t) {
        var chapters = [];

        // Ch1: Quick Start
        chapters.push(this._buildChapter(1, t.ch1Icon, t.ch1Title,
            '<div class="help-step"><span class="help-step-num">1</span><span class="help-step-text">' + t.ch1Step1 + '</span></div>' +
            '<div class="help-step"><span class="help-step-num">2</span><span class="help-step-text">' + t.ch1Step2 + '</span></div>' +
            '<div class="help-step"><span class="help-step-num">3</span><span class="help-step-text">' + t.ch1Step3 + '</span></div>' +
            '<div class="help-tip-box tip"><span class="mi">lightbulb</span><span>' + t.ch1Tip + '</span></div>',
            t.ch1Step1 + ' ' + t.ch1Step2 + ' ' + t.ch1Step3 + ' ' + t.ch1Tip
        ));

        // Ch2: Action Types
        chapters.push(this._buildChapter(2, t.ch2Icon, t.ch2Title,
            '<div class="help-desc-item">' + t.ch2Shutdown + '</div>' +
            '<div class="help-desc-item">' + t.ch2Restart + '</div>' +
            '<div class="help-desc-item">' + t.ch2Sleep + '</div>' +
            '<div class="help-desc-item">' + t.ch2Lock + '</div>' +
            '<div class="help-desc-item">' + t.ch2Monitor + '</div>' +
            '<div class="help-desc-item">' + t.ch2Logoff + '</div>',
            t.ch2Shutdown + ' ' + t.ch2Restart + ' ' + t.ch2Sleep + ' ' + t.ch2Lock + ' ' + t.ch2Monitor + ' ' + t.ch2Logoff
        ));

        // Ch3: Triggers
        chapters.push(this._buildChapter(3, t.ch3Icon, t.ch3Title,
            '<div class="help-sub-title">' + t.ch3Sub1 + '</div>' +
            '<div class="help-desc-item">' + t.ch3Sub1Desc + '</div>' +
            '<div class="help-sub-title">' + t.ch3Sub2 + '</div>' +
            '<div class="help-desc-item">' + t.ch3Sub2Desc + '</div>' +
            '<div class="help-sub-title">' + t.ch3Sub3 + '</div>' +
            '<div class="help-desc-item">' + t.ch3Sub3Desc + '</div>' +
            '<div class="help-sub-title">' + t.ch3Sub4 + '</div>' +
            '<div class="help-desc-item">' + t.ch3Sub4Desc + '</div>',
            t.ch3Sub1 + ' ' + t.ch3Sub1Desc + ' ' + t.ch3Sub2 + ' ' + t.ch3Sub2Desc + ' ' + t.ch3Sub3 + ' ' + t.ch3Sub3Desc + ' ' + t.ch3Sub4 + ' ' + t.ch3Sub4Desc
        ));

        // Ch4: Bluetooth Lock Guide
        chapters.push(this._buildChapter(4, t.ch4Icon, t.ch4Title,
            '<div class="help-desc-item">' + t.ch4Intro + '</div>' +
            '<div class="help-step"><span class="help-step-num">1</span><span class="help-step-text">' + t.ch4Step1 + '</span></div>' +
            '<div class="help-step"><span class="help-step-num">2</span><span class="help-step-text">' + t.ch4Step2 + '</span></div>' +
            '<div class="help-step"><span class="help-step-num">3</span><span class="help-step-text">' + t.ch4Step3 + '</span></div>' +
            '<div class="help-step"><span class="help-step-num">4</span><span class="help-step-text">' + t.ch4Step4 + '</span></div>' +
            '<div class="help-tip-box warn"><span class="mi">warning</span><span>' + t.ch4Warn + '</span></div>' +
            '<div class="help-sub-title">' + t.ch4Sub1 + '</div>' +
            '<div class="help-desc-item">' + t.ch4Sub1Desc + '</div>' +
            '<div class="help-sub-title">' + t.ch4Sub2 + '</div>' +
            '<div class="help-desc-item">' + t.ch4Sub2Desc + '</div>',
            t.ch4Intro + ' ' + t.ch4Step1 + ' ' + t.ch4Step2 + ' ' + t.ch4Step3 + ' ' + t.ch4Step4 + ' ' + t.ch4Warn + ' ' + t.ch4Sub1 + ' ' + t.ch4Sub1Desc + ' ' + t.ch4Sub2 + ' ' + t.ch4Sub2Desc
        ));

        // Ch5: Settings
        chapters.push(this._buildChapter(5, t.ch5Icon, t.ch5Title,
            '<div class="help-desc-item">' + t.ch5Theme + '</div>' +
            '<div class="help-desc-item">' + t.ch5Lang + '</div>' +
            '<div class="help-desc-item">' + t.ch5Logs + '</div>' +
            '<div class="help-desc-item">' + t.ch5Startup + '</div>' +
            '<div class="help-desc-item">' + t.ch5Taskbar + '</div>' +
            '<div class="help-desc-item">' + t.ch5Countdown + '</div>' +
            '<div class="help-desc-item">' + t.ch5CountdownSec + '</div>' +
            '<div class="help-desc-item">' + t.ch5Import + '</div>',
            t.ch5Theme + ' ' + t.ch5Lang + ' ' + t.ch5Logs + ' ' + t.ch5Startup + ' ' + t.ch5Taskbar + ' ' + t.ch5Countdown + ' ' + t.ch5CountdownSec + ' ' + t.ch5Import
        ));

        // Ch6: Menus
        chapters.push(this._buildChapter(6, t.ch6Icon, t.ch6Title,
            '<div class="help-sub-title">' + t.ch6Sub1 + '</div>' +
            '<div class="help-desc-item">' + t.ch6Toolbar1 + '</div>' +
            '<div class="help-desc-item">' + t.ch6Toolbar2 + '</div>' +
            '<div class="help-desc-item">' + t.ch6Toolbar3 + '</div>' +
            '<div class="help-sub-title">' + t.ch6Sub2 + '</div>' +
            '<div class="help-desc-item">' + t.ch6Menu1 + '</div>' +
            '<div class="help-sub-title">' + t.ch6Sub3 + '</div>' +
            '<div class="help-desc-item">' + t.ch6Menu2 + '</div>' +
            '<div class="help-sub-title">' + t.ch6Sub4 + '</div>' +
            '<div class="help-desc-item">' + t.ch6Menu3 + '</div>',
            t.ch6Sub1 + ' ' + t.ch6Toolbar1 + ' ' + t.ch6Toolbar2 + ' ' + t.ch6Toolbar3 + ' ' + t.ch6Sub2 + ' ' + t.ch6Menu1 + ' ' + t.ch6Sub3 + ' ' + t.ch6Menu2 + ' ' + t.ch6Sub4 + ' ' + t.ch6Menu3
        ));

        // Ch7: Tips & FAQ
        chapters.push(this._buildChapter(7, t.ch7Icon, t.ch7Title,
            '<div class="help-tip-box tip"><span class="mi">lightbulb</span><span>' + t.ch7Tip1 + '</span></div>' +
            '<div class="help-tip-box tip"><span class="mi">lightbulb</span><span>' + t.ch7Tip2 + '</span></div>' +
            '<div class="help-tip-box tip"><span class="mi">lightbulb</span><span>' + t.ch7Tip3 + '</span></div>' +
            '<div class="help-tip-box tip"><span class="mi">lightbulb</span><span>' + t.ch7Tip4 + '</span></div>' +
            '<div class="help-tip-box tip"><span class="mi">lightbulb</span><span>' + t.ch7Tip5 + '</span></div>' +
            '<div class="help-tip-box tip"><span class="mi">lightbulb</span><span>' + t.ch7Tip6 + '</span></div>',
            t.ch7Tip1 + ' ' + t.ch7Tip2 + ' ' + t.ch7Tip3 + ' ' + t.ch7Tip4 + ' ' + t.ch7Tip5 + ' ' + t.ch7Tip6
        ));

        return chapters;
    },

    render() {
        var L = Bridge.lang.bind(Bridge);
        var t = this._copy();
        var chapters = this._buildChapters(t);

        // TOC
        var tocHtml = '<div class="help-toc"><div class="help-toc-title">' + t.tocTitle + '</div><ul class="help-toc-list">';
        for (var i = 0; i < chapters.length; i++) {
            var ch = chapters[i];
            tocHtml += '<li><a class="help-toc-link" data-chapter="' + ch.num + '"><span class="mi">' + ch.icon + '</span>' + ch.num + '. ' + ch.title + '</a></li>';
        }
        tocHtml += '</ul></div>';

        // Chapters
        var chaptersHtml = '';
        for (var j = 0; j < chapters.length; j++) {
            var c = chapters[j];
            chaptersHtml +=
                '<div class="help-chapter" data-chapter="' + c.num + '" data-search="' + c.searchableText.replace(/"/g, '&quot;') + '">' +
                    '<div class="help-chapter-header">' +
                        '<span class="help-chapter-number">' + c.num + '</span>' +
                        '<span class="mi help-chapter-icon">' + c.icon + '</span>' +
                        '<span class="help-chapter-title">' + c.title + '</span>' +
                    '</div>' +
                    '<div class="help-chapter-body">' + c.bodyHtml + '</div>' +
                '</div>';
        }

        return '' +
        '<div class="card">' +
            '<div class="card-title">' +
                '<span class="mi">help</span>' +
                (L('HelpMenuItem') || 'Help') +
            '</div>' +
            '<div class="help-content">' +
                '<div class="help-search-box">' +
                    '<span class="mi">search</span>' +
                    '<input type="text" class="help-search-input" id="help-search" placeholder="' + t.searchPlaceholder + '">' +
                '</div>' +
                tocHtml +
                '<div id="help-chapters">' + chaptersHtml + '</div>' +
                '<div class="help-no-result" id="help-no-result" style="display:none">' + t.noResult + '</div>' +
            '</div>' +
        '</div>';
    },

    afterRender() {
        var searchInput = document.getElementById('help-search');
        if (!searchInput) return;

        var noResult = document.getElementById('help-no-result');
        var toc = document.querySelector('.help-toc');
        var chapters = document.querySelectorAll('.help-chapter');

        // Search filter
        searchInput.addEventListener('input', function () {
            var q = this.value.toLowerCase().trim();
            var anyVisible = false;

            for (var i = 0; i < chapters.length; i++) {
                var ch = chapters[i];
                var text = ch.getAttribute('data-search') || '';
                if (!q || text.indexOf(q) >= 0) {
                    ch.classList.remove('hidden');
                    anyVisible = true;
                } else {
                    ch.classList.add('hidden');
                }
            }

            if (noResult) {
                noResult.style.display = anyVisible ? 'none' : '';
            }
            if (toc) {
                toc.style.display = q ? 'none' : '';
            }
        });

        // TOC click scroll
        var tocLinks = document.querySelectorAll('.help-toc-link[data-chapter]');
        for (var k = 0; k < tocLinks.length; k++) {
            tocLinks[k].addEventListener('click', function () {
                var num = this.getAttribute('data-chapter');
                var target = document.querySelector('.help-chapter[data-chapter="' + num + '"]');
                if (target) {
                    target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            });
        }
    }
};
