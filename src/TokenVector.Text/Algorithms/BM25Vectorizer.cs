using System;
using System.Collections.Generic;
using System.Linq;

namespace TokenVector.Text.Algorithms;

/// <summary>
/// High-performance BM25 sparse vectorizer and relevance ranker for Hybrid Search &amp; RAG.
/// </summary>
public sealed class BM25Vectorizer
{
    private readonly double _k1;
    private readonly double _b;
    private readonly List<Dictionary<int, int>> _docTermFreqs = new();
    private readonly List<int> _docLengths = new();
    private readonly Dictionary<int, int> _docFrequencies = new();
    private double _avgDocLength;
    private int _corpusSize;

    public int CorpusSize => _corpusSize;
    public double AvgDocLength => _avgDocLength;

    public BM25Vectorizer(double k1 = 1.5, double b = 0.75)
    {
        _k1 = k1;
        _b = b;
    }

    /// <summary>
    /// Fits and indexes a corpus of tokenized documents.
    /// </summary>
    public void Fit(IEnumerable<ReadOnlyMemory<int>> tokenizedCorpus)
    {
        ArgumentNullException.ThrowIfNull(tokenizedCorpus);

        _docTermFreqs.Clear();
        _docLengths.Clear();
        _docFrequencies.Clear();

        long totalLength = 0;

        foreach (var doc in tokenizedCorpus)
        {
            var span = doc.Span;
            int len = span.Length;
            _docLengths.Add(len);
            totalLength += len;

            var tf = new Dictionary<int, int>();
            var seenTerms = new HashSet<int>();

            for (int i = 0; i < len; i++)
            {
                int term = span[i];
                tf[term] = tf.TryGetValue(term, out int count) ? count + 1 : 1;
                seenTerms.Add(term);
            }

            _docTermFreqs.Add(tf);

            foreach (var term in seenTerms)
            {
                _docFrequencies[term] = _docFrequencies.TryGetValue(term, out int df) ? df + 1 : 1;
            }
        }

        _corpusSize = _docLengths.Count;
        _avgDocLength = _corpusSize > 0 ? (double)totalLength / _corpusSize : 0.0;
    }

    /// <summary>
    /// Computes BM25 relevance scores for all documents given a query token array.
    /// </summary>
    public float[] Score(ReadOnlySpan<int> queryTokens)
    {
        if (_corpusSize == 0) return Array.Empty<float>();

        float[] scores = new float[_corpusSize];

        for (int q = 0; q < queryTokens.Length; q++)
        {
            int term = queryTokens[q];
            if (!_docFrequencies.TryGetValue(term, out int df)) continue;

            // IDF formula: ln(1 + (N - df + 0.5) / (df + 0.5))
            double idf = Math.Log(1.0 + (_corpusSize - df + 0.5) / (df + 0.5));

            for (int d = 0; d < _corpusSize; d++)
            {
                if (_docTermFreqs[d].TryGetValue(term, out int tf))
                {
                    double docLen = _docLengths[d];
                    double numerator = tf * (_k1 + 1.0);
                    double denominator = tf + _k1 * (1.0 - _b + _b * (docLen / _avgDocLength));
                    scores[d] += (float)(idf * (numerator / denominator));
                }
            }
        }

        return scores;
    }

    /// <summary>
    /// Finds top-K most relevant document indices for a query.
    /// </summary>
    public (int DocIndex, float Score)[] QueryTopK(ReadOnlySpan<int> queryTokens, int k = 10)
    {
        float[] scores = Score(queryTokens);
        k = Math.Min(k, scores.Length);

        return scores
            .Select((s, idx) => (DocIndex: idx, Score: s))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(k)
            .ToArray();
    }
}
