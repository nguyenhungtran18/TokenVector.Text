using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TokenVector.Text.Vocab.Loaders;

/// <summary>
/// Loader for legacy GPT-2 / RoBERTa vocab.json and merges.txt files.
/// </summary>
public static class LegacyBpeLoader
{
    public static (VocabTable Vocab, MergesTable Merges) Load(string vocabFilePath, string mergesFilePath)
    {
        if (!File.Exists(vocabFilePath))
        {
            throw new FileNotFoundException($"Vocab file not found: {vocabFilePath}", vocabFilePath);
        }
        if (!File.Exists(mergesFilePath))
        {
            throw new FileNotFoundException($"Merges file not found: {mergesFilePath}", mergesFilePath);
        }

        // 1. Parse vocab.json
        string vocabJson = File.ReadAllText(vocabFilePath);
        using var doc = JsonDocument.Parse(vocabJson);
        var vocabDict = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            vocabDict[prop.Name] = prop.Value.GetInt32();
        }
        var vocabTable = new VocabTable(vocabDict);

        // 2. Parse merges.txt
        var mergesList = new List<(string First, string Second)>();
        foreach (var line in File.ReadLines(mergesFilePath))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

            int spaceIdx = trimmed.IndexOf(' ');
            if (spaceIdx > 0)
            {
                string first = trimmed.Substring(0, spaceIdx);
                string second = trimmed.Substring(spaceIdx + 1);
                mergesList.Add((first, second));
            }
        }
        var mergesTable = new MergesTable(mergesList, vocabTable);

        return (vocabTable, mergesTable);
    }
}
