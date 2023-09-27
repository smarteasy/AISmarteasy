using System.Diagnostics;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Connector.OpenAI.TextCompletion.Chat;
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

    public AIRequestSettings RequestSettings { get; private set; } = new();

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
            Name = Name,
            PluginName = PluginName,
            Description = Description,
            Parameters = Parameters,
        };
    }

  public async Task<SKContext> InvokeAsync(
        AIRequestSettings? settings,
        CancellationToken cancellationToken = default)
    {
        var kernel = KernelProvider.Kernel;
        AddDefaultValues(kernel.Context.Variables);
        return await RunPromptAsync(kernel.AIService, settings, cancellationToken).ConfigureAwait(false);
    }

  public ISKFunction SetDefaultPluginCollection(IReadOnlyPluginCollection plugins)
  {
      return this;
  }


  public ISKFunction SetAIService(Func<IAIService> serviceFactory)
    {
        Verify.NotNull(serviceFactory);
        _textCompletion = new Lazy<IAIService>(serviceFactory);
        return this;
    }

    public ISKFunction SetAIConfiguration(AIRequestSettings settings)
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
        Verify.ValidPluginName(skillName);
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
    private IReadOnlyPluginCollection? _pluginCollection;
    private Lazy<IAIService>? _textCompletion;
    public IPromptTemplate PromptTemplate { get; }

    private static async Task<string> RunAsync(IReadOnlyList<ITextResult> completions, CancellationToken cancellationToken = default)
    {
        return await completions[0].GetCompletionAsync(cancellationToken).ConfigureAwait(false);
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{Name} ({Description})";

     private void AddDefaultValues(ContextVariables variables)
    {
        foreach (var parameter in Parameters)
        {
            if (!variables.ContainsKey(parameter.Name) && parameter.DefaultValue != null)
            {
                variables[parameter.Name] = parameter.DefaultValue;
            }
        }
    }


    private async Task<SKContext> RunPromptAsync(
        IAIService? client,
        AIRequestSettings? requestSettings,
        CancellationToken cancellationToken)
    {

        var context = KernelProvider.Kernel.Context;

        Verify.NotNull(client);
        Verify.NotNull(requestSettings);

        try
        {
            if (client is OpenAITextCompletion)
            {
                var prompt = await PromptTemplate.RenderAsync(context, cancellationToken).ConfigureAwait(false);
                var answer = await client.RunTextCompletion(prompt, (CompleteRequestSettings)requestSettings, cancellationToken).ConfigureAwait(false);
                context.Variables.Update(answer.Text);
            }
            else
            {

                var prompt = await PromptTemplate.RenderAsync(context, cancellationToken).ConfigureAwait(false);
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);
                var chtHistory = await client.RunChatCompletion(chatHistory, (CompleteRequestSettings)requestSettings, cancellationToken).ConfigureAwait(false);
                context.Variables.Update(chtHistory.Messages[1].Content);
            }
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            _logger?.LogError(ex, "Semantic function {Plugin}.{Name} execution failed with error {Error}", PluginName, Name, ex.Message);
            throw;
        }

        return context;
    }
}
