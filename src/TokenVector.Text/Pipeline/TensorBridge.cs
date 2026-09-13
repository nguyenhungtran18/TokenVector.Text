using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using TokenVector.Numerics.Core;
using TokenVector.Text.Models;

namespace TokenVector.Text.Pipeline;

/// <summary>
/// High-performance zero-copy bridge between TokenVector.Text tokenized sequences and TokenVector.Numerics NDArray tensors.
/// </summary>
public static class TensorBridge
{
    /// <summary>
    /// Constructs a 2D NDArray[Batch, SeqLen] and BatchEncodingResult from encoded sequences.
    /// </summary>
    public static BatchEncodingResult BuildBatchTensor(
        IReadOnlyList<EncodingResult> encodings,
        int padTokenId = 0,
        int maxLength = -1,
        PaddingStrategy padding = PaddingStrategy.Longest,
        TruncationStrategy truncation = TruncationStrategy.DoNotTruncate,
        PaddingDirection paddingDirection = PaddingDirection.Right)
    {
        int batchSize = encodings.Count;
        if (batchSize == 0)
        {
            var emptyInput = new NDArray<int>(0, 0);
            var emptyMask = new NDArray<int>(0, 0);
            return new BatchEncodingResult(emptyInput, emptyMask, null, null, encodings);
        }

        // Determine target sequence length
        int targetLen = 0;
        for (int i = 0; i < batchSize; i++)
        {
            int len = encodings[i].Length;
            if (len > targetLen) targetLen = len;
        }

        if (truncation == TruncationStrategy.TruncateToMax && maxLength > 0)
        {
            targetLen = Math.Min(targetLen, maxLength);
        }

        if (padding == PaddingStrategy.MaxFixed && maxLength > 0)
        {
            targetLen = maxLength;
        }
        else if (padding == PaddingStrategy.DoNotPad)
        {
            // If DoNotPad is specified, sequences must all be same length or we use max length
        }

        if (targetLen == 0) targetLen = 1;

        // Allocate NDArray tensors for input_ids and attention_mask
        var inputIdsTensor = new NDArray<int>(batchSize, targetLen);
        var attentionMaskTensor = new NDArray<int>(batchSize, targetLen);
        var positionIdsTensor = new NDArray<int>(batchSize, targetLen);

        Span<int> inputSpan = inputIdsTensor.Buffer.AsSpan();
        Span<int> maskSpan = attentionMaskTensor.Buffer.AsSpan();
        Span<int> posSpan = positionIdsTensor.Buffer.AsSpan();

        // Fill background with padding token and 0 mask
        inputSpan.Fill(padTokenId);
        maskSpan.Fill(0);
        posSpan.Fill(0);

        // Populate tensor rows
        for (int b = 0; b < batchSize; b++)
        {
            var enc = encodings[b];
            ReadOnlySpan<int> ids = enc.Span;
            int copyLen = ids.Length;

            if (truncation == TruncationStrategy.TruncateToMax && maxLength > 0 && copyLen > maxLength)
            {
                copyLen = maxLength;
            }
            if (copyLen > targetLen)
            {
                copyLen = targetLen;
            }

            int rowOffset = b * targetLen;

            if (paddingDirection == PaddingDirection.Right)
            {
                ids.Slice(0, copyLen).CopyTo(inputSpan.Slice(rowOffset, copyLen));
                maskSpan.Slice(rowOffset, copyLen).Fill(1);
                for (int p = 0; p < copyLen; p++)
                {
                    posSpan[rowOffset + p] = p;
                }
            }
            else // Left padding
            {
                int padCount = targetLen - copyLen;
                ids.Slice(0, copyLen).CopyTo(inputSpan.Slice(rowOffset + padCount, copyLen));
                maskSpan.Slice(rowOffset + padCount, copyLen).Fill(1);
                for (int p = 0; p < copyLen; p++)
                {
                    posSpan[rowOffset + padCount + p] = p;
                }
            }
        }

        return new BatchEncodingResult(inputIdsTensor, attentionMaskTensor, positionIdsTensor, null, encodings);
    }
}
