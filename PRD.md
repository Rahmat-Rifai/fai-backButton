Buatkan aplikasi Windows sederhana bernama "GBF Right Click Back".

TUJUAN:
Saat aplikasi dijalankan, klik kanan mouse (Right Mouse Button / RButton) harus berfungsi sebagai tombol Back di Google Chrome, tetapi HANYA ketika Chrome sedang membuka website:

https://game.granbluefantasy.jp/

Di luar website tersebut, klik kanan harus tetap berfungsi normal.

REQUIREMENTS:

1. PLATFORM
- Windows 10/11.
- Aplikasi berjalan sebagai background process/tray app.
- Tidak membutuhkan browser extension.
- Tidak membutuhkan administrator privilege jika memang tidak diperlukan.

2. DETEKSI CHROME
- Deteksi apakah window aktif adalah Google Chrome.
- Jangan memengaruhi Firefox, Edge, aplikasi Windows lain, desktop, File Explorer, game lain, dll.

3. DETEKSI WEBSITE
Aplikasi harus memastikan tab Chrome yang sedang aktif berada di:
https://game.granbluefantasy.jp/

Jangan hanya mengecek bahwa Chrome sedang terbuka.
Harus mengecek URL tab aktif.

Jika URL bukan game.granbluefantasy.jp, RButton harus normal.

4. MOUSE BEHAVIOR
Ketika kondisi terpenuhi:

Active window = Google Chrome
AND
Active tab URL = https://game.granbluefantasy.jp/*

maka:
RButton → browser Back

Gunakan mekanisme browser Back yang paling reliable, misalnya:
Alt + Left

PENTING:
- Jangan melakukan right-click biasa setelah mengirim Back.
- Jadi ketika user klik kanan di GBF, context menu Chrome tidak boleh muncul.
- Left click, middle click, mouse movement, keyboard, dan tombol mouse lainnya harus tetap normal.

5. URL MATCHING
Anggap URL berikut sebagai halaman GBF:

https://game.granbluefantasy.jp/
https://game.granbluefantasy.jp/*
http://game.granbluefantasy.jp/*
https://game.granbluefantasy.jp

Tetapi JANGAN aktif pada:
https://example.com
https://game.granbluefantasy.jp.example.com
atau domain lain.

6. CARA MENGECEK TAB AKTIF
Pilih metode yang paling reliable.

Jika memungkinkan, gunakan Chrome DevTools/remote debugging hanya jika user memang mengaktifkannya.

Jika menggunakan Chrome DevTools Protocol:
- Jelaskan cara menjalankan Chrome dengan remote debugging.
- Jangan mengubah profile Chrome user atau merusak session/cookies.
- Handle jika Chrome tidak dijalankan dengan remote debugging.

Alternatif jika ada metode Windows API/UI Automation yang lebih sederhana dan reliable, boleh digunakan.

7. PERFORMANCE
- Harus sangat ringan.
- Jangan melakukan polling terlalu cepat.
- Jangan menggunakan CPU tinggi.
- Hindari loop yang terus menerus tanpa sleep.
- Idealnya gunakan event/window hook jika memungkinkan.

8. TRAY APP
Tambahkan system tray icon dengan menu:
- Enable/Disable
- Status: Active / Inactive
- Exit

Default:
Enable = ON

Status harus menunjukkan apakah aplikasi sedang aktif.

9. SAFETY
Jangan melakukan:
- keylogger
- pencatatan keyboard
- pencatatan URL ke server
- network request yang tidak diperlukan
- telemetry
- data collection

Semua proses harus lokal di komputer user.

10. SOURCE CODE
Gunakan bahasa yang paling cocok untuk Windows, prioritaskan:
- C#/.NET atau
- Python jika implementasinya jauh lebih sederhana.

Jika menggunakan Python:
- gunakan library yang mature.
- berikan requirements.txt.
- berikan script build menjadi .exe.

Jika menggunakan C#:
- buat project yang bisa langsung dibuild dengan dotnet.

11. DELIVERABLES
Buat:
- source code lengkap
- README.md
- cara install dependency
- cara menjalankan
- cara build menjadi .exe
- cara membuat aplikasi start otomatis bersama Windows (opsional)
- troubleshooting

12. TESTING
Sediakan checklist pengujian:

Test 1:
Chrome membuka GBF
→ klik kanan
→ browser Back
→ context menu tidak muncul.

Test 2:
Chrome membuka Google
→ klik kanan
→ context menu normal.

Test 3:
Chrome membuka website lain
→ klik kanan
→ normal.

Test 4:
Chrome tidak aktif / aplikasi lain aktif
→ klik kanan
→ normal.

Test 5:
Chrome ditutup
→ aplikasi tetap aman dan tidak error.

Test 6:
Chrome dibuka kembali
→ aplikasi kembali mendeteksi kondisi dengan benar.

Test 7:
User menekan RButton beberapa kali
→ tidak menyebabkan stuck key atau mouse button state.

PENTING:
Sebelum memilih implementasi, analisis terlebih dahulu bagaimana cara paling reliable untuk mengetahui URL TAB AKTIF Chrome di Windows.

Jangan membuat solusi yang hanya mendeteksi judul window Chrome, karena title window tidak cukup reliable untuk menentukan URL.

Berikan implementasi final yang siap dijalankan di Windows 11.