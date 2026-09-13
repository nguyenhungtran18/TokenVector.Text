using System;
using System.Collections.Generic;
using TokenVector.Text.Models;
using TokenVector.Text.Tokenizers.Unigram;
using TokenVector.Text.Vocab;
using Xunit;

namespace TokenVector.Text.Tests;

public class UnigramTokenizerTests
{
    [Fact]
    public void Unigram_ViterbiSegmentation_PicksOptimalPath()
    {
        // Setup pieces with scores (log probs)
        var pieces = new List<(string Text, double Score)>
        {
            ("<unk>", 0.0), // 0
            ("<s>", 0.0),   // 1
            ("</s>", 0.0),  // 2
            (" ", -1.0),    // 3
            ("\u2581token", -2.0),// 4
            ("\u2581vector", -2.0),// 5
            ("t", -5.0),    // 6
            ("o", -5.0),    // 7
            ("k", -5.0),    // 8
            ("e", -5.0),    // 9
            ("n", -5.0),    // 10
        };

        var model = new UnigramModel(pieces);
        var options = new UnigramOptions
        {
            AddDummyPrefix = true,
            BosToken = null,
            EosToken = null
        };

        var tokenizer = new UnigramTokenizer(model, options);
        var result = tokenizer.Encode("token vector".AsSpan(), addSpecialTokens: false);

        // Expect optimal pieces " token" (4) and " vector" (5)
        Assert.Equal(2, result.Length);
        Assert.Equal(4, result.Ids[0]); //  token
        Assert.Equal(5, result.Ids[1]); //  vector

        string decoded = tokenizer.Decode(result.Span, skipSpecialTokens: true);
        Assert.Equal("token vector", decoded);
    }
}
