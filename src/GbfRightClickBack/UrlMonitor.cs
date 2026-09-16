using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GbfRightClickBack.UrlProviders;

namespace GbfRightClickBack;

/// <summary>
/// Mesin utama pemantau tombol mouse dan evaluasi aturan pemicu:
/// - Menerima event mouse hook (XButton1, XButton2, Klik Kanan, Klik Tengah).
/// - Mengecek apakah ada aturan aktif di Config.Rules yang cocok dengan window/scope saat ini (Global, Aplikasi Tertentu, atau Domain Web Tertentu).
/// - Jika cocok: blokir klik asli dan kirim aksi yang dikonfigurasi (Back, Forward, shortcut kustom, dll).
/// </summary>
internal sealed class UrlMonitor : IDisposable
{
    private readonly MouseHook _mouseHook = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly ForegroundTracker _fgTracker = new();
    private readonly List<IUrlProvider> _providers = new();

    private TriggerMouseButton? _downBlockedButton;
    private string? _downBlockedKey;
    private long _downBlockedTimeMs;

    public bool Enabled { get; set; } = true;

    public bool IsCurrentlyActive { get; private set; }
    public string ActiveDetail { get; private set; } = "Siap";

    /// <summary>Dipanggil saat status aktif berubah (untuk tray & status UI).</summary>
    public event Action<bool>? StateChanged;

    public UrlMonitor()
    {
        _mouseHook.OnMouseAction += OnMouseAction;
        _keyboardHook.OnKeyboardAction += OnKeyboardAction;
        _fgTracker.ForegroundChanged += OnForegroundChanged;

        // Provider UIA selalu ada (tanpa setup khusus).
        var uia = new UiaUrlProvider();
        _providers.Add(uia);

        // CDP jika browser dijalankan dengan remote debugging.
        try
        {
            if (CdpUrlProvider.IsAvailable())
            {
                _providers.Add(new CdpUrlProvider());
            }
        }
        catch
        {
            // biarkan tanpa CDP
        }
    }

    public void ReloadProviders()
    {
        for (int i = _providers.Count - 1; i >= 0; i--)
        {
            if (_providers[i] is CdpUrlProvider cdp)
            {
                cdp.Dispose();
                _providers.RemoveAt(i);
            }
        }

        try
        {
            if (CdpUrlProvider.IsAvailable())
            {
                _providers.Add(new CdpUrlProvider());
            }
        }
        catch
        {
        }
    }

    public void Start()
    {
        _mouseHook.Install();
        _keyboardHook.Install();
        _fgTracker.Start();
        RefreshActiveState();
    }

    private bool OnMouseAction(int msg, NativeMethods.MSLLHOOKSTRUCT data)
    {
        if (!Enabled)
            return false;

        switch (msg)
        {
            case NativeMethods.WM_RBUTTONDOWN:
                return HandleTriggerDown(TriggerMouseButton.Right);
            case NativeMethods.WM_RBUTTONUP:
                return HandleTriggerUp(TriggerMouseButton.Right);

            case NativeMethods.WM_MBUTTONDOWN:
                return HandleTriggerDown(TriggerMouseButton.Middle);
            case NativeMethods.WM_MBUTTONUP:
                return HandleTriggerUp(TriggerMouseButton.Middle);

            case NativeMethods.WM_XBUTTONDOWN:
            {
                ushort x = (ushort)((data.mouseData >> 16) & 0xFFFF);
                var which = x == NativeMethods.XBUTTON2 ? TriggerMouseButton.XButton2 : TriggerMouseButton.XButton1;
                return HandleTriggerDown(which);
            }
            case NativeMethods.WM_XBUTTONUP:
            {
                ushort xUp = (ushort)((data.mouseData >> 16) & 0xFFFF);
                var whichUp = xUp == NativeMethods.XBUTTON2 ? TriggerMouseButton.XButton2 : TriggerMouseButton.XButton1;
                return HandleTriggerUp(whichUp);
            }

            default:
                return false;
        }
    }

    private bool OnKeyboardAction(int msg, NativeMethods.KBDLLHOOKSTRUCT data)
    {
        if (!Enabled)
            return false;

        // Jangan proses event injected dari SendInput kita sendiri
        if ((data.flags & 0x10) != 0) // LLKHF_INJECTED
            return false;

        string keyName = ((System.Windows.Forms.Keys)data.vkCode).ToString();

        if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
            return HandleTriggerDown(TriggerInputType.Keyboard, null, keyName);
        else if (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP)
            return HandleTriggerUp(TriggerInputType.Keyboard, null, keyName);

        return false;
    }

    private bool HandleTriggerDown(TriggerInputType type, TriggerMouseButton? button = null, string? key = null)
    {
        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return false;

        if (FindMatchingRule(type, button, key, hwnd, out ButtonRule? matchedRule) && matchedRule != null)
        {
            if (type == TriggerInputType.Mouse)
                _downBlockedButton = button;
            else
                _downBlockedKey = key;
                
            _downBlockedTimeMs = Environment.TickCount64;

            // Eksekusi aksi yang ditentukan aturan
            ActionSender.SendForRule(matchedRule, hwnd);
            SetActiveState(true, $"Aktif: {matchedRule.Name}");

            // Blokir tombol asli agar perilaku default Windows tidak muncul
            return true;
        }

        if (type == TriggerInputType.Mouse) _downBlockedButton = null;
        else _downBlockedKey = null;
        
        return false;
    }

    private bool HandleTriggerUp(TriggerInputType type, TriggerMouseButton? button = null, string? key = null)
    {
        bool isBlocked = type == TriggerInputType.Mouse 
            ? _downBlockedButton == button 
            : _downBlockedKey == key;

        if (isBlocked && (Environment.TickCount64 - _downBlockedTimeMs) < 2000)
        {
            if (type == TriggerInputType.Mouse) _downBlockedButton = null;
            else _downBlockedKey = null;
            return true; // Blokir UP untuk mencegah menu muncul
        }

        if (type == TriggerInputType.Mouse) _downBlockedButton = null;
        else _downBlockedKey = null;
        return false;
    }

    private bool HandleTriggerDown(TriggerMouseButton button) => HandleTriggerDown(TriggerInputType.Mouse, button);
    private bool HandleTriggerUp(TriggerMouseButton button) => HandleTriggerUp(TriggerInputType.Mouse, button);

    /// <summary>
    /// Evaluasi aturan pemetaan tombol yang cocok dengan tombol dan window/scope saat ini.
    /// </summary>
    private bool FindMatchingRule(TriggerInputType type, TriggerMouseButton? button, string? key, IntPtr hwnd, out ButtonRule? matchedRule)
    {
        matchedRule = null;
        if (hwnd == IntPtr.Zero)
            return false;

        string procExe = GetProcessExeName(hwnd);

        foreach (var rule in Config.Rules)
        {
            if (!rule.Enabled || rule.TriggerType != type)
                continue;

            if (type == TriggerInputType.Mouse && rule.TriggerButton != button)
                continue;
            if (type == TriggerInputType.Keyboard && !string.Equals(rule.TriggerKey, key, StringComparison.OrdinalIgnoreCase))
                continue;

            switch (rule.Scope)
            {
                case RuleScope.Global:
                    matchedRule = rule;
                    return true;

                case RuleScope.SpecificApps:
                    if (rule.AppExecutables.Any(exe => exe.Equals(procExe, StringComparison.OrdinalIgnoreCase) || exe == "*"))
                    {
                        matchedRule = rule;
                        return true;
                    }
                    break;

                case RuleScope.SpecificDomains:
                    bool isBrowser = rule.AppExecutables.Count == 0 ||
                                     rule.AppExecutables.Any(exe => exe.Equals(procExe, StringComparison.OrdinalIgnoreCase) || exe == "*") ||
                                     Config.BrowserExecutables.Any(exe => exe.Equals(procExe, StringComparison.OrdinalIgnoreCase));
                    if (isBrowser)
                    {
                        foreach (var provider in _providers)
                        {
                            string? url = provider.GetActiveTabUrl(hwnd);
                            if (url != null && IsUrlInDomains(url, rule.Domains))
                            {
                                matchedRule = rule;
                                return true;
                            }
                        }
                    }
                    break;
            }
        }

        return false;
    }

    private void OnForegroundChanged(IntPtr hwnd)
    {
        if (!Enabled || hwnd == IntPtr.Zero)
        {
            SetActiveState(false, "Nonaktif");
            return;
        }

        string procExe = GetProcessExeName(hwnd);
        bool anyActive = false;
        string detail = "Siap";

        foreach (var rule in Config.Rules)
        {
            if (!rule.Enabled) continue;

            if (rule.Scope == RuleScope.Global)
            {
                anyActive = true;
                detail = $"Aktif: {rule.Name} (Global)";
                break;
            }

            if (rule.Scope == RuleScope.SpecificApps &&
                rule.AppExecutables.Any(exe => exe.Equals(procExe, StringComparison.OrdinalIgnoreCase) || exe == "*"))
            {
                anyActive = true;
                detail = $"Aktif: {rule.Name} ({procExe})";
                break;
            }

            if (rule.Scope == RuleScope.SpecificDomains)
            {
                bool isBrowser = rule.AppExecutables.Count == 0 ||
                                 rule.AppExecutables.Any(exe => exe.Equals(procExe, StringComparison.OrdinalIgnoreCase) || exe == "*") ||
                                 Config.BrowserExecutables.Any(exe => exe.Equals(procExe, StringComparison.OrdinalIgnoreCase));
                if (isBrowser)
                {
                    foreach (var provider in _providers)
                    {
                        string? url = provider.GetActiveTabUrl(hwnd);
                        if (url != null && IsUrlInDomains(url, rule.Domains))
                        {
                            anyActive = true;
                            detail = $"Aktif: {rule.Name} ({url})";
                            break;
                        }
                    }
                }
                if (anyActive) break;
            }
        }

        SetActiveState(anyActive, detail);
    }

    public void RefreshActiveState()
    {
        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        OnForegroundChanged(hwnd);
    }

    private void SetActiveState(bool active, string detail)
    {
        ActiveDetail = detail;
        if (IsCurrentlyActive != active)
        {
            IsCurrentlyActive = active;
            StateChanged?.Invoke(active);
        }
    }

    public static string GetProcessExeName(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return "";

        uint threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        if (threadId == 0 || pid == 0)
            return "";

        IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProcess == IntPtr.Zero)
            return "";

        try
        {
            var sb = new StringBuilder(1024);
            int size = sb.Capacity;
            if (NativeMethods.QueryFullProcessImageName(hProcess, 0, sb, ref size))
            {
                return System.IO.Path.GetFileName(sb.ToString());
            }
            return "";
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }

    public static (string ProcessExe, string WindowTitle) GetActiveWindowInfo()
    {
        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return ("", "");

        string procExe = GetProcessExeName(hwnd);
        var sb = new StringBuilder(512);
        NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
        return (procExe, sb.ToString());
    }

    public static bool IsUrlInDomains(string? url, List<string> domains)
    {
        if (string.IsNullOrWhiteSpace(url) || domains == null || domains.Count == 0)
            return false;

        try
        {
            var uri = new Uri(url);
            foreach (string domain in domains)
            {
                if (string.Equals(uri.Host, domain, StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
        {
            // fallback match substring
            foreach (string domain in domains)
            {
                if (UrlContainsDomain(url, domain))
                    return true;
            }
        }

        return false;
    }

    private static bool UrlContainsDomain(string url, string domain)
    {
        int idx = url.IndexOf(domain, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return false;

        if (idx > 0)
        {
            char prev = url[idx - 1];
            if (char.IsLetterOrDigit(prev) || prev == '.' || prev == '-')
                return false;
        }

        int end = idx + domain.Length;
        if (end < url.Length)
        {
            char next = url[end];
            if (char.IsLetterOrDigit(next) || next == '.' || next == '-')
                return false;
        }

        return true;
    }

    public void Dispose()
    {
        _mouseHook.OnMouseAction -= OnMouseAction;
        _fgTracker.ForegroundChanged -= OnForegroundChanged;

        _mouseHook.Dispose();
        _fgTracker.Dispose();

        foreach (var provider in _providers)
        {
            (provider as IDisposable)?.Dispose();
        }
        _providers.Clear();
    }

    public string GetDiagnostic()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Back Button Customizer - Diagnostic ===");
        sb.AppendLine($"Enabled: {Enabled}");
        sb.AppendLine($"IsCurrentlyActive: {IsCurrentlyActive} ({ActiveDetail})");
        sb.AppendLine($"Total Rules: {Config.Rules.Count}");
        foreach (var rule in Config.Rules)
        {
            sb.AppendLine($"  - [{ (rule.Enabled ? "ON" : "OFF") }] {rule.Name} | Button: {rule.TriggerButton} -> Action: {rule.Action} ({rule.CustomHotkey}) | Scope: {rule.Scope}");
        }
        sb.AppendLine();

        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        sb.AppendLine($"Foreground HWND: {hwnd}");
        if (hwnd != IntPtr.Zero)
        {
            string proc = GetProcessExeName(hwnd);
            sb.AppendLine($"Foreground Process: {proc}");
            var winInfo = GetActiveWindowInfo();
            sb.AppendLine($"Window Title: {winInfo.WindowTitle}");

            foreach (var provider in _providers)
            {
                string? url = null;
                string? error = null;
                try
                {
                    url = provider.GetActiveTabUrl(hwnd);
                    if (provider is UiaUrlProvider uia)
                        error = uia.LastError;
                }
                catch (Exception ex)
                {
                    url = $"(error: {ex.Message})";
                }
                string providerName = provider.GetType().Name;
                sb.AppendLine($"[{providerName}] URL: {url ?? "(null)"}");
                if (error != null)
                    sb.AppendLine($"[{providerName}] Error: {error}");
            }
        }
        else
        {
            sb.AppendLine("(No foreground window)");
        }

        return sb.ToString();
    }
}
