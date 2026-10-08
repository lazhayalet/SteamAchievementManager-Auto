# Steam Achievement Manager Auto 8.0

[![Release](https://img.shields.io/github/v/release/lazhayalet/SteamAchievementManager-Auto)](https://github.com/lazhayalet/SteamAchievementManager-Auto/releases)
[![License](https://img.shields.io/github/license/lazhayalet/SteamAchievementManager-Auto)](./LICENSE.txt)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Arch](https://img.shields.io/badge/arch-x86-lightgrey)

Bu proje, [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager)'in güncellenmiş ve **otomatik toplu başarı açma** özelliği eklenmiş fork'udur.

This is an updated fork of [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager) with **bulk achievement unlocking**.

## ⬇️ İndir / Download

Hazır paketi **Releases** sayfasından indirin / Grab the ready-to-run package from **Releases**:

👉 https://github.com/lazhayalet/SteamAchievementManager-Auto/releases

- `SAM-Auto-8.0.0-win-x86.zip` dosyasını indirin, bir klasöre çıkarın ve `SAM.Picker.exe` dosyasını çalıştırın.
- Download `SAM-Auto-8.0.0-win-x86.zip`, extract it to a folder and run `SAM.Picker.exe`.
- Kurulum gerekmez, taşınabilirdir / No installation needed, portable.

---

## 🇹🇷 Türkçe

Steam Achievement Manager Auto (SAM Auto 8.0), Steam'deki başarıları ve istatistikleri yönetmek için hafif, taşınabilir bir araçtır. **Steam istemcisi açık olmalı ve giriş yapılmış olmalıdır.**

### Bu fork'a eklenen özellikler

- **🔓 Unlock All** butonu – kütüphanenizdeki **TÜM** oyunların başarılarını tek tıkla açar
- **Unlock Selected** butonu – yalnızca seçtiğiniz oyunların başarılarını açar (çoklu seçim için Ctrl+Click)
- Gizli otomatik mod: `SAM.Game.exe <appId> --unlock-all` tek oyunu arayüzsüz açar, sonucu `SAM_AutoUnlock.log` dosyasına yazar ve çıkar

> ⚠️ **Dikkat:** Toplu açılan başarılar Steam profilinizde görünür ve geri alınamaz. Sorumlu kullanın.

### Kullanım

1. Steam'i açın ve giriş yapın
2. **Releases** sayfasından `SAM-Auto-8.0.0-win-x86.zip` dosyasını indirin
3. ZIP dosyasını bir klasöre çıkarın
4. Çıkarılan klasörden `SAM.Picker.exe` dosyasını çalıştırın
5. Araç çubuğundaki **🔓 Unlock All** veya **Unlock Selected** butonunu kullanın

### Kurulum / Build

Gereksinim: Windows üzerinde [.NET 8 SDK](https://dotnet.microsoft.com/download) (derleme için) + çalıştırma için .NET Framework 4.8.

```
dotnet build SAM.sln -c Release -p:Platform=x86
```

Çalıştırılabilir dosyalar `upload` klasöründe oluşur. .NET Framework 4.8 referansları `Directory.Build.props` ile otomatik yüklenir.

---

## 🇬🇧 English

Steam Achievement Manager Auto (SAM Auto 8.0) is a lightweight, portable application used to manage achievements and statistics in Steam. **The Steam client must be running and you must be logged in.**

### Features added in this fork

- **🔓 Unlock All** button – unlocks achievements for **every** game in your library with one click
- **Unlock Selected** button – unlocks achievements only for selected games (Ctrl+Click for multi-select)
- Headless auto mode: `SAM.Game.exe <appId> --unlock-all` unlocks all achievements for a single game without opening the UI, logs to `SAM_AutoUnlock.log`, and exits

> ⚠️ **Warning:** Unlocked achievements are visible on your Steam profile and cannot be undone. Use responsibly.

### Usage

1. Start Steam and log in
2. Download `SAM-Auto-8.0.0-win-x86.zip` from the **Releases** page
3. Extract the ZIP file to a folder
4. Run `SAM.Picker.exe` from the extracted folder
5. Use the **🔓 Unlock All** or **Unlock Selected** buttons in the toolbar

### Building

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download) on Windows (+ .NET Framework 4.8 to run).

```
dotnet build SAM.sln -c Release -p:Platform=x86
```

The binaries are written to the `upload` folder. .NET Framework 4.8 reference assemblies are restored automatically via `Directory.Build.props`.

---

## Attribution / Kaynak

Most (if not all) icons are from the [Fugue Icons](https://p.yusukekamiyamane.com/) set.

Based on [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager), Zlib license. See [LICENSE.txt](./LICENSE.txt).
