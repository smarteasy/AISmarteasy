using AISmarteasy.Core.Context;
using AISmarteasy.Core.Function;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Prompt.Blocks;

public sealed class CodeBlock : Block, ICodeRendering
{
    public override BlockTypeKind Type => BlockTypeKind.Code;

    public CodeBlock(string? content, ILoggerFactory? loggerFactory)
        : this(new CodeTokenizer(loggerFactory).Tokenize(content), content?.Trim(), loggerFactory)
    {
    }

    public CodeBlock(List<Block> blocks, string? content, ILoggerFactory? loggerFactory)
        : base(content?.Trim(), loggerFactory)
    {
        _blocks = blocks;
    }

    public override bool IsValid(out string errorMsg)
    {
        errorMsg = "";

        foreach (var token in _blocks)
        {
            if (!token.IsValid(out errorMsg))
            {
                Logger.LogError(errorMsg);
                return false;
            }
        }

        if (_blocks.Count > 0 && _blocks[0].Type == BlockTypeKind.NamedArg)
        {
            errorMsg = "Unexpected named argument found. Expected function name first.";
            Logger.LogError(errorMsg);
            return false;
        }

        if (_blocks.Count > 1 && !IsValidFunctionCall(out errorMsg))
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

        switch (_blocks[0].Type)
        {
            case BlockTypeKind.Value:
            case BlockTypeKind.Variable:
                return ((ITextRendering)_blocks[0]).Render(context.Variables);

            case BlockTypeKind.FunctionId:
                return await RenderFunctionCallAsync((FunctionIdBlock)_blocks[0], context).ConfigureAwait(false);
        }

        throw new SKException($"Unexpected first token type: {_blocks[0].Type:G}");
    }

    private bool _validated;
    private readonly List<Block> _blocks;

    private async Task<string> RenderFunctionCallAsync(FunctionIdBlock fBlock, SKContext context)
    {
        var function = GetFunctionFromPlugins(fBlock);
        if (function==null)
        {
            var errorMsg = $"Function `{fBlock.Content}` not found";
            Logger.LogError(errorMsg);
            throw new SKException(errorMsg);
        }

        SKContext contextClone = context.Clone();

        if (_blocks.Count > 1)
        {
            contextClone = PopulateContextWithFunctionArguments(context);
        }

        try
        {
            await function.InvokeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Function {Plugin}.{Function} execution failed with error {Error}", function.PluginName, function.Name, ex.Message);
            throw;
        }

        return contextClone.Result;
    }

    private ISKFunction? GetFunctionFromPlugins(FunctionIdBlock functionBlock)
    {
        foreach (var plugin in KernelProvider.Kernel.Plugins.Values)
        {
            try
            {
                return plugin.GetFunction(functionBlock.FunctionName);
            }
            catch (SKException)
            {
                return null;
            }
        }

        return null;
    }

    private bool IsValidFunctionCall(out string errorMsg)
    {
        errorMsg = "";
        if (_blocks[0].Type != BlockTypeKind.FunctionId)
        {
            errorMsg = $"Unexpected second token found: {_blocks[1].Content}";
            Logger.LogError(errorMsg);
            return false;
        }

        if (_blocks[1].Type is not BlockTypeKind.Value and not BlockTypeKind.Variable and not BlockTypeKind.NamedArg)
        {
            errorMsg = "The first arg of a function must be a quoted string, variable or named argument";
            Logger.LogError(errorMsg);
            return false;
        }

        for (int i = 2; i < _blocks.Count; i++)
        {
            if (_blocks[i].Type is not BlockTypeKind.NamedArg)
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
        var contextClone = context.Clone();
        var firstArg = _blocks[1];

        Logger.LogTrace("Passing variable/value: `{Content}`", firstArg.Content);

        var namedArgsStartIndex = 1;
        if (firstArg.Type is not BlockTypeKind.NamedArg)
        {
            string input = ((ITextRendering)_blocks[1]).Render(contextClone.Variables);
            contextClone.Variables.Update(input);
            namedArgsStartIndex++;
        }

        for (int i = namedArgsStartIndex; i < _blocks.Count; i++)
        {
            var arg = _blocks[i] as NamedArgBlock;

            if (arg == null)
            {
                var errorMsg = "Functions support up to one positional argument";
                Logger.LogError(errorMsg);
                throw new SKException($"Unexpected first token type: {_blocks[i].Type:G}");
            }

            Logger.LogTrace("Passing variable/value: `{Content}`", arg.Content);

            contextClone.Variables.Set(arg.Name, arg.GetValue(context.Variables));
        }

        return contextClone;
    }
}
