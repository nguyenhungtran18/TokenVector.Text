using System;
using System.Text;
using System.Threading.Tasks;
using TokenVector.Text.Models;
using Xunit;

namespace TokenVector.Text.Tests;

public class ConcurrencyAndEdgeCaseTests
{
    [Fact]
    public void ConcurrentEncoding_ThreadSafe_NoExceptions()
    {
        var tokenizer = Tokenizer.CreateVietnameseBpe();

        string sample = "Hệ sinh thái TokenVector AI Engine và mô hình ngôn ngữ lớn LLM hiệu năng cao!";

        // Run 500 parallel tasks encoding and decoding simultaneously
        Parallel.For(0, 500, i =>
        {
            var encoding = tokenizer.Encode(sample.AsSpan(), addSpecialTokens: false);
            Assert.NotEmpty(encoding.Ids);

            string decoded = tokenizer.Decode(encoding.Span, skipSpecialTokens: false);
            Assert.Equal(sample, decoded);
        });
    }

    [Fact]
    public void EncodeUtf8_MatchesCharSpanEncoding()
    {
        var tokenizer = Tokenizer.CreateVietnameseBpe();
        string sample = "Kiểm thử mã hóa trực tiếp UTF-8 không cần chuỗi trung gian 🚀";

        byte[] utf8Bytes = Encoding.UTF8.GetBytes(sample);

        var resChar = tokenizer.Encode(sample.AsSpan(), addSpecialTokens: false);
        var resUtf8 = tokenizer.EncodeUtf8(utf8Bytes.AsSpan(), addSpecialTokens: false);

        Assert.Equal(resChar.Ids.Length, resUtf8.Ids.Length);
        for (int i = 0; i < resChar.Ids.Length; i++)
        {
            Assert.Equal(resChar.Ids[i], resUtf8.Ids[i]);
        }
    }

    [Fact]
    public void LeftPaddingAndTruncation_WorksAccurately()
    {
        var tokenizer = Tokenizer.CreateVietnameseBpe();
        string[] texts = new[]
        {
            "Ngắn",
            "Đây là một câu dài hơn rất nhiều để kiểm tra khả năng padding bên trái"
        };

        var batchResult = tokenizer.EncodeBatch(
            texts,
            maxLength: 8,
            padding: PaddingStrategy.MaxFixed,
            truncation: TruncationStrategy.TruncateToMax);

        Assert.Equal(2, batchResult.BatchSize);
        Assert.Equal(8, batchResult.SequenceLength);
    }
}
