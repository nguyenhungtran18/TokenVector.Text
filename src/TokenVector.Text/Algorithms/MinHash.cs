using System;
using System.Collections.Generic;

namespace TokenVector.Text.Algorithms;

/// <summary>
/// MinHash locality-sensitive hashing for estimating Jaccard similarity and document deduplication.
/// </summary>
public sealed class MinHash
{
    private readonly int _numPermutations;
    private readonly uint[] _aCoeffs;
    private readonly uint[] _bCoeffs;
    private const uint Prime = 2147483647; // 2^31 - 1 (Mersenne prime)

    public int NumPermutations => _numPermutations;

    public MinHash(int numPermutations = 128, int seed = 42)
    {
        _numPermutations = numPermutations;
        _aCoeffs = new uint[numPermutations];
        _bCoeffs = new uint[numPermutations];

        var rng = new Random(seed);
        for (int i = 0; i < numPermutations; i++)
        {
            _aCoeffs[i] = (uint)(rng.Next(1, int.MaxValue));
            _bCoeffs[i] = (uint)(rng.Next(0, int.MaxValue));
        }
    }

    /// <summary>
    /// Computes MinHash signature array for a token sequence.
    /// </summary>
    public uint[] ComputeSignature(ReadOnlySpan<int> tokenIds)
    {
        uint[] signature = new uint[_numPermutations];
        Array.Fill(signature, uint.MaxValue);

        for (int t = 0; t < tokenIds.Length; t++)
        {
            uint elem = (uint)tokenIds[t];
            for (int p = 0; p < _numPermutations; p++)
            {
                uint hash = (uint)((((ulong)_aCoeffs[p] * elem) + _bCoeffs[p]) % Prime);
                if (hash < signature[p])
                {
                    signature[p] = hash;
                }
            }
        }

        return signature;
    }

    /// <summary>
    /// Computes estimated Jaccard similarity between two MinHash signatures.
    /// </summary>
    public static double JaccardSimilarity(ReadOnlySpan<uint> sigA, ReadOnlySpan<uint> sigB)
    {
        if (sigA.Length != sigB.Length || sigA.IsEmpty) return 0.0;

        int matches = 0;
        for (int i = 0; i < sigA.Length; i++)
        {
            if (sigA[i] == sigB[i])
            {
                matches++;
            }
        }

        return (double)matches / sigA.Length;
    }
}
