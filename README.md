# Back Button Customizer for Windows

Aplikasi Windows modern untuk mengkustomisasi tombol mouse (tombol samping fisik **Back / XButton1**, **Forward / XButton2**, **Klik Kanan**, dan **Klik Tengah**) agar dapat memicu berbagai aksi seperti **Browser / File Explorer Back (`Alt+Left`)**, **Forward (`Alt+Right`)**, **Tutup Tab (`Ctrl+W`)**, **Buka Ulang Tab (`Ctrl+Shift+T`)**, **Reload (`F5`)**, **Copy / Paste**, **navigasi ke URL custom**, hingga **Kombinasi Shortcut Keyboard Kustom** apa pun.

Aplikasi ini dapat berjalan secara:
1. **Global (Semua Aplikasi Windows)** — Berfungsi di File Explorer, browser, software office, code editor, dll.
2. **Aplikasi Tertentu (.exe)** — Hanya aktif di aplikasi target (misal `chrome.exe`, `msedge.exe`, `code.exe`).
3. **Website / Domain Tertentu** — Hanya aktif ketika browser membuka website tertentu (misal `game.granbluefantasy.jp`, `youtube.com`, dll).

> Ringan (~0% CPU), tanpa browser extension, tanpa butuh hak Administrator, aman dan 100% proses lokal.

---

## Fitur Utama

- **Kustomisasi Multi-Tombol**:
  - Tombol Samping 1 (**XButton1 / Back mouse**)
  - Tombol Samping 2 (**XButton2 / Forward mouse**)
  - **Klik Kanan** (Right Mouse Button)
  - **Klik Tengah** (Middle Click / Scroll Wheel Click)
- **Aksi Fleksibel & Lengkap**:
  - `Back (Alt + ←)`
  - `Forward (Alt + →)`
  - `Tutup Tab (Ctrl + W)`
  - `Buka Ulang Tab (Ctrl + Shift + T)`
  - `Reload (F5)`
  - `Tab Baru (Ctrl + T)`
  - `Tab Berikutnya (Ctrl + Tab)`
  - `Copy (Ctrl + C)` / `Paste (Ctrl + V)` / `Undo (Ctrl + Z)`
  - `Enter` / `Escape`
  - **Kustom Shortcut Keyboard**: Rekam tombol langsung via GUI (misal `Ctrl+Shift+P`, `Win+D`, dll).
  - **Buka URL (Custom)**: Navigasi tab browser yang sedang aktif ke URL yang diinput user. Pakai **CDP WebSocket** (`Page.navigate`) jika browser dijalankan dengan `--remote-debugging-port=9222`; otomatis fallback ke browser default (`Process.Start`) bila CDP tidak tersedia.
- **Fleksibilitas Cakupan (Scope)**:
  - **Global**: Berlaku di seluruh sistem Windows.
  - **Aplikasi Tertentu**: Bisa pilih file `.exe` atau klik **"Ambil Window..."** untuk mendeteksi langsung dari aplikasi yang sedang terbuka.
  - **Website Tertentu**: Deteksi tab aktif browser via **Windows UI Automation** dan **CDP** (otomatis tanpa extension).
- **Multi-Aturan (Multi-Rule)**: Pengguna bisa membuat banyak aturan sekaligus (misal: Tombol Back Mouse = Back Global, dan Klik Kanan = Back khusus saat bermain Granblue Fantasy di Chrome).
- **Preset Cepat**: Tombol menu preset untuk konfigurasi cepat sekali klik.
- **Autostart Windows**: Opsi langsung di aplikasi untuk otomatis berjalan saat Windows dinyalakan.
- **Tema UI Terang & Gelap (Light / Dark Mode)**.
- **System Tray**: Minimalkan ke tray, status aktif real-time, toggle Enable/Disable, dan mode diagnostik.

---

## Cara Menjalankan

Aplikasi sudah dibuild dan siap digunakan di folder `dist\`:

1. **Versi Portable (Rekomendasi)**:
   Buka `dist\portable\GbfRightClickBack.exe` (langsung jalan tanpa install .NET Runtime).
2. **Versi Framework-Dependent**:
   Buka `dist\framework-dependent\GbfRightClickBack.exe` (ukuran file sangat kecil ~180 KB, membutuhkan .NET 8 Runtime).

Saat dijalankan:
- Jendela pengaturan akan otomatis muncul pada saat aplikasi pertama kali dibuka.
- Ikon aplikasi akan berada di System Tray (pojok kanan taskbar dekat jam).
- Double-click ikon tray kapan saja untuk membuka kembali Pengaturan.

---

## Cara Menggunakan GUI Pengaturan

1. **Memilih / Menambah Aturan**:
   - Di panel kiri terdapat daftar aturan aktif.
   - Klik **"+ Baru"** untuk membuat aturan baru, atau klik **"Preset…"** untuk memilih konfigurasi siap pakai.
2. **Mengatur Tombol & Aksi**:
   - Pilih tombol mouse pemicu (misal: `Tombol Samping 1 (XButton1 / Back)` atau `Klik Kanan`).
   - Pilih aksi yang diinginkan (misal: `Back (Alt + ←)` atau `Kustom…`).
   - Jika memilih `Kustom…`, klik **"Rekam…"** lalu tekan kombinasi tombol di keyboard.
3. **Menentukan Cakupan Target (Scope)**:
   - Pilih **Semua Aplikasi (Global Windows)** jika ingin tombol tersebut selalu berfungsi di mana pun.
   - Pilih **Aplikasi Tertentu (.exe)** lalu klik **"Pilih .exe…"** atau **"Ambil Window…"** untuk memilih software target.
   - Pilih **Website / Domain Tertentu** dan masukkan nama domain (misal: `game.granbluefantasy.jp`).
4. **Menyimpan Perubahan**:
   - Klik **"Tes Aksi"** untuk mencoba aksi.
   - Klik tombol **"Simpan"** untuk menyimpan ke `config.json`. Pengaturan langsung aktif secara instan tanpa perlu merestart aplikasi.

---

## Build dari Source Code

### Persyaratan
- Windows 10 / 11 (x64)
- .NET 8 SDK (<https://dotnet.microsoft.com/download/dotnet/8.0>)

### Langkah Build

Cukup jalankan file batch:
```bat
build.bat
```

Atau via command line dotnet:
```bat
REM Build Release Framework-Dependent:
dotnet publish src\GbfRightClickBack\GbfRightClickBack.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist\framework-dependent

REM Build Release Portable (Self-Contained):
dotnet publish src\GbfRightClickBack\GbfRightClickBack.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist\portable
```

---

## File Konfigurasi (`config.json`)

Konfigurasi disimpan secara otomatis di sebelah file `.exe` (atau di `%APPDATA%\BackButtonCustomizer\config.json`). Anda juga dapat membukanya langsung dengan mengklik tombol **"Buka Folder Config"** di bagian bawah jendela aplikasi.
