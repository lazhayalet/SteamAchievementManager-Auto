# Steam Achievement Manager Auto 8.0

Bu proje, [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager)'in güncellenmiş ve **otomatik toplu başarı açma** özelliği eklenmiş fork'udur.

This is an updated fork of [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager) with **bulk achievement unlocking**.

---

## 🇹🇷 Türkçe

Steam Achievement Manager (SAM), Steam'deki başarıları ve istatistikleri yönetmek için hafif, taşınabilir bir araçtır. **Steam istemcisi açık olmalı ve giriş yapılmış olmalıdır.**

Bu fork'a eklenen özellikler:

- **🔓 Unlock All** butonu – kütüphanenizdeki **TÜM** oyunların başarılarını tek tıkla açar
- **Unlock Selected** butonu – yalnızca seçtiğiniz oyunların başarılarını açar (çoklu seçim için Ctrl+Click)
- Gizli otomatik mod: `SAM.Game.exe <appId> --unlock-all` tek oyunu arayüzsüz açar, sonucu `SAM_AutoUnlock.log` dosyasına yazar ve çıkar

> ⚠️ **Dikkat:** Toplu açılan başarılar Steam profilinizde görünür ve geri alınamaz. Sorumlu kullanın.

### Kurulum / Build

Gereksinim: Windows üzerinde [.NET 8 SDK](https://dotnet.microsoft.com/download).

```
dotnet build SAM.sln -c Release -p:Platform=x86
```

Çalıştırılabilir dosyalar `upload` klasöründe oluşur. .NET Framework 4.8 referansları `Directory.Build.props` ile otomatik yüklenir.

### Kullanım

1. Steam'i açın ve giriş yapın
2. `upload` klasöründen `SAM.Picker.exe` dosyasını çalıştırın
3. Aracı çubuğundaki **🔓 Unlock All** veya **Unlock Selected** butonunu kullanın

---

## 🇬🇧 English

Steam Achievement Manager (SAM) is a lightweight, portable application used to manage achievements and statistics in Steam. **The Steam client must be running and you must be logged in.**

Features added in this fork:

- **🔓 Unlock All** button – unlocks achievements for **every** game in your library with one click
- **Unlock Selected** button – unlocks achievements only for selected games (Ctrl+Click for multi-select)
- Headless auto mode: `SAM.Game.exe <appId> --unlock-all` unlocks all achievements for a single game without opening the UI, logs to `SAM_AutoUnlock.log`, and exits

> ⚠️ **Warning:** Unlocked achievements are visible on your Steam profile and cannot be undone. Use responsibly.

### Building

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download) on Windows.

```
dotnet build SAM.sln -c Release -p:Platform=x86
```

The binaries are written to the `upload` folder. .NET Framework 4.8 reference assemblies are restored automatically via `Directory.Build.props`.

### Usage

1. Start Steam and log in
2. Run `SAM.Picker.exe` from the `upload` folder
3. Use the **🔓 Unlock All** or **Unlock Selected** buttons in the toolbar

---

## Attribution

Most (if not all) icons are from the [Fugue Icons](https://p.yusukekamiyamane.com/) set.

Based on [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager), Zlib license.
