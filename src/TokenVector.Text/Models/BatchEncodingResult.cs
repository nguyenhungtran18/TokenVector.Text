using System;
using System.Collections.Generic;
using TokenVector.Numerics.Core;

namespace TokenVector.Text.Models;

/// <summary>
/// Result of batch encoding sequences directly mapped to TokenVector.Numerics NDArray tensors.
/// </summary>
public sealed class BatchEncodingResult : IDisposable
{
    private readonly NDArray<int> _inputIds;
    private readonly NDArray<int> _attentionMask;
    private readonly NDArray<int>? _positionIds;
    private readonly NDArray<int>? _tokenTypeIds;
    private readonly IReadOnlyList<EncodingResult> _encodings;
    private bool _disposed;

    /// <summary>
    /// Tensor of shape [BatchSize, SequenceLength] containing token IDs.
    /// </summary>
    public NDArray<int> InputIds => _inputIds;

    /// <summary>
    /// Tensor of shape [BatchSize, SequenceLength] containing 1s for real tokens and 0s for padding.
    /// </summary>
    public NDArray<int> AttentionMask => _attentionMask;

    /// <summary>
    /// Optional Tensor of shape [BatchSize, SequenceLength] containing position indices.
    /// </summary>
    public NDArray<int>? PositionIds => _positionIds;

    /// <summary>
    /// Optional Tensor of shape [BatchSize, SequenceLength] containing segment type IDs.
    /// </summary>
    public NDArray<int>? TokenTypeIds => _tokenTypeIds;

    /// <summary>
    /// Individual encoding results for each sequence in the batch.
    /// </summary>
    public IReadOnlyList<EncodingResult> Encodings => _encodings;

    public int BatchSize => _inputIds.Shape[0];
    public int SequenceLength => _inputIds.Shape[1];

    public BatchEncodingResult(
        NDArray<int> inputIds,
        NDArray<int> attentionMask,
        NDArray<int>? positionIds = null,
        NDArray<int>? tokenTypeIds = null,
        IReadOnlyList<EncodingResult>? encodings = null)
    {
        _inputIds = inputIds ?? throw new ArgumentNullException(nameof(inputIds));
        _attentionMask = attentionMask ?? throw new ArgumentNullException(nameof(attentionMask));
        _positionIds = positionIds;
        _tokenTypeIds = tokenTypeIds;
        _encodings = encodings ?? Array.Empty<EncodingResult>();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _inputIds.Dispose();
            _attentionMask.Dispose();
            _positionIds?.Dispose();
            _tokenTypeIds?.Dispose();
            _disposed = true;
        }
    }
}
