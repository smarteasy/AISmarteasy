using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AISmarteasy.Core.Connecting.OpenAI;
using AISmarteasy.Core.PluginFunction;

namespace AISmarteasy.Core.Planning;

public abstract class Worker
{
    private const string AVAILABLE_FUNCTIONS_KEY = "available_functions";
    private static readonly Regex ActionPlanRegex = new("^[^{}]*(((?'Open'{)[^{}]*)+((?'Close-Open'})[^{}]*)+)*(?(Open)(?!))", RegexOptions.Singleline | RegexOptions.Compiled);

    public PromptTemplateConfig PromptTemplateConfig { get; } = PromptTemplateConfigBuilder.Build();

    public Plan? Plan { get; set; }

    public string Goal { get; set; }
    
    public WorkerTypeKind Type { get; set; }

    protected Worker(WorkerTypeKind plannerType, string goal)
    {
        Type = plannerType;
        Goal = goal;
        Plan = null;
        //is._logger = this._kernel.LoggerFactory.CreateLogger(GetType());
    }

    public abstract Task BuildPlanAsync();

    public abstract Task<Plan?> RunPlanAsync(CancellationToken cancellationToken = default);

    public async Task BuildPlanContentAsync(WorkerTypeKind workerType, Plan plan, string goal,
        Function planningFunction,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(KernelProvider.Kernel);

        var kernel = KernelProvider.Kernel;

        var functionViews = kernel.BuildFunctionViews();

        var parameters = new Dictionary<string, string>
        {
            { AVAILABLE_FUNCTIONS_KEY, functionViews },
            { "input", goal }
        };

        await kernel.RunFunctionAsync(planningFunction, parameters).ConfigureAwait(false);

        var planXml = kernel.ContextVariablesInput.Trim();

        if (string.IsNullOrWhiteSpace(planXml))
        {
            throw new SKException(
                "Unable to create plan. No response from Function Flow function. " +
                $"\nGoal:{goal}\nFunctions:\n{planXml}");
        }

        plan.Content = planXml;

        if (workerType==WorkerTypeKind.Sequential)
        {
            try
            {
                SequentialPlanParser.ToPlanFromXml(planXml, plan);
            }
            catch (SKException e)
            {
                throw new SKException(
                    $"Unable to create plan for goal with available functions.\nGoal:{goal}\nFunctions:\n{planXml}", e);
            }

            if (plan.Steps.Count == 0)
            {
                throw new SKException($"Not possible to create plan for goal with available functions.");
            }
        }
    }

    public static ActionPlanResponse? ParsePlannerResult(string plannerResult)
    {
        Match match = ActionPlanRegex.Match(plannerResult);

        if (match.Success && match.Groups["Close"].Length > 0)
        {
            string planJson = $"{{{match.Groups["Close"]}}}";
            try
            {
                return JsonSerializer.Deserialize<ActionPlanResponse?>(planJson, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    DictionaryKeyPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    PropertyNameCaseInsensitive = true,
                });
            }
            catch (Exception e)
            {
                throw new SKException("Plan parsing error, invalid JSON", e);
            }
        }

        throw new SKException($"Failed to extract valid json string from planner result: '{plannerResult}'");
    }

    public static void SplitPluginFunctionName(string pluginFunctionName, out string pluginName, out string functionName)
    {
        var pluginFunctionNameParts = pluginFunctionName.Split('.');
        pluginName = pluginFunctionNameParts.Length > 1 ? pluginFunctionNameParts[0] : string.Empty;
        functionName = pluginFunctionNameParts.Length > 1 ? pluginFunctionNameParts[1] : pluginFunctionName;
    }
}