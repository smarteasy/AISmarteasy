using System.Threading;
using SemanticKernel.Context;
using SemanticKernel.Function;

namespace SemanticKernel.Prompt;

public sealed class PromptTemplate : IPromptTemplate
{
    private readonly string _template;
    private readonly IPromptTemplateEngine _templateEngine;
    private readonly PromptTemplateConfig _promptConfig;


    public PromptTemplate(string template, PromptTemplateConfig promptTemplateConfig, IKernel kernel)
        : this(template, promptTemplateConfig, kernel.PromptTemplateEngine)
    {
    }

    public PromptTemplate(
        string template,
        PromptTemplateConfig promptTemplateConfig,
        IPromptTemplateEngine promptTemplateEngine)
    {
        _template = template;
        _templateEngine = promptTemplateEngine;
        _promptConfig = promptTemplateConfig;
    }

    public IList<ParameterView> GetParameters()
    {
        Dictionary<string, ParameterView> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in this._promptConfig.Input.Parameters)
        {
            result[parameter.Name] = new ParameterView(parameter.Name, parameter.Description, parameter.DefaultValue);
        }

        return result.Values.ToList();
    }

    public async Task<string> RenderAsync(SKContext context, CancellationToken cancellationToken = default)
    {
        return await _templateEngine.RenderAsync(_template, context, cancellationToken).ConfigureAwait(false);
    }
}