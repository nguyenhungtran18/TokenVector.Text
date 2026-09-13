using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using TokenVector.Text.Models;
using TokenVector.Text.Tokenizers.Bpe;
using TokenVector.Text.Unicode;
using TokenVector.Text.Vocab;

namespace TokenVector.Text.Training;

/// <summary>
/// High-performance Byte-Pair Encoding (BPE) vocabulary trainer.
/// Learns optimal subwords from raw text corpora and exports industry-standard tokenizer.json format.
/// </summary>
public sealed class BpeTrainer
{
    private readonly int _targetVocabSize;
    private readonly int _minFrequency;
    private readonly List<string> _specialTokens;
    private readonly RegexPreTokenizer _preTokenizer;

    public int TargetVocabSize => _targetVocabSize;
    public int MinFrequency => _minFrequency;

    public BpeTrainer(
        int targetVocabSize = 32_000,
        int minFrequency = 2,
        IEnumerable<string>? specialTokens = null,
        string? pattern = null)
    {
        _targetVocabSize = targetVocabSize;
        _minFrequency = minFrequency;
        _specialTokens = specialTokens?.ToList() ?? new List<string> { "<|endoftext|>", "<|im_start|>", "<|im_end|>" };
        _preTokenizer = new RegexPreTokenizer(pattern ?? RegexPreTokenizer.Gpt4Pattern);
    }

    /// <summary>
    /// Trains a BPE tokenizer from an enumerable of text documents.
    /// </summary>
    public ITokenizer Train(IEnumerable<string> corpus)
    {
        ArgumentNullException.ThrowIfNull(corpus);

        // 1. Pre-tokenize and count word frequencies
        var wordFreqs = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var text in corpus)
        {
            if (string.IsNullOrWhiteSpace(text)) continue;

            var matches = _preTokenizer.Matches(text);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Length == 0) continue;
                string piece = match.Value;

                // Map to byte-level BPE representation
                byte[] utf8 = Encoding.UTF8.GetBytes(piece);
                char[] mapped = new char[utf8.Length];
                ByteLevelBpeHelper.BytesToChars(utf8, mapped);
                string mappedStr = new string(mapped);

                wordFreqs[mappedStr] = wordFreqs.TryGetValue(mappedStr, out int count) ? count + 1 : 1;
            }
        }

        // 2. Initialize base vocabulary with 256 byte characters
        var vocabDict = new Dictionary<string, int>(StringComparer.Ordinal);
        int nextId = 0;

        for (int b = 0; b < 256; b++)
        {
            vocabDict[ByteLevelBpeHelper.ByteToString((byte)b)] = nextId++;
        }

        // Add special tokens
        var specialTokenSet = new SpecialTokenSet();
        foreach (var spec in _specialTokens)
        {
            if (!vocabDict.ContainsKey(spec))
            {
                int sId = 100000 + nextId++;
                vocabDict[spec] = sId;
                specialTokenSet.Add(spec, sId);
            }
        }

        // 3. Initialize word representations as lists of single-character symbols
        var wordSplits = new Dictionary<string, List<string>>(wordFreqs.Count, StringComparer.Ordinal);
        foreach (var (word, _) in wordFreqs)
        {
            var symbols = new List<string>(word.Length);
            for (int i = 0; i < word.Length; i++)
            {
                symbols.Add(word[i].ToString());
            }
            wordSplits[word] = symbols;
        }

        var mergesList = new List<(string First, string Second)>();

        // 4. Iterative merge training loop
        while (vocabDict.Count < _targetVocabSize)
        {
            // Count pair frequencies across all words
            var pairFreqs = new Dictionary<ulong, int>();
            var pairLookup = new Dictionary<ulong, (string First, string Second)>();

            foreach (var (word, count) in wordFreqs)
            {
                var symbols = wordSplits[word];
                if (symbols.Count < 2) continue;

                for (int i = 0; i < symbols.Count - 1; i++)
                {
                    string first = symbols[i];
                    string second = symbols[i + 1];

                    if (vocabDict.TryGetValue(first, out int id1) && vocabDict.TryGetValue(second, out int id2))
                    {
                        ulong key = MergesTable.MakePairKey(id1, id2);
                        pairFreqs[key] = pairFreqs.TryGetValue(key, out int freq) ? freq + count : count;
                        if (!pairLookup.ContainsKey(key))
                        {
                            pairLookup[key] = (first, second);
                        }
                    }
                }
            }

            if (pairFreqs.Count == 0) break;

            // Find pair with highest frequency
            ulong bestPairKey = 0;
            int maxFreq = -1;
            foreach (var (key, freq) in pairFreqs)
            {
                if (freq > maxFreq)
                {
                    maxFreq = freq;
                    bestPairKey = key;
                }
            }

            if (maxFreq < _minFrequency) break;

            var (bestFirst, bestSecond) = pairLookup[bestPairKey];
            string newMergedSymbol = bestFirst + bestSecond;

            mergesList.Add((bestFirst, bestSecond));
            if (!vocabDict.ContainsKey(newMergedSymbol))
            {
                vocabDict[newMergedSymbol] = nextId++;
            }

            // Apply merge to all word splits
            foreach (var (word, _) in wordFreqs)
            {
                var symbols = wordSplits[word];
                if (symbols.Count < 2) continue;

                int i = 0;
                while (i < symbols.Count - 1)
                {
                    if (symbols[i] == bestFirst && symbols[i + 1] == bestSecond)
                    {
                        symbols[i] = newMergedSymbol;
                        symbols.RemoveAt(i + 1);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        var vocabTable = new VocabTable(vocabDict);
        var mergesTable = new MergesTable(mergesList, vocabTable);
        var options = BpeOptions.Gpt4;

        return new BpeTokenizer(vocabTable, mergesTable, options, specialTokenSet);
    }

    /// <summary>
    /// Exports a trained tokenizer configuration to Hugging Face tokenizer.json format using Utf8JsonWriter.
    /// </summary>
    public static void ExportToJson(ITokenizer tokenizer, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(tokenizer);
        ArgumentNullException.ThrowIfNull(outputPath);

        if (tokenizer is not BpeTokenizer bpe)
        {
            throw new InvalidOperationException("Only BpeTokenizer can be exported to standard BPE tokenizer.json.");
        }

        using var fs = File.Create(outputPath);
        using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("version", "1.0");

        // added_tokens
        writer.WriteStartArray("added_tokens");
        foreach (var (k, v) in bpe.SpecialTokens.AsDictionary())
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", v);
            writer.WriteString("content", k);
            writer.WriteBoolean("single_word", false);
            writer.WriteBoolean("lstrip", false);
            writer.WriteBoolean("rstrip", false);
            writer.WriteBoolean("normalized", false);
            writer.WriteBoolean("special", true);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        // model
        writer.WriteStartObject("model");
        writer.WriteString("type", "BPE");
        if (bpe.Options.UnkToken != null)
        {
            writer.WriteString("unk_token", bpe.Options.UnkToken);
        }
        else
        {
            writer.WriteNull("unk_token");
        }

        // vocab
        writer.WriteStartObject("vocab");
        foreach (var (k, v) in bpe.Vocab.AsDictionary())
        {
            writer.WriteNumber(k, v);
        }
        writer.WriteEndObject();

        // merges
        writer.WriteStartArray("merges");
        writer.WriteEndArray();

        writer.WriteEndObject(); // model
        writer.WriteEndObject(); // root
    }
}
