using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace GbfRightClickBack.UrlProviders;

/// <summary>
/// Mendapatkan URL tab aktif Chrome via Chrome DevTools Protocol (CDP).
/// Hanya aktif jika user menjalankan Chrome dengan --remote-debugging-port.
/// Untuk menentukan tab MANA yang aktif, digunakan judul tab dari UIA
/// (selected tab item) kemudian dicocokkan dengan daftar target CDP.
/// </summary>
internal sealed class CdpUrlProvider : IUrlProvider, IDisposable
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromMilliseconds(Config.CdpTimeoutMs)
    };

    private readonly UiaUrlProvider _uia;

    public CdpUrlProvider()
    {
        _uia = new UiaUrlProvider();
    }

    /// <summary>
    /// Memeriksa apakah Chrome sedang dijalankan dengan remote debugging.
    /// </summary>
    public static bool IsAvailable()
    {
        try
        {
            using var cts = new CancellationTokenSource(Config.CdpTimeoutMs);
            var resp = _http.GetAsync($"http://127.0.0.1:{Config.CdpPort}/json/version", cts.Token)
                .GetAwaiter().GetResult();
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public string? GetActiveTabUrl(IntPtr chromeHwnd)
    {
        try
        {
            var target = GetActiveTarget(chromeHwnd);
            return target?.Url;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Navigasi tab browser yang sedang aktif ke URL baru via CDP WebSocket.
    /// Fallback: jika CDP tidak tersedia atau gagal, return false agar pemanggil
    /// bisa menggunakan metode lain (mis. Process.Start ke browser default).
    /// </summary>
    public bool NavigateActiveTabToUrl(IntPtr chromeHwnd, string url)
    {
        try
        {
            var target = GetActiveTarget(chromeHwnd);
            if (target == null)
                return false;

            string wsUrl = target.WebSocketDebuggerUrl;
            if (string.IsNullOrWhiteSpace(wsUrl))
                return false;

            using var ws = new ClientWebSocket();
            using var cts = new CancellationTokenSource(5000);
            ws.ConnectAsync(new Uri(wsUrl), cts.Token).GetAwaiter().GetResult();

            // Enable Page domain, lalu navigate
            SendCdpCommand(ws, 1, "Page.enable", cts.Token);
            SendCdpCommand(ws, 2, "Page.navigate", cts.Token, url);

            // Tunggu response cukup lama agar navigate terproses
            DrainResponses(ws, 2, 600);

            try
            {
                ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cts.Token)
                    .GetAwaiter().GetResult();
            }
            catch { }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Mengambil CDP target tab yang sedang aktif (halaman page yang cocok judulnya).
    /// </summary>
    private CdpTarget? GetActiveTarget(IntPtr chromeHwnd)
    {
        using var cts = new CancellationTokenSource(Config.CdpTimeoutMs);
        var json = _http.GetStringAsync($"http://127.0.0.1:{Config.CdpPort}/json", cts.Token)
            .GetAwaiter().GetResult();

        var targets = JsonSerializer.Deserialize<List<CdpTarget>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (targets == null)
            return null;

        var pages = targets
            .Where(t => t.Type == "page" && !string.IsNullOrWhiteSpace(t.Url))
            .ToList();

        if (pages.Count == 0)
            return null;

        // Tentukan tab aktif lewat judul tab terpilih dari UIA
        string? activeTitle = _uia.GetActiveTabTitle(chromeHwnd);
        if (!string.IsNullOrWhiteSpace(activeTitle))
        {
            var match = pages.FirstOrDefault(t =>
                string.Equals(t.Title, activeTitle, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
        }

        // Fallback: hanya 1 halaman -> anggap itu tab aktif
        if (pages.Count == 1)
            return pages[0];

        return null;
    }

    private static void SendCdpCommand(ClientWebSocket ws, int id, string method, CancellationToken ct, string? urlParam = null)
    {
        string json = urlParam != null
            ? $"{{\"id\":{id},\"method\":\"{method}\",\"params\":{{\"url\":\"{EscapeJson(urlParam)}\"}}}}"
            : $"{{\"id\":{id},\"method\":\"{method}\",\"params\":{{}}}}";

        var bytes = Encoding.UTF8.GetBytes(json);
        ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, ct)
            .GetAwaiter().GetResult();
    }

    private static void DrainResponses(ClientWebSocket ws, int expectedMessages = 2, int timeoutMs = 600)
    {
        long deadline = Environment.TickCount64 + timeoutMs;
        var buffer = new byte[4096];
        int received = 0;

        while (received < expectedMessages && Environment.TickCount64 < deadline)
        {
            int remaining = (int)(deadline - Environment.TickCount64);
            if (remaining <= 0) break;

            try
            {
                using var cts = new CancellationTokenSource(remaining);
                var result = ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token)
                    .GetAwaiter().GetResult();
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
                if (result.EndOfMessage)
                    received++;
            }
            catch (OperationCanceledException) { break; }
            catch { break; }
        }
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    public void Dispose() => _uia.Dispose();

    private sealed class CdpTarget
    {
        public string Type { get; set; } = "";
        public string Url { get; set; } = "";
        public string Title { get; set; } = "";
        public string WebSocketDebuggerUrl { get; set; } = "";
    }
}
