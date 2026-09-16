using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GbfRightClickBack.UrlProviders;

namespace GbfRightClickBack;

/// <summary>
/// Mengirim aksi yang dikonfigurasi (Back, Forward, tutup tab, kombinasi kustom, dll)
/// ke foreground window menggunakan SendInput.
/// Semua keydown selalu ditutup dengan keyup (reverse order) agar tidak ada stuck key.
/// </summary>
internal static class ActionSender
{
    /// <summary>
    /// Ambil string kombinasi tombol untuk sebuah aksi.
    /// </summary>
    public static string GetHotkeyString(TriggerAction action, string? customHotkey = null) => action switch
    {
        TriggerAction.Back => "Alt+Left",
        TriggerAction.Forward => "Alt+Right",
        TriggerAction.CloseTab => "Ctrl+W",
        TriggerAction.ReopenTab => "Ctrl+Shift+T",
        TriggerAction.Reload => "F5",
        TriggerAction.NewTab => "Ctrl+T",
        TriggerAction.SwitchTab => "Ctrl+Tab",
        TriggerAction.Copy => "Ctrl+C",
        TriggerAction.Paste => "Ctrl+V",
        TriggerAction.Undo => "Ctrl+Z",
        TriggerAction.Enter => "Enter",
        TriggerAction.Escape => "Escape",
        TriggerAction.Custom => !string.IsNullOrWhiteSpace(customHotkey) ? customHotkey : Config.CustomHotkey,
        _ => "Alt+Left",
    };

    /// <summary>
    /// Kirim aksi berdasarkan objek ButtonRule.
    /// </summary>
    public static bool SendForRule(ButtonRule rule)
    {
        return SendForRule(rule, IntPtr.Zero);
    }

    /// Kirim aksi berdasarkan objek ButtonRule.
    /// 
    /// @param hwnd Handle window browser aktif (IntPtr.Zero untuk fallback ke browser default).
    /// </summary>
    public static bool SendForRule(ButtonRule rule, IntPtr hwnd)
    {
        if (rule.Action == TriggerAction.OpenUrl)
        {
            return OpenUrlAction(rule, hwnd);
        }
        else if (rule.Action == TriggerAction.LeftClick)
        {
            if (rule.SendEscapeFirst)
                SendSteps([new KeyStep(NativeMethods.VK_ESCAPE, IsExtended: false)]);
            SendLeftClick();
            return true;
        }

        string spec = GetHotkeyString(rule.Action, rule.CustomHotkey);
        if (!HotkeyParser.TryParse(spec, out List<KeyStep> steps, out _))
            return false;

        if (rule.SendEscapeFirst)
            SendSteps([new KeyStep(NativeMethods.VK_ESCAPE, IsExtended: false)]);

        SendSteps(steps);
        return true;
    }

    private static bool OpenUrlAction(ButtonRule rule, IntPtr hwnd)
    {
        if (string.IsNullOrWhiteSpace(rule.Url))
            return false;

        // Coba navigasi tab browser aktif via CDP (jika hwnd browser dan CDP tersedia)
        if (hwnd != IntPtr.Zero)
        {
            try
            {
                if (CdpUrlProvider.IsAvailable())
                {
                    using var cdp = new CdpUrlProvider();
                    if (cdp.NavigateActiveTabToUrl(hwnd, rule.Url))
                        return true;
                }
            }
            catch { }
        }

        // Fallback: buka di browser default
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rule.Url)
            {
                UseShellExecute = true
            });
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Kirim aksi umum ke foreground window (misal tombol tes). Return false jika kombinasi tidak valid.
    /// </summary>
    public static bool SendForAction(TriggerAction action, string? customHotkey = null, bool sendEscapeFirst = false)
    {
        if (action == TriggerAction.LeftClick)
        {
            if (sendEscapeFirst)
                SendSteps([new KeyStep(NativeMethods.VK_ESCAPE, IsExtended: false)]);
            SendLeftClick();
            return true;
        }

        if (action == TriggerAction.OpenUrl)
        {
            // Memerlukan URL dari aturan (ButtonRule); tidak didukung di sini.
            return false;
        }

        string spec = GetHotkeyString(action, customHotkey);
        if (!HotkeyParser.TryParse(spec, out List<KeyStep> steps, out _))
            return false;

        if (sendEscapeFirst)
            SendSteps([new KeyStep(NativeMethods.VK_ESCAPE, IsExtended: false)]);

        SendSteps(steps);
        return true;
    }

    /// <summary>
    /// Kirim daftar langkah tombol: semua keydown berurutan, lalu semua keyup
    /// dalam urutan terbalik (atomic dalam satu panggilan SendInput).
    /// </summary>
    private static void SendSteps(List<KeyStep> steps)
    {
        int count = steps.Count;
        if (count == 0)
            return;

        var inputs = new NativeMethods.INPUT[count * 2];
        int i = 0;

        // Keydown: modifier dulu, lalu key utama.
        foreach (var (vk, ext) in steps)
            inputs[i++] = MakeKeyInput(vk, ext, keyDown: true);

        // Keyup: urutan terbalik (key utama dulu, lalu modifier).
        for (int j = count - 1; j >= 0; j--)
            inputs[i++] = MakeKeyInput(steps[j].Vk, steps[j].IsExtended, keyDown: false);

        uint result = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        if (result != inputs.Length)
        {
            System.Diagnostics.Debug.WriteLine($"SendInput failed: sent {result} of {inputs.Length}");
        }
    }

    private static NativeMethods.INPUT MakeKeyInput(ushort vk, bool extended, bool keyDown)
    {
        uint flags = keyDown ? 0u : NativeMethods.KEYEVENTF_KEYUP;
        if (extended)
            flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;

        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    private static void SendLeftClick()
    {
        var inputs = new NativeMethods.INPUT[2];
        inputs[0] = MakeMouseInput(NativeMethods.MOUSEEVENTF_LEFTDOWN);
        inputs[1] = MakeMouseInput(NativeMethods.MOUSEEVENTF_LEFTUP);

        uint result = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        if (result != inputs.Length)
        {
            System.Diagnostics.Debug.WriteLine($"SendInput failed: sent {result} of {inputs.Length}");
        }
    }

    private static NativeMethods.INPUT MakeMouseInput(uint dwFlags)
    {
        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            U = new NativeMethods.InputUnion
            {
                mi = new NativeMethods.MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = dwFlags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }
}
