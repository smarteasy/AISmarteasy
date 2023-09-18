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
    ILoggerFactory LoggerFactory { get; }

    ISemanticTextMemory Memory { get; }

    IPromptTemplateEngine PromptTemplateEngine { get; }

    PromptTemplateConfig PromptTemplateConfig { get; }

    IReadOnlySkillCollection Skills { get; }

    IDelegatingHandlerFactory HttpHandlerFactory { get; }
    
    ISKFunction RegisterSemanticFunction(
        string functionName,
        SemanticFunctionConfig functionConfig);

    ISKFunction RegisterSemanticFunction(
        string skillName,
        string functionName,
        SemanticFunctionConfig functionConfig);

    ISKFunction RegisterCustomFunction(ISKFunction customFunction);

    IDictionary<string, ISKFunction> ImportSkill(object skillInstance, string? skillName = null);

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

    ISKFunction Func(string skillName, string functionName);

    SKContext CreateNewContext();
    

    IAIService AIService { get; }

    Task<SemanticAnswer> RunCompletion(string prompt);
}
