using System.Runtime.CompilerServices;
using System.Text.Json;
using AISmarteasy.Core.Connector.OpenAI.TextCompletion;
using AISmarteasy.Core.Connector.OpenAI.TextCompletion.Chat;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.Prompt;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Util;

namespace AISmarteasy.Core.Function;

public class SemanticFunction : ISKFunction
{
    public string Name { get; }

    public string PluginName { get; }

    public string Description { get; }

    public AIRequestSettings RequestSettings { get; private set; } = new();

    public IList<ParameterView> Parameters { get; set; }

    public IPromptTemplate PromptTemplate { get; }

    private readonly ILogger _logger;

    public FunctionView Describe()
    {
        return new FunctionView(Name, PluginName, Description) { Parameters = Parameters };
    }

    public Task InvokeAsync(AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        var kernel = KernelProvider.Kernel;
        AddDefaultValues(kernel.Context.Variables);
        return RunPromptAsync(kernel.AIService, requestSettings, cancellationToken);
    }

    public ISKFunction SetDefaultPluginCollection(IPlugin plugins)
    {
        return this;
    }


    public ISKFunction SetAIConfiguration(AIRequestSettings settings)
    {
        Verify.NotNull(settings);
        this.RequestSettings = settings;
        return this;
    }

    public override string ToString()
        => ToString(false);

    public string ToString(bool writeIndented)
        => JsonSerializer.Serialize(this,
            options: writeIndented ? ToStringIndentedSerialization : ToStringStandardSerialization);

    internal SemanticFunction(
        IPromptTemplate promptTemplate,
        string pluginName,
        string functionName,
        string description,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(promptTemplate);
        Verify.ValidPluginName(pluginName);
        Verify.ValidFunctionName(functionName);

        _logger = loggerFactory is not null
            ? loggerFactory.CreateLogger(typeof(SemanticFunction))
            : NullLogger.Instance;

        PromptTemplate = promptTemplate;
        Parameters = PromptTemplate.Parameters;
        Verify.ParametersUniqueness(Parameters);

        Name = functionName;
        PluginName = pluginName;
        Description = description;
    }

    private static readonly JsonSerializerOptions ToStringStandardSerialization = new();
    private static readonly JsonSerializerOptions ToStringIndentedSerialization = new() { WriteIndented = true };

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

    private async Task RunPromptAsync(IAIService? client, AIRequestSettings? requestSettings,
        CancellationToken cancellationToken)
    {
        Verify.NotNull(client);
        Verify.NotNull(requestSettings);

        var context = KernelProvider.Kernel.Context;

        try
        {
            if (client is OpenAITextCompletion)
            {
                var prompt = await PromptTemplate.RenderAsync(cancellationToken).ConfigureAwait(false);
                var answer = await client
                    .RunTextCompletion(prompt, (CompleteRequestSettings)requestSettings, cancellationToken)
                    .ConfigureAwait(false);
                context.Variables.Update(answer.Text);
            }
            else
            {

                var prompt = await PromptTemplate.RenderAsync(cancellationToken).ConfigureAwait(false);
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);
                var chtHistory = await client
                    .RunChatCompletion(chatHistory, (CompleteRequestSettings)requestSettings, cancellationToken)
                    .ConfigureAwait(false);
                context.Variables.Update(chtHistory.Messages[1].Content);
            }
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            _logger.LogError(ex, "Semantic function {Plugin}.{Name} execution failed with error {Error}", PluginName,
                Name, ex.Message);
            throw;
        }
    }

    public static ISKFunction FromSemanticConfig(string pluginName, string functionName, SemanticFunctionConfig functionConfig,
        ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(functionConfig);

        var func = new SemanticFunction(promptTemplate: functionConfig.PromptTemplate, description: functionConfig.PromptTemplateConfig.Description,
            pluginName: pluginName, functionName: functionName, loggerFactory: loggerFactory
        );

        return func;
    }
}
