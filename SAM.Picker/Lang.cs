/* SAM Auto 9.0 — lightweight TR/EN localization
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace SAM.Picker
{
    /// <summary>
    /// Tiny localization helper with a language switch (TR/EN).
    /// Choice is persisted to %APPDATA%\SAM-Auto\settings.ini.
    /// </summary>
    internal static class Lang
    {
        public const string English = "en";
        public const string Turkish = "tr";

        public static event Action Changed;

        public static string Current { get; private set; } = Turkish;

        /// <summary>Selected theme: "dark" or "light".</summary>
        public static string Theme { get; private set; } = "dark";

        public static string T(string key)
        {
            var dict = Current == Turkish ? _tr : _en;
            if (dict.TryGetValue(key, out var value) == true)
            {
                return value;
            }

            return _en.TryGetValue(key, out value) == true ? value : key;
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        public static void SetLanguage(string code)
        {
            if (code != English && code != Turkish)
            {
                return;
            }

            if (Current == code)
            {
                return;
            }

            Current = code;
            Save();
            Changed?.Invoke();
        }

        public static void SetTheme(string theme)
        {
            if (theme != "dark" && theme != "light")
            {
                return;
            }

            if (Theme == theme)
            {
                return;
            }

            Theme = theme;
            Save();
        }

        #region Duration formatting

        /// <summary>Formats a duration like "3m 20s" / "3dk 20sn" using the active language.</summary>
        public static string FormatDuration(TimeSpan ts)
        {
            if (ts < TimeSpan.FromSeconds(1))
            {
                ts = TimeSpan.FromSeconds(1);
            }

            if (ts.TotalHours >= 1)
            {
                return F("dur.hm", (int)ts.TotalHours, ts.Minutes);
            }

            if (ts.TotalMinutes >= 1)
            {
                return F("dur.ms", (int)ts.TotalMinutes, ts.Seconds);
            }

            return F("dur.s", (int)Math.Ceiling(ts.TotalSeconds));
        }

        #endregion

        #region Persistence

        private static string SettingsDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SAM-Auto");

        private static string SettingsFile => Path.Combine(SettingsDir, "settings.ini");

        public static string DataDirectory
        {
            get
            {
                try
                {
                    Directory.CreateDirectory(SettingsDir);
                }
                catch
                {
                    // best effort
                }

                return SettingsDir;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsFile) == false)
                {
                    return;
                }

                foreach (var line in File.ReadAllLines(SettingsFile))
                {
                    if (line.StartsWith("lang=", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var code = line.Substring(5).Trim();
                        if (code == English || code == Turkish)
                        {
                            Current = code;
                        }
                    }
                    else if (line.StartsWith("theme=", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var theme = line.Substring(6).Trim();
                        if (theme == "dark" || theme == "light")
                        {
                            Theme = theme;
                        }
                    }
                }
            }
            catch
            {
                // corrupted settings should never break startup
            }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                File.WriteAllText(
                    SettingsFile,
                    "lang=" + Current + Environment.NewLine +
                    "theme=" + Theme + Environment.NewLine);
            }
            catch
            {
                // best effort
            }
        }

        #endregion

        #region Dictionaries

        private static readonly Dictionary<string, string> _en = new()
        {
            ["dur.hm"] = "{0}h {1}m",
            ["dur.ms"] = "{0}m {1}s",
            ["dur.s"] = "{0}s",

            ["picker.title"] = "Steam Achievement Manager Auto 9.0 | Pick a game... Any game...",
            ["picker.refresh"] = "Refresh Games",
            ["picker.addGame"] = "Add Game",
            ["picker.unlockAll"] = "🔓 Unlock All",
            ["picker.unlockSelected"] = "Unlock Selected",
            ["picker.freeGames"] = "👻 Free Games",
            ["picker.filter"] = "Filter",
            ["picker.theme"] = "🎨 Theme",
            ["picker.themeDark"] = "Dark (Ghost)",
            ["picker.themeLight"] = "Light",
            ["picker.showGames"] = "Show &games",
            ["picker.showDemos"] = "Show &demos",
            ["picker.showMods"] = "Show &mods",
            ["picker.showJunk"] = "Show &junk",
            ["picker.displaying"] = "Displaying {0} games. Total {1} games.",
            ["picker.downloadingList"] = "Downloading game list...",
            ["picker.checkingOwnership"] = "Checking game ownership...",
            ["picker.downloadingIcons"] = "Downloading {0} game icons...",

            ["picker.sort"] = "⇅ Sort",
            ["picker.sortName"] = "By name",
            ["picker.sortLockedFirst"] = "Locked first",
            ["picker.sortUnlockedFirst"] = "Unlocked first",
            ["picker.scanningAchievements"] = "Scanning local achievement data...",
            ["picker.pause"] = "⏸ Pause",
            ["picker.resume"] = "▶ Resume",
            ["picker.stop"] = "⏹ Stop",
            ["picker.paused"] = "Paused — press Resume to continue.",

            ["unlock.none"] = "No games found.",
            ["unlock.selectFirst"] = "Select one or more games first (Ctrl+Click).",
            ["unlock.cancelled"] = "Stopped. {0}/{1} games completed.",
            ["unlock.confirmAllTitle"] = "Confirm Unlock All",
            ["unlock.confirmAll"] = "Unlock ALL achievements for {0} games?\n\nEstimated time: ~{1}.\nThis cannot be undone.",
            ["unlock.confirmSelectedTitle"] = "Confirm Unlock Selected",
            ["unlock.confirmSelected"] = "Unlock ALL achievements for {0} selected game(s)?\n\nEstimated time: ~{1}.\nThis cannot be undone.",
            ["unlock.progress"] = "[{0}/{1}] Unlocking: {2}",
            ["unlock.progressEta"] = "[{0}/{1}] Unlocking: {2}  —  ~{3} left",
            ["unlock.failedStart"] = "[{0}/{1}] Failed to start for: {2} ({3})",
            ["unlock.done"] = "Done. Unlocked achievements for {0} games.",
            ["unlock.doneTitle"] = "Done",
            ["unlock.doneBody"] = "Unlock process finished.\nDetails: SAM_AutoUnlock.log",
            ["unlock.info"] = "Info",
            ["unlock.error"] = "Error",

            ["fg.title"] = "Free Games — claim free Steam content",
            ["fg.header"] = "Free Games",
            ["fg.subtitle"] = "Adds free games, DLC, software and more to your account. One-time QR sign-in with the Steam mobile app.",
            ["fg.signin"] = "1. Steam sign-in",
            ["fg.connectQr"] = "Sign in with QR code",
            ["fg.disconnect"] = "Disconnect",
            ["fg.rememberMe"] = "Remember me on this PC",
            ["fg.notConnected"] = "Not connected.",
            ["fg.connecting"] = "Connecting to Steam...",
            ["fg.awaitingScan"] = "Scan the QR code with the Steam mobile app...",
            ["fg.loggedInAs"] = "Signed in as: {0}",
            ["fg.loginFailed"] = "Sign-in failed: {0}",
            ["fg.tokenFailed"] = "Saved session expired — please scan the QR code again.",
            ["fg.categories"] = "2. What should be added?",
            ["fg.catGames"] = "Games",
            ["fg.catDlc"] = "DLC",
            ["fg.catSoftware"] = "Software",
            ["fg.catDemos"] = "Demos",
            ["fg.catMods"] = "Mods",
            ["fg.fetch"] = "Fetch free list",
            ["fg.fetching"] = "Fetching free {0} list... page {1}",
            ["fg.fetchDone"] = "{0}: {1} items",
            ["fg.totalFound"] = "Total: {0} free items found. Estimated claim time: ~{1} (Valve rate limits may extend this).",
            ["fg.claim"] = "Add all to my account",
            ["fg.cancel"] = "Cancel",
            ["fg.confirmClaimTitle"] = "Add free content",
            ["fg.confirmClaim"] = "{0} free items will be added to your account.\nEstimated time: ~{1} (Valve limits ~50 new additions per hour; the tool waits automatically).\n\nContinue?",
            ["fg.claimStart"] = "Starting... {0} items queued.",
            ["fg.claimBatch"] = "Batch {0}/{1} — Added: {2} | Already owned: {3} | Failed: {4} — ~{5} left",
            ["fg.rateLimited"] = "Valve rate limit reached — waiting {0}... (this is normal, the tool will continue automatically)",
            ["fg.claimDone"] = "Done! Newly added: {0} | Already owned: {1} | Failed: {2}",
            ["fg.claimCancelled"] = "Cancelled. Newly added: {0} | Already owned: {1} | Failed: {2}",
            ["fg.needLogin"] = "Please sign in first (step 1).",
            ["fg.needFetch"] = "Fetch the free list first (step 2).",
            ["fg.fetchError"] = "Failed to fetch the list: {0}",
            ["fg.log"] = "Log",
            ["fg.note"] = "Note: permanently-free (F2P) content and 100%-off promos are claimed. The Steam client does not need to run for this — a separate connection is used.",
            ["fg.catDeals"] = "100% off deals",
            ["fg.security"] = "🔒 Security & privacy",
            ["fg.securityTitle"] = "Security & privacy",
            ["fg.securityBody"] = "• Your password is NEVER asked for or seen by this app — sign-in happens only through Valve's official QR system in the Steam mobile app.\n\n• Your session token is stored only on this PC, encrypted with Windows DPAPI — it can only be decrypted by your Windows user account on this machine.\n\n• Location: %APPDATA%\\SAM-Auto. The 'Disconnect' button deletes it completely.\n\n• The app talks only to Valve's servers (Steam network + store). Nothing is sent anywhere else.\n\n• The source code is public — you can review every line.",
            ["fg.cacheLoaded"] = "Loaded from cache: {0} items ({1} old). Click 'Fetch free list' to refresh.",
            ["fg.storeRateLimited"] = "Store rate limit — waiting 10s and retrying...",
            ["fg.dealsStart"] = "Deals phase: resolving and claiming {0} promo packages...",
            ["fg.dealsSkippedNoSession"] = "Store session unavailable — skipping 100%-off deals (try signing in again).",
            ["fg.dealProgress"] = "Deal {0}/{1} — added: {2} | owned: {3} | failed: {4} — ~{5} left",
        };

        private static readonly Dictionary<string, string> _tr = new()
        {
            ["dur.hm"] = "{0}sa {1}dk",
            ["dur.ms"] = "{0}dk {1}sn",
            ["dur.s"] = "{0}sn",

            ["picker.title"] = "Steam Achievement Manager Auto 9.0 | Bir oyun seç... herhangi bir oyun...",
            ["picker.refresh"] = "Oyunları Yenile",
            ["picker.addGame"] = "Oyun Ekle",
            ["picker.unlockAll"] = "🔓 Tümünü Aç",
            ["picker.unlockSelected"] = "Seçilenleri Aç",
            ["picker.freeGames"] = "👻 Bedava Oyunlar",
            ["picker.filter"] = "Filtre",
            ["picker.theme"] = "🎨 Tema",
            ["picker.themeDark"] = "Koyu (Hayalet)",
            ["picker.themeLight"] = "Açık",
            ["picker.showGames"] = "&Oyunları göster",
            ["picker.showDemos"] = "&Demoları göster",
            ["picker.showMods"] = "&Modları göster",
            ["picker.showJunk"] = "&Gereksizleri göster",
            ["picker.displaying"] = "{0} oyun gösteriliyor. Toplam {1} oyun.",
            ["picker.downloadingList"] = "Oyun listesi indiriliyor...",
            ["picker.checkingOwnership"] = "Oyun sahipliği kontrol ediliyor...",
            ["picker.downloadingIcons"] = "{0} oyun simgesi indiriliyor...",

            ["picker.sort"] = "⇅ Sırala",
            ["picker.sortName"] = "İsme göre",
            ["picker.sortLockedFirst"] = "Açılmamışlar önce",
            ["picker.sortUnlockedFirst"] = "Açılmışlar önce",
            ["picker.scanningAchievements"] = "Yerel başarı verileri taranıyor...",
            ["picker.pause"] = "⏸ Duraklat",
            ["picker.resume"] = "▶ Devam et",
            ["picker.stop"] = "⏹ Durdur",
            ["picker.paused"] = "Duraklatıldı — devam etmek için Devam et'e bas.",

            ["unlock.none"] = "Oyun bulunamadı.",
            ["unlock.selectFirst"] = "Önce bir veya daha fazla oyun seçin (Ctrl+Click).",
            ["unlock.cancelled"] = "Durduruldu. {0}/{1} oyun tamamlandı.",
            ["unlock.confirmAllTitle"] = "Tümünü Aç Onayı",
            ["unlock.confirmAll"] = "{0} oyunun TÜM başarıları açılsın mı?\n\nTahmini süre: ~{1}.\nBu işlem geri alınamaz.",
            ["unlock.confirmSelectedTitle"] = "Seçilenleri Aç Onayı",
            ["unlock.confirmSelected"] = "Seçili {0} oyunun TÜM başarıları açılsın mı?\n\nTahmini süre: ~{1}.\nBu işlem geri alınamaz.",
            ["unlock.progress"] = "[{0}/{1}] Açılıyor: {2}",
            ["unlock.progressEta"] = "[{0}/{1}] Açılıyor: {2}  —  ~{3} kaldı",
            ["unlock.failedStart"] = "[{0}/{1}] Başlatılamadı: {2} ({3})",
            ["unlock.done"] = "Bitti. {0} oyunun başarıları açıldı.",
            ["unlock.doneTitle"] = "Bitti",
            ["unlock.doneBody"] = "Açma işlemi tamamlandı.\nDetaylar: SAM_AutoUnlock.log",
            ["unlock.info"] = "Bilgi",
            ["unlock.error"] = "Hata",

            ["fg.title"] = "Bedava Oyunlar — ücretsiz Steam içeriklerini hesabına ekle",
            ["fg.header"] = "Bedava Oyunlar",
            ["fg.subtitle"] = "Bedava oyun, DLC, yazılım ve daha fazlasını hesabına ekler. Steam mobil uygulamasıyla tek seferlik QR girişi yeterli.",
            ["fg.signin"] = "1. Steam girişi",
            ["fg.connectQr"] = "QR kod ile giriş yap",
            ["fg.disconnect"] = "Bağlantıyı kes",
            ["fg.rememberMe"] = "Bu bilgisayarda beni hatırla",
            ["fg.notConnected"] = "Bağlı değil.",
            ["fg.connecting"] = "Steam'e bağlanılıyor...",
            ["fg.awaitingScan"] = "QR kodu Steam mobil uygulamasıyla okut...",
            ["fg.loggedInAs"] = "Giriş yapıldı: {0}",
            ["fg.loginFailed"] = "Giriş başarısız: {0}",
            ["fg.tokenFailed"] = "Kayıtlı oturumun süresi dolmuş — lütfen QR kodu tekrar okut.",
            ["fg.categories"] = "2. Neler eklensin?",
            ["fg.catGames"] = "Oyunlar",
            ["fg.catDlc"] = "DLC",
            ["fg.catSoftware"] = "Yazılım",
            ["fg.catDemos"] = "Demolar",
            ["fg.catMods"] = "Modlar",
            ["fg.fetch"] = "Bedava listesini getir",
            ["fg.fetching"] = "Bedava {0} listesi alınıyor... sayfa {1}",
            ["fg.fetchDone"] = "{0}: {1} öğe",
            ["fg.totalFound"] = "Toplam: {0} bedava öğe bulundu. Tahmini ekleme süresi: ~{1} (Valve limitleri uzatabilir).",
            ["fg.claim"] = "Hepsini hesabıma ekle",
            ["fg.cancel"] = "İptal",
            ["fg.confirmClaimTitle"] = "Bedava içerik ekle",
            ["fg.confirmClaim"] = "{0} bedava öğe hesabına eklenecek.\nTahmini süre: ~{1} (Valve saatte ~50 yeni eklemeye izin verir; araç otomatik bekler).\n\nDevam edilsin mi?",
            ["fg.claimStart"] = "Başlıyor... {0} öğe kuyrukta.",
            ["fg.claimBatch"] = "Grup {0}/{1} — Eklendi: {2} | Zaten sende: {3} | Hata: {4} — ~{5} kaldı",
            ["fg.rateLimited"] = "Valve hız limitine takıldı — {0} bekleniyor... (normal, araç otomatik devam edecek)",
            ["fg.claimDone"] = "Bitti! Yeni eklenen: {0} | Zaten sende: {1} | Hata: {2}",
            ["fg.claimCancelled"] = "İptal edildi. Yeni eklenen: {0} | Zaten sende: {1} | Hata: {2}",
            ["fg.needLogin"] = "Önce giriş yap (1. adım).",
            ["fg.needFetch"] = "Önce bedava listesini getir (2. adım).",
            ["fg.fetchError"] = "Liste alınamadı: {0}",
            ["fg.log"] = "Günlük",
            ["fg.note"] = "Not: kalıcı bedava (F2P) içerikler ve %100 indirim fırsatları eklenir. Bu özellik için Steam istemcisinin açık olması gerekmez — ayrı bir bağlantı kullanılır.",
            ["fg.catDeals"] = "%100 fırsatlar",
            ["fg.security"] = "🔒 Güvenlik ve gizlilik",
            ["fg.securityTitle"] = "Güvenlik ve gizlilik",
            ["fg.securityBody"] = "• Şifren bu uygulamaya ASLA sorulmaz ve görülmez — giriş yalnızca Steam mobil uygulamasındaki Valve'ın resmî QR sistemiyle yapılır.\n\n• Oturum anahtarın yalnızca bu bilgisayarda, Windows DPAPI ile şifrelenmiş olarak saklanır — yalnızca bu makinedeki kendi Windows kullanıcın çözebilir.\n\n• Konum: %APPDATA%\\SAM-Auto. 'Bağlantıyı kes' butonu tamamen siler.\n\n• Uygulama yalnızca Valve sunucularıyla konuşur (Steam ağı + mağaza). Hiçbir veri başka yere gönderilmez.\n\n• Kaynak kodu açık — her satırı inceleyebilirsin.",
            ["fg.cacheLoaded"] = "Önbellekten yüklendi: {0} öğe ({1} eski). Güncel için 'Bedava listesini getir'e bas.",
            ["fg.storeRateLimited"] = "Mağaza hız limiti — 10sn beklenip tekrar deneniyor...",
            ["fg.dealsStart"] = "Fırsat aşaması: {0} promosyon paketi çözümlenip ekleniyor...",
            ["fg.dealsSkippedNoSession"] = "Mağaza oturumu alınamadı — %100 fırsatlar atlanıyor (yeniden giriş yapmayı dene).",
            ["fg.dealProgress"] = "Fırsat {0}/{1} — eklendi: {2} | zaten sende: {3} | hata: {4} — ~{5} kaldı",
        };

        #endregion
    }
}
