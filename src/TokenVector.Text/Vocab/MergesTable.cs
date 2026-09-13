using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TokenVector.Text.Vocab;

/// <summary>
/// High-performance merge ranking and fast integer pair lookup table for BPE tokenizers.
/// </summary>
public sealed class MergesTable
{
    public readonly struct MergeInfo
    {
        public readonly int Rank;
        public readonly int MergedId;

        public MergeInfo(int rank, int mergedId)
        {
            Rank = rank;
            MergedId = mergedId;
        }
    }

    private readonly Dictionary<ulong, MergeInfo> _idPairMergeMap;
    private readonly Dictionary<string, int> _stringPairRanks;
    private readonly int _count;

    public int Count => _count;

    public MergesTable(IReadOnlyList<(string First, string Second)> merges, VocabTable? vocab = null)
    {
        ArgumentNullException.ThrowIfNull(merges);
        _count = merges.Count;
        _idPairMergeMap = new Dictionary<ulong, MergeInfo>(_count);
        _stringPairRanks = new Dictionary<string, int>(_count, StringComparer.Ordinal);

        for (int rank = 0; rank < merges.Count; rank++)
        {
            var (first, second) = merges[rank];
            string combined = $"{first} {second}";
            _stringPairRanks[combined] = rank;
        }

        if (vocab != null)
        {
            BindVocab(vocab);
        }
    }

    public MergesTable(IReadOnlyDictionary<ulong, MergeInfo> idPairMerges)
    {
        ArgumentNullException.ThrowIfNull(idPairMerges);
        _count = idPairMerges.Count;
        _idPairMergeMap = new Dictionary<ulong, MergeInfo>(idPairMerges);
        _stringPairRanks = new Dictionary<string, int>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong MakePairKey(int firstId, int secondId)
    {
        return ((ulong)(uint)firstId << 32) | (uint)secondId;
    }

    /// <summary>
    /// Gets the merge rank and merged target token ID for a pair of token IDs in O(1).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMerge(int firstId, int secondId, out int rank, out int mergedId)
    {
        ulong key = MakePairKey(firstId, secondId);
        if (_idPairMergeMap.TryGetValue(key, out var info))
        {
            rank = info.Rank;
            mergedId = info.MergedId;
            return true;
        }
        rank = int.MaxValue;
        mergedId = -1;
        return false;
    }

    /// <summary>
    /// Gets the merge rank for a pair of token IDs.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRank(int firstId, int secondId, out int rank)
    {
        ulong key = MakePairKey(firstId, secondId);
        if (_idPairMergeMap.TryGetValue(key, out var info))
        {
            rank = info.Rank;
            return true;
        }
        rank = int.MaxValue;
        return false;
    }

    /// <summary>
    /// Gets the merge rank for a combined string pair "first second".
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRank(string combinedPair, out int rank)
    {
        return _stringPairRanks.TryGetValue(combinedPair, out rank);
    }

    /// <summary>
    /// Binds vocabulary to merge table, resolving string tokens to integer IDs for zero-alloc integer lookups.
    /// </summary>
    public void BindVocab(VocabTable vocab)
    {
        foreach (var (pairStr, rank) in _stringPairRanks)
        {
            int spaceIdx = pairStr.IndexOf(' ');
            if (spaceIdx > 0)
            {
                var first = pairStr.AsSpan(0, spaceIdx);
                var second = pairStr.AsSpan(spaceIdx + 1);
                if (vocab.TryGetId(first, out int id1) && vocab.TryGetId(second, out int id2))
                {
                    string combined = string.Concat(first, second);
                    int mergedId = vocab.GetIdOrDefault(combined.AsSpan(), -1);

                    ulong key = MakePairKey(id1, id2);
                    _idPairMergeMap[key] = new MergeInfo(rank, mergedId);
                }
            }
        }
    }
}
