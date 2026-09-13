using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace TokenVector.Text.Unicode;

/// <summary>
/// Zero-allocation and low-overhead Unicode normalization, accent handling, and case folding.
/// </summary>
public static class UnicodeNormalizer
{
    /// <summary>
    /// Normalizes a string or char span according to standard Unicode Normalization Form (NFC, NFD, NFKC, NFKD).
    /// </summary>
    public static string Normalize(ReadOnlySpan<char> text, NormalizationForm form = NormalizationForm.FormC)
    {
        if (text.IsEmpty) return string.Empty;

        // If ASCII-only, normalization is a no-op
        if (IsAscii(text))
        {
            return text.ToString();
        }

        string str = text.ToString();
        return str.IsNormalized(form) ? str : str.Normalize(form);
    }

    /// <summary>
    /// Checks if a char span contains only 7-bit ASCII characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAscii(ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] > 127) return false;
        }
        return true;
    }

    /// <summary>
    /// Performs in-place ASCII lowercasing if possible without allocation.
    /// </summary>
    public static void ToLowerAsciiInPlace(Span<char> destination)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            char c = destination[i];
            if ((uint)(c - 'A') <= (uint)('Z' - 'A'))
            {
                destination[i] = (char)(c | 0x20);
            }
        }
    }

    /// <summary>
    /// Strips accents/diacritics from text using NFD decomposition (useful for BERT/uncased models).
    /// </summary>
    public static string StripDiacritics(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return string.Empty;
        if (IsAscii(text)) return text.ToString();

        string normalizedString = text.ToString().Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Cleans and removes control characters / replacement chars.
    /// </summary>
    public static string CleanText(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return string.Empty;

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == 0 || c == 0xFFFD || IsControl(c))
            {
                continue;
            }
            if (IsWhitespace(c))
            {
                sb.Append(' ');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhitespace(char c)
    {
        return c == ' ' || c == '\t' || c == '\n' || c == '\r';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsControl(char c)
    {
        if (c == '\t' || c == '\n' || c == '\r') return false;
        UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
        return cat == UnicodeCategory.Control || cat == UnicodeCategory.OtherNotAssigned;
    }
}
