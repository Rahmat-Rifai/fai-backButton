using System;
using System.Runtime.InteropServices;

namespace GbfRightClickBack;

/// <summary>
/// Low-level keyboard hook (WH_KEYBOARD_LL) untuk menangkap tombol keyboard kustom.
/// Hook dipasang di thread UI yang memiliki message loop.
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;

    /// <summary>
    /// Event dipanggil saat keyboard event terjadi.
    /// Parameter 1: message id (mis. WM_KEYDOWN).
    /// Return true untuk block event, false untuk pass-through.
    /// </summary>
    public event Func<int, NativeMethods.KBDLLHOOKSTRUCT, bool>? OnKeyboardAction;

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    /// <summary>
    /// Pasang low-level keyboard hook.
    /// Harus dipanggil dari thread yang memiliki message loop (Application.Run).
    /// </summary>
    public void Install()
    {
        if (_hookId != IntPtr.Zero)
            return;

        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var mainModule = curProcess.MainModule;
        if (mainModule == null)
            throw new InvalidOperationException("Cannot get main module handle.");

        var moduleHandle = NativeMethods.GetModuleHandle(mainModule.ModuleName);
        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _proc,
            moduleHandle,
            0);

        if (_hookId == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to install keyboard hook. Error code: {error}");
        }
    }

    /// <summary>
    /// Lepas hook.
    /// </summary>
    public void Uninstall()
    {
        if (_hookId == IntPtr.Zero)
            return;

        NativeMethods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            // Panggil event handler (message id dalam bentuk int dari wParam)
            bool blocked = OnKeyboardAction?.Invoke(wParam.ToInt32(), hookStruct) ?? false;
            if (blocked)
                return new IntPtr(1);
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Uninstall();
            _disposed = true;
        }
    }
}
