using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace TokenVector.Text.Unicode;

/// <summary>
/// SIMD-accelerated text scanner for whitespace, delimiters, and character classification.
/// </summary>
public static class SimdTextScanner
{
    /// <summary>
    /// Finds the index of the first whitespace character (' ', '\t', '\n', '\r') in a UTF-8 byte span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int IndexOfFirstWhitespace(ReadOnlySpan<byte> utf8)
    {
        int offset = 0;
        int length = utf8.Length;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
        {
            var vSpace = Vector256.Create((byte)' ');
            var vTab = Vector256.Create((byte)'\t');
            var vLf = Vector256.Create((byte)'\n');
            var vCr = Vector256.Create((byte)'\r');

            while (offset <= length - Vector256<byte>.Count)
            {
                var block = Vector256.LoadUnsafe(in utf8[offset]);
                var match = Vector256.Equals(block, vSpace) |
                            Vector256.Equals(block, vTab) |
                            Vector256.Equals(block, vLf) |
                            Vector256.Equals(block, vCr);

                uint mask = match.ExtractMostSignificantBits();
                if (mask != 0)
                {
                    return offset + BitOperations.TrailingZeroCount(mask);
                }
                offset += Vector256<byte>.Count;
            }
        }
        else if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            var vSpace = Vector128.Create((byte)' ');
            var vTab = Vector128.Create((byte)'\t');
            var vLf = Vector128.Create((byte)'\n');
            var vCr = Vector128.Create((byte)'\r');

            while (offset <= length - Vector128<byte>.Count)
            {
                var block = Vector128.LoadUnsafe(in utf8[offset]);
                var match = Vector128.Equals(block, vSpace) |
                            Vector128.Equals(block, vTab) |
                            Vector128.Equals(block, vLf) |
                            Vector128.Equals(block, vCr);

                uint mask = match.ExtractMostSignificantBits();
                if (mask != 0)
                {
                    return offset + BitOperations.TrailingZeroCount(mask);
                }
                offset += Vector128<byte>.Count;
            }
        }

        // Scalar remainder
        for (; offset < length; offset++)
        {
            byte b = utf8[offset];
            if (b == (byte)' ' || b == (byte)'\t' || b == (byte)'\n' || b == (byte)'\r')
            {
                return offset;
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the index of the first non-whitespace character in a UTF-8 byte span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfFirstNonWhitespace(ReadOnlySpan<byte> utf8)
    {
        for (int i = 0; i < utf8.Length; i++)
        {
            byte b = utf8[i];
            if (b != (byte)' ' && b != (byte)'\t' && b != (byte)'\n' && b != (byte)'\r')
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Checks if a UTF-8 byte span is purely 7-bit ASCII using SIMD.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool IsAllAscii(ReadOnlySpan<byte> utf8)
    {
        int offset = 0;
        int length = utf8.Length;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
        {
            while (offset <= length - Vector256<byte>.Count)
            {
                var block = Vector256.LoadUnsafe(in utf8[offset]);
                uint mask = block.ExtractMostSignificantBits();
                if (mask != 0) return false;
                offset += Vector256<byte>.Count;
            }
        }
        else if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            while (offset <= length - Vector128<byte>.Count)
            {
                var block = Vector128.LoadUnsafe(in utf8[offset]);
                uint mask = block.ExtractMostSignificantBits();
                if (mask != 0) return false;
                offset += Vector128<byte>.Count;
            }
        }

        for (; offset < length; offset++)
        {
            if (utf8[offset] >= 0x80) return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a character is a delimiter/punctuation symbol.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPunctuation(char c)
    {
        if ((c >= 33 && c <= 47) || (c >= 58 && c <= 64) ||
            (c >= 91 && c <= 96) || (c >= 123 && c <= 126))
        {
            return true;
        }
        return char.IsPunctuation(c) || char.IsSymbol(c);
    }
}
