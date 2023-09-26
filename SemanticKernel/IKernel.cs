using Microsoft.Extensions.Logging;
using SemanticKernel.Context;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Planner;
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


    IAIService AIService { get; }

    Task<Plan> RunPlan(string prompt);

    Task<SemanticAnswer> RunCompletion(string prompt);

    Task<SemanticAnswer> RunFunction(ISKFunction function);

    Task<SemanticAnswer> RunFunction(ISKFunction function, IDictionary<string, string> parameters);

    Task<SemanticAnswer> RunPipeline(params ISKFunction[] pipeline);

    ISKFunction CreateSemanticFunction(string pluginName, string functionName, SemanticFunctionConfig functionConfig);

    SKContext CreateNewContext(ContextVariables variables);
}
