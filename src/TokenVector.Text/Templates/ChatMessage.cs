using System;

namespace TokenVector.Text.Templates;

/// <summary>
/// Represents a structured chat message with role and text content.
/// </summary>
public readonly struct ChatMessage : IEquatable<ChatMessage>
{
    public string Role { get; }
    public string Content { get; }
    public string? Name { get; }

    public ChatMessage(string role, string content, string? name = null)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
        Content = content ?? string.Empty;
        Name = name;
    }

    public static ChatMessage System(string content) => new("system", content);
    public static ChatMessage User(string content) => new("user", content);
    public static ChatMessage Assistant(string content) => new("assistant", content);

    public bool Equals(ChatMessage other) =>
        Role == other.Role &&
        Content == other.Content &&
        Name == other.Name;

    public override bool Equals(object? obj) => obj is ChatMessage other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Role, Content, Name);

    public static bool operator ==(ChatMessage left, ChatMessage right) => left.Equals(right);
    public static bool operator !=(ChatMessage left, ChatMessage right) => !left.Equals(right);
}
