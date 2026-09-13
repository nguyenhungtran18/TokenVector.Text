using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TokenVector.Text.Vocab.Loaders;

/// <summary>
/// Loader for SentencePiece vocabulary and score files (.vocab / .tsv / custom models).
/// </summary>
public static class SentencePieceModelLoader
{
    public static UnigramModel LoadFromVocabFile(string vocabFilePath)
    {
        if (!File.Exists(vocabFilePath))
        {
            throw new FileNotFoundException($"SentencePiece vocab file not found: {vocabFilePath}", vocabFilePath);
        }

        var pieces = new List<(string Text, double Score)>();
        foreach (var line in File.ReadLines(vocabFilePath))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            int tabIdx = trimmed.IndexOf('\t');
            if (tabIdx > 0)
            {
                string text = trimmed.Substring(0, tabIdx);
                string scoreStr = trimmed.Substring(tabIdx + 1);
                if (double.TryParse(scoreStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double score))
                {
                    pieces.Add((text, score));
                }
                else
                {
                    pieces.Add((text, 0.0));
                }
            }
            else
            {
                pieces.Add((trimmed, 0.0));
            }
        }

        return new UnigramModel(pieces);
    }
}
