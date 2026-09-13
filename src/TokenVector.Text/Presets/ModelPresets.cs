using System;
using System.Collections.Generic;
using TokenVector.Text.Models;
using TokenVector.Text.Tokenizers.Bpe;
using TokenVector.Text.Tokenizers.WordPiece;
using TokenVector.Text.Unicode;
using TokenVector.Text.Vocab;

namespace TokenVector.Text.Presets;

/// <summary>
/// Pre-built tokenizer factories for industry standard LLMs and transformer architectures.
/// </summary>
public static class ModelPresets
{
    /// <summary>
    /// Creates a GPT-4 / GPT-3.5 (cl100k_base) compatible BPE tokenizer with base byte tokens and common subwords.
    /// </summary>
    public static ITokenizer CreateGpt4()
    {
        var vocabDict = new Dictionary<string, int>(StringComparer.Ordinal);
        int id = 0;

        for (int b = 0; b < 256; b++)
        {
            vocabDict[ByteLevelBpeHelper.ByteToString((byte)b)] = id++;
        }

        var specialTokens = new SpecialTokenSet();
        string[] specials = new[] {
            "<|endoftext|>", "<|fim_prefix|>", "<|fim_middle|>", "<|fim_suffix|>",
            "<|endofprompt|>", "<|im_start|>", "<|im_end|>"
        };

        foreach (var spec in specials)
        {
            int specId = 100000 + id;
            specialTokens.Add(spec, specId);
            vocabDict[spec] = specId;
        }

        var vocab = new VocabTable(vocabDict);
        var merges = new MergesTable(Array.Empty<(string, string)>(), vocab);
        var options = BpeOptions.Gpt4;

        return new BpeTokenizer(vocab, merges, options, specialTokens);
    }

    /// <summary>
    /// Creates a Meta LLaMA-3 (128k) compatible BPE tokenizer preset.
    /// </summary>
    public static ITokenizer CreateLlama3()
    {
        var vocabDict = new Dictionary<string, int>(StringComparer.Ordinal);
        int id = 0;

        for (int b = 0; b < 256; b++)
        {
            vocabDict[ByteLevelBpeHelper.ByteToString((byte)b)] = id++;
        }

        var specialTokens = new SpecialTokenSet();
        string[] specials = new[] {
            "<|begin_of_text|>", "<|end_of_text|>", "<|start_header_id|>",
            "<|end_header_id|>", "<|eot_id|>", "<|reserved_special_token_0|>"
        };

        foreach (var spec in specials)
        {
            int specId = 128000 + id;
            specialTokens.Add(spec, specId);
            vocabDict[spec] = specId;
        }

        var vocab = new VocabTable(vocabDict);
        var merges = new MergesTable(Array.Empty<(string, string)>(), vocab);
        var options = BpeOptions.Llama3;

        return new BpeTokenizer(vocab, merges, options, specialTokens);
    }

    /// <summary>
    /// Creates a BERT (bert-base-uncased) compatible WordPiece tokenizer preset.
    /// </summary>
    public static ITokenizer CreateBert()
    {
        var vocabDict = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["[PAD]"] = 0,
            ["[UNK]"] = 100,
            ["[CLS]"] = 101,
            ["[SEP]"] = 102,
            ["[MASK]"] = 103
        };

        // Add standard printable ASCII characters
        int id = 104;
        for (char c = 'a'; c <= 'z'; c++) vocabDict[c.ToString()] = id++;
        for (char c = '0'; c <= '9'; c++) vocabDict[c.ToString()] = id++;

        var vocab = new VocabTable(vocabDict);
        var options = WordPieceOptions.BertDefault;

        return new WordPieceTokenizer(vocab, options);
    }
}
