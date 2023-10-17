using System.Diagnostics.CodeAnalysis;

namespace AISmarteasy.Core.Handling;

internal static class StringExtensions
{
    internal static string NormalizeLineEndings(this string src)
    {
        return src.ReplaceLineEndings("\n");
    }

    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? data)
    {
        return string.IsNullOrEmpty(data);
    }

    public static bool IsNullOrWhitespace([NotNullWhen(false)] this string? data)
    {
        return string.IsNullOrWhiteSpace(data);
    }
}
