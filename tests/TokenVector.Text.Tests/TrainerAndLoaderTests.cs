using System;
using System.IO;
using System.Text;
using TokenVector.Text.Training;
using TokenVector.Text.Vocab.Loaders;
using Xunit;

namespace TokenVector.Text.Tests;

public class TrainerAndLoaderTests
{
    [Fact]
    public void BpeTrainer_TrainsSubwordsFromCorpus_Successfully()
    {
        var corpus = new[]
        {
            "Hệ sinh thái TokenVector AI Engine và mô hình ngôn ngữ lớn LLM.",
            "TokenVector là nền tảng tensor và toán học hiệu năng cao.",
            "Tôi yêu tiếng Việt và các công nghệ trí tuệ nhân tạo mới nhất.",
            "Học máy và học sâu phát triển rất mạnh mẽ trên toàn cầu."
        };

        var trainer = new BpeTrainer(targetVocabSize: 300, minFrequency: 1);
        var trainedTokenizer = trainer.Train(corpus);

        Assert.NotNull(trainedTokenizer);
        Assert.True(trainedTokenizer.Vocab.Count >= 256);

        string testSentence = "Hệ sinh thái TokenVector";
        var encoded = trainedTokenizer.Encode(testSentence.AsSpan());
        Assert.NotEmpty(encoded.Ids);

        string decoded = trainedTokenizer.Decode(encoded.Span);
        Assert.Equal(testSentence, decoded);
    }

    [Fact]
    public void TiktokenBpeLoader_LoadsFromStream_Accurately()
    {
        // Construct a mini .tiktoken string
        // base64("hello") = "aGVsbG8=", base64("world") = "d29ybGQ="
        string tiktokenContent = "aGVsbG8= 0\nd29ybGQ= 1\n";
        byte[] bytes = Encoding.UTF8.GetBytes(tiktokenContent);

        using var stream = new MemoryStream(bytes);
        var (vocab, merges) = TiktokenBpeLoader.Load(stream);

        Assert.NotNull(vocab);
        Assert.Equal(2, vocab.Count);
        Assert.True(vocab.TryGetId("hello", out int id0));
        Assert.Equal(0, id0);
        Assert.True(vocab.TryGetId("world", out int id1));
        Assert.Equal(1, id1);
    }
}
