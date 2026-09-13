using System;
using System.Collections.Generic;

namespace TokenVector.Text.Models;

/// <summary>
/// Result of encoding a single text sequence.
/// </summary>
public sealed class EncodingResult
{
    private readonly int[] _ids;
    private readonly int[] _attentionMask;
    private readonly int[]? _typeIds;
    private readonly (int Start, int End)[]? _offsets;
    private readonly string[]? _tokens;
    private readonly bool[]? _specialTokensMask;

    public int[] Ids => _ids;
    public int Length => _ids.Length;
    public ReadOnlySpan<int> Span => _ids.AsSpan();
    public ReadOnlyMemory<int> Memory => _ids.AsMemory();

    public int[] AttentionMask => _attentionMask;
    public ReadOnlySpan<int> AttentionMaskSpan => _attentionMask.AsSpan();

    public int[]? TypeIds => _typeIds;
    public (int Start, int End)[]? Offsets => _offsets;
    public string[]? Tokens => _tokens;
    public bool[]? SpecialTokensMask => _specialTokensMask;

    public EncodingResult(
        int[] ids,
        int[]? attentionMask = null,
        int[]? typeIds = null,
        (int Start, int End)[]? offsets = null,
        string[]? tokens = null,
        bool[]? specialTokensMask = null)
    {
        ArgumentNullException.ThrowIfNull(ids);
        _ids = ids;

        if (attentionMask != null)
        {
            _attentionMask = attentionMask;
        }
        else
        {
            _attentionMask = new int[ids.Length];
            Array.Fill(_attentionMask, 1);
        }

        _typeIds = typeIds;
        _offsets = offsets;
        _tokens = tokens;
        _specialTokensMask = specialTokensMask;
    }

    public static EncodingResult Empty => new(Array.Empty<int>(), Array.Empty<int>());

    public override string ToString() => $"EncodingResult(Length={_ids.Length}, Ids=[{string.Join(", ", _ids)}])";
}
