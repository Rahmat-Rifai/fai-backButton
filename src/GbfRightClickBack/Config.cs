using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace GbfRightClickBack;

/// <summary>Tombol mouse yang bisa dipilih sebagai pemicu.</summary>
internal enum TriggerMouseButton
{
    XButton1 = 0, // Tombol samping 1 (Back mouse)
    XButton2 = 1, // Tombol samping 2 (Forward mouse)
    Right = 2,    // Klik kanan
    Middle = 3,   // Klik tengah (scroll wheel click)
}

/// <summary>Tipe pemicu (Mouse atau Keyboard).</summary>
internal enum TriggerInputType
{
    Mouse = 0,
    Keyboard = 1,
}

/// <summary>Aksi yang dikirim saat tombol pemicu ditekan.</summary>
internal enum TriggerAction
{
    Back = 0,
    Forward = 1,
    CloseTab = 2,
    ReopenTab = 3,
    Reload = 4,
    NewTab = 5,
    SwitchTab = 6,
    Copy = 7,
    Paste = 8,
    Undo = 9,
    Enter = 10,
    Escape = 11,
    LeftClick = 12,
    Custom = 13,
    OpenUrl = 14,
}

/// <summary>Cakupan target berlakunya aturan tombol.</summary>
internal enum RuleScope
{
    Global = 0,          // Berlaku untuk semua aplikasi di Windows
    SpecificApps = 1,    // Hanya aplikasi tertentu (berdasarkan nama file .exe)
    SpecificDomains = 2, // Hanya website/domain tertentu saat browser aktif
}

/// <summary>Satu aturan pemetaan tombol mouse ke aksi tertentu.</summary>
internal sealed class ButtonRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Aturan Baru";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("triggerButton")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TriggerMouseButton TriggerButton { get; set; } = TriggerMouseButton.XButton1;

    [JsonPropertyName("triggerType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TriggerInputType TriggerType { get; set; } = TriggerInputType.Mouse;

    [JsonPropertyName("triggerKey")]
    public string TriggerKey { get; set; } = "";

    [JsonPropertyName("action")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TriggerAction Action { get; set; } = TriggerAction.Back;

    [JsonPropertyName("customHotkey")]
    public string CustomHotkey { get; set; } = "Alt+Left";

    /// <summary>URL tujuan untuk aksi OpenUrl (navigasi tab browser aktif).</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = "https://www.google.com";

    [JsonPropertyName("scope")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RuleScope Scope { get; set; } = RuleScope.Global;

    [JsonPropertyName("appExecutables")]
    public List<string> AppExecutables { get; set; } = ["chrome.exe", "msedge.exe"];

    [JsonPropertyName("domains")]
    public List<string> Domains { get; set; } = ["game.granbluefantasy.jp"];

    [JsonPropertyName("sendEscapeFirst")]
    public bool SendEscapeFirst { get; set; }

    public ButtonRule Clone()
    {
        return new ButtonRule
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = Name + " (Salinan)",
            Enabled = Enabled,
            TriggerButton = TriggerButton,
            TriggerType = TriggerType,
            TriggerKey = TriggerKey,
            Action = Action,
            CustomHotkey = CustomHotkey,
            Url = Url,
            Scope = Scope,
            AppExecutables = new List<string>(AppExecutables),
            Domains = new List<string>(Domains),
            SendEscapeFirst = SendEscapeFirst,
        };
    }
}

/// <summary>
/// Konfigurasi aplikasi. Semua nilai bisa diubah via GUI Pengaturan
/// atau dengan mengedit config.json.
/// </summary>
internal static class Config
{
    private static string? _configPath;

    /// <summary>Lokasi file config.json yang dipakai.</summary>
    public static string ConfigFilePath => _configPath ?? DetermineConfigPath();

    // ---------- Daftar Aturan (Multi-Rule) ----------

    /// <summary>Daftar aturan pemetaan tombol aktif.</summary>
    public static List<ButtonRule> Rules { get; set; } = CreateDefaultRules();

    // ---------- Pengaturan Umum ----------

    /// <summary>Daftar nama file exe browser target (mis. chrome.exe, msedge.exe).</summary>
    public static List<string> BrowserExecutables { get; set; } = ["chrome.exe", "msedge.exe", "brave.exe"];

    /// <summary>Nama tampilan aplikasi untuk tray/status.</summary>
    public static string BrowserDisplayName { get; set; } = "Back Button Customizer";

    /// <summary>Port Chrome DevTools Protocol (CDP), default 9222.</summary>
    public static int CdpPort { get; set; } = 9222;

    /// <summary>Timeout koneksi CDP (ms).</summary>
    public static int CdpTimeoutMs { get; set; } = 500;

    /// <summary>Timeout query UIA (ms).</summary>
    public static int UiaTimeoutMs { get; set; } = 1500;

    /// <summary>Tampilkan jendela pengaturan setiap kali aplikasi dibuka (default: aktif).</summary>
    public static bool ShowWindowOnStart { get; set; } = true;

    /// <summary>Tema UI jendela: "Light" atau "Dark".</summary>
    public static string Theme { get; set; } = "Light";

    /// <summary>Master switch aktif/nonaktif aplikasi.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Jalankan otomatis saat Windows login.</summary>
    public static bool StartWithWindows
    {
        get => AutostartHelper.IsAutostartEnabled();
        set => AutostartHelper.SetAutostart(value);
    }

    // ---------- Legacy Compatibility Properties ----------
    public static TriggerMouseButton TriggerButton
    {
        get => Rules.FirstOrDefault()?.TriggerButton ?? TriggerMouseButton.XButton1;
        set { if (Rules.Count > 0) Rules[0].TriggerButton = value; }
    }

    public static TriggerAction TriggerAction
    {
        get => Rules.FirstOrDefault()?.Action ?? TriggerAction.Back;
        set { if (Rules.Count > 0) Rules[0].Action = value; }
    }

    public static string CustomHotkey
    {
        get => Rules.FirstOrDefault()?.CustomHotkey ?? "Alt+Left";
        set { if (Rules.Count > 0) Rules[0].CustomHotkey = value; }
    }

    public static List<string> TriggerDomains
    {
        get => Rules.FirstOrDefault(r => r.Scope == RuleScope.SpecificDomains)?.Domains ?? ["game.granbluefantasy.jp"];
        set
        {
            var r = Rules.FirstOrDefault(r => r.Scope == RuleScope.SpecificDomains);
            if (r != null) r.Domains = value;
        }
    }

    public static bool SendEscapeFirst
    {
        get => Rules.FirstOrDefault()?.SendEscapeFirst ?? false;
        set { if (Rules.Count > 0) Rules[0].SendEscapeFirst = value; }
    }

    // ---------- Default Rules ----------

    public static List<ButtonRule> CreateDefaultRules()
    {
        return
        [
            new ButtonRule
            {
                Id = "default-back-global",
                Name = "Tombol Back Mouse (Global)",
                Enabled = true,
                TriggerButton = TriggerMouseButton.XButton1,
                Action = TriggerAction.Back,
                CustomHotkey = "Alt+Left",
                Scope = RuleScope.Global,
                AppExecutables = ["*"],
                Domains = [],
                SendEscapeFirst = false,
            },
            new ButtonRule
            {
                Id = "default-forward-global",
                Name = "Tombol Forward Mouse (Global)",
                Enabled = true,
                TriggerButton = TriggerMouseButton.XButton2,
                Action = TriggerAction.Forward,
                CustomHotkey = "Alt+Right",
                Scope = RuleScope.Global,
                AppExecutables = ["*"],
                Domains = [],
                SendEscapeFirst = false,
            },
            new ButtonRule
            {
                Id = "default-gbf-rightclick",
                Name = "Klik Kanan -> Back di Granblue Fantasy",
                Enabled = true,
                TriggerButton = TriggerMouseButton.Right,
                Action = TriggerAction.Back,
                CustomHotkey = "Alt+Left",
                Scope = RuleScope.SpecificDomains,
                AppExecutables = ["chrome.exe", "msedge.exe", "brave.exe"],
                Domains = ["game.granbluefantasy.jp"],
                SendEscapeFirst = false,
            }
        ];
    }

    // ---------- Persistensi ----------

    public static void Load()
    {
        string path = ConfigFilePath;
        try
        {
            if (!File.Exists(path))
            {
                Save();
                return;
            }

            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            string json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<SettingsDto>(json, opts);
            if (dto != null)
                Apply(dto);
        }
        catch
        {
            // Config rusak -> pakai default
        }
    }

    public static bool Save()
    {
        try
        {
            var dto = new SettingsDto
            {
                Rules = Rules,
                BrowserExecutables = BrowserExecutables,
                BrowserDisplayName = BrowserDisplayName,
                CdpPort = CdpPort,
                CdpTimeoutMs = CdpTimeoutMs,
                UiaTimeoutMs = UiaTimeoutMs,
                ShowWindowOnStart = ShowWindowOnStart,
                Theme = Theme,
                Enabled = Enabled,
                StartWithWindows = StartWithWindows,
                // Legacy support
                TriggerButton = TriggerButton.ToString(),
                TriggerAction = TriggerAction.ToString(),
                CustomHotkey = CustomHotkey,
                TriggerDomains = TriggerDomains,
                SendEscapeFirst = SendEscapeFirst,
            };

            var opts = new JsonSerializerOptions { WriteIndented = true };
            string dir = Path.GetDirectoryName(ConfigFilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(dto, opts));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void Apply(SettingsDto dto)
    {
        try
        {
            if (dto.Rules is { Count: > 0 })
            {
                Rules = dto.Rules;
            }
            else
            {
                // Migrasi dari config legacy versi 1
                Rules = CreateDefaultRules();
                if (!string.IsNullOrEmpty(dto.TriggerButton) &&
                    Enum.TryParse(dto.TriggerButton, ignoreCase: true, out TriggerMouseButton legacyBtn))
                {
                    var gbfRule = Rules.FirstOrDefault(r => r.Id == "default-gbf-rightclick");
                    if (gbfRule != null)
                    {
                        gbfRule.TriggerButton = legacyBtn;
                        if (!string.IsNullOrEmpty(dto.TriggerAction) &&
                            Enum.TryParse(dto.TriggerAction, ignoreCase: true, out TriggerAction legacyAct))
                            gbfRule.Action = legacyAct;
                        if (!string.IsNullOrWhiteSpace(dto.CustomHotkey))
                            gbfRule.CustomHotkey = dto.CustomHotkey.Trim();
                        if (dto.TriggerDomains is { Count: > 0 })
                            gbfRule.Domains = dto.TriggerDomains;
                        if (dto.SendEscapeFirst.HasValue)
                            gbfRule.SendEscapeFirst = dto.SendEscapeFirst.Value;
                    }
                }
            }

            if (dto.BrowserExecutables is { Count: > 0 })
                BrowserExecutables = dto.BrowserExecutables
                    .Select(BrowserHelper.NormalizeExeName)
                    .Where(s => s.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (!string.IsNullOrWhiteSpace(dto.BrowserDisplayName))
                BrowserDisplayName = dto.BrowserDisplayName.Trim();

            if (dto.CdpPort is > 0 and <= 65535)
                CdpPort = dto.CdpPort.Value;

            if (dto.CdpTimeoutMs is >= 100 and <= 30000)
                CdpTimeoutMs = dto.CdpTimeoutMs.Value;

            if (dto.UiaTimeoutMs is >= 100 and <= 30000)
                UiaTimeoutMs = dto.UiaTimeoutMs.Value;

            if (!string.IsNullOrWhiteSpace(dto.Theme))
                Theme = dto.Theme.Trim();

            if (dto.ShowWindowOnStart.HasValue)
                ShowWindowOnStart = dto.ShowWindowOnStart.Value;

            if (dto.Enabled.HasValue)
                Enabled = dto.Enabled.Value;
        }
        catch
        {
            // Pertahankan default jika terjadi parsing error
        }
    }

    public static string NormalizeDomain(string raw)
    {
        string s = raw.Trim();
        if (s.Length == 0)
            return "";

        int scheme = s.IndexOf("://", StringComparison.Ordinal);
        if (scheme >= 0)
            s = s[(scheme + 3)..];

        int slash = s.IndexOf('/');
        if (slash >= 0)
            s = s[..slash];

        return s.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static string DetermineConfigPath()
    {
        string exeDir = AppContext.BaseDirectory;
        string exePath = Path.Combine(exeDir, "config.json");
        if (File.Exists(exePath))
        {
            _configPath = exePath;
            return exePath;
        }

        if (IsDirectoryWritable(exeDir))
        {
            _configPath = exePath;
            return exePath;
        }

        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BackButtonCustomizer", "config.json");
        if (File.Exists(appDataPath))
        {
            _configPath = appDataPath;
            return appDataPath;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(appDataPath)!);
        _configPath = appDataPath;
        return appDataPath;
    }

    private static bool IsDirectoryWritable(string dir)
    {
        try
        {
            string probe = Path.Combine(dir, $".cfg-probe-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "probe");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class SettingsDto
    {
        [JsonPropertyName("rules")] public List<ButtonRule>? Rules { get; set; }
        [JsonPropertyName("browserExecutables")] public List<string>? BrowserExecutables { get; set; }
        [JsonPropertyName("browserDisplayName")] public string? BrowserDisplayName { get; set; }
        [JsonPropertyName("cdpPort")] public int? CdpPort { get; set; }
        [JsonPropertyName("cdpTimeoutMs")] public int? CdpTimeoutMs { get; set; }
        [JsonPropertyName("uiaTimeoutMs")] public int? UiaTimeoutMs { get; set; }
        [JsonPropertyName("showWindowOnStart")] public bool? ShowWindowOnStart { get; set; }
        [JsonPropertyName("theme")] public string? Theme { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("startWithWindows")] public bool? StartWithWindows { get; set; }

        // Legacy compatibility
        [JsonPropertyName("triggerButton")] public string? TriggerButton { get; set; }
        [JsonPropertyName("triggerAction")] public string? TriggerAction { get; set; }
        [JsonPropertyName("customHotkey")] public string? CustomHotkey { get; set; }
        [JsonPropertyName("triggerDomains")] public List<string>? TriggerDomains { get; set; }
        [JsonPropertyName("sendEscapeFirst")] public bool? SendEscapeFirst { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
    }
}

/// <summary>Helper untuk mengatur autostart di Windows (HKCU Run key).</summary>
internal static class AutostartHelper
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "BackButtonCustomizer";

    public static bool IsAutostartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetAutostart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return false;

            if (enable)
            {
                string exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(exePath)) return false;
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>Helper normalisasi nama exe browser.</summary>
internal static class BrowserHelper
{
    public static string NormalizeExeName(string raw)
    {
        string s = raw.Trim().Trim('"');
        if (s.Length == 0)
            return "";

        s = Path.GetFileName(s);
        if (!s.Contains('.'))
            s += ".exe";
        return s;
    }
}
