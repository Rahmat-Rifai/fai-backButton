using System;
using System.Windows.Forms;

namespace GbfRightClickBack;

/// <summary>
/// Entry point Right Click Back.
/// Aplikasi berjalan sebagai tray app; jendela pengaturan (MainWindow) tampil otomatis
/// (jika ShowWindowOnStart aktif) dan bisa dibuka lagi via double-click icon tray.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        string logFile = System.IO.Path.Combine(AppContext.BaseDirectory, "startup.log");
        void Log(string msg) {
            try { System.IO.File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}"); } catch { }
        }

        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
            Log($"UNHANDLED APPDOMAIN: {e.ExceptionObject}");
        };
        Application.ThreadException += (s, e) => {
            Log($"UNHANDLED THREAD: {e.Exception}");
        };

        Log("Program starting...");

        // Muat config.json (di sebelah exe, fallback %APPDATA%) sebelum komponen apa pun dibuat.
        try
        {
            Config.Load();
            Log($"Config loaded. ShowWindowOnStart={Config.ShowWindowOnStart}, Enabled={Config.Enabled}");
        }
        catch (Exception ex)
        {
            Log($"Config.Load ERROR: {ex}");
        }

        if (args.Length > 0 && args[0].Equals("--check", StringComparison.OrdinalIgnoreCase))
        {
            // Mode diagnostic: hasil ditulis ke file (tanpa GUI/tray)
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);
            RunDiagnostic();
            return;
        }

        try
        {
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);

            Log("Creating AppController...");
            using var controller = new AppController();
            Log("Starting AppController...");
            controller.Start();

            // Tampilkan jendela pengaturan saat start (bisa dimatikan di config/UI).
            if (Config.ShowWindowOnStart)
            {
                Log("Showing settings window...");
                controller.ShowSettings();
            }

            Log("Running Application.Run()...");
            // Message loop tetap berjalan -> hook mouse bisa memproses event.
            Application.Run();

            Log("Application.Run() ended. Stopping controller...");
            controller.Stop();
        }
        catch (Exception ex)
        {
            string crashInfo = $"CRASH AT {DateTime.Now}:{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}Inner: {ex.InnerException}";
            try
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
                System.IO.File.WriteAllText(path, crashInfo);
            }
            catch { }
            MessageBox.Show(crashInfo, "Back Button Customizer - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void RunDiagnostic()
    {
        using var monitor = new UrlMonitor();
        string diag = monitor.GetDiagnostic();
        diag += $"{Environment.NewLine}Config file: {Config.ConfigFilePath}";
        string appDir = AppContext.BaseDirectory;
        string path = System.IO.Path.Combine(appDir, "gbf-diag.log");
        System.IO.File.WriteAllText(path, diag);
        // WinExe tidak punya console, jadi tulis ke file saja.
        System.Diagnostics.Debug.WriteLine(diag);
    }
}

/// <summary>
/// Merangkai semua komponen: tray, hook mouse, tracker foreground, URL monitor, jendela utama.
/// </summary>
internal sealed class AppController : IDisposable
{
    private readonly TrayApp _tray;
    private readonly UrlMonitor _monitor;
    private MainWindow? _window;
    private bool _started;

    public AppController()
    {
        _tray = new TrayApp(Config.Enabled);
        _monitor = new UrlMonitor { Enabled = Config.Enabled };

        _tray.EnableChanged += enabled =>
        {
            _monitor.Enabled = enabled;
            Config.Enabled = enabled;
            Config.Save(); // pulihkan status enable saat start berikutnya

            if (_window is { IsDisposed: false })
                _window.SyncEnabled(enabled);
        };

        _tray.ExitRequested += () => Application.Exit();

        _tray.CheckRequested += () =>
            MessageBox.Show(_monitor.GetDiagnostic(), "Back Button Customizer - Detection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

        _tray.SettingsRequested += ShowSettings;

        _monitor.StateChanged += active => _tray.SetActive(active);
    }

    public void Start()
    {
        if (_started)
            return;
        _started = true;

        _monitor.Start();
        _tray.SetActive(_monitor.IsCurrentlyActive);
    }

    public void Stop()
    {
        if (!_started)
            return;
        _started = false;
    }

    /// <summary>
    /// Buka jendela pengaturan (modeless). Monitor tetap aktif — klik di dalam jendela
    /// aman karena foreground saat itu adalah jendela ini, bukan browser target.
    /// </summary>
    public void ShowSettings()
    {
        if (_window is { IsDisposed: false })
        {
            if (_window.WindowState == FormWindowState.Minimized)
                _window.WindowState = FormWindowState.Normal;
            _window.Show();
            _window.BringToFront();
            _window.Activate();
            return;
        }

        _window = new MainWindow(_monitor);
        _window.EnabledToggled += enabled =>
        {
            if (_monitor.Enabled == enabled)
                return;

            _monitor.Enabled = enabled;
            Config.Enabled = enabled;
            Config.Save();
            _tray.SetEnabled(enabled); // sinkron checkbox tray
        };
        _window.Show();
        _window.BringToFront();
        _window.Activate();
    }

    public void Dispose()
    {
        _monitor.Dispose();
        _tray.Dispose();
        _window?.Dispose();
    }
}
