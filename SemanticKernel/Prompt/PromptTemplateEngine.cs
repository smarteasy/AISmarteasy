using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Context;
using SemanticKernel.Exception;
using SemanticKernel.Prompt.Blocks;

namespace SemanticKernel.Prompt;

public class PromptTemplateEngine : IPromptTemplateEngine
{
    private readonly ILogger _logger;
    private readonly TemplateTokenizer _tokenizer;

     public PromptTemplateEngine(ILoggerFactory? loggerFactory = null)
    {
        loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = loggerFactory.CreateLogger(typeof(PromptTemplateEngine));
        _tokenizer = new TemplateTokenizer(loggerFactory);
    }

    public async Task<string> RenderAsync(string template, SKContext context, CancellationToken cancellationToken = default)
    {
        this._logger.LogTrace("Rendering string template: {0}", template);
        var blocks = ExtractBlocks(template);
        return await RenderAsync(blocks, context, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<string> RenderAsync(IList<Block> blocks, SKContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Rendering list of {0} blocks", blocks.Count);
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
        foreach (Task<string> task in tasks)
        {
            result.Append(await task.ConfigureAwait(false));
        }

        this._logger.LogTrace("Rendered prompt: {0}", result);

        return result.ToString();
    }

    private IList<Block> ExtractBlocks(string? template, bool validate = true)
    {
        this._logger.LogTrace("Extracting blocks from template: {0}", template);
        var blocks = _tokenizer.Tokenize(template);

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
}
