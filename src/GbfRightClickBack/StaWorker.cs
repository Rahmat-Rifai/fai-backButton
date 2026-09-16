using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace GbfRightClickBack;

/// <summary>
/// Thread pekerja STA (Single-Threaded Apartment) yang dibutuhkan untuk query
/// UI Automation. Menjaga query UIA tetap di satu thread + message pump kecil,
/// sehingga thread utama (hook callback) tidak tersendat oleh COM marshaling.
/// </summary>
internal sealed class StaWorker : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;
    private bool _disposed;

    public StaWorker(string name)
    {
        _thread = new Thread(MainLoop)
        {
            IsBackground = true,
            Name = name
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    private void MainLoop()
    {
        while (true)
        {
            Action? item;
            try
            {
                item = _queue.Take();
            }
            catch (InvalidOperationException)
            {
                // _queue.CompleteAdding() -> keluar dari loop
                break;
            }

            try
            {
                item();
            }
            catch
            {
                // Exception ditangani lewat TaskCompletionSource di Run<T>()
            }

            // Dispatch pesan Windows yang mungkin diperlukan oleh UIA core
            PumpPendingMessages();
        }
    }

    private static void PumpPendingMessages()
    {
        int safety = 0;
        while (NativeMethods.PeekMessage(out var msg, IntPtr.Zero, 0, 0, NativeMethods.PM_REMOVE))
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessage(ref msg);
            if (++safety > 100)
                break;
        }
    }

    /// <summary>
    /// Menjalankan fungsi pada thread STA dan mengembalikan hasil sebagai Task.
    /// </summary>
    public Task<T> Run<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            _queue.Add(() =>
            {
                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
        }
        catch (InvalidOperationException)
        {
            tcs.TrySetException(new ObjectDisposedException(nameof(StaWorker)));
        }
        return tcs.Task;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _queue.CompleteAdding();
        try { _thread.Join(2000); } catch { /* abaikan */ }
    }
}
