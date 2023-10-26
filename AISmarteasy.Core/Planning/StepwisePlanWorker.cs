using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AISmarteasy.Core.Connecting.OpenAI.Text;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.Handling;
using AISmarteasy.Core.PluginFunction;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Planning;

public class StepwisePlanWorker : Worker
{
    private static readonly Regex FinalAnswerRegex = new(@"\[FINAL[_\s\-]?ANSWER\](?<final_answer>.+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex ThoughtRegex = new(@"(\[THOUGHT\])?(?<thought>.+?)(?=\[ACTION\]|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private const string NO_FINAL_ANSWER_FOUND_MESSAGE = "Result not found, review 'stepsTaken' to see what happened.";
    private const string TRIM_MESSAGE_FORMAT = "... I've removed the first {0} steps of my previous work to make room for the new stuff ...";

    private const string THOUGHT = "[THOUGHT]";
    private const string ACTION = "[ACTION]";
    private const string OBSERVATION = "[OBSERVATION]";

    private readonly StepwisePlannerConfig _config;
    private readonly ILogger? _logger;
    private readonly Kernel _kernel;

    private readonly PromptTemplateConfig _promptConfig;

    public StepwisePlanWorker(string goal)
        : base(WorkerTypeKind.Stepwise, goal)
    {
        Verify.NotNull(KernelProvider.Kernel);

        _kernel = KernelProvider.Kernel;
        _logger = _kernel.LoggerFactory.CreateLogger(GetType());

        _config = new();

        var stepwiseFunction = (SemanticFunction)_kernel.FindFunction("OrchestratorSkill", "StepwisePlanner")!;
        _promptConfig = stepwiseFunction.PromptTemplate.PromptConfig;
        _promptConfig.Completion.MaxTokens = _config.MaxCompletionTokens;
    }

    public override Task BuildPlanAsync()
    {
        Plan plan = new();
        plan.Parameters.Add(new ParameterView("question", Goal));
        plan.Outputs.Add("stepCount");
        plan.Outputs.Add("functionCount");
        plan.Outputs.Add("stepsTaken");
        plan.Outputs.Add("iterations");

        Plan = plan;

        return Task.CompletedTask;
    }

    public override async Task<Plan?> RunPlanAsync(CancellationToken cancellationToken = default)
    {
        var kernelContext = _kernel.Context;

        if (string.IsNullOrEmpty(Goal))
        {
            kernelContext.Variables.Update("Question not found.");
        }

        ChatHistory chatHistory = await InitializeChatHistoryAsync(cancellationToken).ConfigureAwait(false);
        ITextCompletion aiService = _kernel.TextCompletionService;
        
        if (aiService is null)
        {
            throw new SKException("No AIService available for getting completions.");
        }

        int startingMessageCount = chatHistory.Count;
        List<SystemStep> stepsTaken = new List<SystemStep>();

        SystemStep? lastStep = null;

        for (int i = 0; i < _config.MaxIterations; i++)
        {
            if (i > 0)
            {
                await Task.Delay(_config.MinIterationTimeMs, cancellationToken).ConfigureAwait(false);
            }

            var nextStep = await GetNextStepAsync().ConfigureAwait(false);
            var finalContext = TryGetFinalAnswer(nextStep, i + 1, kernelContext);
            if (finalContext is not null)
            {
                kernelContext = finalContext;
            }

            if (TryGetObservations(nextStep))
            {
                continue;
            }

            nextStep = AddNextStep(nextStep);

            if (await TryGetActionObservationAsync(nextStep).ConfigureAwait(false))
            {
                continue;
            }

            _logger?.LogInformation("Action: No action to take");

            TryGetThought(nextStep);
        }

        async Task<SystemStep> GetNextStepAsync()
        {
            var actionText = await GetNextStepCompletionAsync(stepsTaken, chatHistory, aiService, startingMessageCount, cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("Response: {ActionText}", actionText);
            return ParseResult(actionText);
        }

        SKContext? TryGetFinalAnswer(SystemStep step, int iterations, SKContext context)
        {
            if (!string.IsNullOrEmpty(step.FinalAnswer))
            {
                _logger?.LogInformation("Final Answer: {FinalAnswer}", step.FinalAnswer);
                _kernel.Context.Variables.Update(step.FinalAnswer);
                stepsTaken.Add(step);
                AddExecutionStatsToContext(stepsTaken, context, iterations);
                return context;
            }

            return null;
        }

        bool TryGetObservations(SystemStep step)
        {
            if (string.IsNullOrEmpty(step.Action) && string.IsNullOrEmpty(step.Thought))
            {
                if (!string.IsNullOrEmpty(step.Observation))
                {
                    _logger?.LogWarning("Invalid response from LLM, observation: {Observation}", step.Observation);
                    chatHistory.AddUserMessage($"{OBSERVATION} {step.Observation}");
                    stepsTaken.Add(step);
                    lastStep = step;
                    return true;
                }

                if (lastStep is not null && string.IsNullOrEmpty(lastStep.Action))
                {
                    _logger?.LogWarning("No response from LLM, expected Action");
                    chatHistory.AddUserMessage(ACTION);
                }
                else
                {
                    _logger?.LogWarning("No response from LLM, expected Thought");
                    chatHistory.AddUserMessage(THOUGHT);
                }

                return true;
            }

            return false;
        }

        SystemStep AddNextStep(SystemStep step)
        {
            if (string.IsNullOrEmpty(step.Thought) && lastStep is not null && string.IsNullOrEmpty(lastStep.Action))
            {
                lastStep.Action = step.Action;
                lastStep.ActionVariables = step.ActionVariables;

                lastStep.OriginalResponse += step.OriginalResponse;
                step = lastStep;
                if (chatHistory.Count > startingMessageCount)
                {
                    chatHistory.RemoveAt(chatHistory.Count - 1);
                }
            }
            else
            {
                _logger?.LogInformation("Thought: {Thought}", step.Thought);
                stepsTaken.Add(step);
                lastStep = step;
            }

            return step;
        }

        async Task<bool> TryGetActionObservationAsync(SystemStep step)
        {
            if (!string.IsNullOrEmpty(step.Action))
            {
                _logger?.LogInformation("Action: {Action}({ActionVariables}).", step.Action, JsonSerializer.Serialize(step.ActionVariables));

                var actionMessage = $"{ACTION} {{\"action\": \"{step.Action}\",\"action_variables\": {JsonSerializer.Serialize(step.ActionVariables)}}}";
                var message = string.IsNullOrEmpty(step.Thought) ? actionMessage : $"{THOUGHT} {step.Thought}\n{actionMessage}";

                chatHistory.AddAssistantMessage(message);

                try
                {
                    var result = await InvokeActionAsync(step.Action, step.ActionVariables).ConfigureAwait(false);
                    step.Observation = string.IsNullOrEmpty(result) ? "Got no result from action" : result;
                }
                catch (Exception ex) when (!ex.IsCriticalException())
                {
                    step.Observation = $"Error invoking action {step.Action} : {ex.Message}";
                    _logger?.LogWarning(ex, "Error invoking action {Action}", step.Action);
                }

                _logger?.LogInformation("Observation: {Observation}", step.Observation);
                chatHistory.AddUserMessage($"{OBSERVATION} {step.Observation}");

                return true;
            }

            return false;
        }

        void TryGetThought(SystemStep step)
        {
            if (!string.IsNullOrEmpty(step.Thought))
            {
                chatHistory.AddAssistantMessage($"{THOUGHT} {step.Thought}");
            }
        }

        AddExecutionStatsToContext(stepsTaken, kernelContext, _config.MaxIterations);
        kernelContext.Variables.Update(NO_FINAL_ANSWER_FOUND_MESSAGE);

        return Plan;
    }

    private async Task<string?> InvokeActionAsync(string actionName, Dictionary<string, string> actionVariables)
    {
        SplitPluginFunctionName(actionName, out var pluginName, out var functionName);
        if (string.IsNullOrEmpty(functionName))
        {
            _logger?.LogDebug("Attempt to invoke action {Action} failed", actionName);
            return $"Could not parse functionName from actionName: {actionName}. Please try again using one of the [AVAILABLE FUNCTIONS].";
        }

        var targetFunction = _kernel.FindFunction(pluginName, functionName);

        if (targetFunction == null)
        {
            _logger?.LogDebug("Attempt to invoke action {Action} failed", actionName);
            return $"{actionName} is not in [AVAILABLE FUNCTIONS]. Please try again using one of the [AVAILABLE FUNCTIONS].";
        }

        try
        {
            await _kernel.RunFunctionAsync(targetFunction, actionVariables).ConfigureAwait(false);
            var result = _kernel.ContextVariablesInput;
            _logger?.LogTrace("Invoked {FunctionName}. Result: {Result}", targetFunction.Name, result);
            return result;
        }
        catch (Exception e) when (!e.IsCriticalException())
        {
            _logger?.LogError(e, "Something went wrong in system step: {Plugin}.{Function}. Error: {Error}", targetFunction.PluginName, targetFunction.Name, e.Message);
            throw;
        }
    }

    private void AddExecutionStatsToContext(List<SystemStep> stepsTaken, SKContext context, int iterations)
    {
        context.Variables.Set("stepCount", stepsTaken.Count.ToString(CultureInfo.InvariantCulture));
        context.Variables.Set("stepsTaken", JsonSerializer.Serialize(stepsTaken));
        context.Variables.Set("iterations", iterations.ToString(CultureInfo.InvariantCulture));

        Dictionary<string, int> actionCounts = new();
        foreach (var step in stepsTaken)
        {
            if (string.IsNullOrEmpty(step.Action)) { continue; }

            _ = actionCounts.TryGetValue(step.Action, out int currentCount);
            actionCounts[step.Action] = ++currentCount;
        }

        var functionCallListWithCounts = string.Join(", ", actionCounts.Keys.Select(function =>
            $"{function}({actionCounts[function]})"));

        var functionCallCountStr = actionCounts.Values.Sum().ToString(CultureInfo.InvariantCulture);

        context.Variables.Set("functionCount", $"{functionCallCountStr} ({functionCallListWithCounts})");
    }

    protected internal virtual SystemStep ParseResult(string input)
    {
        var result = new SystemStep
        {
            OriginalResponse = input
        };

        Match finalAnswerMatch = FinalAnswerRegex.Match(input);

        if (finalAnswerMatch.Success)
        {
            result.FinalAnswer = finalAnswerMatch.Groups[1].Value.Trim();
            return result;
        }

        Match thoughtMatch = ThoughtRegex.Match(input);

        if (thoughtMatch.Success)
        {
            if (!thoughtMatch.Value.Contains(ACTION))
            {
                result.Thought = thoughtMatch.Value.Trim();
            }
        }
        else if (!input.Contains(ACTION))
        {
            result.Thought = input;
        }
        else
        {
            return result;
        }

        result.Thought = result.Thought.Replace(THOUGHT, string.Empty).Trim();

        int actionIndex = input.IndexOf(ACTION, StringComparison.OrdinalIgnoreCase);

        if (actionIndex != -1)
        {
            int jsonStartIndex = input.IndexOf("{", actionIndex, StringComparison.OrdinalIgnoreCase);
            if (jsonStartIndex != -1)
            {
                int jsonEndIndex = input.Substring(jsonStartIndex).LastIndexOf("}", StringComparison.OrdinalIgnoreCase);
                if (jsonEndIndex != -1)
                {
                    string json = input.Substring(jsonStartIndex, jsonEndIndex + 1);

                    try
                    {
                        var systemStepResults = JsonSerializer.Deserialize<SystemStep>(json);

                        if (systemStepResults is not null)
                        {
                            result.Action = systemStepResults.Action;
                            result.ActionVariables = systemStepResults.ActionVariables;
                        }
                    }
                    catch (JsonException je)
                    {
                        result.Observation = $"Action parsing error: {je.Message}\nInvalid action: {json}";
                    }
                }
            }
        }

        return result;
    }

    private async Task<ChatHistory> InitializeChatHistoryAsync(CancellationToken cancellationToken)
    {
        var descriptions = _kernel.BuildFunctionViews();
        _kernel.Context.Variables.Set("functionDescriptions", descriptions);
        _kernel.Context.Variables.Set("question", Goal);
        string userManual = await GetUserManualAsync(cancellationToken).ConfigureAwait(false);
        string userQuestion = await GetUserQuestionAsync(cancellationToken).ConfigureAwait(false);

        var context = _kernel.Context;

        context.Variables.Set("suffix", _config.Suffix);
        context.Variables.Set("functionDescriptions", userManual);
        string systemMessage = await GetSystemMessageAsync(cancellationToken).ConfigureAwait(false);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemMessage);
        chatHistory.AddUserMessage(userQuestion);

        return chatHistory;
    }

    private async Task<string> GetUserManualAsync(CancellationToken cancellationToken)
    {
        var function = _kernel.FindFunction("OrchestratorSkill", "RenderFunctionManual") as SemanticFunction;
        return await function!.PromptTemplate.RenderAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetUserQuestionAsync(CancellationToken cancellationToken)
    {
        var function = _kernel.FindFunction("OrchestratorSkill", "RenderQuestion") as SemanticFunction;
        return await function!.PromptTemplate.RenderAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetSystemMessageAsync(CancellationToken cancellationToken)
    {
        var function = _kernel.FindFunction("OrchestratorSkill", "StepwisePlanner") as SemanticFunction;
        return await function!.PromptTemplate.RenderAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task<string> GetNextStepCompletionAsync(List<SystemStep> stepsTaken, ChatHistory chatHistory, IAIService aiService, int startingMessageCount, CancellationToken cancellationToken)
    {
        var skipStart = startingMessageCount;
        var skipCount = 0;
        var lastObservationIndex = chatHistory.FindLastIndex(m => m.Content.StartsWith(OBSERVATION, StringComparison.OrdinalIgnoreCase));
        var messagesToKeep = lastObservationIndex >= 0 ? chatHistory.Count - lastObservationIndex : 0;

        string? originalThought = null;

        var tokenCount = chatHistory.GetTokenCount();
        while (tokenCount >= _config.MaxPromptTokens && chatHistory.Count > (skipStart + skipCount + messagesToKeep))
        {
            originalThought = $"{THOUGHT} {stepsTaken.FirstOrDefault()?.Thought}";
            tokenCount = chatHistory.GetTokenCount($"{originalThought}\n{string.Format(CultureInfo.InvariantCulture, TRIM_MESSAGE_FORMAT, skipCount)}", skipStart, ++skipCount);
        }

        if (tokenCount >= _config.MaxPromptTokens)
        {
            throw new SKException("ChatHistory is too long to get a completion. Try reducing the available functions.");
        }

        var reducedChatHistory = new ChatHistory();
        reducedChatHistory.AddRange(chatHistory.Where((_, i) => i < skipStart || i >= skipStart + skipCount));

        if (skipCount > 0 && originalThought is not null)
        {
            reducedChatHistory.InsertMessage(skipStart, AuthorRole.Assistant, string.Format(CultureInfo.InvariantCulture, TRIM_MESSAGE_FORMAT, skipCount));
            reducedChatHistory.InsertMessage(skipStart, AuthorRole.Assistant, originalThought);
        }

        return GetCompletionAsync(aiService, reducedChatHistory, stepsTaken.Count == 0, cancellationToken);
    }

    private async Task<string> GetCompletionAsync(IAIService service, ChatHistory chatHistory, bool addThought, CancellationToken cancellationToken)
    {
        if (service.ServiceType==AIServiceTypeKind.ChatCompletion || service.ServiceType == AIServiceTypeKind.ChatCompletionWithGpt35)
        {
            var chatCompletion = (ITextCompletion)service;
            var result = await chatCompletion.GenerateMessageAsync(chatHistory, _promptConfig.GetDefaultRequestSettings(), cancellationToken).ConfigureAwait(false);
            return result;
        }

        if (service.ServiceType==AIServiceTypeKind.TextCompletion)
        {
            var chatCompletion = (ITextCompletion)service;
            var thoughtProcess = string.Join("\n", chatHistory.Select(m => m.Content));

            if (addThought)
            {
                thoughtProcess = $"{thoughtProcess}\n{THOUGHT}";
            }

            thoughtProcess = $"{thoughtProcess}\n";
            var result = await chatCompletion.RunTextCompletionAsync(thoughtProcess, _promptConfig.GetDefaultRequestSettings(), cancellationToken).ConfigureAwait(false);

            if (result.Text == string.Empty)
            {
                throw new SKException("No completions returned.");
            }

            return result.Text;
        }

        throw new SKException("No AIService available for getting completions.");
    }
}
