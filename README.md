# Back Button Customizer for Windows

Aplikasi Windows untuk me-remap tombol mouse (tombol samping Back/XButton1, Forward/XButton2, Klik Kanan, dan Klik Tengah) ke berbagai aksi seperti navigasi browser, shortcut keyboard, dan URL custom.

Scope aturan:
1. Global: Berfungsi di semua aplikasi Windows (File Explorer, browser, text editor).
2. Per-aplikasi: Aktif hanya pada file `.exe` tertentu (misalnya `chrome.exe` atau `code.exe`).
3. Per-domain: Aktif saat tab browser membuka website tertentu (misalnya `game.granbluefantasy.jp`). Deteksi tab memakai Windows UI Automation dan Chrome DevTools Protocol (CDP) tanpa perlu ekstensi browser.

## Fitur

- Tombol mouse yang didukung: XButton1 (Back), XButton2 (Forward), Klik Kanan, Klik Tengah.
- Aksi bawaan: Back (`Alt+Left`), Forward (`Alt+Right`), Tutup Tab (`Ctrl+W`), Buka Ulang Tab (`Ctrl+Shift+T`), Reload (`F5`), Tab Baru (`Ctrl+T`), Tab Berikutnya (`Ctrl+Tab`), Copy/Paste/Undo, Enter, Escape.
- Shortcut keyboard custom: Rekam kombinasi tombol langsung lewat antarmuka pengaturan.
- Buka URL custom: Mengarahkan tab browser yang aktif via CDP WebSocket (`Page.navigate`) jika browser dijalankan dengan remote debugging, atau fallback ke browser default (`Process.Start`).
- Multi-rule: Konfigurasi terpisah untuk tiap kombinasi tombol dan target window/website.
- System tray: Minimalkan ke taskbar tray, status aktif real-time, toggle aktif/nonaktif, dan opsi autostart saat Windows boot.

## Cara Menjalankan

Aplikasi siap digunakan di folder `dist\`:

1. Versi portable:
   Buka `dist\portable\GbfRightClickBack.exe` (berjalan mandiri tanpa perlu install .NET Runtime).
2. Versi framework-dependent:
   Buka `dist\framework-dependent\GbfRightClickBack.exe` (membutuhkan .NET 8 Runtime).

Penggunaan:
- Jendela pengaturan muncul otomatis saat pertama kali dibuka.
- Ikon aplikasi berada di system tray. Klik dua kali ikon tray untuk membuka kembali pengaturan.

## Cara Mengatur Aturan

1. Tambah aturan:
   Klik "+ Baru" di panel kiri, atau klik "Preset..." untuk konfigurasi siap pakai.
2. Atur tombol & aksi:
   Pilih tombol pemicu dan aksi yang diinginkan. Jika memilih opsi custom, klik "Rekam..." lalu tekan kombinasi shortcut di keyboard.
3. Tentukan scope:
   Pilih "Semua Aplikasi", "Aplikasi Tertentu (.exe)" (bisa via "Ambil Window..."), atau "Website / Domain Tertentu".
4. Simpan:
   Klik "Tes Aksi" untuk mencoba, lalu klik "Simpan". Perubahan langsung aktif tanpa restart aplikasi.

## Build dari Source Code

Prasyarat:
- Windows 10/11 (x64)
- .NET 8 SDK

Jalankan script batch:
```bat
build.bat
```

Atau via CLI dotnet:
```bat
REM Build Release Framework-Dependent:
dotnet publish src\GbfRightClickBack\GbfRightClickBack.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist\framework-dependent

REM Build Release Portable (Self-Contained):
dotnet publish src\GbfRightClickBack\GbfRightClickBack.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist\portable
```

## File Konfigurasi

Konfigurasi disimpan di sebelah file `.exe` atau di `%APPDATA%\BackButtonCustomizer\config.json`. Anda juga bisa membukanya lewat tombol "Buka Folder Config" di jendela aplikasi.
