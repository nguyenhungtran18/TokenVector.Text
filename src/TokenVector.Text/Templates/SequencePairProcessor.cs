using System;
using TokenVector.Text.Models;

namespace TokenVector.Text.Templates;

/// <summary>
/// Sequence pair post-processor for sentence pair classification and cross-encoder tasks (e.g. NLI, Q&amp;A, Re-ranking).
/// </summary>
public static class SequencePairProcessor
{
    /// <summary>
    /// Combines two sequences into a single sequence pair with segment type IDs (0 for seqA, 1 for seqB).
    /// </summary>
    public static EncodingResult EncodePair(
        ITokenizer tokenizer,
        ReadOnlySpan<char> textA,
        ReadOnlySpan<char> textB,
        int clsTokenId = 101,
        int sepTokenId = 102)
    {
        var encA = tokenizer.Encode(textA, addSpecialTokens: false);
        var encB = tokenizer.Encode(textB, addSpecialTokens: false);

        int totalLen = 1 + encA.Length + 1 + encB.Length + 1; // [CLS] A [SEP] B [SEP]
        int[] ids = new int[totalLen];
        int[] typeIds = new int[totalLen];
        int[] mask = new int[totalLen];

        int idx = 0;

        // [CLS]
        ids[idx] = clsTokenId;
        typeIds[idx] = 0;
        mask[idx] = 1;
        idx++;

        // Sequence A
        for (int i = 0; i < encA.Length; i++)
        {
            ids[idx] = encA.Ids[i];
            typeIds[idx] = 0;
            mask[idx] = 1;
            idx++;
        }

        // [SEP]
        ids[idx] = sepTokenId;
        typeIds[idx] = 0;
        mask[idx] = 1;
        idx++;

        // Sequence B
        for (int i = 0; i < encB.Length; i++)
        {
            ids[idx] = encB.Ids[i];
            typeIds[idx] = 1;
            mask[idx] = 1;
            idx++;
        }

        // Final [SEP]
        ids[idx] = sepTokenId;
        typeIds[idx] = 1;
        mask[idx] = 1;

        return new EncodingResult(ids, mask, typeIds);
    }
}
