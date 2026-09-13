using System;
using System.Text;
using TokenVector.Text.Unicode;

namespace TokenVector.Text.Streaming;

/// <summary>
/// High-performance stateful streaming decoder for LLM generation.
/// Uses .NET stateful UTF-8 Decoder to buffer partial multi-byte UTF-8 sequences across streaming tokens.
/// </summary>
public sealed class IncrementalDecoder
{
    private readonly ITokenizer _tokenizer;
    private readonly bool _skipSpecialTokens;
    private readonly Decoder _utf8Decoder = Encoding.UTF8.GetDecoder();

    public IncrementalDecoder(ITokenizer tokenizer, bool skipSpecialTokens = true)
    {
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        _skipSpecialTokens = skipSpecialTokens;
    }

    /// <summary>
    /// Processes a single incoming token ID and returns any completed decoded text (or string.Empty if waiting for more bytes).
    /// </summary>
    public string DecodeToken(int tokenId)
    {
        if (_skipSpecialTokens && _tokenizer.SpecialTokens.IsSpecialId(tokenId))
        {
            return string.Empty;
        }

        string? tokenStr = _tokenizer.Vocab.GetToken(tokenId);
        if (tokenStr == null) return string.Empty;

        // Check special token
        if (_tokenizer.SpecialTokens.IsSpecialId(tokenId))
        {
            return tokenStr;
        }

        // Convert token string to raw byte sequence
        int maxBytes = tokenStr.Length * 4;
        byte[] incomingBytes = new byte[maxBytes];
        int byteCount = 0;
        byte[] charUtf8Buf = new byte[8];

        for (int c = 0; c < tokenStr.Length; c++)
        {
            char ch = tokenStr[c];
            if (ByteLevelBpeHelper.TryCharToByte(ch, out byte b))
            {
                incomingBytes[byteCount++] = b;
            }
            else
            {
                int written = Encoding.UTF8.GetBytes(tokenStr.AsSpan(c, 1), charUtf8Buf);
                for (int k = 0; k < written; k++)
                {
                    incomingBytes[byteCount++] = charUtf8Buf[k];
                }
            }
        }

        if (byteCount == 0) return string.Empty;

        // Use Decoder.Convert for native streaming decode
        char[] charBuffer = new char[byteCount * 2 + 8];
        _utf8Decoder.Convert(
            incomingBytes,
            0,
            byteCount,
            charBuffer,
            0,
            charBuffer.Length,
            flush: false,
            out int bytesUsed,
            out int charsUsed,
            out bool completed);

        if (charsUsed == 0) return string.Empty;

        return new string(charBuffer, 0, charsUsed);
    }

    /// <summary>
    /// Flushes any remaining bytes in the decoder and resets state.
    /// </summary>
    public string Flush()
    {
        char[] charBuffer = new char[16];
        _utf8Decoder.Convert(
            Array.Empty<byte>(),
            0,
            0,
            charBuffer,
            0,
            charBuffer.Length,
            flush: true,
            out int bytesUsed,
            out int charsUsed,
            out bool completed);

        _utf8Decoder.Reset();
        return charsUsed > 0 ? new string(charBuffer, 0, charsUsed) : string.Empty;
    }

    /// <summary>
    /// Resets the decoder state.
    /// </summary>
    public void Reset()
    {
        _utf8Decoder.Reset();
    }
}
