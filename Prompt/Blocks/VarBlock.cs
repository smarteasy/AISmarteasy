using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Prompt.Blocks;

public sealed class VarBlock : Block, ITextRendering
{
    public override BlockTypeKind Type => BlockTypeKind.Variable;

    public string Name { get; } = string.Empty;

    public VarBlock(string? content, ILoggerFactory? loggerFactory = null) : base(content?.Trim(), loggerFactory)
    {
        if (Content.Length < 2)
        {
            Logger.LogError("The variable name is empty");
            return;
        }

        Name = Content[1..];
    }

#pragma warning disable CA2254 // error strings are used also internally, not just for logging
    // ReSharper disable TemplateIsNotCompileTimeConstantProblem
    public override bool IsValid(out string errorMsg)
    {
        errorMsg = string.Empty;

        if (string.IsNullOrEmpty(Content))
        {
            errorMsg = $"A variable must start with the symbol {Symbols.VarPrefix} and have a name";
            Logger.LogError(errorMsg);
            return false;
        }

        if (Content[0] != Symbols.VarPrefix)
        {
            errorMsg = $"A variable must start with the symbol {Symbols.VarPrefix}";
            Logger.LogError(errorMsg);
            return false;
        }

        if (Content.Length < 2)
        {
            errorMsg = "The variable name is empty";
            Logger.LogError(errorMsg);
            return false;
        }

        if (!ValidNameRegex.IsMatch(Name))
        {
            errorMsg = $"The variable name '{Name}' contains invalid characters. " +
                       "Only alphanumeric chars and underscore are allowed.";
            Logger.LogError(errorMsg);
            return false;
        }

        return true;
    }
#pragma warning restore CA2254

    public string Render(ContextVariables? variables)
    {
        if (variables == null) { return string.Empty; }

        if (string.IsNullOrEmpty(Name))
        {
            const string ErrMsg = "Variable rendering failed, the variable name is empty";
            Logger.LogError(ErrMsg);
            throw new SKException(ErrMsg);
        }

        if (variables.TryGetValue(Name, out string? value))
        {
            return value;
        }

        Logger.LogWarning("Variable `{0}{1}` not found", Symbols.VarPrefix, Name);

        return string.Empty;
    }

    private static readonly Regex ValidNameRegex = new("^[a-zA-Z0-9_]*$");
}
