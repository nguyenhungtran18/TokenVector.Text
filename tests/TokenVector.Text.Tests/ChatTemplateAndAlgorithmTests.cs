using System;
using System.Collections.Generic;
using TokenVector.Text.Algorithms;
using TokenVector.Text.Templates;
using Xunit;

namespace TokenVector.Text.Tests;

public class ChatTemplateAndAlgorithmTests
{
    [Fact]
    public void ChatML_Template_FormatsCorrectly()
    {
        var messages = new[]
        {
            ChatMessage.System("You are a helpful AI."),
            ChatMessage.User("What is TokenVector?"),
            ChatMessage.Assistant("TokenVector is a high-performance AI framework.")
        };

        string rendered = ChatTemplate.ChatML.Render(messages, addGenerationPrompt: true);

        Assert.Contains("<|im_start|>system\nYou are a helpful AI.<|im_end|>\n", rendered);
        Assert.Contains("<|im_start|>user\nWhat is TokenVector?<|im_end|>\n", rendered);
        Assert.Contains("<|im_start|>assistant\nTokenVector is a high-performance AI framework.<|im_end|>\n", rendered);
        Assert.EndsWith("<|im_start|>assistant\n", rendered);
    }

    [Fact]
    public void Llama3_Template_FormatsCorrectly()
    {
        var messages = new[]
        {
            ChatMessage.User("Hello LLaMA!")
        };

        string rendered = ChatTemplate.Llama3.Render(messages, addGenerationPrompt: true);

        Assert.StartsWith("<|begin_of_text|>", rendered);
        Assert.Contains("<|start_header_id|>user<|end_header_id|>\n\nHello LLaMA!<|eot_id|>", rendered);
        Assert.EndsWith("<|start_header_id|>assistant<|end_header_id|>\n\n", rendered);
    }

    [Fact]
    public void BM25_RanksRelevantDocumentHighest()
    {
        var tokenizer = Tokenizer.CreateVietnameseBpe();

        var corpus = new[]
        {
            "Học máy và trí tuệ nhân tạo phát triển rất nhanh.",
            "TokenVector là nền tảng tensor và tính toán hiệu năng cao.",
            "Thời tiết hôm nay rất đẹp và nắng ấm."
        };

        var tokenizedCorpus = new List<ReadOnlyMemory<int>>();
        foreach (var doc in corpus)
        {
            tokenizedCorpus.Add(tokenizer.Encode(doc.AsSpan()).Memory);
        }

        var bm25 = new BM25Vectorizer();
        bm25.Fit(tokenizedCorpus);

        var query = tokenizer.Encode("TokenVector hiệu năng cao".AsSpan());
        var topResults = bm25.QueryTopK(query.Span, k: 3);

        Assert.NotEmpty(topResults);
        Assert.Equal(1, topResults[0].DocIndex); // Document 1 is "TokenVector là nền tảng..."
    }

    [Fact]
    public void MinHash_ComputesHighSimilarityForDuplicates()
    {
        var minHash = new MinHash(numPermutations: 128);

        int[] docA = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int[] docB = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 11 }; // 90% identical
        int[] docC = new[] { 101, 102, 103, 104, 105 }; // completely different

        var sigA = minHash.ComputeSignature(docA);
        var sigB = minHash.ComputeSignature(docB);
        var sigC = minHash.ComputeSignature(docC);

        double simAB = MinHash.JaccardSimilarity(sigA, sigB);
        double simAC = MinHash.JaccardSimilarity(sigA, sigC);

        Assert.True(simAB > 0.7);
        Assert.True(simAC < 0.1);
    }

    [Fact]
    public void SequencePairProcessor_EncodesPairsWithSegmentIds()
    {
        var tokenizer = Tokenizer.CreateBert();

        var res = SequencePairProcessor.EncodePair(
            tokenizer,
            "hello".AsSpan(),
            "world".AsSpan(),
            clsTokenId: 101,
            sepTokenId: 102);

        Assert.NotNull(res.TypeIds);
        Assert.Equal(res.Length, res.TypeIds!.Length);

        // Sequence A part should have TypeId 0
        Assert.Equal(0, res.TypeIds[0]); // [CLS]
        Assert.Equal(0, res.TypeIds[1]); // hello
        Assert.Equal(0, res.TypeIds[2]); // [SEP]

        // Sequence B part should have TypeId 1
        Assert.Equal(1, res.TypeIds[3]); // world
        Assert.Equal(1, res.TypeIds[4]); // [SEP]
    }
}
