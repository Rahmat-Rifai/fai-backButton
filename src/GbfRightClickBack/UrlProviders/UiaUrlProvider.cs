using System;
using System.Collections.Generic;
using System.Windows.Automation;

namespace GbfRightClickBack.UrlProviders;

/// <summary>
/// Mendapatkan URL tab aktif Chrome melalui Windows UI Automation (UIA).
/// Strategi berlapis (tanpa LegacyIAccessiblePattern yang tidak tersedia di .NET 8):
///   1. Omnibox via AutomationId "Address and search bar" → ValuePattern.
///   2. Omnibox via Name / ControlType.Edit → ValuePattern.
///   3. Cari elemen Edit dengan value mengandung "://" (URL).
/// Tidak memerlukan setup apa pun.
/// </summary>
internal sealed class UiaUrlProvider : IUrlProvider
{
    private readonly StaWorker _sta = new("UiaUrlProvider");

    /// <summary>Pesan error terakhir untuk keperluan diagnostik.</summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Membaca URL tab aktif. Return null jika gagal/tidak ditemukan.
    /// </summary>
    public string? GetActiveTabUrl(IntPtr chromeHwnd)
    {
        LastError = null;
        try
        {
            var task = _sta.Run(() => QueryUrl(chromeHwnd));
            if (!task.Wait(Config.UiaTimeoutMs))
            {
                LastError = $"Timeout setelah {Config.UiaTimeoutMs}ms";
                return null; // timeout -> pass-through (safe)
            }

            if (task.IsCompletedSuccessfully)
                return task.Result;

            if (task.Exception != null)
                LastError = task.Exception.GetBaseException().Message;
            return null;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Membaca judul tab aktif (digunakan untuk matching CDP).
    /// </summary>
    public string? GetActiveTabTitle(IntPtr chromeHwnd)
    {
        try
        {
            var task = _sta.Run(() => QueryTitle(chromeHwnd));
            if (!task.Wait(Config.UiaTimeoutMs))
                return null;
            return task.IsCompletedSuccessfully ? task.Result : null;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() => _sta.Dispose();

    // ---------- (dijalankan di thread STA) ----------

    private static string? QueryUrl(IntPtr hwnd)
    {
        try
        {
            var window = AutomationElement.FromHandle(hwnd);
            if (window == null)
                return null;

            // 1. Omnibox via Name (paling stabil di Chrome modern)
            var url = GetUrlViaOmniboxName(window);
            if (url != null)
                return url;

            // 2. Omnibox via AutomationId (fallback untuk versi Chrome tertentu)
            url = GetUrlViaOmniboxId(window);
            if (url != null)
                return url;

            // 3. Cari elemen Edit dengan value URL (heuristic fallback)
            url = GetUrlViaAnyEdit(window);
            return url;
        }
        catch
        {
            return null;
        }
    }

    private static string? QueryTitle(IntPtr hwnd)
    {
        try
        {
            var window = AutomationElement.FromHandle(hwnd);
            if (window == null)
                return null;

            // Cari tab yang sedang terpilih (SelectionItemPattern.IsSelected == true)
            var tabCond = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.IsSelectionItemPatternAvailableProperty, true));

            var tabs = window.FindAll(TreeScope.Descendants, tabCond);
            foreach (AutomationElement tab in tabs)
            {
                if (tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var patternObj))
                {
                    var sel = (SelectionItemPattern)patternObj;
                    if (sel.Current.IsSelected)
                    {
                        var title = tab.Current.Name;
                        if (!string.IsNullOrWhiteSpace(title))
                            return title;
                    }
                }
            }

            // Fallback: judul jendela Chrome itu sendiri
            var name = window.Current.Name;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Cari omnibox via AutomationId "Address and search bar".
    /// </summary>
    private static string? GetUrlViaOmniboxId(AutomationElement window)
    {
        try
        {
            var cond = new PropertyCondition(AutomationElement.AutomationIdProperty, "Address and search bar");
            var omnibox = window.FindFirst(TreeScope.Descendants, cond);
            if (omnibox != null)
                return ReadValuePattern(omnibox);
        }
        catch
        {
            // lanjut ke fallback
        }
        return null;
    }

    /// <summary>
    /// Cari omnibox via Name (mengandung "Address" atau "address").
    /// </summary>
    private static string? GetUrlViaOmniboxName(AutomationElement window)
    {
        try
        {
            var editCond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
            var edits = window.FindAll(TreeScope.Descendants, editCond);
            foreach (AutomationElement edit in edits)
            {
                string name = edit.Current.Name ?? "";
                if (name.Contains("Address", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("address", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ReadValuePattern(edit);
                    if (val != null)
                        return val;
                }
            }
        }
        catch
        {
            // abaikan
        }
        return null;
    }

    /// <summary>
    /// Cari elemen Edit yang value-nya mengandung "://" (indikasi URL).
    /// </summary>
    private static string? GetUrlViaAnyEdit(AutomationElement window)
    {
        try
        {
            var editCond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
            var edits = window.FindAll(TreeScope.Descendants, editCond);
            foreach (AutomationElement edit in edits)
            {
                var val = ReadValuePattern(edit);
                if (val != null && (val.Contains("://") || val.Contains("game.granbluefantasy")))
                    return val;
            }
        }
        catch
        {
            // abaikan
        }
        return null;
    }

    /// <summary>
    /// Membaca ValuePattern dari elemen.
    /// </summary>
    private static string? ReadValuePattern(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var patternObj))
            {
                var vp = (ValuePattern)patternObj;
                var value = vp.Current.Value;
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }
        catch
        {
            // abaikan
        }
        return null;
    }
}