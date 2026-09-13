using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TokenVector.Text.Models;
using TokenVector.Text.Unicode;
using TokenVector.Text.Vocab;

namespace TokenVector.Text.Tokenizers.WordPiece;

/// <summary>
/// High-performance WordPiece tokenizer fully compatible with BERT, DistilBERT, and RoBERTa architectures.
/// </summary>
public sealed class WordPieceTokenizer : BaseTokenizer
{
    private readonly WordPieceOptions _options;
    private readonly int _clsTokenId;
    private readonly int _sepTokenId;

    public WordPieceOptions Options => _options;
    public int ClsTokenId => _clsTokenId;
    public int SepTokenId => _sepTokenId;

    public WordPieceTokenizer(
        VocabTable vocab,
        WordPieceOptions? options = null,
        SpecialTokenSet? specialTokens = null)
        : base(vocab, specialTokens, options?.PadToken, options?.UnkToken, options?.ClsToken, options?.SepToken)
    {
        _options = options ?? WordPieceOptions.BertDefault;

        _clsTokenId = _vocab.GetIdOrDefault(_options.ClsToken.AsSpan(), 101);
        _sepTokenId = _vocab.GetIdOrDefault(_options.SepToken.AsSpan(), 102);

        // Register default special tokens if not present
        if (_specialTokens.Count == 0)
        {
            if (_vocab.TryGetId(_options.PadToken, out int padId)) _specialTokens.Add(_options.PadToken, padId);
            if (_vocab.TryGetId(_options.UnkToken, out int unkId)) _specialTokens.Add(_options.UnkToken, unkId);
            if (_vocab.TryGetId(_options.ClsToken, out int clsId)) _specialTokens.Add(_options.ClsToken, clsId);
            if (_vocab.TryGetId(_options.SepToken, out int sepId)) _specialTokens.Add(_options.SepToken, sepId);
            if (_vocab.TryGetId(_options.MaskToken, out int maskId)) _specialTokens.Add(_options.MaskToken, maskId);
        }
    }

    public override EncodingResult Encode(ReadOnlySpan<char> text, bool addSpecialTokens = true)
    {
        if (text.IsEmpty)
        {
            if (addSpecialTokens && _clsTokenId != -1 && _sepTokenId != -1)
            {
                return new EncodingResult(new[] { _clsTokenId, _sepTokenId });
            }
            return EncodingResult.Empty;
        }

        // Clean & Normalize
        string cleaned = UnicodeNormalizer.CleanText(text);
        if (_options.StripAccents)
        {
            cleaned = UnicodeNormalizer.StripDiacritics(cleaned.AsSpan());
        }
        if (_options.DoLowerCase)
        {
            cleaned = cleaned.ToLowerInvariant();
        }

        var idList = new List<int>();

        // Add [CLS] at beginning
        if (addSpecialTokens && _clsTokenId != -1)
        {
            idList.Add(_clsTokenId);
        }

        // Basic Tokenization (Whitespace & Punctuation Splitting)
        var words = BasicTokenize(cleaned.AsSpan());

        foreach (var word in words)
        {
            // Check for special tokens
            if (_specialTokens.TryGetSpecialId(word.AsSpan(), out int specialId))
            {
                idList.Add(specialId);
                continue;
            }

            TokenizeWordPiece(word.AsSpan(), idList);
        }

        // Add [SEP] at end
        if (addSpecialTokens && _sepTokenId != -1)
        {
            idList.Add(_sepTokenId);
        }

        return new EncodingResult(idList.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void TokenizeWordPiece(ReadOnlySpan<char> word, List<int> outputIds)
    {
        if (word.Length > _options.MaxInputCharsPerWord)
        {
            outputIds.Add(_unkTokenId != -1 ? _unkTokenId : 100);
            return;
        }

        bool isBad = false;
        int start = 0;
        var subTokens = new List<int>();

        while (start < word.Length)
        {
            int end = word.Length;
            int matchedId = -1;

            while (start < end)
            {
                var subSlice = word.Slice(start, end - start);
                string candidate = start > 0 ? $"{_options.ContinuingSubwordPrefix}{subSlice}" : subSlice.ToString();

                if (_vocab.TryGetId(candidate, out int id))
                {
                    matchedId = id;
                    break;
                }
                end--;
            }

            if (matchedId == -1)
            {
                isBad = true;
                break;
            }

            subTokens.Add(matchedId);
            start = end;
        }

        if (isBad)
        {
            outputIds.Add(_unkTokenId != -1 ? _unkTokenId : 100);
        }
        else
        {
            outputIds.AddRange(subTokens);
        }
    }

    private static List<string> BasicTokenize(ReadOnlySpan<char> text)
    {
        var tokens = new List<string>();
        int i = 0;
        int len = text.Length;

        while (i < len)
        {
            // Skip whitespace
            while (i < len && char.IsWhiteSpace(text[i])) i++;
            if (i >= len) break;

            // Check if punctuation
            if (SimdTextScanner.IsPunctuation(text[i]))
            {
                tokens.Add(text[i].ToString());
                i++;
                continue;
            }

            // Consume continuous word
            int start = i;
            while (i < len && !char.IsWhiteSpace(text[i]) && !SimdTextScanner.IsPunctuation(text[i]))
            {
                i++;
            }
            tokens.Add(text.Slice(start, i - start).ToString());
        }

        return tokens;
    }

    public override string Decode(ReadOnlySpan<int> tokenIds, bool skipSpecialTokens = true)
    {
        if (tokenIds.IsEmpty) return string.Empty;

        var sb = new StringBuilder();
        bool isFirst = true;

        for (int i = 0; i < tokenIds.Length; i++)
        {
            int id = tokenIds[i];
            if (skipSpecialTokens && _specialTokens.IsSpecialId(id))
            {
                continue;
            }

            string? tokenStr = _vocab.GetToken(id);
            if (tokenStr == null) continue;

            if (tokenStr.StartsWith(_options.ContinuingSubwordPrefix, StringComparison.Ordinal))
            {
                sb.Append(tokenStr.AsSpan(_options.ContinuingSubwordPrefix.Length));
            }
            else
            {
                if (!isFirst && sb.Length > 0 && !char.IsPunctuation(tokenStr[0]))
                {
                    sb.Append(' ');
                }
                sb.Append(tokenStr);
                isFirst = false;
            }
        }

        return sb.ToString();
    }
}
