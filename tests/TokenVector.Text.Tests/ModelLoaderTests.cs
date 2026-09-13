using System;
using TokenVector.Text.Vocab.Loaders;
using Xunit;

namespace TokenVector.Text.Tests;

public class ModelLoaderTests
{
    [Fact]
    public void HuggingFaceJsonLoader_ParseBpeConfig_Successfully()
    {
        string json = """
        {
          "version": "1.0",
          "added_tokens": [
            { "id": 50256, "content": "<|endoftext|>", "single_word": false, "lstrip": false, "rstrip": false, "normalized": false, "special": true }
          ],
          "model": {
            "type": "BPE",
            "unk_token": null,
            "vocab": {
              "h": 0,
              "e": 1,
              "l": 2,
              "o": 3,
              "he": 4,
              "ll": 5,
              "hello": 6
            },
            "merges": [
              "h e",
              "l l",
              "he ll",
              "hell o"
            ]
          }
        }
        """;

        var config = HuggingFaceJsonLoader.LoadFromJson(json);

        Assert.Equal("BPE", config.ModelType);
        Assert.Equal(8, config.Vocab.Count); // 7 + 1 added token
        Assert.Equal(4, config.Merges.Count);
        Assert.Equal(50256, config.AddedTokens["<|endoftext|>"]);

        var tokenizer = Tokenizer.FromPretrained(json);
        Assert.NotNull(tokenizer);
    }
}
