using Microsoft.Extensions.Logging;
using SemanticKernel.Context;
using SemanticKernel.Exception;
using SemanticKernel.Function;

namespace SemanticKernel.Prompt.Blocks;

#pragma warning disable CA2254 // error strings are used also internally, not just for logging
#pragma warning disable CA1031 // IsCriticalException is an internal utility and should not be used by extensions

// ReSharper disable TemplateIsNotCompileTimeConstantProblem
internal sealed class CodeBlock : Block, ICodeRendering
{
    public override BlockTypeKind Type => BlockTypeKind.Code;

    public CodeBlock(string? content, ILoggerFactory? loggerFactory)
        : this(new CodeTokenizer(loggerFactory).Tokenize(content), content?.Trim(), loggerFactory)
    {
    }

    public CodeBlock(List<Block> tokens, string? content, ILoggerFactory? loggerFactory)
        : base(content?.Trim(), loggerFactory)
    {
        _tokens = tokens;
    }

    public override bool IsValid(out string errorMsg)
    {
        errorMsg = "";

        foreach (Block token in _tokens)
        {
            if (!token.IsValid(out errorMsg))
            {
                Logger.LogError(errorMsg);
                return false;
            }
        }

        if (_tokens.Count > 0 && _tokens[0].Type == BlockTypeKind.NamedArg)
        {
            errorMsg = "Unexpected named argument found. Expected function name first.";
            Logger.LogError(errorMsg);
            return false;
        }

        if (_tokens.Count > 1 && !IsValidFunctionCall(out errorMsg))
        {
            return false;
        }

        _validated = true;

        return true;
    }

    public async Task<string> RenderCodeAsync(SKContext context, CancellationToken cancellationToken = default)
    {
        if (!_validated && !IsValid(out var error))
        {
            throw new SKException(error);
        }

        Logger.LogTrace("Rendering code: `{Content}`", Content);

        switch (_tokens[0].Type)
        {
            case BlockTypeKind.Value:
            case BlockTypeKind.Variable:
                return ((ITextRendering)_tokens[0]).Render(context.Variables);

            case BlockTypeKind.FunctionId:
                return await RenderFunctionCallAsync((FunctionIdBlock)_tokens[0], context).ConfigureAwait(false);
        }

        throw new SKException($"Unexpected first token type: {_tokens[0].Type:G}");
    }

    private bool _validated;
    private readonly List<Block> _tokens;

    private async Task<string> RenderFunctionCallAsync(FunctionIdBlock fBlock, SKContext context)
    {
        if (context.Skills == null)
        {
            throw new SKException("Skill collection not found in the context");
        }

        if (!GetFunctionFromSkillCollection(context.Skills!, fBlock, out ISKFunction? function))
        {
            var errorMsg = $"Function `{fBlock.Content}` not found";
            Logger.LogError(errorMsg);
            throw new SKException(errorMsg);
        }

        SKContext contextClone = context.Clone();

        // If the code syntax is {{functionName $varName}} use $varName instead of $input
        // If the code syntax is {{functionName 'value'}} use "value" instead of $input
        if (_tokens.Count > 1)
        {
            contextClone = PopulateContextWithFunctionArguments(contextClone);
        }

        try
        {
            contextClone = await function!.InvokeAsync(contextClone).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            Logger.LogError(ex, "Function {Plugin}.{Function} execution failed with error {Error}", function!.SkillName, function.Name, ex.Message);
            throw;
        }

        return contextClone.Result;
    }

    private bool GetFunctionFromSkillCollection(
        IReadOnlySkillCollection skills,
        FunctionIdBlock fBlock,
        out ISKFunction? function)
    {
        if (string.IsNullOrEmpty(fBlock.SkillName))
        {
            // Function in the global skill
            return skills.TryGetFunction(fBlock.FunctionName, out function);
        }

        // Function within a specific skill
        return skills.TryGetFunction(fBlock.SkillName, fBlock.FunctionName, out function);
    }

    private bool IsValidFunctionCall(out string errorMsg)
    {
        errorMsg = "";
        if (_tokens[0].Type != BlockTypeKind.FunctionId)
        {
            errorMsg = $"Unexpected second token found: {_tokens[1].Content}";
            Logger.LogError(errorMsg);
            return false;
        }

        if (_tokens[1].Type is not BlockTypeKind.Value and not BlockTypeKind.Variable and not BlockTypeKind.NamedArg)
        {
            errorMsg = "The first arg of a function must be a quoted string, variable or named argument";
            Logger.LogError(errorMsg);
            return false;
        }

        for (int i = 2; i < _tokens.Count; i++)
        {
            if (_tokens[i].Type is not BlockTypeKind.NamedArg)
            {
                errorMsg = $"Functions only support named arguments after the first argument. Argument {i} is not named.";
                Logger.LogError(errorMsg);
                return false;
            }
        }

        return true;
    }

    private SKContext PopulateContextWithFunctionArguments(SKContext context)
    {
        // Clone the context to avoid unexpected and hard to test input mutation
        var contextClone = context.Clone();
        var firstArg = _tokens[1];

        // Sensitive data, logging as trace, disabled by default
        Logger.LogTrace("Passing variable/value: `{Content}`", firstArg.Content);

        var namedArgsStartIndex = 1;
        if (firstArg.Type is not BlockTypeKind.NamedArg)
        {
            string input = ((ITextRendering)_tokens[1]).Render(contextClone.Variables);
            // Keep previous trust information when updating the input
            contextClone.Variables.Update(input);
            namedArgsStartIndex++;
        }

        for (int i = namedArgsStartIndex; i < _tokens.Count; i++)
        {
            var arg = _tokens[i] as NamedArgBlock;

            // When casting fails because the block isn't a NamedArg, arg is null
            if (arg == null)
            {
                var errorMsg = "Functions support up to one positional argument";
                Logger.LogError(errorMsg);
                throw new SKException($"Unexpected first token type: {_tokens[i].Type:G}");
            }

            // Sensitive data, logging as trace, disabled by default
            Logger.LogTrace("Passing variable/value: `{Content}`", arg.Content);

            contextClone.Variables.Set(arg.Name, arg.GetValue(context.Variables));
        }

        return contextClone;
    }
}
// ReSharper restore TemplateIsNotCompileTimeConstantProblem
#pragma warning restore CA2254
