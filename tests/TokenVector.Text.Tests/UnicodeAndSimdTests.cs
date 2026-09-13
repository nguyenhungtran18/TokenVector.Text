using System;
using System.Text;
using TokenVector.Text.Unicode;
using Xunit;

namespace TokenVector.Text.Tests;

public class UnicodeAndSimdTests
{
    [Fact]
    public void ByteLevelBpeHelper_All256Bytes_RoundTripReversible()
    {
        byte[] allBytes = new byte[256];
        for (int i = 0; i < 256; i++) allBytes[i] = (byte)i;

        Span<char> chars = stackalloc char[256];
        int charsWritten = ByteLevelBpeHelper.BytesToChars(allBytes, chars);
        Assert.Equal(256, charsWritten);

        Span<byte> roundtripBytes = stackalloc byte[256];
        int bytesWritten = ByteLevelBpeHelper.CharsToBytes(chars, roundtripBytes);
        Assert.Equal(256, bytesWritten);

        for (int i = 0; i < 256; i++)
        {
            Assert.Equal(allBytes[i], roundtripBytes[i]);
        }
    }

    [Fact]
    public void UnicodeNormalizer_DiacriticsStripping_Works()
    {
        string text = "Nguyễn Hùng Trần - Tiếng Việt thân thương";
        string stripped = UnicodeNormalizer.StripDiacritics(text.AsSpan());

        Assert.Equal("Nguyen Hung Tran - Tieng Viet than thuong", stripped);
    }

    [Fact]
    public void SimdTextScanner_WhitespaceDetection_MatchesScalar()
    {
        byte[] buffer = Encoding.UTF8.GetBytes("HelloWorld ThisIsATestWithSpacesAndTabs\tEnd");

        int wsIdx = SimdTextScanner.IndexOfFirstWhitespace(buffer.AsSpan());
        Assert.Equal(10, wsIdx); // ' ' after "HelloWorld"
    }

    [Fact]
    public void SimdTextScanner_AsciiDetection_Correct()
    {
        byte[] ascii = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog 1234567890!");
        byte[] nonAscii = Encoding.UTF8.GetBytes("Tiếng Việt với ký tự có dấu và biểu cảm 🚀");

        Assert.True(SimdTextScanner.IsAllAscii(ascii.AsSpan()));
        Assert.False(SimdTextScanner.IsAllAscii(nonAscii.AsSpan()));
    }
}
