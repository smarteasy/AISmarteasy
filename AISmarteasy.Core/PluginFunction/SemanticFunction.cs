using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.Connecting.OpenAI;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.Handling;
using AISmarteasy.Core.Prompt;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.PluginFunction;

public class SemanticFunction : Function
{
    public IPromptTemplate PromptTemplate { get; }

    private readonly ILogger _logger;

    public SemanticFunction(IPromptTemplate promptTemplate, string pluginName, string name, string description,
        ILoggerFactory? loggerFactory = null)
        : base(pluginName, name, description, true, promptTemplate.Parameters)
    {
        _logger = loggerFactory is not null
            ? loggerFactory.CreateLogger(typeof(SemanticFunction))
            : NullLogger.Instance;

        PromptTemplate = promptTemplate;
    }

    public SemanticFunction()
        : this(new PromptTemplate(), string.Empty, string.Empty, string.Empty, null)
    {
    }

    public FunctionView BuildView()
    {
        return new FunctionView(Name, PluginName, Description, true, PromptTemplate.Parameters);
    }

    public override Task RunAsync(AIRequestSettings requestSettings, CancellationToken cancellationToken = default)
    {
        var kernel = KernelProvider.Kernel;
        AddDefaultValues(kernel!.Context.Variables);
        return RunAsync(kernel.TextCompletionService, requestSettings, cancellationToken);
    }

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

    public async Task RunAsync(ITextCompletion? client, AIRequestSettings? requestSettings, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(client);
        Verify.NotNull(requestSettings);

        var context = KernelProvider.Kernel!.Context;

        try
        {
            if (client is OpenAITextCompletion)
            {
                var prompt = await PromptTemplate.RenderAsync(cancellationToken).ConfigureAwait(false);
                var answer = await client
                    .RunTextCompletionAsync(prompt, requestSettings, cancellationToken)
                    .ConfigureAwait(false);
                context.Variables.Update(answer.Text);
            }
            else if (client is OpenAIChatCompletion)
            {
                var prompt = await PromptTemplate.RenderAsync(cancellationToken).ConfigureAwait(false);
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);
                var chtHistory = await client
                    .RunChatCompletionAsync(chatHistory, requestSettings, cancellationToken)
                    .ConfigureAwait(false);
                context.Variables.Update(chtHistory.Messages[1].Content);
            }
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            _logger.LogError(ex, "Semantic function {Plugin}.{Name} execution failed with error {Error}", 
                PluginName, Name, ex.Message);
            throw;
        }
    }

    public static Function FromSemanticConfig(string pluginName, string functionName, SemanticFunctionConfig functionConfig,
        ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(functionConfig);
        Verify.ParametersUniqueness(functionConfig.PromptTemplate.Parameters);

        return new SemanticFunction(functionConfig.PromptTemplate, pluginName, functionName, functionConfig.PromptTemplateConfig.Description, loggerFactory);
    }
}
