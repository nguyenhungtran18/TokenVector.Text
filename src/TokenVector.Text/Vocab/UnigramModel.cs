using System;
using System.Collections.Generic;

namespace TokenVector.Text.Vocab;

/// <summary>
/// Vocabulary and score model for Unigram Tokenizers (SentencePiece / T5 / Gemma).
/// </summary>
public sealed class UnigramModel
{
    public readonly struct Piece
    {
        public string Text { get; }
        public double Score { get; }
        public int Id { get; }

        public Piece(string text, double score, int id)
        {
            Text = text;
            Score = score;
            Id = id;
        }
    }

    private readonly List<Piece> _pieces = new();
    private readonly VocabTable _vocabTable;
    private readonly double[] _scores;
    private readonly string[] _idToText;
    private readonly int _maxPieceLength;

    public int Count => _pieces.Count;
    public int MaxPieceLength => _maxPieceLength;

    public UnigramModel(IEnumerable<(string Text, double Score)> pieces)
    {
        ArgumentNullException.ThrowIfNull(pieces);

        int id = 0;
        int maxLen = 0;
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);
        var list = new List<Piece>();

        foreach (var (text, score) in pieces)
        {
            if (!dict.ContainsKey(text))
            {
                var piece = new Piece(text, score, id);
                list.Add(piece);
                dict[text] = id;
                if (text.Length > maxLen) maxLen = text.Length;
                id++;
            }
        }

        _pieces = list;
        _scores = new double[_pieces.Count];
        _idToText = new string[_pieces.Count];

        for (int i = 0; i < _pieces.Count; i++)
        {
            _scores[i] = _pieces[i].Score;
            _idToText[i] = _pieces[i].Text;
        }

        _maxPieceLength = maxLen;
        _vocabTable = new VocabTable(dict);
    }

    public bool TryGetId(ReadOnlySpan<char> text, out int id) => _vocabTable.TryGetId(text, out id);

    public double GetScore(int id)
    {
        if ((uint)id < (uint)_scores.Length) return _scores[id];
        return -1e10;
    }

    public string? GetPiece(int id)
    {
        if ((uint)id < (uint)_idToText.Length) return _idToText[id];
        return null;
    }

    public VocabTable ToVocabTable() => _vocabTable;
}
