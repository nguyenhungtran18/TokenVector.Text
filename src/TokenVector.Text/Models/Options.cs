using System;

namespace TokenVector.Text.Models;

/// <summary>
/// Strategy for sequence padding in batch operations.
/// </summary>
public enum PaddingStrategy
{
    DoNotPad = 0,
    Longest = 1,
    MaxFixed = 2
}

/// <summary>
/// Direction to insert padding tokens.
/// </summary>
public enum PaddingDirection
{
    Right = 0,
    Left = 1
}

/// <summary>
/// Strategy for sequence truncation.
/// </summary>
public enum TruncationStrategy
{
    DoNotTruncate = 0,
    TruncateToMax = 1
}

/// <summary>
/// Direction to truncate tokens.
/// </summary>
public enum TruncationDirection
{
    Right = 0,
    Left = 1
}

/// <summary>
/// Configuration options for BPE tokenizers.
/// </summary>
public sealed class BpeOptions
{
    public string? UnkToken { get; set; } = null;
    public string? BosToken { get; set; } = null;
    public string? EosToken { get; set; } = null;
    public string? PadToken { get; set; } = null;
    public string? Pattern { get; set; } = null;
    public bool ByteLevelFallback { get; set; } = true;
    public bool AddPrefixSpace { get; set; } = false;
    public bool TrimOffsets { get; set; } = false;

    public static BpeOptions Gpt4 => new()
    {
        EosToken = "<|endoftext|>",
        PadToken = "<|endoftext|>",
        ByteLevelFallback = true,
        Pattern = @"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]|\s+(?!\S)|\s+"
    };

    public static BpeOptions Gpt2 => new()
    {
        UnkToken = "<|endoftext|>",
        BosToken = "<|endoftext|>",
        EosToken = "<|endoftext|>",
        PadToken = "<|endoftext|>",
        ByteLevelFallback = true,
        AddPrefixSpace = false,
        Pattern = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+"
    };

    public static BpeOptions Llama3 => new()
    {
        BosToken = "<|begin_of_text|>",
        EosToken = "<|end_of_text|>",
        PadToken = "<|end_of_text|>",
        ByteLevelFallback = true,
        Pattern = @"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]|\s+(?!\S)|\s+"
    };
}

/// <summary>
/// Configuration options for WordPiece tokenizers (e.g., BERT).
/// </summary>
public sealed class WordPieceOptions
{
    public string UnkToken { get; set; } = "[UNK]";
    public string ClsToken { get; set; } = "[CLS]";
    public string SepToken { get; set; } = "[SEP]";
    public string PadToken { get; set; } = "[PAD]";
    public string MaskToken { get; set; } = "[MASK]";
    public string ContinuingSubwordPrefix { get; set; } = "##";
    public int MaxInputCharsPerWord { get; set; } = 100;
    public bool DoLowerCase { get; set; } = true;
    public bool StripAccents { get; set; } = false;

    public static WordPieceOptions BertDefault => new();
}

/// <summary>
/// Configuration options for Unigram tokenizers (e.g., SentencePiece, Gemma, T5).
/// </summary>
public sealed class UnigramOptions
{
    public string UnkToken { get; set; } = "<unk>";
    public string? BosToken { get; set; } = "<s>";
    public string? EosToken { get; set; } = "</s>";
    public string? PadToken { get; set; } = "<pad>";
    public bool TreatWhitespaceAsSpace { get; set; } = false;
    public string ReplacementChar { get; set; } = " ";
    public bool AddDummyPrefix { get; set; } = true;

    public static UnigramOptions SentencePieceDefault => new();
}
