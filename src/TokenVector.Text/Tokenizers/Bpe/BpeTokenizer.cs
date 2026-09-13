using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TokenVector.Text.Models;
using TokenVector.Text.Unicode;
using TokenVector.Text.Vocab;

namespace TokenVector.Text.Tokenizers.Bpe;

/// <summary>
/// Ultra high-performance Byte-Pair Encoding (BPE) tokenizer with zero-allocation stackalloc doubly-linked-list.
/// </summary>
public sealed class BpeTokenizer : BaseTokenizer
{
    private readonly MergesTable _merges;
    private readonly BpeOptions _options;
    private readonly RegexPreTokenizer _preTokenizer;

    public MergesTable Merges => _merges;
    public BpeOptions Options => _options;

    public BpeTokenizer(
        VocabTable vocab,
        MergesTable merges,
        BpeOptions? options = null,
        SpecialTokenSet? specialTokens = null)
        : base(vocab, specialTokens, options?.PadToken, options?.UnkToken, options?.BosToken, options?.EosToken)
    {
        _merges = merges ?? throw new ArgumentNullException(nameof(merges));
        _options = options ?? BpeOptions.Gpt4;
        _preTokenizer = new RegexPreTokenizer(_options.Pattern);

        _merges.BindVocab(_vocab);
    }

    private ref struct ValueBuffer<T>
    {
        private T[]? _arrayFromPool;
        private Span<T> _buffer;
        private int _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueBuffer(Span<T> initialBuffer)
        {
            _arrayFromPool = null;
            _buffer = initialBuffer;
            _count = 0;
        }

        public readonly int Count => _count;

        public readonly ReadOnlySpan<T> AsSpan() => _buffer.Slice(0, _count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if ((uint)_count >= (uint)_buffer.Length)
            {
                Grow();
            }
            _buffer[_count++] = item;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow()
        {
            int newSize = Math.Max(_buffer.Length * 2, 64);
            var newArray = ArrayPool<T>.Shared.Rent(newSize);
            _buffer.Slice(0, _count).CopyTo(newArray);
            if (_arrayFromPool != null)
            {
                ArrayPool<T>.Shared.Return(_arrayFromPool);
            }
            _arrayFromPool = newArray;
            _buffer = newArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArrayAndDispose()
        {
            var result = _buffer.Slice(0, _count).ToArray();
            if (_arrayFromPool != null)
            {
                ArrayPool<T>.Shared.Return(_arrayFromPool);
                _arrayFromPool = null;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_arrayFromPool != null)
            {
                ArrayPool<T>.Shared.Return(_arrayFromPool);
                _arrayFromPool = null;
            }
        }
    }

    public override EncodingResult Encode(ReadOnlySpan<char> text, bool addSpecialTokens = true)
    {
        if (text.IsEmpty)
        {
            return EncodingResult.Empty;
        }

        Span<int> stackBuf = stackalloc int[128];
        var idBuffer = new ValueBuffer<int>(stackBuf);

        if (addSpecialTokens && _bosTokenId != -1)
        {
            idBuffer.Add(_bosTokenId);
        }

        int currentPos = 0;
        while (currentPos < text.Length)
        {
            var slice = text.Slice(currentPos);
            var (matchIdx, matchLen, specialId) = _specialTokens.FindFirst(slice);

            if (matchIdx == -1)
            {
                EncodeNormalText(slice, ref idBuffer);
                break;
            }
            else
            {
                if (matchIdx > 0)
                {
                    EncodeNormalText(slice.Slice(0, matchIdx), ref idBuffer);
                }

                idBuffer.Add(specialId);
                currentPos += matchIdx + matchLen;
            }
        }

        if (addSpecialTokens && _eosTokenId != -1)
        {
            idBuffer.Add(_eosTokenId);
        }

        return new EncodingResult(idBuffer.ToArrayAndDispose());
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void EncodeNormalText(ReadOnlySpan<char> text, scoped ref ValueBuffer<int> outputIds)
    {
        if (text.IsEmpty) return;

        int lastIndex = 0;
        var enumerator = _preTokenizer.EnumerateMatches(text);

        while (enumerator.MoveNext())
        {
            var match = enumerator.Current;
            if (match.Length == 0) continue;

            if (match.Index > lastIndex)
            {
                var gapSpan = text.Slice(lastIndex, match.Index - lastIndex);
                EncodePiece(gapSpan, ref outputIds);
            }

            var tokenSpan = text.Slice(match.Index, match.Length);
            EncodePiece(tokenSpan, ref outputIds);
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            var remainingSpan = text.Slice(lastIndex);
            EncodePiece(remainingSpan, ref outputIds);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void EncodePiece(ReadOnlySpan<char> piece, scoped ref ValueBuffer<int> outputIds)
    {
        if (_options.ByteLevelFallback)
        {
            int len = piece.Length;
            bool isAscii = true;
            for (int i = 0; i < len; i++)
            {
                if (piece[i] > 127)
                {
                    isAscii = false;
                    break;
                }
            }

            if (isAscii)
            {
                Span<char> mappedChars = len <= 256 ? stackalloc char[len] : new char[len];
                for (int i = 0; i < len; i++)
                {
                    mappedChars[i] = ByteLevelBpeHelper.ByteToChar((byte)piece[i]);
                }

                if (_vocab.TryGetId(mappedChars, out int mappedDirectId))
                {
                    outputIds.Add(mappedDirectId);
                    return;
                }

                BpeMergeMappedCharsFast(mappedChars, ref outputIds);
                return;
            }

            // Non-ASCII fallback with single-pass UTF-8 encoding
            int maxByteLen = piece.Length * 3;
            Span<byte> utf8Bytes = maxByteLen <= 256 ? stackalloc byte[maxByteLen] : new byte[maxByteLen];
            int writtenBytes = Encoding.UTF8.GetBytes(piece, utf8Bytes);
            var actualBytes = utf8Bytes.Slice(0, writtenBytes);

            Span<char> nonAsciiMappedChars = writtenBytes <= 256 ? stackalloc char[writtenBytes] : new char[writtenBytes];
            ByteLevelBpeHelper.BytesToChars(actualBytes, nonAsciiMappedChars);

            if (_vocab.TryGetId(nonAsciiMappedChars, out int nonAsciiDirectId))
            {
                outputIds.Add(nonAsciiDirectId);
                return;
            }

            BpeMergeMappedCharsFast(nonAsciiMappedChars, ref outputIds);
        }
        else
        {
            if (_vocab.TryGetId(piece, out int directId))
            {
                outputIds.Add(directId);
                return;
            }

            BpeMergeCharsFast(piece, ref outputIds);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void BpeMergeMappedCharsFast(scoped ReadOnlySpan<char> mappedChars, scoped ref ValueBuffer<int> outputIds)
    {
        int len = mappedChars.Length;
        if (len == 1)
        {
            if (_vocab.TryGetId(mappedChars, out int singleId))
            {
                outputIds.Add(singleId);
            }
            else if (_unkTokenId != -1)
            {
                outputIds.Add(_unkTokenId);
            }
            return;
        }

        // Use stackalloc linked list for zero-allocation merging
        Span<int> symbolIds = len <= 64 ? stackalloc int[len] : new int[len];
        Span<int> prevIdx = len <= 64 ? stackalloc int[len] : new int[len];
        Span<int> nextIdx = len <= 64 ? stackalloc int[len] : new int[len];

        for (int i = 0; i < len; i++)
        {
            symbolIds[i] = _vocab.GetIdOrDefault(mappedChars.Slice(i, 1), -1);
            prevIdx[i] = i - 1;
            nextIdx[i] = i < len - 1 ? i + 1 : -1;
        }

        // Iterative merge loop on doubly-linked-list
        while (true)
        {
            int minRank = int.MaxValue;
            int bestI = -1;
            int bestNext = -1;
            int bestMergedId = -1;

            int curr = 0;
            while (curr != -1)
            {
                int next = nextIdx[curr];
                if (next != -1)
                {
                    int idA = symbolIds[curr];
                    int idB = symbolIds[next];

                    if (idA != -1 && idB != -1 && _merges.TryGetMerge(idA, idB, out int rank, out int mergedId))
                    {
                        if (rank < minRank && mergedId != -1)
                        {
                            minRank = rank;
                            bestI = curr;
                            bestNext = next;
                            bestMergedId = mergedId;
                        }
                    }
                }
                curr = next;
            }

            if (bestI == -1) break; // No more merges possible

            // Merge best pair in-place
            symbolIds[bestI] = bestMergedId;
            int afterNext = nextIdx[bestNext];
            nextIdx[bestI] = afterNext;
            if (afterNext != -1)
            {
                prevIdx[afterNext] = bestI;
            }
        }

        // Emit final merged token IDs
        int emitCurr = 0;
        while (emitCurr != -1)
        {
            int id = symbolIds[emitCurr];
            if (id != -1)
            {
                outputIds.Add(id);
            }
            else if (_unkTokenId != -1)
            {
                outputIds.Add(_unkTokenId);
            }
            emitCurr = nextIdx[emitCurr];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void BpeMergeCharsFast(scoped ReadOnlySpan<char> chars, scoped ref ValueBuffer<int> outputIds)
    {
        int len = chars.Length;
        if (len == 1)
        {
            if (_vocab.TryGetId(chars, out int singleId))
            {
                outputIds.Add(singleId);
            }
            else if (_unkTokenId != -1)
            {
                outputIds.Add(_unkTokenId);
            }
            return;
        }

        Span<int> symbolIds = len <= 64 ? stackalloc int[len] : new int[len];
        Span<int> prevIdx = len <= 64 ? stackalloc int[len] : new int[len];
        Span<int> nextIdx = len <= 64 ? stackalloc int[len] : new int[len];

        for (int i = 0; i < len; i++)
        {
            symbolIds[i] = _vocab.GetIdOrDefault(chars.Slice(i, 1), -1);
            prevIdx[i] = i - 1;
            nextIdx[i] = i < len - 1 ? i + 1 : -1;
        }

        while (true)
        {
            int minRank = int.MaxValue;
            int bestI = -1;
            int bestNext = -1;
            int bestMergedId = -1;

            int curr = 0;
            while (curr != -1)
            {
                int next = nextIdx[curr];
                if (next != -1)
                {
                    int idA = symbolIds[curr];
                    int idB = symbolIds[next];

                    if (idA != -1 && idB != -1 && _merges.TryGetMerge(idA, idB, out int rank, out int mergedId))
                    {
                        if (rank < minRank && mergedId != -1)
                        {
                            minRank = rank;
                            bestI = curr;
                            bestNext = next;
                            bestMergedId = mergedId;
                        }
                    }
                }
                curr = next;
            }

            if (bestI == -1) break;

            symbolIds[bestI] = bestMergedId;
            int afterNext = nextIdx[bestNext];
            nextIdx[bestI] = afterNext;
            if (afterNext != -1)
            {
                prevIdx[afterNext] = bestI;
            }
        }

        int emitCurr = 0;
        while (emitCurr != -1)
        {
            int id = symbolIds[emitCurr];
            if (id != -1)
            {
                outputIds.Add(id);
            }
            else if (_unkTokenId != -1)
            {
                outputIds.Add(_unkTokenId);
            }
            emitCurr = nextIdx[emitCurr];
        }
    }

    public override string Decode(ReadOnlySpan<int> tokenIds, bool skipSpecialTokens = true)
    {
        if (tokenIds.IsEmpty) return string.Empty;

        if (_options.ByteLevelFallback)
        {
            var rawBytes = new List<byte>();
            Span<byte> charUtf8Buffer = stackalloc byte[8];

            for (int i = 0; i < tokenIds.Length; i++)
            {
                int id = tokenIds[i];
                if (skipSpecialTokens && _specialTokens.IsSpecialId(id))
                {
                    continue;
                }

                if (_specialTokens.IsSpecialId(id))
                {
                    foreach (var (specToken, specId) in _specialTokens.AsDictionary())
                    {
                        if (specId == id)
                        {
                            int bCount = Encoding.UTF8.GetByteCount(specToken);
                            var bSpan = bCount <= 128 ? stackalloc byte[bCount] : new byte[bCount];
                            Encoding.UTF8.GetBytes(specToken, bSpan);
                            for (int k = 0; k < bSpan.Length; k++) rawBytes.Add(bSpan[k]);
                            break;
                        }
                    }
                    continue;
                }

                string? tokenStr = _vocab.GetToken(id);
                if (tokenStr == null) continue;

                for (int c = 0; c < tokenStr.Length; c++)
                {
                    char ch = tokenStr[c];
                    if (ByteLevelBpeHelper.TryCharToByte(ch, out byte b))
                    {
                        rawBytes.Add(b);
                    }
                    else
                    {
                        int count = Encoding.UTF8.GetBytes(tokenStr.AsSpan(c, 1), charUtf8Buffer);
                        for (int k = 0; k < count; k++)
                        {
                            rawBytes.Add(charUtf8Buffer[k]);
                        }
                    }
                }
            }

            return Encoding.UTF8.GetString(rawBytes.ToArray());
        }
        else
        {
            var sb = new StringBuilder();
            for (int i = 0; i < tokenIds.Length; i++)
            {
                int id = tokenIds[i];
                if (skipSpecialTokens && _specialTokens.IsSpecialId(id))
                {
                    continue;
                }

                string? tokenStr = _vocab.GetToken(id);
                if (tokenStr != null)
                {
                    sb.Append(tokenStr);
                }
            }
            return sb.ToString();
        }
    }
}
