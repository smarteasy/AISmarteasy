using Microsoft.Extensions.Logging;
using SemanticKernel.Context;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Prompt;
using SemanticKernel.Service;

namespace SemanticKernel;

public interface IKernel
{
    public SKContext Context { get; set; }

    ILoggerFactory LoggerFactory { get; }

    ISemanticTextMemory Memory { get; }

    IPromptTemplate PromptTemplate { get; }

    PromptTemplateConfig PromptTemplateConfig { get; }

    IReadOnlyPluginCollection Plugins { get; }

    IDelegatingHandlerFactory HttpHandlerFactory { get; }

    void RegisterSemanticFunction(
        string pluginName,
        string functionName,
        SemanticFunctionConfig functionConfig);

   void RegisterNativeFunction(ISKFunction function);

    ISKFunction RegisterCustomFunction(ISKFunction customFunction);

    void RegisterMemory(ISemanticTextMemory memory);

    Task<SKContext> RunAsync(
        ISKFunction skFunction,
        ContextVariables? variables = null,
        CancellationToken cancellationToken = default);

    Task<SKContext> RunAsync(
        params ISKFunction[] pipeline);

    Task<SKContext> RunAsync(
        string input,
        params ISKFunction[] pipeline);

    Task<SKContext> RunAsync(
        ContextVariables variables,
        params ISKFunction[] pipeline);

    Task<SKContext> RunAsync(
        CancellationToken cancellationToken,
        params ISKFunction[] pipeline);

    Task<SKContext> RunAsync(
        string input,
        CancellationToken cancellationToken,
        params ISKFunction[] pipeline);

    Task<SKContext> RunAsync(
        ContextVariables variables,
        CancellationToken cancellationToken,
        params ISKFunction[] pipeline);

    IAIService AIService { get; }

    Task<SemanticAnswer> RunCompletion(string prompt);

    Task<SemanticAnswer> RunFunction(IKernel kernel, ISKFunction function,
        IDictionary<string, string> parameters);
}
