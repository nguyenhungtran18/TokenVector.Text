using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TokenVector.Text.Models;
using TokenVector.Text.Unicode;

namespace TokenVector.Text.Vocab.Loaders;

/// <summary>
/// High-speed loader for OpenAI .tiktoken files (base64-encoded token + integer rank format).
/// </summary>
public static class TiktokenBpeLoader
{
    /// <summary>
    /// Loads a BPE tokenizer from an OpenAI .tiktoken file or stream.
    /// </summary>
    public static (VocabTable Vocab, MergesTable Merges) Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Tiktoken file not found: {filePath}", filePath);
        }

        using var fs = File.OpenRead(filePath);
        return Load(fs);
    }

    /// <summary>
    /// Loads a BPE tokenizer from an OpenAI .tiktoken stream.
    /// </summary>
    public static (VocabTable Vocab, MergesTable Merges) Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var vocabDict = new Dictionary<string, int>(105_000, StringComparer.Ordinal);
        var idPairMerges = new Dictionary<ulong, MergesTable.MergeInfo>(105_000);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;

        Span<char> mappedBuffer = stackalloc char[256];

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            int spaceIdx = line.IndexOf(' ');
            if (spaceIdx <= 0) continue;

            string base64Token = line.Substring(0, spaceIdx);
            string rankStr = line.Substring(spaceIdx + 1);

            if (!int.TryParse(rankStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rank))
            {
                continue;
            }

            try
            {
                byte[] rawBytes = Convert.FromBase64String(base64Token);
                var charSpan = rawBytes.Length <= 256 ? mappedBuffer.Slice(0, rawBytes.Length) : new char[rawBytes.Length];
                ByteLevelBpeHelper.BytesToChars(rawBytes, charSpan);

                string mappedToken = charSpan.ToString();
                vocabDict[mappedToken] = rank;
            }
            catch (FormatException)
            {
                // Ignore invalid base64 line
            }
        }

        var vocabTable = new VocabTable(vocabDict);
        var mergesTable = new MergesTable(idPairMerges);

        return (vocabTable, mergesTable);
    }
}
