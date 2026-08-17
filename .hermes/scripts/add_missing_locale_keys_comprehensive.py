#!/usr/bin/env python3
"""
Fügt fehlende Locale-Keys systematisch zu allen Sprachdateien hinzu
Schritt 1: en.json aktualisieren
Schritt 2: Alle anderen Sprachen aktualisieren
"""

import json
from pathlib import Path

locales_dir = "/mnt/c/RagnaController/Locales"

# Fehlende Keys mit Übersetzungen für alle Sprachen
missing_keys_translations = {
    "Btn": {
        "Calibrate": {
            "en": "Calibrate Stick",
            "de": "Stick kalibrieren",
            "ar": "معايرة العصا",
            "es": "Calibrar el stick",
            "fr": "Calibrer la manette",
            "it": "Calibra lo stick",
            "pt": "Calibrar o stick",
            "nl": "Stick kalibreren",
            "pl": "Kalibruj stick",
            "da": "Kalibrér stick",
            "no": "Kalibrer stick",
            "sv": "Kalibrera stick",
            "fi": "Kalibroi nippa",
            "cs": "Kalibrovat stick",
            "sk": "Kalibrovať stick",
            "hu": "Stick kalibrálása",
            "ro": "Calibrare stick",
            "sl": "Kalibriraj stick",
            "sr": "Калибрација стика",
            "hr": "Kalibriraj stick",
            "uk": "Калібрування стіка",
            "bg": "Калибриране на стик",
            "el": "Καλιμπράρισμα ράβδου",
            "hi": "स्टिक कैलिब्रेट करें",
            "bn": "স্টিক ক্যালিব্রেট করুন",
            "ta": "ஸ்டிக் கலிப்ரேட் செய்யவும்",
            "ur": "اسٹیک کیلیبریٹ کریں",
            "fa": "کالیبراسیون استیک",
            "my": "စတစ် ကယ်လီဘရိတ်ပါ",
            "ms": "Kalibrasi stick",
            "tr": "Stick kalibre et",
            "ru": "Калибровка стика",
            "vi": "Calib Stick"
        }
    },
    "Lbl": {
        "Minutes": {
            "en": "Minutes",
            "de": "Minuten",
            "ar": "دقائق",
            "es": "Minutos",
            "fr": "Minutes",
            "it": "Minuti",
            "pt": "Minutos",
            "nl": "Minuten",
            "pl": "Minuty",
            "da": "Minutter",
            "no": "Minutter",
            "sv": "Minuter",
            "fi": "Minuuttia",
            "cs": "Minuty",
            "sk": "Minúty",
            "hu": "Percek",
            "ro": "Minute",
            "sl": "Minut",
            "sr": "Минута",
            "hr": "Minuta",
            "uk": "Хвилини",
            "bg": "Минута",
            "el": "Λεπτά",
            "hi": "मिनट",
            "bn": "মিনিট",
            "ta": "நிமிடங்கள்",
            "ur": "منٹ",
            "fa": "دقیقه",
            "my": "မိနစ်များ",
            "ms": "Minit",
            "tr": "Dakika",
            "ru": "Минуты",
            "vi": "Phút"
        }
    },
    "Tut": {
        "1_Title": {
            "en": "Getting Started",
            "de": "Erste Schritte",
            "ar": "البدء",
            "es": "Comenzando",
            "fr": "Commencer",
            "it": "Iniziare",
            "pt": "Começando",
            "nl": "Aan de slag",
            "pl": "Rozpoczęcie",
            "da": "Kom igang",
            "no": "Kom i gang",
            "sv": "Kom igång",
            "fi": "Alkuun",
            "cs": "Začínáme",
            "sk": "Začíname",
            "hu": "Kezdés",
            "ro": "Început",
            "sl": "Začetek",
            "sr": "Почетак",
            "hr": "Počinjanje",
            "uk": "Початок",
            "bg": "Започване",
            "el": "Έναρξη",
            "hi": "शुरुआत",
            "bn": "শুরু করা",
            "ta": "தொடக்கம்",
            "ur": "شروعات",
            "fa": "شروع",
            "my": "စတင်ခြင်း",
            "ms": "Memulakan",
            "tr": "Başlangıç",
            "ru": "Начало",
            "vi": "Bắt đầu"
        },
        "1_Desc": {
            "en": "Welcome to RagnaController! This tutorial will help you get started.",
            "de": "Willkommen bei RagnaController! Dieser Tutorial hilft Ihnen beim Start.",
            "ar": "مرحباً بك في RagnaController! سيساعدك هذا الدليل على البدء.",
            "es": "¡Bienvenido a RagnaController! Este tutorial te ayudará a comenzar.",
            "fr": "Bienvenue sur RagnaController! Ce tutoriel vous aidera à commencer.",
            "it": "Benvenuto su RagnaController! Questo tutorial ti aiuterà ad iniziare.",
            "pt": "Bem-vindo ao RagnaController! Este tutorial ajudará você a começar.",
            "nl": "Welkom bij RagnaController! Deze handleiding helpt je aan de slag.",
            "pl": "Witamy w RagnaController! Ten poradnik pomoże Ci zacząć.",
            "da": "Velkommen til RagnaController! Denne tutorial hjælper dig på vej.",
            "no": "Velkommen til RagnaController! Denne veiledningen hjelper deg på vei.",
            "sv": "Välkommen till RagnaController! Denna handledning hjälper dig komma igång.",
            "fi": "Tervetuloa RagnaControlleriin! Tämä opas auttaa sinua aloittamaan.",
            "cs": "Vítejte v RagnaController! Tento průvodce vám pomůže začít.",
            "sk": "Vitajte v RagnaController! Tento sprievodca vám pomôže začať.",
            "hu": "Üdvözöljük a RagnaControllerben! Ez a útmutató segít elindulni.",
            "ro": "Bun venit la RagnaController! Acest tutorial vă va ajuta să începeți.",
            "sl": "Dobrodošli v RagnaController! Ta vodnik vam bo pomagal začeti.",
            "sr": "Добродошли у РгнаЦонтроллер! Овај водич ће вам помоћи да почнете.",
            "hr": "Dobrodošli u RagnaController! Ovaj vodič će vam pomoći da počnete.",
            "uk": "Ласкаво просимо до RagnaController! Цей посібник допоможе вам почати.",
            "bg": "Добре дошли в RagnaController! Този ръководство ще ви помогне да започнете.",
            "el": "Καλώς ήρθατε στο RagnaController! Αυτή η οδηγία θα σας βοηθήσει να ξεκινήσετε.",
            "hi": "RagnaController में आपका स्वागत है! यह ट्यूटोरियल आपको शुरू करने में मदद करेगा।",
            "bn": "RagnaController-এ আপনাকে স্বাগতম! এই টিউটোরিয়ালটি আপনাকে শুরু করতে সাহায্য করবে।",
            "ta": "RagnaController வரவேற்கிறோம்! இந்த பயிற்சி உங்களைத் தொடங்க உதவும்.",
            "ur": "RagnaController میں آپ کا خیر مقدم! یہ ٹیوٹوریل آپ کو شروع کرنے میں مدد کرے گا۔",
            "fa": "به RagnaController خوش آمدید! این راهنما به شما در شروع کردن کمک می‌کند.",
            "my": "RagnaController ကို ကြိုဆိုပါတယ်! ဒီတော်လှန်ရေးက စတင်ဖို့ ကူညီပါလိမ့်မယ်။",
            "ms": "Selamat datang di RagnaController! Tutorial ini akan membantu anda bermula.",
            "tr": "RagnaController'ya Hoş Geldiniz! Bu eğitim başlamanıza yardımcı olacaktır.",
            "ru": "Добро пожаловать в RagnaController! Этот учебник поможет вам начать.",
            "vi": "Chào mừng đến với RagnaController! Bài hướng dẫn này sẽ giúp bạn bắt đầu."
        },
        "Btn_Next": {
            "en": "Next",
            "de": "Weiter",
            "ar": "التالي",
            "es": "Siguiente",
            "fr": "Suivant",
            "it": "Prossimo",
            "pt": "Próximo",
            "nl": "Volgende",
            "pl": "Następny",
            "da": "Næste",
            "no": "Neste",
            "sv": "Nästa",
            "fi": "Seuraava",
            "cs": "Další",
            "sk": "Ďalší",
            "hu": "Következő",
            "ro": "Următor",
            "sl": "Naslednji",
            "sr": "Следећи",
            "hr": "Sljedeći",
            "uk": "Наступний",
            "bg": "Следващ",
            "el": "Επόμενο",
            "hi": "अगला",
            "bn": "পরবর্তী",
            "ta": "அடுத்தது",
            "ur": "اگلے",
            "fa": "بعدی",
            "my": "နောက်တစ်ခု",
            "ms": "Seterusnya",
            "tr": "Sonraki",
            "ru": "Следующий",
            "vi": "Tiếp theo"
        },
        "Btn_Prev": {
            "en": "Previous",
            "de": "Zurück",
            "ar": "السابق",
            "es": "Anterior",
            "fr": "Précédent",
            "it": "Precedente",
            "pt": "Anterior",
            "nl": "Vorige",
            "pl": "Poprzedni",
            "da": "Forrige",
            "no": "Forrige",
            "sv": "Föregående",
            "fi": "Edellinen",
            "cs": "Předchozí",
            "sk": "Predchádzajúci",
            "hu": "Előző",
            "ro": "Anterior",
            "sl": "Prejšnji",
            "sr": "Претходни",
            "hr": "Prethodni",
            "uk": "Попередній",
            "bg": "Предишен",
            "el": "Προηγούμενο",
            "hi": "पिछला",
            "bn": "আগামী",
            "ta": "முந்தையது",
            "ur": "پچھلا",
            "fa": "قبلی",
            "my": "ရှေ့တစ်ခု",
            "ms": "Sebelumnya",
            "tr": "Önceki",
            "ru": "Предыдущий",
            "vi": "Trước"
        }
    },
    "Tooltip": {
        "DiscordRPC": {
            "en": "Enables Discord Rich Presence to show your game status",
            "de": "Aktiviert Discord Rich Presence zum Anzeigen Ihres Spielstatus",
            "ar": "تفعيل Discord Rich Presence لعرض حالة اللعبة",
            "es": "Habilita la Presencia Rica de Discord para mostrar tu estado de juego",
            "fr": "Active la présence riche de Discord pour afficher votre statut de jeu",
            "it": "Abilita la Presenza Ricca di Discord per mostrare lo stato del gioco",
            "pt": "Habilita a Presença Rica do Discord para mostrar seu status de jogo",
            "nl": "Schakelt Discord Rich Presence in om uw spelstatus weer te geven",
            "pl": "Włącza bogatą obecność Discord, aby wyświetlić swój status gry",
            "da": "Aktiverer Discord Rigtig Præsence for at vise din spilstatus",
            "no": "Aktiverer Discord rik tilstedeværelse for å vise spillstatusen din",
            "sv": "Aktiverar Discord rik närvaro för att visa ditt spelstatus",
            "fi": "Ota Discordin rikas läsnäolomuoto käyttöön näytelläksesi pelisi tilaa",
            "cs": "Povolit bohatou přítomnost Discordu pro zobrazení stavu hry",
            "sk": "Povoliť bohatú prítomnosť Discordu na zobrazenie stavu hry",
            "hu": "Engedélyezi a Discord gazdag jelenlétet a játék állapotának megjelenítéséhez",
            "ro": "Activează Prezența Richă de Discord pentru a afișa statusul jocului tău",
            "sl": "Omogoči bogato prisotnost Discorda za prikaz stanja igre",
            "sr": "Омогућава Богату присуство Дискорда за приказивање статуса игре",
            "hr": "Omogući bogatu prisutnost Discorda za prikazivanje statusa igre",
            "uk": "Увімкнути багатий статус Discord для відображення стану гри",
            "bg": "Активиране на Богато присъствие на Discord за показване на състоянието на играта",
            "el": "Ενεργοποίηση Πλούσιας Παρουσίας Discord για εμφάνιση κατάστασης παιχνιδιού",
            "hi": "डिस्कोर्ड रीच प्रेजेंस को सक्रिय करें अपने गेम स्थिति को दिखाने के लिए",
            "bn": "ডিসকর্ড রিচ প্রেজেন্স সক্রিয় করুন আপনার গেমের অবস্থা দেখানোর জন্য",
            "ta": "டிகோர்ட் ரிச் பிரென்ஸ் ஓட்டியை உங்கள் விளையாட்டு நிலையைக் காண்பிக்க",
            "ur": "ڈسکورڈ ریچ پریزنس کو اپنے گیم اسٹیٹس دکھانے کے لیے فعال کریں",
            "fa": "فعال کردن حضور غنی دیسکورد برای نمایش وضعیت بازی شما",
            "my": "ဒစ်စကော့ရစ် ပြင်ဆင်မှုကို သင့်ဂိမ်းအခြေအနေကို ပြသရန် အသုံးပြုပါ",
            "ms": "Mengaktifkan Kehadiran Kaya Discord untuk menunjukkan status permainan anda",
            "tr": "Discord Zengin Varlığını oyun durumunuzu göstermek için etkinleştirir",
            "ru": "Включает Богатое присутствие Discord для отображения статуса игры",
            "vi": "Bật Discord Rich Presence để hiển thị trạng thái trò chơi của bạn"
        },
        "HapticMetronome": {
            "en": "Provides rhythmic haptic feedback during combat",
            "de": "Bietet rhythmischen haptischen Feedback während des Kampfes",
            "ar": "يوفر تغذية راجعة هaptic إيقاعي أثناء القتال",
            "es": "Proporciona retroalimentación háptica rítmica durante el combate",
            "fr": "Fournit un retour haptique rythmique pendant le combat",
            "it": "Fornisce feedback tattico ritmico durante il combattimento",
            "pt": "Fornece feedback háptico rítmico durante o combate",
            "nl": "Biedt ritmisch haptisch feedback tijdens gevecht",
            "pl": "Dostarcza rytmiczne dotykowe sprzężenie zwrotne podczas walki",
            "da": "Leverer rytmisk haptisk feedback under kamp",
            "no": "Gir rytmisk haptisk tilbakemelding under kamp",
            "sv": "Ger rytmisk haptisk feedback under strid",
            "fi": "Tarjoaa rytmistä haptista palautetta taistelun aikana",
            "cs": "Poskytuje rytmické haptické zpětné vazby během boje",
            "sk": "Poskytuje rytmické haptické spätné väzby počas bitky",
            "hu": "Ritmusos haptikus visszajelzést nyújt a harc során",
            "ro": "Furnizează feedback haptic ritmic în timpul luptei",
            "sl": "Nudi ritmično taktilno povratno informiranje med bitko",
            "sr": "Омогућава ритмички тактилни повратне информације током борбе",
            "hr": "Nudi ritmično taktilno povratne informacije tijekom borbe",
            "uk": "Надає ритмічний тактильний зворотний зв'язок під час бою",
            "bg": "Предоставя ритмичен тактилен обратна връзка по време на битка",
            "el": "Παρέχει ρυθμική τακτική ανατροφοδότηση κατά τη διάρκεια της μάχης",
            "hi": "युद्ध के दौरान ध्वनिक हaptिक फीडबैक प्रदान करता है",
            "bn": "যুদ্ধের সময় ধ্বনিত হaptিক ফিডব্যাক প্রদান করে",
            "ta": "போர் போது இசை ரீதியான ஹப்டிக் பின்னூட்டத்தை வழங்குகிறது",
            "ur": "جنگ کے دوران ریتمک ہپٹک فیدبیک فراہم کرتا ہے",
            "fa": "در طول نبرد بازخورد لمسی ریتمیک ارائه می‌دهد",
            "my": "ပွဲတိုင်းတွင် ချိန်ညှိထားသော ဟပ်တစ် ပြန်လည်ပြသမှုကို ပေးပါသည်",
            "ms": "Menyediakan maklum balas haptik ritmik semasa pertempuran",
            "tr": "Savaş sırasında ritmik haptik geri bildirim sağlar",
            "ru": "Предоставляет ритмическую тактильную обратную связь во время боя",
            "vi": "Cung cấp phản hồi xúc giác nhịp điệu trong khi chiến đấu"
        },
        "SmartStandby": {
            "en": "Automatically saves and suspends when idle",
            "de": "Speichert und pausiert automatisch bei Inaktivität",
            "ar": "يحفظ ويقيد تلقائياً عند الخمول",
            "es": "Guarda y suspende automáticamente cuando está inactivo",
            "fr": "Enregistre et met en pause automatiquement au repos",
            "it": "Salva e sospende automaticamente quando è inattivo",
            "pt": "Salva e pausa automaticamente quando está inativo",
            "nl": "Bewaart en pauzeert automatisch bij inactiviteit",
            "pl": "Automatycznie zapisuje i zawiesza w trybie bezczynności",
            "da": "Gemmer og suspenderer automatisk når du er inaktiv",
            "no": "Lagrer og suspenderer automatisk når du er inaktiv",
            "sv": "Sparar och pausar automatiskt när den är inaktiv",
            "fi": "Tallentaa ja keskeyttää automaattisesti kun olet inaktiivinen",
            "cs": "Automaticky ukládá a pozastavuje při nečinnosti",
            "sk": "Automaticky ukladá a pozastavuje pri nečinnosti",
            "hu": "Automatikusan ment és felfüggeszt inaktív állapotban",
            "ro": "Salvează automat și suspendă când este inactiv",
            "sl": "Samodejno shrani in obdobje počitka",
            "sr": "Аутоматски се чува и обуставља када је неактивно",
            "hr": "Automatski sprema i obustavlja kada je neaktivan",
            "uk": "Автоматично зберігає і призупиняє, коли ви бездіяльні",
            "bg": "Автоматично запазва и паузира при бездействие",
            "el": "Αυτόματα αποθηκεύει και αναστέλλει όταν είναι αδρανές",
            "hi": "निष्क्रिय होने पर स्वचालित रूप से सहेजता और निलंबित करता है",
            "bn": "নিষ্ক্রিয় হলে স্বয়ংক্রিয়ভাবে সংরক্ষণ এবং নিষিদ্ধ করে",
            "ta": "செயலிழந்த போது தானாகவே சேமிக்கவும் நிறுத்தவும்",
            "ur": "غیر فعال ہونے پر خودکار طریقے سے محفوظ کرتا ہے اور عارضی طور پر رکھتا ہے",
            "fa": "به صورت خودکار هنگام بی‌کاری ذخیره و تعلیق می‌کند",
            "my": "မလုပ်ဆောင်ဘဲ ရှိနေသောအခါ အလိုအလျောက် သိမ်းဆည်းပြီး ရပ်တန့်ပါသည်",
            "ms": "Menyimpan dan menangguhkan secara automatik apabila tidak aktif",
            "tr": "Boşta olduğunda otomatik olarak kaydeder ve askıya alır",
            "ru": "Автоматически сохраняет и приостанавливает в режиме ожидания",
            "vi": "Tự động lưu và tạm dừng khi không hoạt động"
        },
        "VoiceAnnouncer": {
            "en": "Announces events via text-to-speech",
            "de": "Gibt Ereignisse über Text-zu-Sprache bekannt",
            "ar": "ينشر الأحداث عبر تحويل النص إلى كلام",
            "es": "Anuncia eventos mediante texto a voz",
            "fr": "Annonce les événements via la synthèse vocale",
            "it": "Annuncia eventi tramite sintesi vocale",
            "pt": "Anuncia eventos via texto para fala",
            "nl": "Kondigt gebeurtenissen aan via tekst-naar-spraak",
            "pl": "Poinformuje o zdarzeniach za pomocą mowy syntetycznej",
            "da": "Melder hændelser via tekst-til-tale",
            "no": "Melder hendelser via tekst-til-tale",
            "sv": "Meddelar händelser via text-till-tal",
            "fi": "Ilmoittaa tapahtumia tekstin puheeksi muuntamalla",
            "cs": "Oznamuje události pomocí textu na řeč",
            "sk": "Oznamuje udalosti pomocou textu na reč",
            "hu": "Hirdeti eseményeket szövegből beszédre",
            "ro": "Anunță evenimente prin sinteză vocală",
            "sl": "Obvesti dogodke prek besede iz besedila",
            "sr": "Обавештава догађаје кроз текст у говор",
            "hr": "Obavještava događaje putem teksta u govor",
            "uk": "Повідомляє події через текст у мову",
            "bg": "Анонсира събития чрез текст към реч",
            "el": "Ανακοινώνει γεγονότα μέσω κειμένου σε ομιλία",
            "hi": "टेक्स्ट-से-वॉइस के माध्यम से घटनाओं की घोषणा करता है",
            "bn": "টেক্সট থেকে কথা বলার মাধ্যমে ঘটনাগুলি ঘোষণা করে",
            "ta": "உரைக்கு உரை மூலம் நிகழ்வுகளை அறிவிக்கிறது",
            "ur": "متن سے بولنے کے ذریعے واقعات کا اعلان کرتا ہے",
            "fa": "رویدادها را از طریق متن به گفتار اعلام می‌کند",
            "my": "စာသားမှ အသံသို့ ပြောင်းလဲခြင်းဖြင့် ဖြစ်စဉ်များကို ကြေညာပါသည်",
            "ms": "Mengumumkan peristiwa melalui teks ke suara",
            "tr": "Metin ile konuşma yoluyla olayları duyurur",
            "ru": "Анонсирует события через текст в речь",
            "vi": "Thông báo sự kiện qua văn bản thành giọng nói"
        }
    },
    "Settings": {
        "DiscordRPC": {
            "en": "Discord Rich Presence",
            "de": "Discord Rich Presence",
            "ar": "حضور غني Discord",
            "es": "Presencia Rica de Discord",
            "fr": "Présence Riche de Discord",
            "it": "Presenza Ricca di Discord",
            "pt": "Presença Rica do Discord",
            "nl": "Discord Rich Presence",
            "pl": "Bogata Obecność Discord",
            "da": "Discord Rigtig Præsence",
            "no": "Discord rik tilstedeværelse",
            "sv": "Discord rik närvaro",
            "fi": "Discordin rikas läsnäolomuoto",
            "cs": "Discord bohatá přítomnost",
            "sk": "Discord bohatá prítomnosť",
            "hu": "Discord gazdag jelenlét",
            "ro": "Prezență Richă de Discord",
            "sl": "Bogata prisotnost Discorda",
            "sr": "Богата присуство Дискорда",
            "hr": "Bogata prisutnost Discorda",
            "uk": "Багатий статус Discord",
            "bg": "Богато присъствие на Discord",
            "el": "Πλούσια Παρουσία Discord",
            "hi": "डिस्कोर्ड रीच प्रेजेंस",
            "bn": "ডিসকর্ড রিচ প্রেজেন্স",
            "ta": "டிகோர்ட் ரிச் பிரென்ஸ்",
            "ur": "ڈسکورڈ ریچ پریزنس",
            "fa": "حضور غنی دیسکورد",
            "my": "ဒစ်စကော့ရစ် ပြင်ဆင်မှု",
            "ms": "Kehadiran Kaya Discord",
            "tr": "Discord Zengin Varlık",
            "ru": "Богатое присутствие Discord",
            "vi": "Discord Rich Presence"
        },
        "HapticMetronome": {
            "en": "Haptic Metronome",
            "de": "Haptischer Metronom",
            "ar": "المترونوم اللمسي",
            "es": "Metronómano Háptico",
            "fr": "Métronome Haptique",
            "it": "Metronomo Tattico",
            "pt": "Metronômetro Háptico",
            "nl": "Haptische Metronoom",
            "pl": "Dotykowy Metronom",
            "da": "Haptisk Metronom",
            "no": "Haptisk metronom",
            "sv": "Haptisk metronom",
            "fi": "Haptinen metronomi",
            "cs": "Dotykový metronom",
            "sk": "Dotykový metronóm",
            "hu": "Haptikus metronom",
            "ro": "Metronom Haptic",
            "sl": "Taktilni metronom",
            "sr": "Тактилни метроном",
            "hr": "Taktilni metronom",
            "uk": "Тактильний метроном",
            "bg": "Тактилен метроном",
            "el": "Τακτικό μετρονόμο",
            "hi": "हैप्टिक मेट्रोनोम",
            "bn": "হ্যাপটিক মেট্রোনোম",
            "ta": "ஹப்டிக் மெட்ரோனோம்",
            "ur": "ہپٹک میٹرونوم",
            "fa": "مترونوم لمسی",
            "my": "ဟပ်တစ် မီထရိုနိုမ",
            "ms": "Haptik Metronom",
            "tr": "Haptik Metronom",
            "ru": "Тактильный метроном",
            "vi": "Haptical Metronome"
        },
        "SmartStandby": {
            "en": "Smart Standby",
            "de": "Intelligenter Standby",
            "ar": "وضع الانتظار الذكي",
            "es": "Modo de Espera Inteligente",
            "fr": "Mode Veille Intelligent",
            "it": "Modalità Standby Intelligente",
            "pt": "Modo de Standby Inteligente",
            "nl": "Slimme Standby",
            "pl": "Inteligentny Tryb Oczekiwania",
            "da": "Smart Standby",
            "no": "Smart standby",
            "sv": "Smart standby",
            "fi": "Älykäs odotustila",
            "cs": "Chytrý režim čekání",
            "sk": "Chytrý režim čakania",
            "hu": "Okos pihenő mód",
            "ro": "Mod Standy Inteligent",
            "sl": "Pametno počitek",
            "sr": "Паметан пауза",
            "hr": "Pametni standby",
            "uk": "Розумний режим очікування",
            "bg": "Умен режим на изчакване",
            "el": "Έξυπνη κατάσταση αναμονής",
            "hi": "स्मार्ट स्टैंडीबाय",
            "bn": "স্মার্ট স্ট্যান্ডবাই",
            "ta": "சமர்ப்பண நிலை",
            "ur": "سمارٹ اسٹینڈ بائی",
            "fa": "وضعیت آماده‌ی هوشمند",
            "my": "စောင့်ဆိုင်းခြင်း အဆင့်",
            "ms": "Modo Standby Pintar",
            "tr": "Akıllı Bekleme",
            "ru": "Умный режим ожидания",
            "vi": "Chế độ Standby Thông minh"
        },
        "StickCalibration": {
            "en": "Stick Calibration",
            "de": "Stick-Kalibrierung",
            "ar": "معايرة العصا",
            "es": "Calibración del Stick",
            "fr": "Calibration de la manette",
            "it": "Calibrazione dello Stick",
            "pt": "Calibração do Stick",
            "nl": "Stick Kalibratie",
            "pl": "Kalibracja sticka",
            "da": "Stick Kalibrering",
            "no": "Stick kalibrering",
            "sv": "Stick kalibrering",
            "fi": "Nippakalibrointi",
            "cs": "Kalibrace sticku",
            "sk": "Kalibrácia sticku",
            "hu": "Stick kalibrálás",
            "ro": "Calibrare Stick",
            "sl": "Kalibracija stika",
            "sr": "Калибрација стика",
            "hr": "Kalibracija stika",
            "uk": "Калібрування стіка",
            "bg": "Калибриране на стик",
            "el": "Καλιμπράρισμα ράβδου",
            "hi": "स्टिक कैलिब्रेशन",
            "bn": "স্টিক ক্যালিব্রেশন",
            "ta": "ஸ்டிக் கலிப்ரேஷன்",
            "ur": "اسٹیک کیلیبریشن",
            "fa": "کالیبراسیون استیک",
            "my": "စတစ် ကယ်လီဘရိတ်",
            "ms": "Kalibrasi Stick",
            "tr": "Stick Kalibrasyonu",
            "ru": "Калибровка стика",
            "vi": "Calib Stick"
        },
        "TutorialBtn": {
            "en": "Show Tutorial",
            "de": "Tutorial anzeigen",
            "ar": "إظهار الدليل",
            "es": "Mostrar Tutorial",
            "fr": "Afficher le tutoriel",
            "it": "Mostra Tutorial",
            "pt": "Mostrar Tutorial",
            "nl": "Toon Handleiding",
            "pl": "Pokaż Poradnik",
            "da": "Vis Tutorial",
            "no": "Vis veiledning",
            "sv": "Vis handledning",
            "fi": "Näytä opas",
            "cs": "Zobrazit průvodce",
            "sk": "Zobraziť sprievodcu",
            "hu": "Mutatás útmutató",
            "ro": "Arată Tutorial",
            "sl": "Pokaži vodnik",
            "sr": "Прикажи водич",
            "hr": "Prikaži vodič",
            "uk": "Показати посібник",
            "bg": "Покажи ръководство",
            "el": "Εμφάνιση Οδηγίας",
            "hi": "ट्यूटोरियल दिखाएं",
            "bn": "টিউটোরিয়াল দেখান",
            "ta": "பயிற்சியைக் காட்டவும்",
            "ur": "ٹیوٹوریل دکھائیں",
            "fa": "نمایش راهنما",
            "my": "တော်လှန်ရေးကို ပြသပါ",
            "ms": "Paparan Tutorial",
            "tr": "Eğitimi Göster",
            "ru": "Показать учебник",
            "vi": "Hiển thị hướng dẫn"
        },
        "VoiceAnnouncer": {
            "en": "Voice Announcer",
            "de": "Sprach-Ankündiger",
            "ar": "المعلن الصوتي",
            "es": "Anunciador de Voz",
            "fr": "Annonciateur Vocal",
            "it": "Annunciatore Vocale",
            "pt": "Anunciador de Voz",
            "nl": "Stem Aankondiger",
            "pl": "Mówca głosowy",
            "da": "Stemme Announcer",
            "no": "Stemme annonsør",
            "sv": "Röstmeddelare",
            "fi": "Äänenvoittaja",
            "cs": "Hlasový oznamovač",
            "sk": "Hlasový oznamovateľ",
            "hu": "Hanghirdető",
            "ro": "Anunțător Vocal",
            "sl": "Glasovni obvestilec",
            "sr": "Гласни обавештавао",
            "hr": "Glasovni najavitelj",
            "uk": "Голосовий оголошувач",
            "bg": "Гласов анонсатор",
            "el": "Φωνητικός Ανακοινωτής",
            "hi": "आवाज घोषक",
            "bn": "অডিও ঘোষক",
            "ta": "குரல் அறிவிப்பாளர்",
            "ur": "آواز کا اعلان کنندہ",
            "fa": "اعلام‌کننده صوتی",
            "my": "အသံ ကြေညာသူ",
            "ms": "Pengumuman Suara",
            "tr": "Ses Duyurucu",
            "ru": "Голосовой анонсатор",
            "vi": "Giọng nói thông báo"
        }
    }
}

# Bước 1: Cập nhật en.json
print("=" * 80)
print("BƯỚC 1: Cập nhật en.json")
print("=" * 80)
print()

en_file = Path(locales_dir) / "en.json"
with open(en_file, 'r', encoding='utf-8') as f:
    en_data = json.load(f)

# Thêm các key thiếu vào en.json
for category, keys in missing_keys_translations.items():
    if category not in en_data:
        en_data[category] = {}
    for key_name, translations in keys.items():
        en_data[category][key_name] = translations["en"]

# Lưu lại en.json
with open(en_file, 'w', encoding='utf-8') as f:
    json.dump(en_data, f, ensure_ascii=False, indent=2)

print(f"✓ Đã cập nhật {en_file.name}")
print()

# Bước 2: Cập nhật tất cả các ngôn ngữ khác
print("=" * 80)
print("BƯỚC 2: Cập nhật tất cả các ngôn ngữ khác")
print("=" * 80)
print()

locales_files = list(Path(locales_dir).glob("*.json"))
for locale_file in locales_files:
    if locale_file.name == "en.json":
        continue
    
    try:
        with open(locale_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        lang = locale_file.stem
        
        # Thêm các key thiếu vào ngôn ngữ này
        for category, keys in missing_keys_translations.items():
            if category not in data:
                data[category] = {}
            for key_name, translations in keys.items():
                if lang == "en":
                    value = translations["en"]
                elif lang == "de":
                    value = translations["de"]
                elif lang == "ar":
                    value = translations["ar"]
                elif lang == "es":
                    value = translations["es"]
                elif lang == "fr":
                    value = translations["fr"]
                elif lang == "it":
                    value = translations["it"]
                elif lang == "pt":
                    value = translations["pt"]
                elif lang == "nl":
                    value = translations["nl"]
                elif lang == "pl":
                    value = translations["pl"]
                elif lang == "da":
                    value = translations["da"]
                elif lang == "no":
                    value = translations["no"]
                elif lang == "sv":
                    value = translations["sv"]
                elif lang == "fi":
                    value = translations["fi"]
                elif lang == "cs":
                    value = translations["cs"]
                elif lang == "sk":
                    value = translations["sk"]
                elif lang == "hu":
                    value = translations["hu"]
                elif lang == "ro":
                    value = translations["ro"]
                elif lang == "sl":
                    value = translations["sl"]
                elif lang == "sr":
                    value = translations["sr"]
                elif lang == "hr":
                    value = translations["hr"]
                elif lang == "uk":
                    value = translations["uk"]
                elif lang == "bg":
                    value = translations["bg"]
                elif lang == "el":
                    value = translations["el"]
                elif lang == "hi":
                    value = translations["hi"]
                elif lang == "bn":
                    value = translations["bn"]
                elif lang == "ta":
                    value = translations["ta"]
                elif lang == "ur":
                    value = translations["ur"]
                elif lang == "fa":
                    value = translations["fa"]
                elif lang == "my":
                    value = translations["my"]
                elif lang == "ms":
                    value = translations["ms"]
                elif lang == "tr":
                    value = translations["tr"]
                elif lang == "ru":
                    value = translations["ru"]
                elif lang == "vi":
                    value = translations["vi"]
                else:
                    value = translations["en"]
                
                data[category][key_name] = value
        
        # Lưu lại file ngôn ngữ này
        with open(locale_file, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        print(f"✓ Đã cập nhật {locale_file.name}")
        
    except Exception as e:
        print(f"✗ Lỗi khi cập nhật {locale_file.name}: {e}")

print()
print("Tất cả các file ngôn ngữ đã được cập nhật!")
