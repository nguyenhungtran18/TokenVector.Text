using System;
using System.Buffers;
using TokenVector.Numerics.Core;
using TokenVector.Text.Models;
using TokenVector.Text.Vocab;

namespace TokenVector.Text;

/// <summary>
/// Unified interface for high-performance zero-allocation tokenizers in TokenVector.Text.
/// </summary>
public interface ITokenizer
{
    /// <summary>
    /// Vocabulary table associated with this tokenizer.
    /// </summary>
    VocabTable Vocab { get; }

    /// <summary>
    /// Set of registered special tokens and their assigned IDs.
    /// </summary>
    SpecialTokenSet SpecialTokens { get; }

    /// <summary>
    /// Encodes a text character span into token IDs and metadata.
    /// </summary>
    EncodingResult Encode(ReadOnlySpan<char> text, bool addSpecialTokens = true);

    /// <summary>
    /// Encodes raw UTF-8 byte span into token IDs and metadata.
    /// </summary>
    EncodingResult EncodeUtf8(ReadOnlySpan<byte> utf8Text, bool addSpecialTokens = true);

    /// <summary>
    /// Batch encodes multiple text sequences into an unmanaged tensor-backed BatchEncodingResult.
    /// </summary>
    BatchEncodingResult EncodeBatch(
        ReadOnlySpan<string> texts,
        int maxLength = -1,
        PaddingStrategy padding = PaddingStrategy.Longest,
        TruncationStrategy truncation = TruncationStrategy.DoNotTruncate);

    /// <summary>
    /// Direct zero-copy export of tokenized input IDs into a TokenVector.Numerics NDArray[Batch, SeqLen].
    /// </summary>
    NDArray<int> EncodeToTensor(ReadOnlySpan<string> texts, int maxLength = -1);

    /// <summary>
    /// Decodes an array or span of token IDs back into text.
    /// </summary>
    string Decode(ReadOnlySpan<int> tokenIds, bool skipSpecialTokens = true);

    /// <summary>
    /// Decodes token IDs directly into a byte buffer writer without allocating managed strings.
    /// </summary>
    void DecodeUtf8(ReadOnlySpan<int> tokenIds, IBufferWriter<byte> output, bool skipSpecialTokens = true);
}
