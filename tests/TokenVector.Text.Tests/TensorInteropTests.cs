using System;
using TokenVector.Numerics.Core;
using TokenVector.Text.Models;
using Xunit;

namespace TokenVector.Text.Tests;

public class TensorInteropTests
{
    [Fact]
    public void EncodeToTensor_OutputsValid2DNDArray()
    {
        var tokenizer = Tokenizer.CreateVietnameseBpe();

        string[] texts = new[]
        {
            "Hệ sinh thái TokenVector",
            "Mô hình ngôn ngữ lớn AI",
            "Zero-Copy Tensor Bridge"
        };

        using var tensor = tokenizer.EncodeToTensor(texts);

        Assert.Equal(2, tensor.Rank);
        Assert.Equal(3, tensor.Shape[0]); // Batch size = 3
        Assert.True(tensor.Shape[1] > 0);  // Max seq length

        // Check values in tensor
        int batchSize = tensor.Shape[0];
        int seqLen = tensor.Shape[1];

        for (int b = 0; b < batchSize; b++)
        {
            for (int s = 0; s < seqLen; s++)
            {
                int val = tensor[b, s];
                Assert.True(val >= 0);
            }
        }
    }

    [Fact]
    public void EncodeBatch_WithPaddingAndTruncation_WorksCorrectly()
    {
        var tokenizer = Tokenizer.CreateVietnameseBpe();

        string[] texts = new[]
        {
            "Ngắn",
            "Câu văn này dài hơn câu thứ nhất rất nhiều để kiểm tra padding và truncation"
        };

        using var batchResult = tokenizer.EncodeBatch(
            texts,
            maxLength: 10,
            padding: PaddingStrategy.MaxFixed,
            truncation: TruncationStrategy.TruncateToMax);

        Assert.Equal(2, batchResult.BatchSize);
        Assert.Equal(10, batchResult.SequenceLength);

        // Verify attention mask
        var mask = batchResult.AttentionMask;
        Assert.Equal(1, mask[0, 0]); // First token is real
        Assert.Equal(0, mask[0, 9]); // Last token is padded

        Assert.Equal(1, mask[1, 0]);
        Assert.Equal(1, mask[1, 9]); // Truncated to max length so all are real
    }
}
