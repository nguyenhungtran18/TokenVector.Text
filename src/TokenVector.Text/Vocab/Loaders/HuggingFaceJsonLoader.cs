using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TokenVector.Text.Models;

namespace TokenVector.Text.Vocab.Loaders;

/// <summary>
/// AOT-compliant JSON deserializer and loader for Hugging Face tokenizer.json configurations.
/// </summary>
public static class HuggingFaceJsonLoader
{
    public sealed class LoadedTokenizerConfig
    {
        public string ModelType { get; set; } = "BPE";
        public Dictionary<string, int> Vocab { get; set; } = new(StringComparer.Ordinal);
        public List<(string First, string Second)> Merges { get; set; } = new();
        public List<(string Text, double Score)> UnigramPieces { get; set; } = new();
        public Dictionary<string, int> AddedTokens { get; set; } = new(StringComparer.Ordinal);
        public string? UnkToken { get; set; }
        public string? BosToken { get; set; }
        public string? EosToken { get; set; }
        public string? PadToken { get; set; }
        public string? Pattern { get; set; }
        public bool ByteLevel { get; set; } = true;
    }

    /// <summary>
    /// Loads tokenizer configuration directly from a tokenizer.json file.
    /// </summary>
    public static LoadedTokenizerConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Hugging Face tokenizer file not found: {filePath}", filePath);
        }

        string json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

    /// <summary>
    /// Loads tokenizer configuration from JSON string.
    /// </summary>
    public static LoadedTokenizerConfig LoadFromJson(string json)
    {
        var config = new LoadedTokenizerConfig();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // 1. Added Tokens
        if (root.TryGetProperty("added_tokens", out var addedTokensElement) && addedTokensElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in addedTokensElement.EnumerateArray())
            {
                if (item.TryGetProperty("content", out var contentProp) && item.TryGetProperty("id", out var idProp))
                {
                    string content = contentProp.GetString() ?? string.Empty;
                    int id = idProp.GetInt32();
                    config.AddedTokens[content] = id;
                    config.Vocab[content] = id;
                }
            }
        }

        // 2. Model section
        if (root.TryGetProperty("model", out var modelElement))
        {
            if (modelElement.TryGetProperty("type", out var typeProp))
            {
                config.ModelType = typeProp.GetString() ?? "BPE";
            }

            if (modelElement.TryGetProperty("unk_token", out var unkProp) && unkProp.ValueKind == JsonValueKind.String)
            {
                config.UnkToken = unkProp.GetString();
            }

            // Vocab
            if (modelElement.TryGetProperty("vocab", out var vocabElement))
            {
                if (vocabElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in vocabElement.EnumerateObject())
                    {
                        config.Vocab[prop.Name] = prop.Value.GetInt32();
                    }
                }
                else if (vocabElement.ValueKind == JsonValueKind.Array)
                {
                    // Array of [piece, score] for Unigram
                    int id = 0;
                    foreach (var item in vocabElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Array)
                        {
                            var enumerator = item.EnumerateArray();
                            if (enumerator.MoveNext())
                            {
                                string piece = enumerator.Current.GetString() ?? string.Empty;
                                double score = 0.0;
                                if (enumerator.MoveNext())
                                {
                                    score = enumerator.Current.GetDouble();
                                }
                                config.UnigramPieces.Add((piece, score));
                                config.Vocab[piece] = id++;
                            }
                        }
                    }
                }
            }

            // Merges
            if (modelElement.TryGetProperty("merges", out var mergesElement) && mergesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var mergeItem in mergesElement.EnumerateArray())
                {
                    if (mergeItem.ValueKind == JsonValueKind.String)
                    {
                        string line = mergeItem.GetString() ?? string.Empty;
                        int spaceIdx = line.IndexOf(' ');
                        if (spaceIdx > 0)
                        {
                            string first = line.Substring(0, spaceIdx);
                            string second = line.Substring(spaceIdx + 1);
                            config.Merges.Add((first, second));
                        }
                    }
                    else if (mergeItem.ValueKind == JsonValueKind.Array)
                    {
                        var arr = mergeItem.EnumerateArray();
                        if (arr.MoveNext())
                        {
                            string first = arr.Current.GetString() ?? string.Empty;
                            if (arr.MoveNext())
                            {
                                string second = arr.Current.GetString() ?? string.Empty;
                                config.Merges.Add((first, second));
                            }
                        }
                    }
                }
            }
        }

        return config;
    }
}
