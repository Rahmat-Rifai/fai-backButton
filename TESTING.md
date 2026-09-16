# Testing Checklist — GBF Right Click Back

Dokumen ini menjelaskan cara menguji aplikasi sesuai PRD section 12.

## Prasyarat

- Windows 10/11.
- Google Chrome terinstall.
- Aplikasi sudah dibuild: `build.bat` atau `dotnet build` / `dotnet publish`.
- Jalankan salah satu exe:
  - `dist\portable\GbfRightClickBack.exe` (tanpa dependency), atau
  - `dist\framework-dependent\GbfRightClickBack.exe` (butuh .NET Runtime 8).
- Icon tray harus muncul (cek `^` hidden icons jika tidak terlihat).

Tip diagnostic: klik kanan icon tray → **Check Detection...** atau jalankan `GbfRightClickBack.exe --check` lalu lihat `gbf-diag.log`.

---

## Test 1 — Chrome membuka GBF → klik kanan = Back, tanpa context menu

**Langkah:**
1. Buka Chrome, navigasi ke `https://game.granbluefantasy.jp/` (atau path apa pun di domain itu, mis. `#quest`).
2. Klik beberapa menu di dalam game agar ada history (mis. buka Quest → kembali, atau buka 2 halaman GBF berturut-turut).
3. Pastikan Chrome adalah window aktif (foreground) dan tray menampilkan **Status: Active** (atau `Check Detection...` menampilkan `Is GBF: True`).
4. Klik **kanan** di area game.

**Expected:**
- Browser melakukan **Back** (sama seperti `Alt + Left` / tombol Back mouse).
- **Context menu Chrome tidak muncul.**
- Left click, middle click, scroll tetap normal.

**Fail jika:** context menu muncul, atau tidak terjadi Back.

---

## Test 2 — Chrome membuka Google → klik kanan normal

**Langkah:**
1. Chrome tetap aktif, buka `https://www.google.com`.
2. Tray harus menampilkan **Status: Inactive** (`Is GBF: False`).
3. Klik kanan di halaman Google.

**Expected:** Context menu Chrome muncul normal, tidak terjadi Back.

---

## Test 3 — Chrome membuka website lain → klik kanan normal

**Langkah:**
1. Buka `https://example.com`, `https://youtube.com`, `https://github.com`, dll.
2. Klik kanan di masing-masing.

**Expected:** Semua klik kanan normal (context menu muncul). Khususnya cek:
- `https://game.granbluefantasy.jp.example.com` → **harus normal** (bukan GBF, anti-spoof).
- `https://notgame.granbluefantasy.jp` → **harus normal**.

---

## Test 4 — Chrome tidak aktif / aplikasi lain aktif → klik kanan normal

**Langkah:**
1. Buka Notepad / File Explorer / VS Code / game lain, jadikan foreground.
2. Klik kanan di aplikasi tersebut.

**Expected:** Klik kanan berfungsi normal sepenuhnya, tidak ada Back, tidak ada block.

---

## Test 5 — Chrome ditutup → aplikasi tetap aman

**Langkah:**
1. Tutup semua window Chrome (pastikan `chrome.exe` hilang dari Task Manager, atau biarkan background process — yang penting tidak ada window).
2. Klik kanan di desktop / Explorer.
3. Lihat tray icon tetap ada.

**Expected:** Aplikasi tidak crash, tidak error, klik kanan normal. Tray `Status: Inactive`.

---

## Test 6 — Chrome dibuka kembali → deteksi pulih

**Langkah:**
1. Setelah Test 5, buka kembali Chrome → buka `https://game.granbluefantasy.jp/`.
2. Jadikan Chrome foreground.
3. Cek tray → harus kembali **Status: Active** dalam 1 detik.
4. Klik kanan → harus Back lagi.

**Expected:** Deteksi kembali normal tanpa perlu restart aplikasi.

---

## Test 7 — Klik kanan berulang → tidak stuck

**Langkah:**
1. Chrome di GBF, klik kanan **berkali-kali dengan cepat** (5–10x).
2. Coba juga tahan klik kanan agak lama, lalu lepas.
3. Cek keyboard: ketik di address bar / Notepad, pastikan tombol `Alt` tidak stuck (tidak ada menu yang ter-highlight).

**Expected:** Tidak ada stuck key, tidak ada mouse button state yang tertinggal. `Alt` dan `Left` selalu ter-release (logic `SendInput` atomic 4 event).

---

## Test Tambahan (opsional tapi disarankan)

### Test 8 — Toggle Enable/Disable

1. Tray → klik **Enable** (uncheck) → buka GBF → klik kanan → harus **normal** (tidak Back).
2. Check Enable lagi → klik kanan di GBF → harus Back lagi.

### Test 9 — Multiple Chrome windows

1. Buka 2 window Chrome: Window A = GBF, Window B = Google.
2. Aktifkan Window B → klik kanan → normal.
3. Aktifkan Window A → klik kanan → Back. Pastikan tidak terpengaruh window yang tidak aktif.

### Test 10 — CDP (jika pakai remote debugging)

1. Tutup Chrome, jalankan: `"C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222`
2. Buka GBF, cek `http://127.0.0.1:9222/json/version` harus menampilkan JSON.
3. Jalankan app, `Check Detection...` harus tetap `Is GBF: True` (via UIA + CDP).

### Test 11 — Diagnostic CLI

```bat
GbfRightClickBack.exe --check
```

Buka `gbf-diag.log` di sebelah exe, pastikan format:

```
=== GBF Right Click Back - Diagnostic ===
Enabled: True
IsCurrentlyActive: False/True
Foreground HWND: ...
Process ID: ...
Is Chrome process: True/False
[UiaUrlProvider] URL: ...
[UiaUrlProvider] Is GBF: True/False
```

---

## Checklist Ringkas (copy-paste)

- [ ] Test 1 — GBF → RButton = Back, tanpa context menu
- [ ] Test 2 — Google → RButton normal
- [ ] Test 3 — Website lain + anti-spoof → normal
- [ ] Test 4 — App lain foreground → normal
- [ ] Test 5 — Chrome ditutup → app tidak crash
- [ ] Test 6 — Chrome dibuka lagi → deteksi pulih
- [ ] Test 7 — Klik berulang → tidak stuck
- [ ] Test 8 — Toggle Enable/Disable
- [ ] Test 9 — Multiple windows
- [ ] Test 10 — CDP (opsional)
- [ ] Test 11 — --check / Check Detection...

Jika semua checklist di atas lolos, aplikasi siap dipakai.
