﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AISmarteasy.Core.Function;

public static class Verify
{
    private static readonly Regex AsciiLettersDigitsUnderscoresRegex = new("^[0-9A-Za-z_]*$");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void NotNull([NotNull] object? obj, [CallerArgumentExpression("obj")] string? paramName = null)
    {
        if (obj is null)
        {
            ThrowArgumentNullException(paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotNullOrWhiteSpace([NotNull] string? str, [CallerArgumentExpression("str")] string? paramName = null)
    {
        NotNull(str, paramName);
        if (string.IsNullOrWhiteSpace(str))
        {
            ThrowArgumentWhiteSpaceException(paramName);
        }
    }

    internal static void ValidPluginName([NotNull] string? pluginName)
    {
        NotNullOrWhiteSpace(pluginName);
        if (!AsciiLettersDigitsUnderscoresRegex.IsMatch(pluginName))
        {
            ThrowInvalidName("plugin name", pluginName);
        }
    }

    internal static void ValidFunctionName([NotNull] string? functionName) =>
        ValidName(functionName, "function name");

    internal static void ValidFunctionParamName([NotNull] string? functionParamName) =>
        ValidName(functionParamName, "function parameter name");

    private static void ValidName([NotNull] string? name, string kind)
    {
        NotNullOrWhiteSpace(name);
        if (!AsciiLettersDigitsUnderscoresRegex.IsMatch(name))
        {
            ThrowInvalidName(kind, name);
        }
    }

    internal static void StartsWith(string text, string prefix, string message, [CallerArgumentExpression("text")] string? textParamName = null)
    {
        Debug.Assert(prefix is not null);

        NotNullOrWhiteSpace(text, textParamName);
        if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(textParamName, message);
        }
    }

    internal static void DirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory '{path}' could not be found.");
        }
    }

    internal static void ParametersUniqueness(IList<ParameterView> parameters)
    {
        int count = parameters.Count;
        if (count > 0)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++)
            {
                ParameterView p = parameters[i];
                if (string.IsNullOrWhiteSpace(p.Name))
                {
                    string paramName = $"{nameof(parameters)}[{i}].{p.Name}";
                    ThrowArgumentWhiteSpaceException(paramName);
                }

                if (!seen.Add(p.Name))
                {
                    throw new SKException($"The function has two or more parameters with the same name '{p.Name}'");
                }
            }
        }
    }

    [DoesNotReturn]
    private static void ThrowInvalidName(string kind, string name) =>
        throw new SKException($"A {kind} can contain only ASCII letters, digits, and underscores: '{name}' is not a valid name.");

    [DoesNotReturn]
    internal static void ThrowArgumentNullException(string? paramName) =>
        throw new ArgumentNullException(paramName);

    [DoesNotReturn]
    internal static void ThrowArgumentWhiteSpaceException(string? paramName) =>
        throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);

    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRangeException<T>(string? paramName, T actualValue, string message) =>
        throw new ArgumentOutOfRangeException(paramName, actualValue, message);
}