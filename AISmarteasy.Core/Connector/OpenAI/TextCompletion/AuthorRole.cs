using System.ComponentModel;
using SemanticKernel.Function;

namespace SemanticKernel.Connector.OpenAI.TextCompletion;
public readonly struct AuthorRole : IEquatable<AuthorRole>
{
    public static readonly AuthorRole System = new("system");

    public static readonly AuthorRole Assistant = new("assistant");

    public static readonly AuthorRole User = new("user");

    public static readonly AuthorRole Tool = new("tool");

    public string Label { get; }

    public AuthorRole(string label)
    {
        Verify.NotNull(label, nameof(label));
        Label = label!;
    }

    public static bool operator ==(AuthorRole left, AuthorRole right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(AuthorRole left, AuthorRole right)
        => !(left == right);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj)
        => obj is AuthorRole otherRole && this == otherRole;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode()
        => Label.GetHashCode();

    public bool Equals(AuthorRole other)
        => !ReferenceEquals(other, null)
            && string.Equals(Label, other.Label, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Label;
}
