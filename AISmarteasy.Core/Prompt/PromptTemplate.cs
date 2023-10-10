using System.Text;
using AISmarteasy.Core.Connector.OpenAI;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Prompt.Blocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.Prompt;

public class PromptTemplate : IPromptTemplate
{
    private readonly string _template;
    private readonly TemplateTokenizer _tokenizer;
    private readonly PromptTemplateConfig _promptConfig;
    private readonly ILogger _logger;

    public PromptTemplate()
        : this(string.Empty)
    {
    }
    public PromptTemplate(string template)
    : this(template, PromptTemplateConfigBuilder.Build(), new NullLoggerFactory())
    {
    }
    public PromptTemplate(ILoggerFactory loggerFactory)
        : this(string.Empty, PromptTemplateConfigBuilder.Build(), loggerFactory)
    {
    }
    public PromptTemplate(string template, PromptTemplateConfig config)
        : this(template, config, new NullLoggerFactory())

    {
    }

    public PromptTemplate(string template, PromptTemplateConfig config, ILoggerFactory loggerFactory)
    {
        _template = template;
        _promptConfig = config;
        _logger = loggerFactory.CreateLogger(typeof(PromptTemplate));
        _tokenizer = new TemplateTokenizer(loggerFactory);

        Parameters = InitParameters();
    }

    public IList<ParameterView> Parameters { get; }

    private List<ParameterView> InitParameters()
    {
        Dictionary<string, ParameterView> result = new(_promptConfig.Input.Parameters.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var p in this._promptConfig.Input.Parameters)
        {
            result[p.Name] = new ParameterView(p.Name, p.Description, p.DefaultValue);
        }

        return result.Values.ToList();
    }

    public async Task<string> RenderAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Rendering string template: {0}", _template);
        var blocks = ExtractBlocks(_template);
        return await RenderAsync(blocks, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<string> RenderAsync(IList<Block> blocks, CancellationToken cancellationToken = default)
    {
        var context = KernelProvider.Kernel.Context;
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

        _logger.LogTrace("Rendered prompt: {0}", result);

        return result.ToString();
    }

    private IList<Block> ExtractBlocks(string template, bool validate = true)
    {
        _logger.LogTrace("Extracting blocks from template: {0}", template);
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
