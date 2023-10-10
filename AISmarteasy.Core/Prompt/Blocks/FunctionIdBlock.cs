// Copyright (c) Microsoft. All rights reserved.

using System.Text.RegularExpressions;
using AISmarteasy.Core.Context;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Prompt.Blocks;

internal sealed class FunctionIdBlock : Block, ITextRendering
{
    public override BlockTypeKind Type => BlockTypeKind.FunctionId;

    internal string PluginName { get; } = string.Empty;

    internal string FunctionName { get; }

    public FunctionIdBlock(string? text, ILoggerFactory? loggerFactory = null)
        : base(text?.Trim(), loggerFactory)
    {
        var functionNameParts = Content.Split('.');
        if (functionNameParts.Length > 2)
        {
            Logger.LogError("Invalid function name `{FunctionName}`.", Content);
            throw new SKException($"Invalid function name `{Content}`. A function name can contain at most one dot separating the skill name from the function name");
        }

        if (functionNameParts.Length == 2)
        {
            PluginName = functionNameParts[0];
            FunctionName = functionNameParts[1];
            return;
        }

        FunctionName = Content;
    }

    public override bool IsValid(out string errorMsg)
    {
        if (!ValidContentRegex.IsMatch(Content))
        {
            errorMsg = "The function identifier is empty";
            return false;
        }

        if (HasMoreThanOneDot(Content))
        {
            errorMsg = "The function identifier can contain max one '.' char separating skill name from function name";
            return false;
        }

        errorMsg = "";
        return true;
    }

    public string Render(ContextVariables? variables)
    {
        return Content;
    }

    private static bool HasMoreThanOneDot(string? value)
    {
        if (value == null || value.Length < 2) { return false; }

        int count = 0;
        return value.Any(t => t == '.' && ++count > 1);
    }

    private static readonly Regex ValidContentRegex = new("^[a-zA-Z0-9_.]*$");
}
