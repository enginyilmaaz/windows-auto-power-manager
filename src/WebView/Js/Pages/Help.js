// Help Page - Detailed usage guide (book-like, GitBook style with pagination)
window.HelpPage = {
    _currentPage: 0,
    _chapters: null,

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
                prevPage: 'Önceki',
                nextPage: 'Sonraki',
                searchResultsTitle: 'Arama Sonuçları',
                searchResultCount: ' sonuç bulundu',

                // Ch1 - Hızlı Başlangıç
                ch1Title: 'Hızlı Başlangıç',
                ch1Icon: 'rocket_launch',
                ch1Intro: 'Windows Auto Power Manager, bilgisayarınızı belirli koşullarda otomatik olarak kapatmanızı, kilitlemenizi, uyku moduna almanızı ve daha fazlasını yapmanızı sağlayan bir araçtır. Aşağıdaki adımları takip ederek hızlıca başlayabilirsiniz.',
                ch1Step1: 'Araç çubuğundaki <b>+</b> butonuna tıklayın veya hamburger menüden <b>Yeni Görev</b> seçeneğini kullanın. Bu işlem yeni görev oluşturma penceresini açacaktır.',
                ch1Step2: '<b>Görev Türü</b> açılır listesinden yapmak istediğiniz işlemi seçin. Örneğin: Bilgisayarı kilitle, Bilgisayarı kapat, Bilgisayarı uyut, Ekranı kapat, Yeniden başlat veya Oturumu kapat.',
                ch1Step3: '<b>Tetikleyici</b> açılır listesinden görevin ne zaman çalışacağını belirleyin. Üç farklı tetikleyici mevcuttur: Geriye Sayım, Boşta Kalınan Süre ve Saate Göre Her Gün.',
                ch1Step4: 'Seçtiğiniz tetikleyiciye göre gerekli değerleri girin. Geriye Sayım ve Boşta Kalınan Süre için süre ve birim (saniye/dakika/saat), Saate Göre Her Gün için saat girin.',
                ch1Step5: '<b>Görev Listesine Ekle</b> butonuna tıklayarak görevi kaydedin. Görev ana ekrandaki tabloda görünecektir.',
                ch1Tip: 'Görev listesindeki bir görevi düzenlemek için ilgili satıra tıklayıp sağ tarafta beliren <b>kalem</b> ikonunu kullanabilirsiniz. Silmek için ise <b>çöp kutusu</b> ikonuna tıklayın.',
                ch1Tip2: 'Aynı anda en fazla <b>5 görev</b> oluşturabilirsiniz. Daha fazla görev eklemek için mevcut görevlerden birini silmeniz gerekir.',

                // Ch2 - Görev Türleri
                ch2Title: 'Görev Türleri',
                ch2Icon: 'category',
                ch2Intro: 'Uygulama altı farklı görev türü destekler. Her bir görev türü farklı bir sistem işlemi gerçekleştirir. Aşağıda her birinin detaylı açıklaması yer almaktadır.',
                ch2ShutdownTitle: 'Bilgisayarı Kapat',
                ch2Shutdown: 'Bilgisayarınızı tamamen kapatır. Tüm açık programlar kapatılır ve sistem kapanır. <b>Önemli:</b> Kaydedilmemiş çalışmalarınız kaybolabilir. Bu görev türünü kullanmadan önce tüm dosyalarınızı kaydettiğinizden emin olun. İşletim sistemi, kapatma işlemi öncesinde açık programlara kapanma sinyali gönderir.',
                ch2RestartTitle: 'Yeniden Başlat',
                ch2Restart: 'Bilgisayarınızı yeniden başlatır. Sistem güncellemelerinden sonra veya performans sorunlarında kullanışlıdır. Tüm programlar kapatılır, sistem kapanır ve otomatik olarak tekrar açılır. Kaydedilmemiş çalışmalarınızı kaydetmeyi unutmayın.',
                ch2SleepTitle: 'Bilgisayarı Uyut',
                ch2Sleep: 'Bilgisayarınızı uyku moduna (Sleep) alır. Uyku modunda bilgisayar çok düşük güç tüketir ve çalışma durumunu RAM\'de saklar. Herhangi bir tuşa basarak veya fareyi hareket ettirerek hızlıca geri dönebilirsiniz. Açık programlarınız ve dosyalarınız korunur.',
                ch2LockTitle: 'Bilgisayarı Kilitle',
                ch2Lock: 'Windows oturumunuzu kilitler. Kilit ekranı görüntülenir ve tekrar erişim için parolanızı girmeniz gerekir. Masaüstünüzden uzaklaştığınızda güvenlik için idealdir. Açık programlarınız ve dosyalarınız arka planda çalışmaya devam eder, hiçbir veri kaybı yaşanmaz.',
                ch2MonitorTitle: 'Ekranı Kapat',
                ch2Monitor: 'Yalnızca monitörünüzü (ekranınızı) kapatır. Bilgisayar arka planda çalışmaya devam eder. İndirme işlemi gibi uzun süren işlemler sırasında enerji tasarrufu sağlamak için kullanışlıdır. Fareyi hareket ettirerek veya bir tuşa basarak ekranı tekrar açabilirsiniz.',
                ch2LogoffTitle: 'Oturumu Kapat',
                ch2Logoff: 'Mevcut Windows oturumunuzu kapatır (Log Off). Tüm açık programlar kapatılır ve oturum açma ekranına dönersiniz. Birden fazla kullanıcı hesabı olan bilgisayarlarda kullanıcı değiştirmek için kullanışlıdır. <b>Dikkat:</b> Kaydedilmemiş tüm çalışmalarınız kaybolur.',

                // Ch3 - Tetikleyiciler
                ch3Title: 'Tetikleyiciler',
                ch3Icon: 'timer',
                ch3Intro: 'Tetikleyiciler, oluşturduğunuz görevin hangi koşulda çalışacağını belirler. Her tetikleyici farklı bir zamanlama mantığı kullanır.',
                ch3Sub1: 'Geriye Sayım',
                ch3Sub1Desc: 'Belirttiğiniz süre dolduğunda görev bir kez çalışır. Süreyi saniye, dakika veya saat cinsinden girebilirsiniz. Örneğin: 30 dakika sonra bilgisayarı kapat. Geri sayım başlar başlamaz tablodaki "Süre / Tarih" sütununda hedef zaman görüntülenir.',
                ch3Sub1Detail: '<b>Nasıl çalışır:</b> Görev oluşturulduğu anda geri sayım başlar. Süre dolduğunda işlem bir kez gerçekleştirilir ve görev listeden otomatik olarak kaldırılır. Eğer "İşlem yapılmadan uyarı göster" ayarı açıksa, işlem öncesi bir geri sayım uyarısı gösterilir ve bu süre içinde işlemi iptal edebilirsiniz.',
                ch3Sub1Example: '<b>Örnek kullanım:</b> Film izlerken 2 saat sonra bilgisayarın kapanmasını istiyorsanız, Görev Türü: "Bilgisayarı kapat", Tetikleyici: "Geriye Sayım", Değer: 2, Birim: Saat olarak ayarlayın.',
                ch3Sub2: 'Boşta Kalınan Süre',
                ch3Sub2Desc: 'Klavye ve fare kullanımı olmadığında belirlenen süre kadar beklenir. Bu süre dolduğunda görev çalışır. Herhangi bir fare hareketi veya klavye tuşuna basma, boşta kalma süresini sıfırlar ve görev tetiklenmez.',
                ch3Sub2Detail: '<b>Nasıl çalışır:</b> Sistem sürekli olarak klavye ve fare etkinliğini izler. Belirttiğiniz süre boyunca hiçbir giriş algılanmazsa görev tetiklenir. Eğer "İşlem yapılmadan uyarı göster" ayarı açıksa, geri sayım uyarısı sırasında fare veya klavye ile etkileşim işlemi otomatik olarak iptal eder.',
                ch3Sub2Example: '<b>Örnek kullanım:</b> Bilgisayar başında olmadığınızda 10 dakika sonra ekranın kilitlenmesini istiyorsanız, Görev Türü: "Bilgisayarı kilitle", Tetikleyici: "Boşta kalınan süre", Değer: 10, Birim: Dakika olarak ayarlayın.',
                ch3Sub2Warn: 'Aynı süreye sahip birden fazla boşta kalma görevi oluşturmak çakışmaya neden olabilir. Uygulama bu durumda sizi uyarır.',
                ch3Sub3: 'Saate Göre Her Gün',
                ch3Sub3Desc: 'Her gün belirlenen saatte görev bir kez çalışır. Saat, 24 saat formatında girilir (ör. 23:00). Aynı gün içinde aynı görev tekrar çalışmaz; ertesi gün aynı saatte tekrar tetiklenir.',
                ch3Sub3Detail: '<b>Nasıl çalışır:</b> Uygulama açık olduğu sürece her gün belirlenen saati kontrol eder. Saat geldiğinde görev bir kez çalışır ve o gün için tamamlanmış olarak işaretlenir. Ertesi gün aynı saatte tekrar aktif olur. Görev listeden otomatik silinmez, her gün çalışmaya devam eder.',
                ch3Sub3Example: '<b>Örnek kullanım:</b> Her gece 00:30\'da bilgisayarın uyku moduna girmesini istiyorsanız, Görev Türü: "Bilgisayarı uyut", Tetikleyici: "Saate göre hergün", Saat: 00:30 olarak ayarlayın.',
                // Ch5 - Ayarlar
                ch5Title: 'Ayarlar',
                ch5Icon: 'settings',
                ch5Intro: 'Ayarlar penceresi, uygulamanın davranışını özelleştirmenizi sağlar. Her bir ayarın detaylı açıklaması aşağıdadır.',
                ch5ThemeTitle: 'Tema',
                ch5Theme: 'Uygulamanın görünümünü değiştirir. Üç seçenek mevcuttur: <b>Koyu</b> (karanlık arka plan, göz yorgunluğunu azaltır), <b>Açık</b> (beyaz arka plan, aydınlık ortamlar için), <b>Sistem Varsayılanı</b> (Windows tema ayarınızı takip eder). Tema değişikliği anında uygulanır.',
                ch5LangTitle: 'Dil Seçimi',
                ch5Lang: 'Uygulamanın arayüz dilini değiştirir. Mevcut diller: Türkçe, İngilizce, Almanca, Fransızca, İtalyanca ve Rusça. <b>Otomatik</b> seçeneği Windows sistem dilinizi kullanır. Dil değişikliği için uygulamanın yeniden başlatılması gerekir.',
                ch5LogsTitle: 'Kayıt Tut',
                ch5Logs: 'Aktif olduğunda, uygulama tarafından gerçekleştirilen tüm işlemler (kilitleme, kapatma, uyutma vb.) tarih ve saat bilgisiyle birlikte kaydedilir. Bu kayıtları Kayıtlar (İşlem Geçmişi) penceresinden görüntüleyebilir, filtreleyebilir ve silebilirsiniz.',
                ch5StartupTitle: 'Sistem Başlangıcında Çalışsın',
                ch5Startup: 'Aktif olduğunda, Windows açıldığında uygulama otomatik olarak başlar. Uygulama başlangıçta sistem tepsisinde (görev çubuğunun sağ alt köşesi) arka planda çalışır. Sürekli izleme gerektiren görevler için bu ayarın açık olması önerilir.',
                ch5TaskbarTitle: 'Kapatılsa da Arkaplanda Çalışsın',
                ch5Taskbar: 'Aktif olduğunda, uygulama penceresi kapatıldığında (X butonuna basıldığında) uygulama kapanmaz, sistem tepsisinde çalışmaya devam eder. Görevlerinizin kesintisiz çalışması için bu ayarı açık tutmanız önerilir. Uygulamayı tamamen kapatmak için menüden "Programdan çık" seçeneğini kullanın.',
                ch5CountdownTitle: 'İşlem Yapılmadan Uyarı Göster',
                ch5Countdown: 'Aktif olduğunda, herhangi bir görev çalışmadan önce ekranda bir geri sayım uyarı penceresi gösterilir. Bu pencerede işlemi atlayabilir, görevi silebilir veya yoksayabilirsiniz. Boşta kalma tetikleyicisinde, uyarı sırasında fare veya klavye ile etkileşim işlemi otomatik olarak iptal eder.',
                ch5CountdownSecTitle: 'Uyarı Gösterilecek Süre (sn)',
                ch5CountdownSec: 'Uyarı penceresinin kaç saniye gösterileceğini belirler. Bu süre dolduğunda işlem otomatik olarak gerçekleştirilir. <b>Önerilen: 5-10 saniye.</b> Çok kısa süreler tepki vermenize yetmeyebilir, çok uzun süreler ise gereksiz beklemeye neden olur.',
                ch5ImportTitle: 'İçe Aktar / Dışa Aktar',
                ch5Import: 'Uygulama ayarlarınızı <b>.conf</b> dosyası olarak dışa aktarabilir (yedekleme) veya daha önce kaydedilmiş bir .conf dosyasını içe aktarabilirsiniz (geri yükleme). Bu özellik ayarlarınızı başka bir bilgisayara taşımak veya sıfırlama öncesi yedek almak için kullanışlıdır.',

                // Ch6 - Menüler
                ch6Title: 'Menüler ve Araç Çubuğu',
                ch6Icon: 'menu',
                ch6Intro: 'Uygulama, hızlı erişim için çeşitli menüler ve araç çubuğu butonları sunar.',
                ch6Sub1: 'Araç Çubuğu',
                ch6Toolbar1: '<b>+ (Artı) Butonu:</b> Yeni görev oluşturma penceresini (modal) açar. Görev türü, tetikleyici ve değerlerini bu pencereden girersiniz.',
                ch6Toolbar2: '<b>Duraklat Butonu:</b> Tüm görevleri geçici olarak duraklatır. Duraklama süresini önceden tanımlı seçeneklerden (30 dk, 1 saat, 2 saat, 4 saat, gün sonuna kadar) veya özel süre girerek belirleyebilirsiniz. Duraklatma aktifken üst kısımda turuncu bir banner görünür.',
                ch6Toolbar3: '<b>Devam Et Butonu:</b> Duraklatılmış görevleri tekrar devam ettirir. Duraklatma banner\'ındaki oynat butonuyla da aynı işlemi yapabilirsiniz.',
                ch6Toolbar4: '<b>Arama Kutusu:</b> Görev listesinde anahtar kelime ile anlık arama yapar. Tetikleyici adı, görev türü, süre/tarih veya oluşturulma tarihi gibi alanlarda arama yapabilirsiniz.',
                ch6Sub2: 'Hamburger Menü (Sağ Üst)',
                ch6Menu1: 'Sağ üst köşedeki üç çizgi (≡) ikonuna tıklayarak açılır. Menü öğeleri: <b>Görevler</b> (ana sayfa), <b>Ayarlar</b> (ayrı pencerede açılır), <b>Kayıtlar/İşlem Geçmişi</b> (ayrı pencerede açılır), <b>Yardım</b> (bu sayfa, ayrı pencerede açılır), <b>Hakkında</b> (sürüm bilgisi, ayrı pencerede açılır), <b>Programdan Çık</b> (uygulamayı tamamen kapatır).',
                ch6Sub3: 'Sağ Tık Menü (Görev Listesi)',
                ch6Menu2: 'Görev listesindeki herhangi bir satıra sağ tıklayarak bağlam menüsünü açabilirsiniz. Menü öğeleri: <b>Seçili görevi sil</b> (onay istenir), <b>Tüm görevleri sil</b> (tüm listeyi temizler), <b>Tetikleyici kullanım yardımı</b> (bu yardım sayfasını açar).',
                ch6Sub4: 'Sekme Filtreleri',
                ch6Menu3: 'Görev listesinin üstündeki sekmeler ile görevleri türlerine göre filtreleyebilirsiniz: <b>Tümü</b> (tüm görevler), <b>Kapat</b> (kapatma görevleri), <b>Y. Başlat</b> (yeniden başlatma), <b>Uyku</b> (uyutma), <b>Kilitle</b> (kilitleme), <b>Monitör</b> (ekran kapatma), <b>Oturum Kapat</b> (oturum kapatma). Aktif sekme mavi renkte vurgulanır.',
                ch6Sub5: 'Sistem Tepsisi (Tray Icon)',
                ch6Menu4: 'Uygulama arka planda çalışırken görev çubuğunun sağ alt köşesindeki sistem tepsisinde bir ikon görünür. Bu ikona sağ tıklayarak hızlı menüye erişebilirsiniz: Yeni Görev Oluştur, Ayarlar, Kayıtları Göster, Yardım ve Programdan Çık.',

                // Ch7 - Kayıtlar
                ch7Title: 'Kayıtlar ve İşlem Geçmişi',
                ch7Icon: 'description',
                ch7Intro: 'Kayıtlar penceresi, uygulama tarafından gerçekleştirilen tüm işlemlerin detaylı geçmişini gösterir. Bu özelliğin çalışması için Ayarlar\'daki "Kayıt Tut" seçeneğinin aktif olması gerekir.',
                ch7Desc1: 'Her kayıt, yapılan işlemin türünü (kilitleme, kapatma, uyutma vb.) ve gerçekleştirilme zamanını içerir. Uygulama başlatma ve sonlandırma olayları da kaydedilir.',
                ch7FilterTitle: 'Filtreleme ve Sıralama',
                ch7Desc2: 'Kayıtları çeşitli kriterlere göre filtreleyebilirsiniz: işlem türü (kilitlenme, kapatılma, uyutulma vb.), tarih aralığı (başlangıç ve bitiş tarihi). Sıralama seçenekleri: yeniden eskiye veya eskiden yeniye. <b>Filtreleri sıfırla</b> butonu ile tüm filtreleri temizleyebilirsiniz.',
                ch7Desc3: '<b>Kayıtları Sil</b> butonu ile tüm kayıt geçmişini temizleyebilirsiniz. Bu işlem geri alınamaz.',

                // Ch8 - İpuçları ve SSS
                ch8Title: 'İpuçları ve Sık Sorulan Sorular',
                ch8Icon: 'lightbulb',
                ch8Tip1: '<b>Geriye Sayım</b> tetikleyicisi çalıştıktan sonra görev listeden otomatik olarak kaldırılır. Diğer tetikleyici türleri kalıcıdır.',
                ch8Tip2: '<b>Saate göre her gün</b> tetikleyicisi her gün yalnızca bir kez çalışır. Aynı gün içinde uygulamayı yeniden başlatsanız bile tekrar tetiklenmez.',
                ch8Tip3: '<b>Duraklat/Devam Et</b> özelliği ile tüm görevleri geçici olarak durdurabilirsiniz. Duraklatma süresi dolduğunda görevler otomatik olarak devam eder.',
                ch8Tip4: '<b>Boşta kalma</b> tetikleyicisinde, geri sayım uyarısı sırasında fareyi hareket ettirmek veya bir tuşa basmak işlemi otomatik olarak iptal eder. Bu sayede yanlışlıkla tetiklenen işlemleri engelleyebilirsiniz.',
                ch8Tip6: 'Uygulama penceresi kapatıldığında görevlerin çalışmaya devam etmesi için Ayarlar\'daki <b>"Kapatılsa da arkaplanda çalışsın"</b> seçeneğini aktif edin.',
                ch8Tip7: 'Ayarlarınızı düzenli olarak <b>dışa aktararak</b> yedeklemeniz önerilir. Böylece olası bir sorun durumunda hızlıca geri yükleyebilirsiniz.',
                ch8Faq1Q: '<b>Soru:</b> Uygulama kapatıldığında görevler çalışır mı?',
                ch8Faq1A: '<b>Cevap:</b> "Kapatılsa da arkaplanda çalışsın" ayarı açıksa evet. Pencereyi kapatmak uygulamayı kapatmaz, sistem tepsisinde çalışmaya devam eder. Ancak menüden "Programdan çık" ile tamamen kapatırsanız görevler durur.',
                ch8Faq2Q: '<b>Soru:</b> Bilgisayar yeniden başlatıldığında görevler kaybolur mu?',
                ch8Faq2A: '<b>Cevap:</b> Hayır. Görevler kaydedilir ve uygulama tekrar açıldığında geri yüklenir. "Sistem başlangıcında çalışsın" ayarı açıksa uygulama Windows ile birlikte otomatik başlar.',
                ch8Faq3Q: '<b>Soru:</b> Aynı anda kaç görev oluşturabilrim?',
                ch8Faq3A: '<b>Cevap:</b> En fazla 5 görev aynı anda aktif olabilir. Daha fazla görev eklemek için mevcut görevlerden birini silmeniz gerekir.'
            };
        }

        return {
            searchPlaceholder: 'Search help topics...',
            tocTitle: 'Table of Contents',
            noResult: 'No results found for your search.',
            prevPage: 'Previous',
            nextPage: 'Next',
            searchResultsTitle: 'Search Results',
            searchResultCount: ' results found',

            // Ch1 - Quick Start
            ch1Title: 'Quick Start',
            ch1Icon: 'rocket_launch',
            ch1Intro: 'Windows Auto Power Manager lets you automatically shut down, lock, sleep, and perform other power actions on your computer based on specific conditions. Follow the steps below to get started quickly.',
            ch1Step1: 'Click the <b>+</b> button on the toolbar or select <b>New Action</b> from the hamburger menu. This opens the new action creation window.',
            ch1Step2: 'From the <b>Action Type</b> dropdown, select the action you want to perform. For example: Lock computer, Shutdown computer, Sleep computer, Turn off monitor, Restart computer, or Log off Windows.',
            ch1Step3: 'From the <b>Trigger</b> dropdown, select when the action should run. Three trigger types are available: Countdown, System Idle, and Every Day by Time.',
            ch1Step4: 'Enter the required values based on the selected trigger. For Countdown and System Idle: duration and unit (seconds/minutes/hours). For Every Day by Time: the time.',
            ch1Step5: 'Click <b>Add to Action List</b> to save the action. The action will appear in the table on the main screen.',
            ch1Tip: 'To edit an existing action, click the row and use the <b>pencil</b> icon that appears on the right side. To delete, click the <b>trash</b> icon.',
            ch1Tip2: 'You can create up to <b>5 actions</b> at the same time. To add more, delete an existing action first.',

            // Ch2 - Action Types
            ch2Title: 'Action Types',
            ch2Icon: 'category',
            ch2Intro: 'The application supports six different action types. Each action type performs a different system operation. Below is a detailed description of each.',
            ch2ShutdownTitle: 'Shutdown Computer',
            ch2Shutdown: 'Completely shuts down your computer. All open programs will be closed and the system will power off. <b>Important:</b> Unsaved work may be lost. Make sure to save all your files before using this action type. The operating system sends a close signal to open programs before shutting down.',
            ch2RestartTitle: 'Restart Computer',
            ch2Restart: 'Restarts your computer. Useful after system updates or when experiencing performance issues. All programs will be closed, the system will shut down and automatically boot back up. Remember to save any unsaved work.',
            ch2SleepTitle: 'Sleep Computer',
            ch2Sleep: 'Puts your computer into Sleep mode. In sleep mode, the computer uses very low power and keeps its working state in RAM. You can quickly resume by pressing any key or moving the mouse. Your open programs and files are preserved.',
            ch2LockTitle: 'Lock Computer',
            ch2Lock: 'Locks your Windows session. The lock screen is displayed and you need to enter your password to regain access. Ideal for security when leaving your desk. Your open programs and files continue running in the background with no data loss.',
            ch2MonitorTitle: 'Turn Off Monitor',
            ch2Monitor: 'Only turns off your monitor (screen). The computer continues running in the background. Useful for saving energy during long-running tasks like downloads. Move the mouse or press a key to turn the screen back on.',
            ch2LogoffTitle: 'Log Off Windows',
            ch2Logoff: 'Logs off your current Windows session. All open programs will be closed and you will return to the sign-in screen. Useful for switching users on multi-user computers. <b>Warning:</b> All unsaved work will be lost.',

            // Ch3 - Triggers
            ch3Title: 'Triggers',
            ch3Icon: 'timer',
            ch3Intro: 'Triggers determine under what conditions your created action will run. Each trigger uses a different timing mechanism.',
            ch3Sub1: 'Countdown',
            ch3Sub1Desc: 'Runs the action once when the specified duration ends. You can enter the time in seconds, minutes, or hours. For example: shut down the computer after 30 minutes. As soon as the countdown starts, the target time is displayed in the "Duration / Date" column.',
            ch3Sub1Detail: '<b>How it works:</b> The countdown starts when the action is created. When the time is up, the action runs once and is automatically removed from the list. If "Show alert before action" is enabled in settings, a countdown warning is shown before the action and you can cancel it during this time.',
            ch3Sub1Example: '<b>Example:</b> If you want the computer to shut down after 2 hours while watching a movie, set Action Type: "Shutdown computer", Trigger: "Countdown", Value: 2, Unit: Hours.',
            ch3Sub2: 'System Idle',
            ch3Sub2Desc: 'Waits for the specified duration with no keyboard or mouse input. When this idle time is reached, the action runs. Any mouse movement or keyboard press resets the idle timer and the action will not trigger.',
            ch3Sub2Detail: '<b>How it works:</b> The system continuously monitors keyboard and mouse activity. If no input is detected for the specified duration, the action triggers. If "Show alert before action" is enabled, mouse or keyboard interaction during the countdown warning automatically cancels the action.',
            ch3Sub2Example: '<b>Example:</b> If you want the computer to lock after 10 minutes of inactivity, set Action Type: "Lock computer", Trigger: "System Idle", Value: 10, Unit: Minutes.',
            ch3Sub2Warn: 'Creating multiple idle actions with the same duration may cause conflicts. The application will warn you in this case.',
            ch3Sub3: 'Every Day by Time',
            ch3Sub3Desc: 'Runs the action once per day at the selected time. The time is entered in 24-hour format (e.g. 23:00). The action will not run again on the same day; it triggers again at the same time the next day.',
            ch3Sub3Detail: '<b>How it works:</b> As long as the application is running, it checks for the specified time every day. When the time arrives, the action runs once and is marked as completed for that day. It becomes active again at the same time the next day. The action is not automatically removed from the list and continues to run daily.',
            ch3Sub3Example: '<b>Example:</b> If you want the computer to sleep every night at 00:30, set Action Type: "Sleep computer", Trigger: "Every day by hour", Time: 00:30.',
            // Ch5 - Settings
            ch5Title: 'Settings',
            ch5Icon: 'settings',
            ch5Intro: 'The Settings window allows you to customize the application\'s behavior. Below is a detailed description of each setting.',
            ch5ThemeTitle: 'Theme',
            ch5Theme: 'Changes the application\'s appearance. Three options are available: <b>Dark</b> (dark background, reduces eye strain), <b>Light</b> (white background, for bright environments), <b>System Default</b> (follows your Windows theme setting). Theme changes are applied instantly.',
            ch5LangTitle: 'Language',
            ch5Lang: 'Changes the application\'s interface language. Available languages: English, Turkish, German, French, Italian, and Russian. The <b>Automatic</b> option uses your Windows system language. A restart is required for the language change to take effect.',
            ch5LogsTitle: 'Record Logs',
            ch5Logs: 'When enabled, all actions performed by the application (locking, shutting down, sleeping, etc.) are recorded with date and time information. You can view, filter, and delete these records from the Logs (Log Viewer) window.',
            ch5StartupTitle: 'Start with Windows',
            ch5Startup: 'When enabled, the application starts automatically when Windows boots. The application runs in the background in the system tray (lower-right corner of the taskbar). It is recommended to enable this for actions that require continuous monitoring.',
            ch5TaskbarTitle: 'Run in Background When Closed',
            ch5Taskbar: 'When enabled, closing the application window (clicking the X button) does not close the application; it continues running in the system tray. It is recommended to keep this enabled for uninterrupted task execution. To completely close the application, use "Exit The Program" from the menu.',
            ch5CountdownTitle: 'Show Alert Before Action',
            ch5Countdown: 'When enabled, a countdown warning window is shown on screen before any action runs. In this window, you can skip the action, delete the action, or ignore the warning. For idle triggers, mouse or keyboard interaction during the warning automatically cancels the action.',
            ch5CountdownSecTitle: 'Countdown Seconds',
            ch5CountdownSec: 'Determines how many seconds the warning window is displayed. When this time expires, the action is automatically executed. <b>Recommended: 5-10 seconds.</b> Very short durations may not give you enough time to react; very long durations cause unnecessary waiting.',
            ch5ImportTitle: 'Import / Export',
            ch5Import: 'You can export your application settings as a <b>.conf</b> file (backup) or import a previously saved .conf file (restore). This feature is useful for transferring settings to another computer or backing up before a reset.',

            // Ch6 - Menus
            ch6Title: 'Menus and Toolbar',
            ch6Icon: 'menu',
            ch6Intro: 'The application provides various menus and toolbar buttons for quick access.',
            ch6Sub1: 'Toolbar',
            ch6Toolbar1: '<b>+ (Plus) Button:</b> Opens the new action creation window (modal). You enter the action type, trigger, and values from this window.',
            ch6Toolbar2: '<b>Pause Button:</b> Temporarily pauses all actions. You can select a pause duration from predefined options (30 min, 1 hour, 2 hours, 4 hours, until end of day) or enter a custom duration. When paused, an orange banner appears at the top.',
            ch6Toolbar3: '<b>Resume Button:</b> Resumes paused actions. You can also use the play button on the pause banner for the same action.',
            ch6Toolbar4: '<b>Search Box:</b> Instantly searches the action list by keyword. You can search across fields like trigger name, action type, duration/date, or creation date.',
            ch6Sub2: 'Hamburger Menu (Top Right)',
            ch6Menu1: 'Opens by clicking the three-line (≡) icon in the top-right corner. Menu items: <b>Actions</b> (main page), <b>Settings</b> (opens in separate window), <b>Log Viewer</b> (opens in separate window), <b>Help</b> (this page, opens in separate window), <b>About</b> (version info, opens in separate window), <b>Exit The Program</b> (completely closes the application).',
            ch6Sub3: 'Right-Click Menu (Action List)',
            ch6Menu2: 'Right-click on any row in the action list to open the context menu. Menu items: <b>Delete selected action</b> (confirmation required), <b>Delete all actions</b> (clears entire list), <b>Trigger usage help</b> (opens this help page).',
            ch6Sub4: 'Tab Filters',
            ch6Menu3: 'Use the tabs above the action list to filter actions by type: <b>All</b> (all actions), <b>Shutdown</b> (shutdown actions), <b>Restart</b> (restart), <b>Sleep</b> (sleep), <b>Lock</b> (lock), <b>Monitor</b> (monitor off), <b>Log Off</b> (log off). The active tab is highlighted in blue.',
            ch6Sub5: 'System Tray Icon',
            ch6Menu4: 'When the application runs in the background, an icon appears in the system tray (lower-right corner of the taskbar). Right-click this icon to access the quick menu: Create New Action, Settings, Show Logs, Help, and Exit The Program.',

            // Ch7 - Logs
            ch7Title: 'Logs and History',
            ch7Icon: 'description',
            ch7Intro: 'The Logs window shows a detailed history of all actions performed by the application. The "Record Logs" option must be enabled in Settings for this feature to work.',
            ch7Desc1: 'Each record includes the type of action performed (lock, shutdown, sleep, etc.) and the exact time it occurred. Application start and termination events are also recorded.',
            ch7FilterTitle: 'Filtering and Sorting',
            ch7Desc2: 'You can filter records by various criteria: action type (lock, shutdown, sleep, etc.), date range (start and end date). Sorting options: newest to oldest or oldest to newest. Use the <b>Reset filters</b> button to clear all filters.',
            ch7Desc3: 'The <b>Delete All Logs</b> button clears the entire log history. This action cannot be undone.',

            // Ch8 - Tips & FAQ
            ch8Title: 'Tips & Frequently Asked Questions',
            ch8Icon: 'lightbulb',
            ch8Tip1: '<b>Countdown</b> actions are automatically removed from the list after execution. Other trigger types remain in the list.',
            ch8Tip2: '<b>Every day by time</b> trigger runs only once per day. Even if you restart the application on the same day, it will not trigger again.',
            ch8Tip3: 'Use the <b>Pause/Resume</b> feature to temporarily stop all actions. When the pause duration expires, actions automatically resume.',
            ch8Tip4: 'For the <b>System Idle</b> trigger, moving the mouse or pressing a key during the countdown warning automatically cancels the action. This helps prevent accidentally triggered actions.',
            ch8Tip6: 'To keep actions running when the application window is closed, enable the <b>"Run in background when closed"</b> option in Settings.',
            ch8Tip7: 'It is recommended to regularly <b>export your settings</b> as a backup. This way you can quickly restore them in case of any issues.',
            ch8Faq1Q: '<b>Question:</b> Do actions run when the application is closed?',
            ch8Faq1A: '<b>Answer:</b> Yes, if "Run in background when closed" is enabled. Closing the window does not close the application; it continues running in the system tray. However, if you completely close it via "Exit The Program" from the menu, actions will stop.',
            ch8Faq2Q: '<b>Question:</b> Are actions lost when the computer restarts?',
            ch8Faq2A: '<b>Answer:</b> No. Actions are saved and restored when the application opens again. If "Start with Windows" is enabled, the application starts automatically with Windows.',
            ch8Faq3Q: '<b>Question:</b> How many actions can I create at the same time?',
            ch8Faq3A: '<b>Answer:</b> Up to 5 actions can be active at the same time. To add more, you need to delete an existing action first.'
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

        // TOC sidebar
        var tocHtml = '<div class="help-toc-sidebar" id="help-toc-sidebar">' +
            '<div class="help-toc-header">' +
                '<span class="help-toc-title">' + t.tocTitle + '</span>' +
                '<button class="help-toc-close" id="help-toc-close"><span class="mi">close</span></button>' +
            '</div>' +
            '<ul class="help-toc-list">';
        for (var i = 0; i < this._chapters.length; i++) {
            var ch = this._chapters[i];
            tocHtml += '<li><a class="help-toc-link" data-page="' + i + '">' +
                '<span class="help-toc-num">' + ch.num + '</span>' +
                '<span>' + ch.title + '</span></a></li>';
        }
        tocHtml += '</ul></div>';

        // TOC toggle button
        var toggleHtml = '<button class="help-toc-toggle" id="help-toc-toggle" title="' + t.tocTitle + '"><span class="mi">menu_book</span></button>';

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
        toggleHtml +
        tocHtml;
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
        var toggleBtn = document.getElementById('help-toc-toggle');
        var closeBtn = document.getElementById('help-toc-close');
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

        // TOC sidebar toggle
        function openSidebar() { if (sidebar) sidebar.classList.add('open'); }
        function closeSidebar() { if (sidebar) sidebar.classList.remove('open'); }

        if (toggleBtn) {
            toggleBtn.addEventListener('click', function () {
                if (sidebar && sidebar.classList.contains('open')) closeSidebar();
                else openSidebar();
            });
        }
        if (closeBtn) {
            closeBtn.addEventListener('click', closeSidebar);
        }

        // TOC links navigate to page
        for (var k = 0; k < tocLinks.length; k++) {
            tocLinks[k].addEventListener('click', function () {
                var pageIdx = parseInt(this.getAttribute('data-page'));
                searchInput.value = '';
                showPage(pageIdx);
                closeSidebar();
            });
        }
    }
};
