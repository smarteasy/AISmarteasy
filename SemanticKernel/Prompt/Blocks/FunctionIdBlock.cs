// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SemanticKernel.Context;
using SemanticKernel.Exception;

namespace SemanticKernel.Prompt.Blocks;

internal sealed class FunctionIdBlock : Block, ITextRendering
{
    public override BlockTypeKind Type => BlockTypeKind.FunctionId;

    internal string SkillName { get; } = string.Empty;

    internal string FunctionName { get; } = string.Empty;

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
            SkillName = functionNameParts[0];
            FunctionName = functionNameParts[1];
            return;
        }

        FunctionName = Content;
    }

    public override bool IsValid(out string errorMsg)
    {
        if (!s_validContentRegex.IsMatch(Content))
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

    private static readonly Regex s_validContentRegex = new("^[a-zA-Z0-9_.]*$");
}
