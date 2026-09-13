using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TokenVector.Text.Vocab;

/// <summary>
/// High-speed Special Token detector and manager with zero-allocation Trie-based substring scanner.
/// </summary>
public sealed class SpecialTokenSet
{
    private sealed class TrieNode
    {
        public readonly Dictionary<char, TrieNode> Children = new();
        public int TokenId = -1;
        public string? TokenString = null;
    }

    private readonly TrieNode _root = new();
    private readonly Dictionary<string, int> _specialTokens = new(StringComparer.Ordinal);
    private readonly HashSet<int> _specialIds = new();
    private int _maxTokenLength;

    public int Count => _specialTokens.Count;
    public int MaxTokenLength => _maxTokenLength;

    public SpecialTokenSet()
    {
    }

    public SpecialTokenSet(IReadOnlyDictionary<string, int> specialTokens)
    {
        ArgumentNullException.ThrowIfNull(specialTokens);
        foreach (var (token, id) in specialTokens)
        {
            Add(token, id);
        }
    }

    /// <summary>
    /// Adds a special token to the set.
    /// </summary>
    public void Add(string token, int id)
    {
        if (string.IsNullOrEmpty(token)) return;

        _specialTokens[token] = id;
        _specialIds.Add(id);
        if (token.Length > _maxTokenLength)
        {
            _maxTokenLength = token.Length;
        }

        var node = _root;
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (!node.Children.TryGetValue(c, out var next))
            {
                next = new TrieNode();
                node.Children[c] = next;
            }
            node = next;
        }
        node.TokenId = id;
        node.TokenString = token;
    }

    /// <summary>
    /// Checks if a token ID is a registered special token.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSpecialId(int id) => _specialIds.Contains(id);

    /// <summary>
    /// Checks if a string or char span matches a special token exactly.
    /// </summary>
    public bool TryGetSpecialId(ReadOnlySpan<char> span, out int id)
    {
        var node = _root;
        for (int i = 0; i < span.Length; i++)
        {
            if (!node.Children.TryGetValue(span[i], out var next))
            {
                id = -1;
                return false;
            }
            node = next;
        }

        if (node.TokenId != -1)
        {
            id = node.TokenId;
            return true;
        }

        id = -1;
        return false;
    }

    /// <summary>
    /// Finds the earliest matching special token in a text span.
    /// Returns (index, length, tokenId) or (-1, 0, -1) if none found.
    /// </summary>
    public (int Index, int Length, int TokenId) FindFirst(ReadOnlySpan<char> text)
    {
        if (_specialTokens.Count == 0 || text.IsEmpty)
        {
            return (-1, 0, -1);
        }

        for (int i = 0; i < text.Length; i++)
        {
            var node = _root;
            int matchLength = 0;
            int matchedId = -1;

            for (int j = i; j < text.Length; j++)
            {
                if (!node.Children.TryGetValue(text[j], out var next))
                {
                    break;
                }
                node = next;
                if (node.TokenId != -1)
                {
                    matchedId = node.TokenId;
                    matchLength = j - i + 1;
                }
            }

            if (matchedId != -1)
            {
                return (i, matchLength, matchedId);
            }
        }

        return (-1, 0, -1);
    }

    public IReadOnlyDictionary<string, int> AsDictionary() => _specialTokens;
}
