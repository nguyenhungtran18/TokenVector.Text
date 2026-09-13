using System;
using System.Text;
using TokenVector.Text.Streaming;
using Xunit;

namespace TokenVector.Text.Tests;

public class IncrementalDecoderTests
{
    [Fact]
    public void IncrementalDecoder_StreamsTokensCorrectly()
    {
        var tokenizer = Tokenizer.CreateVietnameseBpe();
        string text = "Hệ sinh thái TokenVector AI Engine!";

        var encoded = tokenizer.Encode(text.AsSpan(), addSpecialTokens: false);
        var decoder = new IncrementalDecoder(tokenizer, skipSpecialTokens: true);

        var sb = new StringBuilder();
        foreach (int tokenId in encoded.Ids)
        {
            string piece = decoder.DecodeToken(tokenId);
            sb.Append(piece);
        }
        sb.Append(decoder.Flush());

        Assert.Equal(text, sb.ToString());
    }

    [Fact]
    public void IncrementalDecoder_HandlesMultiByteSplitsSafely()
    {
        var tokenizer = Tokenizer.CreateVietnameseBpe();
        // Emoji and Vietnamese accented text
        string text = "Xin chào các bạn 🚀 và chúc một ngày tốt lành! 🌟";

        var encoded = tokenizer.Encode(text.AsSpan(), addSpecialTokens: false);
        var decoder = new IncrementalDecoder(tokenizer, skipSpecialTokens: true);

        var sb = new StringBuilder();
        foreach (int tokenId in encoded.Ids)
        {
            string piece = decoder.DecodeToken(tokenId);
            sb.Append(piece);
        }
        sb.Append(decoder.Flush());

        Assert.Equal(text, sb.ToString());
    }
}
