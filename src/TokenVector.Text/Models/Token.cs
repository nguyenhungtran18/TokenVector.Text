using System;

namespace TokenVector.Text.Models;

/// <summary>
/// Represents an individual encoded token with text slice and offset tracking.
/// </summary>
public readonly struct Token : IEquatable<Token>
{
    public int Id { get; }
    public string Value { get; }
    public int StartIndex { get; }
    public int EndIndex { get; }
    public bool IsSpecial { get; }

    public Token(int id, string value, int startIndex = -1, int endIndex = -1, bool isSpecial = false)
    {
        Id = id;
        Value = value ?? string.Empty;
        StartIndex = startIndex;
        EndIndex = endIndex;
        IsSpecial = isSpecial;
    }

    public override string ToString() => $"Token(Id={Id}, Value='{Value}', [{StartIndex}..{EndIndex}])";

    public bool Equals(Token other) =>
        Id == other.Id &&
        Value == other.Value &&
        StartIndex == other.StartIndex &&
        EndIndex == other.EndIndex &&
        IsSpecial == other.IsSpecial;

    public override bool Equals(object? obj) => obj is Token other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, Value, StartIndex, EndIndex, IsSpecial);

    public static bool operator ==(Token left, Token right) => left.Equals(right);
    public static bool operator !=(Token left, Token right) => !left.Equals(right);
}
