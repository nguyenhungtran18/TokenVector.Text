using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TokenVector.Text.Vocab;

/// <summary>
/// High-performance bi-directional vocabulary table supporting zero-allocation Span lookups in .NET 8.
/// </summary>
public sealed class VocabTable
{
    private struct Entry
    {
        public int HashCode;
        public int Next;
        public string Key;
        public int Value;
    }

    private readonly int[] _buckets;
    private readonly Entry[] _entries;
    private readonly string[] _idToToken;
    private readonly Dictionary<string, int> _tokenToId;
    private readonly int _count;
    private readonly int _mask;

    public int Count => _count;

    public VocabTable(IReadOnlyDictionary<string, int> vocab)
    {
        ArgumentNullException.ThrowIfNull(vocab);
        _count = vocab.Count;
        _tokenToId = new Dictionary<string, int>(_count, StringComparer.Ordinal);

        int maxId = 0;
        foreach (var (k, v) in vocab)
        {
            if (v > maxId) maxId = v;
        }

        _idToToken = new string[Math.Max(_count, maxId + 1)];

        // Initialize zero-alloc hash table with power-of-two size
        int size = NextPowerOfTwo(Math.Max(_count * 2, 16));
        _mask = size - 1;
        _buckets = new int[size];
        Array.Fill(_buckets, -1);
        _entries = new Entry[_count];

        int entryIdx = 0;
        foreach (var (token, id) in vocab)
        {
            _tokenToId[token] = id;
            if (id >= 0 && id < _idToToken.Length)
            {
                _idToToken[id] = token;
            }

            int hashCode = string.GetHashCode(token.AsSpan(), StringComparison.Ordinal);
            int bucket = hashCode & _mask;

            _entries[entryIdx] = new Entry
            {
                HashCode = hashCode,
                Next = _buckets[bucket],
                Key = token,
                Value = id
            };
            _buckets[bucket] = entryIdx;
            entryIdx++;
        }
    }

    public VocabTable(IEnumerable<string> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);
        int id = 0;
        foreach (var token in tokens)
        {
            if (!dict.ContainsKey(token))
            {
                dict[token] = id++;
            }
        }

        _count = dict.Count;
        _tokenToId = dict;
        _idToToken = new string[_count];

        int size = NextPowerOfTwo(Math.Max(_count * 2, 16));
        _mask = size - 1;
        _buckets = new int[size];
        Array.Fill(_buckets, -1);
        _entries = new Entry[_count];

        int entryIdx = 0;
        foreach (var (token, tid) in dict)
        {
            _idToToken[tid] = token;
            int hashCode = string.GetHashCode(token.AsSpan(), StringComparison.Ordinal);
            int bucket = hashCode & _mask;

            _entries[entryIdx] = new Entry
            {
                HashCode = hashCode,
                Next = _buckets[bucket],
                Key = token,
                Value = tid
            };
            _buckets[bucket] = entryIdx;
            entryIdx++;
        }
    }

    /// <summary>
    /// Zero-allocation lookup of token ID from a ReadOnlySpan of characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetId(ReadOnlySpan<char> token, out int id)
    {
        if (_count == 0)
        {
            id = -1;
            return false;
        }

        int hashCode = string.GetHashCode(token, StringComparison.Ordinal);
        int bucket = hashCode & _mask;

        for (int i = _buckets[bucket]; i >= 0; i = _entries[i].Next)
        {
            ref readonly var entry = ref _entries[i];
            if (entry.HashCode == hashCode && token.SequenceEqual(entry.Key.AsSpan()))
            {
                id = entry.Value;
                return true;
            }
        }

        id = -1;
        return false;
    }

    /// <summary>
    /// Lookup token ID by string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetId(string token, out int id)
    {
        return _tokenToId.TryGetValue(token, out id);
    }

    /// <summary>
    /// Lookup token ID or return default fallback.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetIdOrDefault(ReadOnlySpan<char> token, int defaultId = -1)
    {
        return TryGetId(token, out int id) ? id : defaultId;
    }

    /// <summary>
    /// Gets the string token for an ID.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? GetToken(int id)
    {
        if ((uint)id < (uint)_idToToken.Length)
        {
            return _idToToken[id];
        }
        return null;
    }

    /// <summary>
    /// Checks if a token exists in the vocabulary.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ReadOnlySpan<char> token)
    {
        return TryGetId(token, out _);
    }

    public IReadOnlyDictionary<string, int> AsDictionary() => _tokenToId;

    private static int NextPowerOfTwo(int value)
    {
        if (value <= 16) return 16;
        return (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)value);
    }
}
