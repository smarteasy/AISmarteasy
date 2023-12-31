﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AISmarteasy.Core.PluginFunction;

public static class Verify
{
    private static readonly Regex AsciiLettersDigitsUnderscoresRegex = new("^[0-9A-Za-z_]*$");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotNull([NotNull] object? obj, [CallerArgumentExpression("obj")] string? paramName = null)
    {
        if (obj is null)
        {
            ThrowArgumentNullException(paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotNullOrWhitespace([NotNull] string? str, [CallerArgumentExpression("str")] string? paramName = null)
    {
        NotNull(str, paramName);
        if (string.IsNullOrWhiteSpace(str))
        {
            ThrowArgumentWhiteSpaceException(paramName);
        }
    }

    public static void ValidPluginName([NotNull] string? pluginName)
    {
        NotNullOrWhitespace(pluginName);
        if (!AsciiLettersDigitsUnderscoresRegex.IsMatch(pluginName))
        {
            ThrowInvalidName("plugin name", pluginName);
        }
    }

    public static void ValidFunctionName([NotNull] string? functionName) =>
        ValidName(functionName, "function name");

    public static void ValidFunctionParamName([NotNull] string? functionParamName) =>
        ValidName(functionParamName, "function parameter name");

    private static void ValidName([NotNull] string? name, string kind)
    {
        NotNullOrWhitespace(name);
        if (!AsciiLettersDigitsUnderscoresRegex.IsMatch(name))
        {
            ThrowInvalidName(kind, name);
        }
    }

    public static void StartsWith(string text, string prefix, string message, [CallerArgumentExpression("text")] string? textParamName = null)
    {
        Debug.Assert(prefix is not null);

        NotNullOrWhitespace(text, textParamName);
        if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(textParamName, message);
        }
    }

    public static void DirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory '{path}' could not be found.");
        }
    }

    public static void ParametersUniqueness(IList<ParameterView> parameters)
    {
        int count = parameters.Count;
        if (count > 0)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++)
            {
                ParameterView parameterView = parameters[i];
                if (string.IsNullOrWhiteSpace(parameterView.Name))
                {
                    string paramName = $"{nameof(parameters)}[{i}].{parameterView.Name}";
                    if (string.IsNullOrEmpty(parameterView.Name))
                    {
                        ThrowArgumentNullException(paramName);
                    }
                    else
                    {
                        ThrowArgumentWhiteSpaceException(paramName);
                    }
                }

                if (!seen.Add(parameterView.Name))
                {
                    throw new SKException($"The function has two or more parameters with the same name '{parameterView.Name}'");
                }
            }
        }
    }

    [DoesNotReturn]
    private static void ThrowInvalidName(string kind, string name) =>
        throw new SKException($"A {kind} can contain only ASCII letters, digits, and underscores: '{name}' is not a valid name.");

    [DoesNotReturn]
    public static void ThrowArgumentNullException(string? paramName) =>
        throw new ArgumentNullException(paramName);

    [DoesNotReturn]
    public static void ThrowArgumentWhiteSpaceException(string? paramName) =>
        throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);

    [DoesNotReturn]
    public static void ThrowArgumentOutOfRangeException<T>(string? paramName, T actualValue, string message) =>
        throw new ArgumentOutOfRangeException(paramName, actualValue, message);
}
