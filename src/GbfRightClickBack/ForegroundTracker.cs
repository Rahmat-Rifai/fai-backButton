using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GbfRightClickBack;

/// <summary>
/// Melacak perubahan foreground window menggunakan WinEventHook.
/// Memberi notifikasi saat window aktif berubah.
/// </summary>
internal sealed class ForegroundTracker : IDisposable
{
    private readonly NativeMethods.WinEventDelegate _eventDelegate;
    private IntPtr _hook = IntPtr.Zero;
    private bool _disposed;

    /// <summary>
    /// Dipanggil saat foreground window berubah.
    /// Parameter: handle foreground window yang baru.
    /// </summary>
    public event Action<IntPtr>? ForegroundChanged;

    public ForegroundTracker()
    {
        _eventDelegate = WinEventProc;
    }

    public void Start()
    {
        if (_hook != IntPtr.Zero)
            return;

        _hook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _eventDelegate,
            0, 0,
            NativeMethods.WINEVENT_OUTOFCONTEXT);

        if (_hook == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"Failed to install foreground hook. Error: {error}");
        }
    }

    public void Stop()
    {
        if (_hook == IntPtr.Zero)
            return;

        NativeMethods.UnhookWinEvent(_hook);
        _hook = IntPtr.Zero;
    }

    private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        ForegroundChanged?.Invoke(hwnd);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}