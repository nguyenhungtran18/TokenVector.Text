using System;
using System.Text.RegularExpressions;

namespace TokenVector.Text.Tokenizers.Bpe;

/// <summary>
/// Pre-tokenizer that splits input text using regex patterns compatible with GPT-4, GPT-2, and LLaMA.
/// </summary>
public sealed partial class RegexPreTokenizer
{
    public const string Gpt4Pattern = @"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]|\s+(?!\S)|\s+";
    public const string Gpt2Pattern = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+";
    public const string SimplePattern = @"\w+|[^\w\s]|\s+";

    private readonly Regex _regex;

    [GeneratedRegex(Gpt4Pattern)]
    private static partial Regex Gpt4GeneratedRegex();

    [GeneratedRegex(Gpt2Pattern)]
    private static partial Regex Gpt2GeneratedRegex();

    [GeneratedRegex(SimplePattern)]
    private static partial Regex SimpleGeneratedRegex();

    public RegexPreTokenizer(string? pattern = null)
    {
        if (pattern == null || pattern == Gpt4Pattern)
        {
            _regex = Gpt4GeneratedRegex();
        }
        else if (pattern == Gpt2Pattern)
        {
            _regex = Gpt2GeneratedRegex();
        }
        else if (pattern == SimplePattern)
        {
            _regex = SimpleGeneratedRegex();
        }
        else
        {
            _regex = new Regex(pattern, RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Enumerates matches over input text string.
    /// </summary>
    public MatchCollection Matches(string text) => _regex.Matches(text);

    /// <summary>
    /// Enumerates matches zero-allocation over ReadOnlySpan in .NET 8.
    /// </summary>
    public Regex.ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> text) => _regex.EnumerateMatches(text);
}
