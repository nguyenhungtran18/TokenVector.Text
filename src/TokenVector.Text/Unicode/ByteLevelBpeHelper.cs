using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TokenVector.Text.Unicode;

/// <summary>
/// High-performance GPT-2/GPT-4 style reversible byte-to-character mapping for byte-level BPE.
/// Maps every byte 0..255 to a distinct printable Unicode character so no byte information is lost.
/// </summary>
public static class ByteLevelBpeHelper
{
    private static readonly char[] ByteToCharMap = new char[256];
    private static readonly short[] CharToByteFastMap = new short[1024];
    private static readonly Dictionary<char, byte> CharToByteMap = new(256);
    private static readonly string[] ByteToStringMap = new string[256];

    static ByteLevelBpeHelper()
    {
        Array.Fill(CharToByteFastMap, (short)-1);

        // Standard GPT-2 / GPT-4 printable byte ranges:
        // '!' (33) to '~' (126)
        // '¡' (161) to '¬' (172)
        // '®' (174) to 'ÿ' (255)
        var bs = new List<int>();
        for (int b = '!'; b <= '~'; b++) bs.Add(b);
        for (int b = '¡'; b <= '¬'; b++) bs.Add(b);
        for (int b = '®'; b <= 'ÿ'; b++) bs.Add(b);

        var cs = new List<int>(bs);
        int n = 0;
        for (int b = 0; b < 256; b++)
        {
            if (!bs.Contains(b))
            {
                bs.Add(b);
                cs.Add(256 + n);
                n++;
            }
        }

        for (int i = 0; i < bs.Count; i++)
        {
            byte b = (byte)bs[i];
            char c = (char)cs[i];
            ByteToCharMap[b] = c;
            CharToByteMap[c] = b;
            if (c < CharToByteFastMap.Length)
            {
                CharToByteFastMap[c] = b;
            }
            ByteToStringMap[b] = c.ToString();
        }
    }

    /// <summary>
    /// Gets the mapped character for a given byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ByteToChar(byte b) => ByteToCharMap[b];

    /// <summary>
    /// Gets the mapped single-character string for a given byte (cached).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ByteToString(byte b) => ByteToStringMap[b];

    /// <summary>
    /// Converts a char back to byte if it exists in the byte-level mapping.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCharToByte(char c, out byte b)
    {
        if ((uint)c < (uint)CharToByteFastMap.Length)
        {
            short val = CharToByteFastMap[c];
            if (val >= 0)
            {
                b = (byte)val;
                return true;
            }
        }
        return CharToByteMap.TryGetValue(c, out b);
    }

    /// <summary>
    /// Converts a span of raw bytes to mapped characters zero-allocation into destination span.
    /// </summary>
    public static int BytesToChars(ReadOnlySpan<byte> source, Span<char> destination)
    {
        if (destination.Length < source.Length)
        {
            throw new ArgumentException("Destination span is too short.", nameof(destination));
        }

        for (int i = 0; i < source.Length; i++)
        {
            destination[i] = ByteToCharMap[source[i]];
        }
        return source.Length;
    }

    /// <summary>
    /// Converts a span of mapped characters back to raw bytes zero-allocation into destination span.
    /// </summary>
    public static int CharsToBytes(ReadOnlySpan<char> source, Span<byte> destination)
    {
        if (destination.Length < source.Length)
        {
            throw new ArgumentException("Destination span is too short.", nameof(destination));
        }

        for (int i = 0; i < source.Length; i++)
        {
            if (CharToByteMap.TryGetValue(source[i], out byte b))
            {
                destination[i] = b;
            }
            else
            {
                destination[i] = (byte)'?';
            }
        }
        return source.Length;
    }

    /// <summary>
    /// Converts a mapped string back into raw UTF-8 bytes.
    /// </summary>
    public static byte[] CharsToBytes(ReadOnlySpan<char> source)
    {
        byte[] bytes = new byte[source.Length];
        CharsToBytes(source, bytes.AsSpan());
        return bytes;
    }
}
