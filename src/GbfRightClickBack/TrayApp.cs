using System;
using System.IO;
using System.Windows.Forms;

namespace GbfRightClickBack;

/// <summary>
/// System tray icon + context menu:
/// - Enable (toggle, dipulihkan dari config saat start)
/// - Status: Active / Inactive
/// - Pengaturan... (buka jendela kustomisasi)
/// - Check Detection...
/// - Exit
/// Double-click icon juga membuka jendela pengaturan.
/// </summary>
internal sealed class TrayApp : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _enableItem;
    private readonly ToolStripMenuItem _statusItem;
    private bool _syncing;

    /// <summary>Dipanggil saat toggle Enable berubah.</summary>
    public event Action<bool>? EnableChanged;

    /// <summary>Dipanggil saat user memilih "Pengaturan...".</summary>
    public event Action? SettingsRequested;

    /// <summary>Dipanggil saat user memilih "Check Detection...".</summary>
    public event Action? CheckRequested;

    /// <summary>Dipanggil saat user memilih Exit.</summary>
    public event Action? ExitRequested;

    public TrayApp(bool initiallyEnabled = true)
    {
        _enableItem = new ToolStripMenuItem("Enable") { Checked = initiallyEnabled, CheckOnClick = true };
        _statusItem = new ToolStripMenuItem("Status: Inactive") { Enabled = false };

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        var settingsItem = new ToolStripMenuItem("Pengaturan...");
        settingsItem.Font = new System.Drawing.Font(settingsItem.Font, System.Drawing.FontStyle.Bold);
        settingsItem.Click += (_, _) => SettingsRequested?.Invoke();

        var checkItem = new ToolStripMenuItem("Check Detection...");
        checkItem.Click += (_, _) => CheckRequested?.Invoke();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_enableItem);
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(checkItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Back Button Customizer",
            ContextMenuStrip = menu,
            Visible = true
        };

        _enableItem.CheckedChanged += (_, _) =>
        {
            if (!_syncing)
                EnableChanged?.Invoke(_enableItem.Checked);
        };

        // Double-click icon tray = buka jendela pengaturan.
        _notifyIcon.DoubleClick += (_, _) => SettingsRequested?.Invoke();
    }

    public bool IsEnabled => _enableItem.Checked;

    /// <summary>Sinkron status checkbox Enable dari luar (tanpa memicu event balik).</summary>
    public void SetEnabled(bool enabled)
    {
        _syncing = true;
        _enableItem.Checked = enabled;
        _syncing = false;
    }

    /// <summary>Update teks status di tray.</summary>
    public void SetActive(bool active)
    {
        _statusItem.Text = active ? "Status: Active" : "Status: Inactive";
        string tip = active
            ? "Back Button Customizer - Aktif"
            : "Back Button Customizer - Nonaktif/Siap";
        if (tip.Length > 63) tip = tip[..63]; // NotifyIcon.Text max 63 chars on older Windows
        _notifyIcon.Text = tip;
    }

    private static System.Drawing.Icon LoadIcon() => AppIcons.Load();

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
