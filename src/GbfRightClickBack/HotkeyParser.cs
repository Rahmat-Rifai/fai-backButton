namespace GbfRightClickBack;

/// <summary>Satu langkah tombol: virtual-key code + flag extended key.</summary>
internal readonly record struct KeyStep(ushort Vk, bool IsExtended);

/// <summary>
/// Parser string kombinasi tombol, mis. "Alt+Left", "Ctrl+Shift+T", "F5".
/// Format: modifier (Ctrl/Alt/Shift/Win) dipisah '+', tombol utama paling akhir.
/// </summary>
internal static class HotkeyParser
{
    private static readonly Dictionary<string, ushort> KeyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Modifier (boleh jadi key utama? tidak — modifier hanya boleh prefix)
        ["ctrl"] = 0x11, ["control"] = 0x11,
        ["shift"] = 0x10,
        ["alt"] = 0x12, ["menu"] = 0x12,
        ["win"] = 0x5B, ["meta"] = 0x5B, ["windows"] = 0x5B,

        // Tanda / kontrol
        ["backspace"] = 0x08, ["back"] = 0x08,
        ["tab"] = 0x09,
        ["enter"] = 0x0D, ["return"] = 0x0D,
        ["esc"] = 0x1B, ["escape"] = 0x1B,
        ["space"] = 0x20,

        // Navigasi (extended keys)
        ["pageup"] = 0x21, ["pgup"] = 0x21, ["prior"] = 0x21,
        ["pagedown"] = 0x22, ["pgdn"] = 0x22, ["next"] = 0x22,
        ["end"] = 0x23,
        ["home"] = 0x24,
        ["left"] = 0x25,
        ["up"] = 0x26,
        ["right"] = 0x27,
        ["down"] = 0x28,
        ["insert"] = 0x2D, ["ins"] = 0x2D,
        ["delete"] = 0x2E, ["del"] = 0x2E,

        // OEM
        ["semicolon"] = 0xBA,
        ["plus"] = 0xBB, ["oemplus"] = 0xBB,
        ["comma"] = 0xBC, ["oemcomma"] = 0xBC,
        ["minus"] = 0xBD, ["oemminus"] = 0xBD,
        ["period"] = 0xBE, ["oemperiod"] = 0xBE,
        ["slash"] = 0xBF,
        ["quote"] = 0xDE,
        ["bracketleft"] = 0xDB,
        ["bracketright"] = 0xDD,
    };

    // Tombol yang butuh flag extended saat dikirim via SendInput.
    private static readonly HashSet<ushort> ExtendedKeys =
    [
        0x21, 0x22, 0x23, 0x24, // PgUp, PgDn, End, Home
        0x25, 0x26, 0x27, 0x28, // Left, Up, Right, Down
        0x2D, 0x2E,             // Insert, Delete
    ];

    public static bool TryParse(string? text, out List<KeyStep> steps, out string error)
    {
        steps = [];
        error = "";

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Kombinasi tombol kosong.";
            return false;
        }

        string[] tokens = text.Split('+', StringSplitOptions.TrimEntries);
        if (tokens.Length is < 1 or > 6)
        {
            error = "Format tidak valid (gunakan 1-5 tombol, mis. Ctrl+Shift+T).";
            return false;
        }

        var pending = new List<ushort>();
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (token.Length == 0)
            {
                error = "Ada bagian kosong pada kombinasi (contoh salah: 'Ctrl++T' — pakai 'Ctrl+Plus').";
                return false;
            }

            if (KeyNames.TryGetValue(token, out ushort vk))
            {
                bool isModifier = vk is 0x11 or 0x10 or 0x12 or 0x5B;
                bool isLast = i == tokens.Length - 1;

                if (isModifier)
                {
                    if (isLast)
                    {
                        error = $"'{token}' adalah modifier — butuh tombol utama setelahnya (mis. {token}+T).";
                        return false;
                    }
                    if (pending.Contains(vk))
                    {
                        error = $"Modifier '{token}' muncul dua kali.";
                        return false;
                    }
                    pending.Add(vk);
                    continue;
                }

                // Key utama
                if (!isLast)
                {
                    error = $"'{token}' bukan modifier — hanya Ctrl/Alt/Shift/Win boleh jadi prefix.";
                    return false;
                }

                foreach (ushort m in pending)
                    steps.Add(new KeyStep(m, IsExtended: false));
                steps.Add(new KeyStep(vk, ExtendedKeys.Contains(vk)));
                return true;
            }

            // Bukan nama khusus: coba huruf A-Z, digit 0-9 / D0-D9, F1-F24.
            if (token.Length == 1)
            {
                char c = char.ToUpperInvariant(token[0]);
                ushort? code = c switch
                {
                    >= 'A' and <= 'Z' => (ushort)(0x41 + (c - 'A')),
                    >= '0' and <= '9' => (ushort)(0x30 + (c - '0')),
                    _ => null,
                };
                if (code != null)
                {
                    if (i != tokens.Length - 1)
                    {
                        error = $"'{token}' bukan modifier — hanya Ctrl/Alt/Shift/Win boleh jadi prefix.";
                        return false;
                    }
                    foreach (ushort m in pending)
                        steps.Add(new KeyStep(m, IsExtended: false));
                    steps.Add(new KeyStep(code.Value, false));
                    return true;
                }
            }
            else if ((token.Length == 2 || token.Length == 3) &&
                     token[0] is 'd' or 'D' &&
                     char.IsDigit(token[1]))
            {
                // WinForms style: D0-D9
                if (i != tokens.Length - 1)
                {
                    error = $"'{token}' bukan modifier — hanya Ctrl/Alt/Shift/Win boleh jadi prefix.";
                    return false;
                }
                ushort code = (ushort)(0x30 + (token[1] - '0'));
                foreach (ushort m in pending)
                    steps.Add(new KeyStep(m, IsExtended: false));
                steps.Add(new KeyStep(code, false));
                return true;
            }
            else if (token.Length is 2 or 3 &&
                     (token[0] is 'f' or 'F') &&
                     int.TryParse(token[1..], out int fnum) &&
                     fnum is >= 1 and <= 24)
            {
                // F1-F24
                if (i != tokens.Length - 1)
                {
                    error = $"'{token}' bukan modifier — hanya Ctrl/Alt/Shift/Win boleh jadi prefix.";
                    return false;
                }
                ushort code = (ushort)(0x70 + fnum - 1);
                foreach (ushort m in pending)
                    steps.Add(new KeyStep(m, IsExtended: false));
                steps.Add(new KeyStep(code, false));
                return true;
            }

            error = $"Tombol '{token}' tidak dikenal. Gunakan nama seperti: A, 1, F5, Left, Delete, Home, Space, dll.";
            return false;
        }

        error = "Kombinasi tidak valid.";
        return false;
    }
}
