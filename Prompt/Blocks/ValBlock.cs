using Microsoft.Extensions.Logging;

namespace SemanticKernel.Prompt.Blocks;

public sealed class ValBlock : Block, ITextRendering
{
    public override BlockTypeKind Type => BlockTypeKind.Value;

    // Cache the first and last char
    private readonly char _first = '\0';
    private readonly char _last = '\0';

    // Content, excluding start/end quote chars
    private readonly string _value = string.Empty;

    /// <summary>
    /// Create an instance
    /// </summary>
    /// <param name="quotedValue">Block content, including the delimiting chars</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public ValBlock(string? quotedValue, ILoggerFactory? loggerFactory = null)
        : base(quotedValue?.Trim(), loggerFactory)
    {
        if (Content.Length < 2)
        {
            Logger.LogError("A value must have single quotes or double quotes on both sides");
            return;
        }

        _first = Content[0];
        _last = Content[Content.Length - 1];
        _value = Content.Substring(1, Content.Length - 2);
    }

#pragma warning disable CA2254 // error strings are used also internally, not just for logging
    // ReSharper disable TemplateIsNotCompileTimeConstantProblem
    public override bool IsValid(out string errorMsg)
    {
        errorMsg = string.Empty;

        // Content includes the quotes, so it must be at least 2 chars long
        if (Content.Length < 2)
        {
            errorMsg = "A value must have single quotes or double quotes on both sides";
            Logger.LogError(errorMsg);
            return false;
        }

        // Check if delimiting chars are consistent
        if (_first != _last)
        {
            errorMsg = "A value must be defined using either single quotes or double quotes, not both";
            Logger.LogError(errorMsg);
            return false;
        }

        return true;
    }
#pragma warning restore CA2254

    public string Render(ContextVariables? variables)
    {
        return _value;
    }

    public static bool HasValPrefix(string? text)
    {
        return !string.IsNullOrEmpty(text)
               && text!.Length > 0
               && text[0] is Symbols.DblQuote or Symbols.SglQuote;
    }
}
