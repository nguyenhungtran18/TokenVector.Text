using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TokenVector.Numerics.Core;
using TokenVector.Text.Models;
using TokenVector.Text.Pipeline;
using TokenVector.Text.Vocab;

namespace TokenVector.Text.Tokenizers;

/// <summary>
/// Abstract base class for high-performance zero-allocation tokenizers in TokenVector.
/// </summary>
public abstract class BaseTokenizer : ITokenizer
{
    protected readonly VocabTable _vocab;
    protected readonly SpecialTokenSet _specialTokens;
    protected readonly int _padTokenId;
    protected readonly int _unkTokenId;
    protected readonly int _bosTokenId;
    protected readonly int _eosTokenId;

    public VocabTable Vocab => _vocab;
    public SpecialTokenSet SpecialTokens => _specialTokens;
    public int PadTokenId => _padTokenId;
    public int UnkTokenId => _unkTokenId;
    public int BosTokenId => _bosTokenId;
    public int EosTokenId => _eosTokenId;

    protected BaseTokenizer(
        VocabTable vocab,
        SpecialTokenSet? specialTokens = null,
        string? padToken = null,
        string? unkToken = null,
        string? bosToken = null,
        string? eosToken = null)
    {
        _vocab = vocab ?? throw new ArgumentNullException(nameof(vocab));
        _specialTokens = specialTokens ?? new SpecialTokenSet();

        _padTokenId = ResolveTokenId(padToken, 0);
        _unkTokenId = ResolveTokenId(unkToken, -1);
        _bosTokenId = ResolveTokenId(bosToken, -1);
        _eosTokenId = ResolveTokenId(eosToken, -1);
    }

    private int ResolveTokenId(string? token, int fallback)
    {
        if (string.IsNullOrEmpty(token)) return fallback;
        if (_specialTokens.TryGetSpecialId(token.AsSpan(), out int sid)) return sid;
        if (_vocab.TryGetId(token, out int vid)) return vid;
        return fallback;
    }

    public abstract EncodingResult Encode(ReadOnlySpan<char> text, bool addSpecialTokens = true);

    public virtual EncodingResult EncodeUtf8(ReadOnlySpan<byte> utf8Text, bool addSpecialTokens = true)
    {
        int charCount = Encoding.UTF8.GetCharCount(utf8Text);
        if (charCount <= 512)
        {
            Span<char> chars = stackalloc char[charCount];
            Encoding.UTF8.GetChars(utf8Text, chars);
            return Encode(chars, addSpecialTokens);
        }
        else
        {
            char[] rented = ArrayPool<char>.Shared.Rent(charCount);
            try
            {
                int written = Encoding.UTF8.GetChars(utf8Text, rented);
                return Encode(rented.AsSpan(0, written), addSpecialTokens);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    public virtual BatchEncodingResult EncodeBatch(
        ReadOnlySpan<string> texts,
        int maxLength = -1,
        PaddingStrategy padding = PaddingStrategy.Longest,
        TruncationStrategy truncation = TruncationStrategy.DoNotTruncate)
    {
        int count = texts.Length;
        var encodings = new EncodingResult[count];

        for (int i = 0; i < count; i++)
        {
            encodings[i] = Encode(texts[i].AsSpan(), addSpecialTokens: true);
        }

        return TensorBridge.BuildBatchTensor(
            encodings,
            _padTokenId,
            maxLength,
            padding,
            truncation);
    }

    /// <summary>
    /// Parallel batch encoding overload for array of texts.
    /// </summary>
    public virtual BatchEncodingResult EncodeBatch(
        string[] texts,
        int maxLength = -1,
        PaddingStrategy padding = PaddingStrategy.Longest,
        TruncationStrategy truncation = TruncationStrategy.DoNotTruncate)
    {
        ArgumentNullException.ThrowIfNull(texts);
        int count = texts.Length;
        var encodings = new EncodingResult[count];

        if (count >= 16)
        {
            Parallel.For(0, count, i =>
            {
                encodings[i] = Encode(texts[i].AsSpan(), addSpecialTokens: true);
            });
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                encodings[i] = Encode(texts[i].AsSpan(), addSpecialTokens: true);
            }
        }

        return TensorBridge.BuildBatchTensor(
            encodings,
            _padTokenId,
            maxLength,
            padding,
            truncation);
    }

    public virtual NDArray<int> EncodeToTensor(ReadOnlySpan<string> texts, int maxLength = -1)
    {
        using var batchResult = EncodeBatch(
            texts,
            maxLength,
            PaddingStrategy.Longest,
            maxLength > 0 ? TruncationStrategy.TruncateToMax : TruncationStrategy.DoNotTruncate);

        var tensor = batchResult.InputIds;
        var result = new NDArray<int>(tensor.Shape);
        tensor.Buffer.AsSpan().CopyTo(result.Buffer.AsSpan());
        return result;
    }

    public abstract string Decode(ReadOnlySpan<int> tokenIds, bool skipSpecialTokens = true);

    public virtual void DecodeUtf8(ReadOnlySpan<int> tokenIds, IBufferWriter<byte> output, bool skipSpecialTokens = true)
    {
        string text = Decode(tokenIds, skipSpecialTokens);
        int byteCount = Encoding.UTF8.GetByteCount(text);
        var span = output.GetSpan(byteCount);
        int written = Encoding.UTF8.GetBytes(text, span);
        output.Advance(written);
    }
}
