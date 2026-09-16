using System;
using System.Threading.Tasks;

namespace GbfRightClickBack.UrlProviders;

/// <summary>
/// Interface untuk mendapatkan URL tab aktif dari Chrome.
/// </summary>
internal interface IUrlProvider : IDisposable
{
    /// <summary>
    /// Mendapatkan URL tab aktif dari Chrome secara synchronous.
    /// </summary>
    /// <param name="chromeHwnd">Handle window Chrome yang aktif.</param>
    /// <returns>URL jika berhasil, null jika gagal.</returns>
    string? GetActiveTabUrl(IntPtr chromeHwnd);
}