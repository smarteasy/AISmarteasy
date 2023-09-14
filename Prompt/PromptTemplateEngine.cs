﻿//// Copyright (c) Microsoft. All rights reserved.

//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

//using Microsoft.SemanticKernel.Diagnostics;
//using Microsoft.SemanticKernel.Orchestration;
//using Microsoft.SemanticKernel.TemplateEngine.Prompt.Blocks;

namespace SemanticKernel.Prompt;

///// <summary>
///// Given a prompt, that might contain references to variables and functions:
///// - Get the list of references
///// - Resolve each reference
/////   - Variable references are resolved using the context variables
/////   - Function references are resolved invoking those functions
/////     - Functions can be invoked passing in variables
/////     - Functions do not receive the context variables, unless specified using a special variable
/////     - Functions can be invoked in order and in parallel so the context variables must be immutable when invoked within the template
///// </summary>
public class PromptTemplateEngine : IPromptTemplateEngine
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly TemplateTokenizer _tokenizer;

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="PromptTemplateEngine"/> class.
    //    /// </summary>
    //    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public PromptTemplateEngine(ILoggerFactory? loggerFactory = null)
    {
        this._loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        this._logger = this._loggerFactory.CreateLogger(typeof(PromptTemplateEngine));
        this._tokenizer = new TemplateTokenizer(loggerFactory);
    }

    public async Task<string> RenderAsync(string templateText, SKContext context, CancellationToken cancellationToken = default)
    {
        this._logger.LogTrace("Rendering string template: {0}", templateText);
        var blocks = this.ExtractBlocks(templateText);
        return await this.RenderAsync(blocks, context, cancellationToken).ConfigureAwait(false);
    }

    internal IList<Block> ExtractBlocks(string? templateText, bool validate = true)
    {
        this._logger.LogTrace("Extracting blocks from template: {0}", templateText);
        var blocks = this._tokenizer.Tokenize(templateText);

        if (validate)
        {
            foreach (var block in blocks)
            {
                if (!block.IsValid(out var error))
                {
                    throw new SKException(error);
                }
            }
        }

        return blocks;
    }

    internal async Task<string> RenderAsync(IList<Block> blocks, SKContext context, CancellationToken cancellationToken = default)
    {
        this._logger.LogTrace("Rendering list of {0} blocks", blocks.Count);
        var tasks = new List<Task<string>>(blocks.Count);
        foreach (var block in blocks)
        {
            switch (block)
            {
                case ITextRendering staticBlock:
                    tasks.Add(Task.FromResult(staticBlock.Render(context.Variables)));
                    break;

                case ICodeRendering dynamicBlock:
                    tasks.Add(dynamicBlock.RenderCodeAsync(context, cancellationToken));
                    break;

                default:
                    const string Error = "Unexpected block type, the block doesn't have a rendering method";
                    this._logger.LogError(Error);
                    throw new SKException(Error);
            }
        }

        var result = new StringBuilder();
        foreach (Task<string> t in tasks)
        {
            result.Append(await t.ConfigureAwait(false));
        }

        // Sensitive data, logging as trace, disabled by default
        this._logger.LogTrace("Rendered prompt: {0}", result);

        return result.ToString();
    }

    internal IList<Block> RenderVariables(IList<Block> blocks, ContextVariables? variables)
    {
        this._logger.LogTrace("Rendering variables");
        return blocks.Select(block => block.Type != BlockTypeKind.Variable
            ? block
            : new TextBlock(((ITextRendering)block).Render(variables), this._loggerFactory)).ToList();
    }
}
