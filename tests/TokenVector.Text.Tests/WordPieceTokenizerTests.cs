using System;
using System.Collections.Generic;
using TokenVector.Text.Models;
using TokenVector.Text.Tokenizers.WordPiece;
using TokenVector.Text.Vocab;
using Xunit;

namespace TokenVector.Text.Tests;

public class WordPieceTokenizerTests
{
    [Fact]
    public void WordPiece_SubwordSegmentation_MatchesBertStandard()
    {
        var vocabDict = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["[PAD]"] = 0,
            ["[UNK]"] = 100,
            ["[CLS]"] = 101,
            ["[SEP]"] = 102,
            ["[MASK]"] = 103,
            ["the"] = 1000,
            ["un"] = 1001,
            ["##aff"] = 1002,
            ["##able"] = 1003,
            ["token"] = 1004,
            ["vector"] = 1005,
            ["ai"] = 1006
        };

        var vocab = new VocabTable(vocabDict);
        var options = new WordPieceOptions
        {
            DoLowerCase = true,
            StripAccents = true
        };

        var tokenizer = new WordPieceTokenizer(vocab, options);
        var result = tokenizer.Encode("unaffable token vector".AsSpan(), addSpecialTokens: true);

        // Expect: [CLS], un, ##aff, ##able, token, vector, [SEP]
        Assert.Equal(7, result.Length);
        Assert.Equal(101, result.Ids[0]); // [CLS]
        Assert.Equal(1001, result.Ids[1]); // un
        Assert.Equal(1002, result.Ids[2]); // ##aff
        Assert.Equal(1003, result.Ids[3]); // ##able
        Assert.Equal(1004, result.Ids[4]); // token
        Assert.Equal(1005, result.Ids[5]); // vector
        Assert.Equal(102, result.Ids[6]); // [SEP]

        string decoded = tokenizer.Decode(result.Span, skipSpecialTokens: true);
        Assert.Equal("unaffable token vector", decoded);
    }

    [Fact]
    public void WordPiece_UnkToken_WhenWordCannotBeSegmented()
    {
        var vocabDict = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["[PAD]"] = 0,
            ["[UNK]"] = 100,
            ["[CLS]"] = 101,
            ["[SEP]"] = 102,
            ["hello"] = 200
        };

        var vocab = new VocabTable(vocabDict);
        var tokenizer = new WordPieceTokenizer(vocab);

        var result = tokenizer.Encode("hello xyzunknown".AsSpan(), addSpecialTokens: false);
        Assert.Equal(2, result.Length);
        Assert.Equal(200, result.Ids[0]); // hello
        Assert.Equal(100, result.Ids[1]); // [UNK]
    }
}
