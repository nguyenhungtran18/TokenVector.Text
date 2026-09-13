using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TokenVector.Text.Models;
using TokenVector.Text.Vocab;

namespace TokenVector.Text.Tokenizers.Unigram;

/// <summary>
/// High-performance Unigram tokenizer utilizing Viterbi dynamic programming (SentencePiece / T5 / Gemma).
/// </summary>
public sealed class UnigramTokenizer : BaseTokenizer
{
    private readonly UnigramModel _model;
    private readonly UnigramOptions _options;
    private const string SpaceSymbol = "\u2581"; // Lower One Eighth Block ' '

    public UnigramModel Model => _model;
    public UnigramOptions Options => _options;

    private readonly struct ViterbiNode
    {
        public readonly int PrevIndex;
        public readonly int TokenId;
        public readonly double Score;

        public ViterbiNode(int prevIndex, int tokenId, double score)
        {
            PrevIndex = prevIndex;
            TokenId = tokenId;
            Score = score;
        }
    }

    public UnigramTokenizer(
        UnigramModel model,
        UnigramOptions? options = null,
        SpecialTokenSet? specialTokens = null)
        : base(model.ToVocabTable(), specialTokens, options?.PadToken, options?.UnkToken, options?.BosToken, options?.EosToken)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _options = options ?? UnigramOptions.SentencePieceDefault;
    }

    public override EncodingResult Encode(ReadOnlySpan<char> text, bool addSpecialTokens = true)
    {
        if (text.IsEmpty)
        {
            return EncodingResult.Empty;
        }

        // Preprocess: Replace spaces with ' ' and add dummy prefix if configured
        var sb = new StringBuilder(text.Length + 4);
        if (_options.AddDummyPrefix)
        {
            sb.Append(SpaceSymbol);
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ')
            {
                sb.Append(SpaceSymbol);
            }
            else
            {
                sb.Append(c);
            }
        }

        string normalized = sb.ToString();
        var idList = new List<int>();

        if (addSpecialTokens && _bosTokenId != -1)
        {
            idList.Add(_bosTokenId);
        }

        // Run Viterbi algorithm for optimal segmentation
        RunViterbi(normalized.AsSpan(), idList);

        if (addSpecialTokens && _eosTokenId != -1)
        {
            idList.Add(_eosTokenId);
        }

        return new EncodingResult(idList.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void RunViterbi(ReadOnlySpan<char> text, List<int> outputIds)
    {
        int len = text.Length;
        if (len == 0) return;

        // DP table: best score and backpointer for each position 0..len
        var dp = new ViterbiNode[len + 1];
        for (int i = 0; i <= len; i++)
        {
            dp[i] = new ViterbiNode(-1, -1, double.NegativeInfinity);
        }
        dp[0] = new ViterbiNode(-1, -1, 0.0);

        int maxPieceLen = _model.MaxPieceLength;

        for (int i = 0; i < len; i++)
        {
            if (double.IsNegativeInfinity(dp[i].Score)) continue;

            int maxEnd = Math.Min(len, i + maxPieceLen);
            for (int end = i + 1; end <= maxEnd; end++)
            {
                var piece = text.Slice(i, end - i);
                if (_model.TryGetId(piece, out int pieceId))
                {
                    double pieceScore = _model.GetScore(pieceId);
                    double newScore = dp[i].Score + pieceScore;

                    if (newScore > dp[end].Score)
                    {
                        dp[end] = new ViterbiNode(i, pieceId, newScore);
                    }
                }
            }

            // Fallback for character if no subword started here
            if (double.IsNegativeInfinity(dp[i + 1].Score))
            {
                var singleChar = text.Slice(i, 1);
                int unkOrId = _model.TryGetId(singleChar, out int chId) ? chId : (_unkTokenId != -1 ? _unkTokenId : 0);
                double fallbackScore = dp[i].Score - 100.0;
                if (fallbackScore > dp[i + 1].Score)
                {
                    dp[i + 1] = new ViterbiNode(i, unkOrId, fallbackScore);
                }
            }
        }

        // Backtrack from end to start
        var segments = new List<int>();
        int curr = len;
        while (curr > 0)
        {
            var node = dp[curr];
            if (node.TokenId != -1)
            {
                segments.Add(node.TokenId);
            }
            curr = node.PrevIndex;
            if (curr < 0 && curr != 0) break;
        }

        // Reverse segments to original order
        for (int i = segments.Count - 1; i >= 0; i--)
        {
            outputIds.Add(segments[i]);
        }
    }

    public override string Decode(ReadOnlySpan<int> tokenIds, bool skipSpecialTokens = true)
    {
        if (tokenIds.IsEmpty) return string.Empty;

        var sb = new StringBuilder();
        for (int i = 0; i < tokenIds.Length; i++)
        {
            int id = tokenIds[i];
            if (skipSpecialTokens && _specialTokens.IsSpecialId(id))
            {
                continue;
            }

            string? piece = _model.GetPiece(id);
            if (piece != null)
            {
                sb.Append(piece);
            }
        }

        // Replace ' ' with space ' '
        string result = sb.ToString().Replace(SpaceSymbol, " ");
        return result.TrimStart();
    }
}
