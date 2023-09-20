using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Context;
using SemanticKernel.Prompt;
using SemanticKernel.Service;
using SemanticKernel.Util;

namespace SemanticKernel.Function;

#pragma warning disable format

[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal sealed class SemanticFunction : ISKFunction, IDisposable
{
    public string Name { get; }

    public string PluginName { get; }

    public string Description { get; }

    public bool IsSemantic => true;

    public CompleteRequestSettings RequestSettings { get; private set; } = new();

    public IList<ParameterView> Parameters { get; }

    public static ISKFunction FromSemanticConfig(
        string skillName,
        string functionName,
        SemanticFunctionConfig functionConfig,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(functionConfig);

        var func = new SemanticFunction(
            template: functionConfig.PromptTemplate,
            description: functionConfig.PromptTemplateConfig.Description,
            skillName: skillName,
            functionName: functionName,
            loggerFactory: loggerFactory
        );

        return func;
    }

  public FunctionView Describe()
    {
        return new FunctionView
        {
            IsSemantic = IsSemantic,
            Name = Name,
            SkillName = PluginName,
            Description = Description,
            Parameters = Parameters,
        };
    }

    public async Task<SKContext> InvokeAsync(
        IKernel kernel, 
        CompleteRequestSettings? settings,
        CancellationToken cancellationToken = default)
    {
        AddDefaultValues(kernel.Context.Variables);
        return await RunPromptAsync(kernel.AIService, settings, kernel.Context, cancellationToken).ConfigureAwait(false);
    }


    public ISKFunction SetDefaultSkillCollection(IReadOnlyPluginCollection skills)
    {
        this._skillCollection = skills;
        return this;
    }

    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory)
    {
        Verify.NotNull(serviceFactory);
        this._textCompletion = new Lazy<ITextCompletion>(serviceFactory);
        return this;
    }

    public ISKFunction SetAIConfiguration(CompleteRequestSettings settings)
    {
        Verify.NotNull(settings);
        this.RequestSettings = settings;
        return this;
    }

    public void Dispose()
    {
        if (this._textCompletion is { IsValueCreated: true } aiService)
        {
            (aiService.Value as IDisposable)?.Dispose();
        }
    }

    public override string ToString()
        => ToString(false);

    public string ToString(bool writeIndented)
        => JsonSerializer.Serialize(this, options: writeIndented ? ToStringIndentedSerialization : ToStringStandardSerialization);

    internal SemanticFunction(
        IPromptTemplate template,
        string skillName,
        string functionName,
        string description,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(template);
        Verify.ValidSkillName(skillName);
        Verify.ValidFunctionName(functionName);

        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(SemanticFunction)) : NullLogger.Instance;

        PromptTemplate = template;
        Parameters = template.Parameters;
        Verify.ParametersUniqueness(Parameters);

        Name = functionName;
        PluginName = skillName;
        Description = description;
    }

    private static readonly JsonSerializerOptions ToStringStandardSerialization = new();
    private static readonly JsonSerializerOptions ToStringIndentedSerialization = new() { WriteIndented = true };
    private readonly ILogger _logger;
    private IReadOnlyPluginCollection? _skillCollection;
    private Lazy<ITextCompletion>? _textCompletion = null;
    public IPromptTemplate PromptTemplate { get; }

    private static async Task<string> RunAsync(IReadOnlyList<ITextResult> completions, CancellationToken cancellationToken = default)
    {
        return await completions[0].GetCompletionAsync(cancellationToken).ConfigureAwait(false);
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{this.Name} ({this.Description})";

     private void AddDefaultValues(ContextVariables variables)
    {
        foreach (var parameter in this.Parameters)
        {
            if (!variables.ContainsKey(parameter.Name) && parameter.DefaultValue != null)
            {
                variables[parameter.Name] = parameter.DefaultValue;
            }
        }
    }


    private async Task<SKContext> RunPromptAsync(
        IAIService? client,
        CompleteRequestSettings? requestSettings,
        SKContext context,
        CancellationToken cancellationToken)
    {
        Verify.NotNull(client);
        Verify.NotNull(requestSettings);

        try
        {
            //var prompt = await PromptTemplate.RenderAsync(context, cancellationToken).ConfigureAwait(false);
            //var answer = await client.RunCompletion(prompt, requestSettings, cancellationToken).ConfigureAwait(false);
            //context.Variables.Update(answer.Text);

            //TODO - 아래 코드를 위와 같이 바꾸었을 때 문제점 파악
            //string renderedPrompt = await this._promptTemplate.RenderAsync(context, cancellationToken).ConfigureAwait(false);
            //var completionResults = await client.GetCompletionsAsync(renderedPrompt, requestSettings, cancellationToken).ConfigureAwait(false);
            //string completion = await GetCompletionsResultContentAsync(completionResults, cancellationToken).ConfigureAwait(false);

            //// Update the result with the completion
            //context.Variables.Update(completion);

            //context.ModelResults = completionResults.Select(c => c.ModelResult).ToArray();
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            _logger?.LogError(ex, "Semantic function {Plugin}.{Name} execution failed with error {Error}", PluginName, Name, ex.Message);
            throw;
        }

        return context;
    }
}
