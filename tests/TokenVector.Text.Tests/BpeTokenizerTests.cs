using System;
using System.Text;
using TokenVector.Text.Models;
using TokenVector.Text.Tokenizers.Bpe;
using TokenVector.Text.Vocab;
using Xunit;

namespace TokenVector.Text.Tests;

public class BpeTokenizerTests
{
    [Fact]
    public void VietnameseBpe_EncodeAndDecode_RoundTripMatches()
    {
        var tokenizer = Tokenizer.CreateVietnameseBpe();

        string[] testSentences = new[]
        {
            "Hệ sinh thái TokenVector AI Engine và mô hình LLM.",
            "Tôi yêu tiếng Việt với đầy đủ thanh điệu: ắ, ằ, ẳ, ẵ, ặ, ế, ề, ể, ễ, ệ, ố, ồ, ổ, ỗ, ộ, ứ, ừ, ử, ữ, ự.",
            "Zero-Copy High-Performance NLP Engine in C# 12 / .NET 8 Native AOT! 🚀🔥",
            "Special tokens: <|im_start|>system\nYou are an AI assistant.<|im_end|>",
            "1234567890 !@#$%^&*()_+~`|}{[]:;?><,./-="
        };

        foreach (var text in testSentences)
        {
            var encoding = tokenizer.Encode(text.AsSpan(), addSpecialTokens: false);
            Assert.NotEmpty(encoding.Ids);

            string decoded = tokenizer.Decode(encoding.Span, skipSpecialTokens: false);
            Assert.Equal(text, decoded);
        }
    }

    [Fact]
    public void VietnameseBpe_SpecialTokens_CorrectlyHandled()
    {
        var tokenizer = Tokenizer.CreateVietnameseBpe();
        string input = "<|im_start|>user\nXin chào TokenVector!<|im_end|>";

        var result = tokenizer.Encode(input.AsSpan(), addSpecialTokens: false);

        Assert.Contains(tokenizer.SpecialTokens.AsDictionary()["<|im_start|>"], result.Ids);
        Assert.Contains(tokenizer.SpecialTokens.AsDictionary()["<|im_end|>"], result.Ids);

        string decodedWithSpecial = tokenizer.Decode(result.Span, skipSpecialTokens: false);
        Assert.Equal(input, decodedWithSpecial);

        string decodedWithoutSpecial = tokenizer.Decode(result.Span, skipSpecialTokens: true);
        Assert.DoesNotContain("<|im_start|>", decodedWithoutSpecial);
        Assert.DoesNotContain("<|im_end|>", decodedWithoutSpecial);
    }

    [Fact]
    public void CustomBpe_PairMerge_FollowsRanks()
    {
        var vocabDict = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["h"] = 0,
            ["e"] = 1,
            ["l"] = 2,
            ["o"] = 3,
            ["he"] = 4,
            ["ll"] = 5,
            ["hello"] = 6
        };

        var mergesList = new List<(string, string)>
        {
            ("h", "e"),
            ("l", "l"),
            ("he", "ll"),
            ("hell", "o")
        };

        var vocab = new VocabTable(vocabDict);
        var merges = new MergesTable(mergesList, vocab);
        var options = new BpeOptions { ByteLevelFallback = false, Pattern = @"\w+" };

        var bpe = new BpeTokenizer(vocab, merges, options);
        var result = bpe.Encode("hello".AsSpan(), addSpecialTokens: false);

        Assert.Single(result.Ids);
        Assert.Equal(6, result.Ids[0]); // Merged into 'hello'
    }
}
