using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace GbfRightClickBack;

/// <summary>
/// Jendela utama aplikasi: Back Button Customizer.
/// Pengguna dapat mengkustomisasi tombol mouse (Back/XButton1, Forward/XButton2, Klik Kanan, Klik Tengah)
/// ke berbagai aksi (Back, Forward, shortcut kustom, tutup tab, reload, copy, paste)
/// secara Global (semua aplikasi), Aplikasi tertentu, maupun Website/Domain tertentu.
/// </summary>
internal sealed class MainWindow : Form
{
    private readonly UrlMonitor _monitor;
    private readonly UiTheme[] _themeOptions = [UiTheme.Light, UiTheme.Dark];
    private UiTheme _theme = UiTheme.Light;
    private bool _loading = true;
    private bool _recording;
    private bool _recordingTrigger;
    private bool _suppressEnableSync;
    private ButtonRule? _selectedRule;

    private readonly System.Windows.Forms.Timer _testTimer = new() { Interval = 1400 };
    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 1400 };

    // Header
    private readonly Panel _header = new();
    private readonly Label _lblTitle = new();
    private readonly Label _lblSub = new();
    private readonly Label _chip = new();

    // Body
    private readonly Panel _body = new() { AutoScroll = true };

    // Status card
    private readonly CheckBox _chkMasterEnable = new();
    private readonly Label _lblDetail = new();
    private readonly Button _btnTest = new();

    // Aturan pemetaan card
    private readonly ListBox _lstRules = new();
    private readonly Button _btnAddRule = new();
    private readonly Button _btnDuplicateRule = new();
    private readonly Button _btnDeleteRule = new();
    private readonly Button _btnPreset = new();
    private readonly ContextMenuStrip _menuPresets = new();

    // Rule editor controls
    private readonly Panel _pnlRuleEditor = new();
    private readonly TextBox _txtRuleName = new();
    private readonly CheckBox _chkRuleEnabled = new();
    private readonly ComboBox _cmbButton = new();
    private readonly TextBox _txtTriggerKey = new();
    private readonly Button _btnRecordTriggerKey = new();
    private readonly ComboBox _cmbAction = new();
    private readonly TextBox _txtHotkey = new();
    private readonly Button _btnRecord = new();
    private readonly Label _lblHotkeyHint = new();
    private readonly TextBox _txtUrl = new();
    private readonly Label _lblUrlHint = new();

    private readonly RadioButton _rbGlobal = new();
    private readonly RadioButton _rbSpecificApps = new();
    private readonly RadioButton _rbSpecificDomains = new();

    private readonly Panel _pnlApps = new();
    private readonly TextBox _txtAppExes = new();
    private readonly Button _btnBrowseApp = new();
    private readonly Button _btnPickCurrentApp = new();

    private readonly Panel _pnlDomains = new();
    private readonly ListBox _lstDomains = new();
    private readonly TextBox _txtNewDomain = new();
    private readonly Button _btnAddDomain = new();
    private readonly Button _btnRemoveDomain = new();
    private readonly CheckBox _chkEscape = new();

    // Pengaturan umum & sistem
    private readonly CheckBox _chkAutostart = new();
    private readonly CheckBox _chkShowOnStart = new();
    private readonly ComboBox _cmbTheme = new();

    // Footer
    private readonly Button _btnOpenConfig = new();
    private readonly Button _btnSave = new();
    private readonly Button _btnClose = new();

    /// <summary>Dipanggil saat user mengubah toggle Enable dari jendela ini.</summary>
    public event Action<bool>? EnabledToggled;

    public MainWindow(UrlMonitor monitor)
    {
        _monitor = monitor;

        Text = "Back Button Customizer — Pengaturan";
        Icon = AppIcons.Load();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(880, 830);
        Font = new Font("Segoe UI", 9F);
        KeyPreview = true;
        KeyDown += OnFormKeyDown;

        BuildUi();
        LoadFromConfig();
        ApplyTheme();
        SetStatus(monitor.IsCurrentlyActive);

        monitor.StateChanged += OnMonitorStateChanged;
        FormClosing += (s, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
        FormClosed += (_, _) =>
        {
            monitor.StateChanged -= OnMonitorStateChanged;
            _testTimer.Dispose();
            _saveTimer.Dispose();
        };

        _testTimer.Tick += (_, _) => { _btnTest.Text = "Tes aksi"; _testTimer.Stop(); };
        _saveTimer.Tick += (_, _) => { _btnSave.Text = "Simpan"; _saveTimer.Stop(); };

        _loading = false;
    }

    private void BuildUi()
    {
        // 1. Header
        _header.Location = new Point(0, 0);
        _header.Size = new Size(880, 76);
        _header.Tag = "header";
        Controls.Add(_header);

        _lblTitle.Text = "Back Button Customizer";
        _lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        _lblTitle.Location = new Point(22, 12);
        _lblTitle.AutoSize = true;
        _header.Controls.Add(_lblTitle);

        _lblSub.Text = "Kustomisasi tombol mouse fisik / klik kanan ke berbagai aksi di Windows, Aplikasi, atau Website";
        _lblSub.Location = new Point(23, 44);
        _lblSub.AutoSize = true;
        _header.Controls.Add(_lblSub);

        _chip.Location = new Point(700, 22);
        _chip.Size = new Size(150, 32);
        _chip.TextAlign = ContentAlignment.MiddleCenter;
        _chip.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _header.Controls.Add(_chip);

        // 2. Body
        _body.Location = new Point(0, 76);
        _body.Size = new Size(880, 690);
        Controls.Add(_body);

        BuildStatusCard(Card("STATUS APLIKASI", 12, 86));
        BuildRulesCard(Card("ATURAN PEMETAAN TOMBOL MOUSE", 108, 450));
        BuildSettingsCard(Card("PENGATURAN UMUM & SISTEM", 568, 106));

        // 3. Footer
        var footer = new Panel { Location = new Point(0, 768), Size = new Size(880, 60) };
        _btnOpenConfig.Text = "Buka Folder Config";
        _btnOpenConfig.Location = new Point(22, 14);
        _btnOpenConfig.Size = new Size(150, 32);
        _btnOpenConfig.Click += OnOpenConfigFolder;
        footer.Controls.Add(_btnOpenConfig);

        _btnSave.Text = "Simpan";
        _btnSave.Tag = "primary";
        _btnSave.Location = new Point(576, 14);
        _btnSave.Size = new Size(134, 32);
        _btnSave.Click += OnSave;
        footer.Controls.Add(_btnSave);

        _btnClose.Text = "Tutup (ke Tray)";
        _btnClose.Location = new Point(720, 14);
        _btnClose.Size = new Size(134, 32);
        _btnClose.Click += (_, _) => Close();
        footer.Controls.Add(_btnClose);

        Controls.Add(footer);
    }

    private Panel Card(string title, int y, int height)
    {
        var card = new Panel
        {
            Location = new Point(20, y),
            Size = new Size(840, height),
            Tag = "card",
        };
        card.Paint += (_, e) => e.Graphics.DrawRectangle(new Pen(Theme.Border), 0, 0, card.Width - 1, card.Height - 1);
        card.Controls.Add(new Label
        {
            Text = title,
            Location = new Point(16, 10),
            AutoSize = true,
            Tag = "sub",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
        });
        _body.Controls.Add(card);
        return card;
    }

    private static Label L(string text, int x, int y, int w) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(w, 20),
        AutoSize = false,
    };

    private void BuildStatusCard(Panel card)
    {
        _chkMasterEnable.Text = "Aktifkan Kustomisasi Mouse (Master Switch)";
        _chkMasterEnable.Location = new Point(16, 32);
        _chkMasterEnable.Size = new Size(360, 24);
        _chkMasterEnable.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _chkMasterEnable.CheckedChanged += (_, _) =>
        {
            if (!_suppressEnableSync)
                EnabledToggled?.Invoke(_chkMasterEnable.Checked);
        };
        card.Controls.Add(_chkMasterEnable);

        _lblDetail.Location = new Point(16, 58);
        _lblDetail.Size = new Size(620, 20);
        _lblDetail.Tag = "sub";
        card.Controls.Add(_lblDetail);

        _btnTest.Text = "Tes aksi";
        _btnTest.Tag = "primary";
        _btnTest.Location = new Point(704, 32);
        _btnTest.Size = new Size(118, 36);
        _btnTest.Click += OnTestAction;
        card.Controls.Add(_btnTest);
    }

    private void BuildRulesCard(Panel card)
    {
        // Kolom Kiri: Daftar aturan + tombol navigasi
        card.Controls.Add(L("Daftar Aturan:", 16, 32, 260));

        _lstRules.Location = new Point(16, 52);
        _lstRules.Size = new Size(290, 340);
        _lstRules.Font = new Font("Segoe UI", 9.25F);
        _lstRules.SelectedIndexChanged += OnRuleSelectionChanged;
        card.Controls.Add(_lstRules);

        _btnAddRule.Text = "+ Baru";
        _btnAddRule.Location = new Point(16, 402);
        _btnAddRule.Size = new Size(65, 28);
        _btnAddRule.Click += OnAddRule;
        card.Controls.Add(_btnAddRule);

        _btnDuplicateRule.Text = "Salin";
        _btnDuplicateRule.Location = new Point(85, 402);
        _btnDuplicateRule.Size = new Size(65, 28);
        _btnDuplicateRule.Click += OnDuplicateRule;
        card.Controls.Add(_btnDuplicateRule);

        _btnDeleteRule.Text = "Hapus";
        _btnDeleteRule.Location = new Point(154, 402);
        _btnDeleteRule.Size = new Size(65, 28);
        _btnDeleteRule.Click += OnDeleteRule;
        card.Controls.Add(_btnDeleteRule);

        _btnPreset.Text = "Preset…";
        _btnPreset.Location = new Point(223, 402);
        _btnPreset.Size = new Size(83, 28);
        _btnPreset.Click += OnShowPresets;
        card.Controls.Add(_btnPreset);

        BuildPresetMenu();

        // Kolom Kanan: Editor aturan yang sedang dipilih
        _pnlRuleEditor.Location = new Point(320, 32);
        _pnlRuleEditor.Size = new Size(502, 402);
        card.Controls.Add(_pnlRuleEditor);

        // 1. Nama aturan & status aktif
        _pnlRuleEditor.Controls.Add(L("Nama Aturan:", 0, 2, 100));
        _txtRuleName.Location = new Point(0, 22);
        _txtRuleName.Size = new Size(330, 23);
        _txtRuleName.TextChanged += (_, _) =>
        {
            if (!_loading && _selectedRule != null)
            {
                _selectedRule.Name = _txtRuleName.Text.Trim();
                RefreshRulesList();
            }
        };
        _pnlRuleEditor.Controls.Add(_txtRuleName);

        _chkRuleEnabled.Text = "Aturan Aktif";
        _chkRuleEnabled.Location = new Point(344, 22);
        _chkRuleEnabled.Size = new Size(120, 23);
        _chkRuleEnabled.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _chkRuleEnabled.CheckedChanged += (_, _) =>
        {
            if (!_loading && _selectedRule != null)
            {
                _selectedRule.Enabled = _chkRuleEnabled.Checked;
                RefreshRulesList();
            }
        };
        _pnlRuleEditor.Controls.Add(_chkRuleEnabled);

        // 2. Tombol Mouse & Aksi
        _pnlRuleEditor.Controls.Add(L("Tombol Mouse Pemicu:", 0, 54, 200));
        _cmbButton.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbButton.Location = new Point(0, 74);
        _cmbButton.Size = new Size(236, 23);
        _cmbButton.Items.AddRange(
        [
            "Tombol Samping 1 (XButton1 / Back)",
            "Tombol Samping 2 (XButton2 / Forward)",
            "Klik Kanan (RButton)",
            "Klik Tengah (MButton)",
            "Tombol Keyboard Kustom...",
        ]);
        _cmbButton.SelectedIndexChanged += (_, _) =>
        {
            bool isKeyboard = _cmbButton.SelectedIndex == 4;
            _txtTriggerKey.Visible = isKeyboard;
            _btnRecordTriggerKey.Visible = isKeyboard;
            
            if (!_loading && _selectedRule != null)
            {
                if (isKeyboard)
                {
                    _selectedRule.TriggerType = TriggerInputType.Keyboard;
                }
                else
                {
                    _selectedRule.TriggerType = TriggerInputType.Mouse;
                    _selectedRule.TriggerButton = (TriggerMouseButton)_cmbButton.SelectedIndex;
                }
                RefreshRulesList();
            }
        };
        _pnlRuleEditor.Controls.Add(_cmbButton);
        
        _txtTriggerKey.Location = new Point(0, 104);
        _txtTriggerKey.Size = new Size(130, 23);
        _txtTriggerKey.ReadOnly = true;
        _pnlRuleEditor.Controls.Add(_txtTriggerKey);

        _btnRecordTriggerKey.Text = "Rekam Pemicu";
        _btnRecordTriggerKey.Location = new Point(136, 103);
        _btnRecordTriggerKey.Size = new Size(100, 25);
        _btnRecordTriggerKey.Click += OnToggleRecordTrigger;
        _pnlRuleEditor.Controls.Add(_btnRecordTriggerKey);

        _pnlRuleEditor.Controls.Add(L("Aksi Yang Dikirim:", 252, 54, 200));
        _cmbAction.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbAction.Location = new Point(252, 74);
        _cmbAction.Size = new Size(244, 23);
        _cmbAction.Items.AddRange(
        [
            "Back (Alt + ←)",
            "Forward (Alt + →)",
            "Tutup Tab (Ctrl+W)",
            "Buka Ulang Tab (Ctrl+Shift+T)",
            "Reload (F5)",
            "Tab Baru (Ctrl+T)",
            "Tab Berikutnya (Ctrl+Tab)",
            "Copy (Ctrl+C)",
            "Paste (Ctrl+V)",
            "Undo (Ctrl+Z)",
            "Enter",
            "Escape",
            "Klik Kiri Mouse",
            "Kustom…",
            "Buka URL (Custom)…",
        ]);
        _cmbAction.SelectedIndexChanged += (_, _) =>
        {
            if (!_loading && _selectedRule != null)
            {
                _selectedRule.Action = (TriggerAction)_cmbAction.SelectedIndex;
                UpdateHotkeyUi();
                RefreshRulesList();
            }
        };
        _pnlRuleEditor.Controls.Add(_cmbAction);

        // 3. Hotkey Kustom (hanya jika aksi = Kustom)
        _txtHotkey.Location = new Point(252, 104);
        _txtHotkey.Size = new Size(150, 23);
        _txtHotkey.ReadOnly = true;
        _pnlRuleEditor.Controls.Add(_txtHotkey);

        _btnRecord.Text = "Rekam…";
        _btnRecord.Location = new Point(408, 103);
        _btnRecord.Size = new Size(88, 25);
        _btnRecord.Click += OnToggleRecord;
        _pnlRuleEditor.Controls.Add(_btnRecord);

        _lblHotkeyHint.Text = "Klik 'Rekam' lalu tekan kombinasi shortcut di keyboard (Esc = batal).";
        _lblHotkeyHint.Location = new Point(0, 106);
        _lblHotkeyHint.Size = new Size(244, 20);
        _lblHotkeyHint.Tag = "sub";
        _lblHotkeyHint.Font = new Font("Segoe UI", 8F);
        _pnlRuleEditor.Controls.Add(_lblHotkeyHint);

        _txtUrl.Location = new Point(252, 104);
        _txtUrl.Size = new Size(244, 23);
        _txtUrl.TextChanged += (_, _) =>
        {
            if (!_loading && _selectedRule != null)
                _selectedRule.Url = _txtUrl.Text.Trim();
        };
        _pnlRuleEditor.Controls.Add(_txtUrl);

        _lblUrlHint.Text = "Masukkan URL lengkap, mis. https://example.com";
        _lblUrlHint.Location = new Point(252, 130);
        _lblUrlHint.Size = new Size(244, 20);
        _lblUrlHint.Tag = "sub";
        _lblUrlHint.Font = new Font("Segoe UI", 8F);
        _pnlRuleEditor.Controls.Add(_lblUrlHint);

        // 4. Cakupan Target (Scope)
        _pnlRuleEditor.Controls.Add(L("Cakupan Berlakunya Tombol (Target Scope):", 0, 136, 350));

        _rbGlobal.Text = "Semua Aplikasi (Global Windows)";
        _rbGlobal.Location = new Point(0, 156);
        _rbGlobal.Size = new Size(230, 22);
        _rbGlobal.CheckedChanged += OnScopeChanged;
        _pnlRuleEditor.Controls.Add(_rbGlobal);

        _rbSpecificApps.Text = "Aplikasi Tertentu (.exe)";
        _rbSpecificApps.Location = new Point(234, 156);
        _rbSpecificApps.Size = new Size(160, 22);
        _rbSpecificApps.CheckedChanged += OnScopeChanged;
        _pnlRuleEditor.Controls.Add(_rbSpecificApps);

        _rbSpecificDomains.Text = "Website / Domain Tertentu";
        _rbSpecificDomains.Location = new Point(0, 180);
        _rbSpecificDomains.Size = new Size(230, 22);
        _rbSpecificDomains.CheckedChanged += OnScopeChanged;
        _pnlRuleEditor.Controls.Add(_rbSpecificDomains);

        // Panel Apps
        _pnlApps.Location = new Point(0, 206);
        _pnlApps.Size = new Size(496, 56);
        _pnlApps.Controls.Add(L("Aplikasi Target — nama file .exe pisahkan dengan koma (mis. chrome.exe, msedge.exe):", 0, 0, 480));
        _txtAppExes.Location = new Point(0, 20);
        _txtAppExes.Size = new Size(260, 23);
        _txtAppExes.TextChanged += (_, _) =>
        {
            if (!_loading && _selectedRule != null)
            {
                _selectedRule.AppExecutables = _txtAppExes.Text
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(BrowserHelper.NormalizeExeName)
                    .ToList();
            }
        };
        _pnlApps.Controls.Add(_txtAppExes);

        _btnBrowseApp.Text = "Pilih .exe…";
        _btnBrowseApp.Location = new Point(266, 19);
        _btnBrowseApp.Size = new Size(95, 25);
        _btnBrowseApp.Click += OnBrowseApp;
        _pnlApps.Controls.Add(_btnBrowseApp);

        _btnPickCurrentApp.Text = "Ambil Window…";
        _btnPickCurrentApp.Location = new Point(366, 19);
        _btnPickCurrentApp.Size = new Size(128, 25);
        _btnPickCurrentApp.Click += OnPickCurrentApp;
        _pnlApps.Controls.Add(_btnPickCurrentApp);

        _pnlRuleEditor.Controls.Add(_pnlApps);

        // Panel Domains
        _pnlDomains.Location = new Point(0, 264);
        _pnlDomains.Size = new Size(496, 104);
        _pnlDomains.Controls.Add(L("Domain Target (mis. game.granbluefantasy.jp):", 0, 0, 350));

        _lstDomains.Location = new Point(0, 20);
        _lstDomains.Size = new Size(330, 52);
        _pnlDomains.Controls.Add(_lstDomains);

        _btnRemoveDomain.Text = "Hapus";
        _btnRemoveDomain.Location = new Point(336, 20);
        _btnRemoveDomain.Size = new Size(74, 25);
        _btnRemoveDomain.Click += OnRemoveDomain;
        _pnlDomains.Controls.Add(_btnRemoveDomain);

        _txtNewDomain.Location = new Point(0, 76);
        _txtNewDomain.Size = new Size(330, 23);
        _txtNewDomain.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                OnAddDomain(_btnAddDomain, EventArgs.Empty);
            }
        };
        _pnlDomains.Controls.Add(_txtNewDomain);

        _btnAddDomain.Text = "Tambah";
        _btnAddDomain.Location = new Point(336, 75);
        _btnAddDomain.Size = new Size(74, 25);
        _btnAddDomain.Click += OnAddDomain;
        _pnlDomains.Controls.Add(_btnAddDomain);

        _pnlRuleEditor.Controls.Add(_pnlDomains);

        // Opsi Kirim Esc Dulu
        _chkEscape.Text = "Kirim Esc dulu sebelum aksi (berguna untuk game/canvas yang mengunci input mouse)";
        _chkEscape.Location = new Point(0, 372);
        _chkEscape.Size = new Size(490, 22);
        _chkEscape.CheckedChanged += (_, _) =>
        {
            if (!_loading && _selectedRule != null)
                _selectedRule.SendEscapeFirst = _chkEscape.Checked;
        };
        _pnlRuleEditor.Controls.Add(_chkEscape);
    }

    private void BuildPresetMenu()
    {
        _menuPresets.Items.Clear();
        _menuPresets.Items.Add("Tombol Back Mouse (Global Windows)", null, (_, _) =>
        {
            AddRulePreset(new ButtonRule
            {
                Name = "Tombol Back Mouse (Global)",
                TriggerButton = TriggerMouseButton.XButton1,
                Action = TriggerAction.Back,
                Scope = RuleScope.Global,
            });
        });

        _menuPresets.Items.Add("Tombol Forward Mouse (Global Windows)", null, (_, _) =>
        {
            AddRulePreset(new ButtonRule
            {
                Name = "Tombol Forward Mouse (Global)",
                TriggerButton = TriggerMouseButton.XButton2,
                Action = TriggerAction.Forward,
                Scope = RuleScope.Global,
            });
        });

        _menuPresets.Items.Add("Klik Kanan -> Back di Granblue Fantasy (Chrome/Edge)", null, (_, _) =>
        {
            AddRulePreset(new ButtonRule
            {
                Name = "Klik Kanan -> Back di Granblue Fantasy",
                TriggerButton = TriggerMouseButton.Right,
                Action = TriggerAction.Back,
                Scope = RuleScope.SpecificDomains,
                AppExecutables = ["chrome.exe", "msedge.exe", "brave.exe"],
                Domains = ["game.granbluefantasy.jp"],
            });
        });

        _menuPresets.Items.Add("Tombol Back Mouse -> Tutup Tab (Ctrl+W) di Browser", null, (_, _) =>
        {
            AddRulePreset(new ButtonRule
            {
                Name = "Tombol Back -> Tutup Tab di Browser",
                TriggerButton = TriggerMouseButton.XButton1,
                Action = TriggerAction.CloseTab,
                Scope = RuleScope.SpecificApps,
                AppExecutables = ["chrome.exe", "msedge.exe", "firefox.exe", "brave.exe"],
            });
        });

        _menuPresets.Items.Add("Klik Tengah -> Buka Ulang Tab (Ctrl+Shift+T) di Browser", null, (_, _) =>
        {
            AddRulePreset(new ButtonRule
            {
                Name = "Klik Tengah -> Buka Ulang Tab Terakhir",
                TriggerButton = TriggerMouseButton.Middle,
                Action = TriggerAction.ReopenTab,
                Scope = RuleScope.SpecificApps,
                AppExecutables = ["chrome.exe", "msedge.exe", "firefox.exe", "brave.exe"],
            });
        });

        _menuPresets.Items.Add("Klik Kanan -> Buka URL Custom di Granblue Fantasy", null, (_, _) =>
        {
            AddRulePreset(new ButtonRule
            {
                Name = "Klik Kanan -> Buka URL Custom",
                TriggerButton = TriggerMouseButton.Right,
                Action = TriggerAction.OpenUrl,
                Url = "https://www.google.com",
                Scope = RuleScope.SpecificDomains,
                AppExecutables = ["chrome.exe", "msedge.exe", "brave.exe"],
                Domains = ["game.granbluefantasy.jp"],
            });
        });
    }

    private void BuildSettingsCard(Panel card)
    {
        _chkAutostart.Text = "Jalankan otomatis saat Windows mulai (Autostart)";
        _chkAutostart.Location = new Point(16, 34);
        _chkAutostart.Size = new Size(380, 22);
        card.Controls.Add(_chkAutostart);

        _chkShowOnStart.Text = "Tampilkan jendela pengaturan ini saat aplikasi dibuka";
        _chkShowOnStart.Location = new Point(16, 64);
        _chkShowOnStart.Size = new Size(380, 22);
        card.Controls.Add(_chkShowOnStart);

        card.Controls.Add(L("Tema Tampilan:", 480, 36, 120));
        _cmbTheme.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbTheme.Location = new Point(604, 32);
        _cmbTheme.Size = new Size(160, 23);
        _cmbTheme.Items.AddRange(["Terang (Light)", "Gelap (Dark)"]);
        _cmbTheme.SelectedIndexChanged += (_, _) =>
        {
            if (!_loading)
            {
                _theme = _themeOptions[Math.Max(_cmbTheme.SelectedIndex, 0)];
                ApplyTheme();
            }
        };
        card.Controls.Add(_cmbTheme);
    }

    // ---------- Tema & Status ----------

    private void ApplyTheme()
    {
        Theme.Apply(this, _theme);

        _header.BackColor = Theme.Accent;
        _lblTitle.ForeColor = Color.White;
        _lblSub.ForeColor = Color.FromArgb(220, 255, 255, 255);

        UpdateHotkeyUi();
        SetStatus(_monitor.IsCurrentlyActive);
        Invalidate(true);
    }

    private void SetStatus(bool active)
    {
        _chip.Text = active ? "●  AKTIF" : "○  NONAKTIF";
        _chip.BackColor = active ? Theme.StatusOn : Theme.StatusOff;
        _chip.ForeColor = Color.White;

        if (!_chkMasterEnable.Checked)
        {
            _lblDetail.Text = "Dinonaktifkan — tombol mouse bekerja dengan fungsi bawaan.";
            return;
        }

        _lblDetail.Text = active
            ? $"{_monitor.ActiveDetail}"
            : "Siap — menunggu tombol pemicu ditekan pada aplikasi / domain yang sesuai.";
    }

    private void OnMonitorStateChanged(bool active) => SetStatus(active);

    public void SyncEnabled(bool enabled)
    {
        _suppressEnableSync = true;
        _chkMasterEnable.Checked = enabled;
        _suppressEnableSync = false;
        SetStatus(_monitor.IsCurrentlyActive);
    }

    // ---------- Load / Bind Rules ----------

    private void LoadFromConfig()
    {
        _chkMasterEnable.Checked = Config.Enabled;
        _chkAutostart.Checked = Config.StartWithWindows;
        _chkShowOnStart.Checked = Config.ShowWindowOnStart;
        _theme = Config.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? UiTheme.Dark : UiTheme.Light;
        _cmbTheme.SelectedIndex = _theme == UiTheme.Dark ? 1 : 0;

        RefreshRulesList();

        if (Config.Rules.Count > 0)
        {
            _lstRules.SelectedIndex = 0;
        }
        else
        {
            OnAddRule(this, EventArgs.Empty);
        }
    }

    private void RefreshRulesList()
    {
        int sel = _lstRules.SelectedIndex;
        _lstRules.Items.Clear();

        for (int i = 0; i < Config.Rules.Count; i++)
        {
            var r = Config.Rules[i];
            string status = r.Enabled ? "[✓]" : "[  ]";
            string btn = r.TriggerType == TriggerInputType.Keyboard ? r.TriggerKey : r.TriggerButton switch
            {
                TriggerMouseButton.XButton1 => "Back Mouse",
                TriggerMouseButton.XButton2 => "Forward Mouse",
                TriggerMouseButton.Right => "Klik Kanan",
                TriggerMouseButton.Middle => "Klik Tengah",
                _ => "Mouse",
            };
            string act = r.Action switch
            {
                TriggerAction.Custom => r.CustomHotkey,
                TriggerAction.LeftClick => "Klik Kiri",
                TriggerAction.OpenUrl => $"URL: {r.Url}",
                _ => r.Action.ToString(),
            };
            string scope = r.Scope switch
            {
                RuleScope.Global => "Global",
                RuleScope.SpecificApps => string.Join(", ", r.AppExecutables.Take(2)),
                RuleScope.SpecificDomains => string.Join(", ", r.Domains.Take(1)),
                _ => "",
            };

            _lstRules.Items.Add($"{status} {r.Name} ({btn} → {act}) [{scope}]");
        }

        if (sel >= 0 && sel < _lstRules.Items.Count)
            _lstRules.SelectedIndex = sel;
    }

    private void OnRuleSelectionChanged(object? sender, EventArgs e)
    {
        if (_lstRules.SelectedIndex < 0 || _lstRules.SelectedIndex >= Config.Rules.Count)
            return;

        _selectedRule = Config.Rules[_lstRules.SelectedIndex];
        DisplayRuleInEditor(_selectedRule);
    }

    private void DisplayRuleInEditor(ButtonRule rule)
    {
        _loading = true;
        try
        {
            _txtRuleName.Text = rule.Name;
            _chkRuleEnabled.Checked = rule.Enabled;
            
            if (rule.TriggerType == TriggerInputType.Keyboard)
                _cmbButton.SelectedIndex = 4;
            else
                _cmbButton.SelectedIndex = Math.Clamp((int)rule.TriggerButton, 0, 3);
                
            _txtTriggerKey.Text = rule.TriggerKey;
            
            _cmbAction.SelectedIndex = Math.Clamp((int)rule.Action, 0, _cmbAction.Items.Count - 1);
            _txtHotkey.Text = rule.CustomHotkey;
            _txtUrl.Text = rule.Url;

            _rbGlobal.Checked = rule.Scope == RuleScope.Global;
            _rbSpecificApps.Checked = rule.Scope == RuleScope.SpecificApps;
            _rbSpecificDomains.Checked = rule.Scope == RuleScope.SpecificDomains;

            _txtAppExes.Text = string.Join(", ", rule.AppExecutables);

            _lstDomains.Items.Clear();
            foreach (var d in rule.Domains)
                _lstDomains.Items.Add(d);

            _chkEscape.Checked = rule.SendEscapeFirst;

            UpdateScopePanelsVisibility(rule.Scope);
            UpdateHotkeyUi();
        }
        finally
        {
            _loading = false;
        }
    }

    private void OnScopeChanged(object? sender, EventArgs e)
    {
        if (_loading || _selectedRule == null) return;

        RuleScope scope = RuleScope.Global;
        if (_rbSpecificApps.Checked) scope = RuleScope.SpecificApps;
        else if (_rbSpecificDomains.Checked) scope = RuleScope.SpecificDomains;

        _selectedRule.Scope = scope;
        UpdateScopePanelsVisibility(scope);
        RefreshRulesList();
    }

    private void UpdateScopePanelsVisibility(RuleScope scope)
    {
        _pnlApps.Visible = scope != RuleScope.Global;
        _pnlDomains.Visible = scope == RuleScope.SpecificDomains;

        if (scope == RuleScope.Global)
        {
            _chkEscape.Location = new Point(0, 215);
        }
        else if (scope == RuleScope.SpecificApps)
        {
            _chkEscape.Location = new Point(0, 270);
        }
        else
        {
            _chkEscape.Location = new Point(0, 372);
        }
    }

    private void UpdateHotkeyUi()
    {
        bool isCustom = _cmbAction.SelectedIndex == (int)TriggerAction.Custom;
        bool isOpenUrl = _cmbAction.SelectedIndex == (int)TriggerAction.OpenUrl;

        _txtHotkey.Visible = isCustom;
        _btnRecord.Visible = isCustom;
        _lblHotkeyHint.Visible = isCustom;

        _txtUrl.Visible = isOpenUrl;
        _lblUrlHint.Visible = isOpenUrl;

        if (!isCustom)
        {
            StopRecording();
        }
        else
        {
            _txtHotkey.BackColor = Theme.Field;
            _txtHotkey.ForeColor = Theme.FieldText;
        }

        if (isOpenUrl)
        {
            _txtUrl.BackColor = Theme.Field;
            _txtUrl.ForeColor = Theme.FieldText;
        }
    }

    // ---------- Rule Management Handlers ----------

    private void OnAddRule(object? sender, EventArgs e)
    {
        var newRule = new ButtonRule
        {
            Name = $"Aturan Baru #{Config.Rules.Count + 1}",
            TriggerButton = TriggerMouseButton.XButton1,
            Action = TriggerAction.Back,
            Scope = RuleScope.Global,
        };
        Config.Rules.Add(newRule);
        RefreshRulesList();
        _lstRules.SelectedIndex = Config.Rules.Count - 1;
    }

    private void OnDuplicateRule(object? sender, EventArgs e)
    {
        if (_selectedRule == null) return;
        var clone = _selectedRule.Clone();
        Config.Rules.Add(clone);
        RefreshRulesList();
        _lstRules.SelectedIndex = Config.Rules.Count - 1;
    }

    private void OnDeleteRule(object? sender, EventArgs e)
    {
        if (_selectedRule == null || Config.Rules.Count <= 1)
        {
            MessageBox.Show(this, "Minimal harus ada satu aturan pemetaan.", "Hapus Aturan",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int idx = _lstRules.SelectedIndex;
        Config.Rules.RemoveAt(idx);
        RefreshRulesList();
        _lstRules.SelectedIndex = Math.Clamp(idx, 0, Config.Rules.Count - 1);
    }

    private void OnShowPresets(object? sender, EventArgs e)
    {
        _menuPresets.Show(_btnPreset, new Point(0, _btnPreset.Height));
    }

    private void AddRulePreset(ButtonRule rule)
    {
        Config.Rules.Add(rule);
        RefreshRulesList();
        _lstRules.SelectedIndex = Config.Rules.Count - 1;
    }

    // ---------- App / Domain Helpers ----------

    private void OnBrowseApp(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Pilih Aplikasi Target (.exe)",
            Filter = "Executable (*.exe)|*.exe",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        string exe = BrowserHelper.NormalizeExeName(dlg.FileName);
        var exes = _txtAppExes.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(BrowserHelper.NormalizeExeName)
            .ToList();

        if (!exes.Contains(exe, StringComparer.OrdinalIgnoreCase))
            exes.Add(exe);

        _txtAppExes.Text = string.Join(", ", exes);
    }

    private void OnPickCurrentApp(object? sender, EventArgs e)
    {
        // Tampilkan dialog pemilihan dari window aktif yang sedang terbuka
        using var dlg = new Form
        {
            Text = "Pilih dari Window yang Sedang Terbuka",
            Size = new Size(460, 380),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
        };

        var lst = new ListBox { Location = new Point(14, 14), Size = new Size(416, 260) };
        var ok = new Button { Text = "Pilih", Location = new Point(244, 290), Size = new Size(88, 30), DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Batal", Location = new Point(342, 290), Size = new Size(88, 30), DialogResult = DialogResult.Cancel };
        dlg.Controls.AddRange([lst, ok, cancel]);

        var windows = EnumerateOpenWindows();
        foreach (var (exe, title) in windows)
        {
            lst.Items.Add($"{exe} — {title}");
        }

        if (lst.Items.Count > 0) lst.SelectedIndex = 0;

        if (dlg.ShowDialog(this) == DialogResult.OK && lst.SelectedIndex >= 0)
        {
            string chosen = windows[lst.SelectedIndex].Exe;
            var exes = _txtAppExes.Text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(BrowserHelper.NormalizeExeName)
                .ToList();

            if (!exes.Contains(chosen, StringComparer.OrdinalIgnoreCase))
                exes.Add(chosen);

            _txtAppExes.Text = string.Join(", ", exes);
        }
    }

    private static List<(string Exe, string Title)> EnumerateOpenWindows()
    {
        var list = new List<(string Exe, string Title)>();
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd))
                return true;

            var sb = new StringBuilder(256);
            NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
            string title = sb.ToString().Trim();
            if (string.IsNullOrEmpty(title))
                return true;

            string exe = UrlMonitor.GetProcessExeName(hwnd);
            if (string.IsNullOrEmpty(exe) || exe.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!list.Any(item => item.Exe.Equals(exe, StringComparison.OrdinalIgnoreCase)))
                list.Add((exe, title));

            return true;
        }, IntPtr.Zero);

        return list;
    }

    private void OnAddDomain(object? sender, EventArgs e)
    {
        string domain = Config.NormalizeDomain(_txtNewDomain.Text);
        if (domain.Length == 0 || _selectedRule == null)
            return;

        if (!_selectedRule.Domains.Any(d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)))
        {
            _selectedRule.Domains.Add(domain);
            _lstDomains.Items.Add(domain);
        }

        _txtNewDomain.Clear();
        _txtNewDomain.Focus();
        RefreshRulesList();
    }

    private void OnRemoveDomain(object? sender, EventArgs e)
    {
        if (_selectedRule == null || _lstDomains.SelectedItem is not string selected)
            return;

        _selectedRule.Domains.Remove(selected);
        _lstDomains.Items.Remove(selected);
        RefreshRulesList();
    }

    // ---------- Hotkey Recording ----------

    private void OnToggleRecord(object? sender, EventArgs e)
    {
        if (_recording)
        {
            StopRecording();
            return;
        }

        _recording = true;
        _btnRecord.Text = "Tekan tombol…";
        _txtHotkey.Text = "";
        Focus();
    }

    private void StopRecording()
    {
        _recording = false;
        _btnRecord.Text = "Rekam…";
    }

    private void OnToggleRecordTrigger(object? sender, EventArgs e)
    {
        if (_recordingTrigger)
        {
            StopRecordingTrigger();
            return;
        }

        _recordingTrigger = true;
        _btnRecordTriggerKey.Text = "Tekan tombol…";
        _txtTriggerKey.Text = "";
        Focus();
    }

    private void StopRecordingTrigger()
    {
        _recordingTrigger = false;
        _btnRecordTriggerKey.Text = "Rekam Pemicu";
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_recording && !_recordingTrigger)
            return;

        e.Handled = true;
        e.SuppressKeyPress = true;

        if (e.KeyCode == Keys.Escape)
        {
            if (_recording)
            {
                StopRecording();
                _txtHotkey.Text = _selectedRule?.CustomHotkey ?? "Alt+Left";
            }
            if (_recordingTrigger)
            {
                StopRecordingTrigger();
                _txtTriggerKey.Text = _selectedRule?.TriggerKey ?? "F5";
            }
            return;
        }

        if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            return;

        var parts = new List<string>();
        if (e.Control) parts.Add("Ctrl");
        if (e.Alt) parts.Add("Alt");
        if (e.Shift) parts.Add("Shift");

        string keyName = e.KeyCode switch
        {
            Keys.D0 or Keys.D1 or Keys.D2 or Keys.D3 or Keys.D4 or
            Keys.D5 or Keys.D6 or Keys.D7 or Keys.D8 or Keys.D9 => ((char)('0' + (e.KeyCode - Keys.D0))).ToString(),
            Keys.Return => "Enter",
            Keys.Back => "Backspace",
            Keys.Prior => "PageUp",
            Keys.Next => "PageDown",
            Keys.Escape => "Esc",
            _ => e.KeyCode.ToString(),
        };
        if (parts.Count > 0)
            keyName = string.Join("+", parts) + "+" + keyName;

        if (_recording)
        {
            _txtHotkey.Text = keyName;
            if (_selectedRule != null)
                _selectedRule.CustomHotkey = keyName;
            StopRecording();
        }
        else if (_recordingTrigger)
        {
            // Pemicu sebaiknya tidak menggunakan modifier kompleks, gunakan nama key asli saja.
            _txtTriggerKey.Text = e.KeyCode.ToString();
            if (_selectedRule != null)
                _selectedRule.TriggerKey = e.KeyCode.ToString();
            StopRecordingTrigger();
        }
        
        RefreshRulesList();
    }

    // ---------- Test & Save ----------

    private void OnTestAction(object? sender, EventArgs e)
    {
        if (_selectedRule != null)
        {
            if (!ActionSender.SendForRule(_selectedRule))
            {
                MessageBox.Show(this, "Shortcut tombol tidak valid.", "Tes Aksi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        else
        {
            ActionSender.SendForAction(TriggerAction.Back);
        }

        _btnTest.Text = "Terkirim ✓";
        _testTimer.Start();
    }

    private void OnOpenConfigFolder(object? sender, EventArgs e)
    {
        try
        {
            string path = Config.ConfigFilePath;
            string args = System.IO.File.Exists(path)
                ? $"/select,\"{path}\""
                : $"/select,\"{System.IO.Path.GetDirectoryName(path)}\"";
            System.Diagnostics.Process.Start("explorer.exe", args);
        }
        catch
        {
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        // Validasi aturan kustom
        foreach (var rule in Config.Rules)
        {
            if (rule.Action == TriggerAction.Custom &&
                !HotkeyParser.TryParse(rule.CustomHotkey, out _, out string err))
            {
                MessageBox.Show(this, $"Aturan '{rule.Name}' memiliki kombinasi shortcut kustom yang tidak valid:\n{err}",
                    "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (rule.Action == TriggerAction.OpenUrl)
            {
                string url = rule.Url.Trim();
                if (url.Length == 0)
                {
                    MessageBox.Show(this, $"Aturan '{rule.Name}' aksi 'Buka URL' tetapi URL kosong.\nIsi URL terlebih dulu.",
                        "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!Uri.TryCreate(url, UriKind.Absolute, out _) &&
                    !url.Contains("://", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, $"Aturan '{rule.Name}' URL tidak valid: \"{url}\".\nGunakan format https://contoh.com",
                        "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
        }

        Config.Enabled = _chkMasterEnable.Checked;
        Config.StartWithWindows = _chkAutostart.Checked;
        Config.ShowWindowOnStart = _chkShowOnStart.Checked;
        Config.Theme = _theme == UiTheme.Dark ? "Dark" : "Light";

        if (!Config.Save())
        {
            MessageBox.Show(this,
                "Gagal menulis file config.json. Pengaturan tetap aktif selama sesi ini berjalan.",
                "Simpan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        _btnSave.Text = "Tersimpan ✓";
        _saveTimer.Start();
        SetStatus(_monitor.IsCurrentlyActive);
    }
}
