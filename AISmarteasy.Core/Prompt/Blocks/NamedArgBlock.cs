using System.Text.RegularExpressions;
using AISmarteasy.Core.Context;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Prompt.Blocks;

internal sealed class NamedArgBlock : Block, ITextRendering
{
    public override BlockTypeKind Type => BlockTypeKind.NamedArg;

    internal string Name { get; }

    public NamedArgBlock(string? text, ILoggerFactory? logger = null)
        : base(TrimWhitespace(text), logger)
    {
        var argParts = Content.Split(Symbols.NamedArgBlockSeparator);
        if (argParts.Length != 2)
        {
            Logger.LogError("Invalid named argument `{Text}`", text);
            throw new SKException($"A function named argument must contain a name and value separated by a '{Symbols.NamedArgBlockSeparator}' character.");
        }

        Name = argParts[0];
        _argNameAsVarBlock = new VariableBlock($"{Symbols.VarPrefix}{argParts[0]}");
        var argValue = argParts[1];
        if (argValue.Length == 0)
        {
            Logger.LogError("Invalid named argument `{Text}`", text);
            throw new SKException($"A function named argument must contain a quoted value or variable after the '{Symbols.NamedArgBlockSeparator}' character.");
        }

        if (argValue[0] == Symbols.VarPrefix)
        {
            _argValueAsVarBlock = new VariableBlock(argValue);
        }
        else
        {
            _valBlock = new ValueBlock(argValue);
        }
    }

     internal string GetValue(ContextVariables? variables)
    {
        var valueIsValidValBlock = _valBlock != null && _valBlock.IsValid(out var errorMessage);
        if (valueIsValidValBlock)
        {
            return _valBlock!.Render(variables);
        }

        var valueIsValidVarBlock = _argValueAsVarBlock != null && _argValueAsVarBlock.IsValid(out var errorMessage2);
        if (valueIsValidVarBlock)
        {
            return _argValueAsVarBlock!.Render(variables);
        }

        return string.Empty;
    }

    public string Render(ContextVariables? variables)
    {
        return Content;
    }

     public override bool IsValid(out string errorMsg)
    {
        errorMsg = string.Empty;
        if (string.IsNullOrEmpty(Name))
        {
            errorMsg = "A named argument must have a name";
            Logger.LogError(errorMsg);
            return false;
        }

        if (_valBlock != null && !_valBlock.IsValid(out var valErrorMsg))
        {
            errorMsg = $"There was an issue with the named argument value for '{Name}': {valErrorMsg}";
            Logger.LogError(errorMsg);
            return false;
        }
        else if (_argValueAsVarBlock != null && !_argValueAsVarBlock.IsValid(out var variableErrorMsg))
        {
            errorMsg = $"There was an issue with the named argument value for '{Name}': {variableErrorMsg}";
            Logger.LogError(errorMsg);
            return false;
        }
        else if (_valBlock == null && _argValueAsVarBlock == null)
        {
            errorMsg = "A named argument must have a value";
            Logger.LogError(errorMsg);
            return false;
        }

        if (!_argNameAsVarBlock.IsValid(out var argNameErrorMsg))
        {
            errorMsg = Regex.Replace(argNameErrorMsg, "a variable", "An argument", RegexOptions.IgnoreCase);
            errorMsg = Regex.Replace(errorMsg, "the variable", "The argument", RegexOptions.IgnoreCase);
            return false;
        }

        return true;
    }

    private readonly VariableBlock _argNameAsVarBlock;
    private readonly ValueBlock? _valBlock;
    private readonly VariableBlock? _argValueAsVarBlock;

    private static string? TrimWhitespace(string? text)
    {
        if (text == null)
        {
            return text;
        }

        string[] trimmedParts = GetTrimmedParts(text);
        switch (trimmedParts?.Length)
        {
            case 2:
                return $"{trimmedParts[0]}{Symbols.NamedArgBlockSeparator}{trimmedParts[1]}";
            case 1:
                return trimmedParts[0];
            default:
                return null;
        }
    }

    private static string[] GetTrimmedParts(string? text)
    {
        if (text == null)
        {
            return Array.Empty<string>();
        }

        string[] parts = text.Split(new char[] { Symbols.NamedArgBlockSeparator }, 2);
        string[] result = new string[parts.Length];
        if (parts.Length > 0)
        {
            result[0] = parts[0].Trim();
        }

        if (parts.Length > 1)
        {
            result[1] = parts[1].Trim();
        }

        return result;
    }
}
