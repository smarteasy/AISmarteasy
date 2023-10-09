using AISmarteasy.Core.Connector.OpenAI.TextCompletion;
using AISmarteasy.Core.Function;

namespace AISmarteasy.Core.Planner;

public sealed class PlanBuilder
{
    private const string AVAILABLE_FUNCTIONS_KEY = "available_functions";

    public async Task<Plan> Build(string goal)
    {
        if (string.IsNullOrEmpty(goal))
        {
            throw new SKException("The goal specified is empty");
        }

        var kernel = KernelProvider.Kernel;

        var pluginName = "OrchestratorSkill";
        var functionName = "SequencePlanner";
        kernel.Plugins.TryGetValue(pluginName, out var plugin);
        var function = (SemanticFunction)plugin!.GetFunction(functionName);

        var plan = new Plan(function.PromptTemplate, function.PluginName, function.Name, function.Description);
        await BuildPlanContentAsync(plan, goal, function).ConfigureAwait(false);

        return plan;
    }

    public async Task BuildPlanContentAsync(IPlan plan, string goal, ISKFunction function, CancellationToken cancellationToken = default)
    {
        var kernel = KernelProvider.Kernel;

        var functionViews = kernel.BuildFunctionViews();

        var parameters = new Dictionary<string, string>
        {
            { AVAILABLE_FUNCTIONS_KEY, functionViews },
            { "input", goal }
        };


        await kernel.RunFunctionAsync(function, parameters).ConfigureAwait(false);

        var planXml = KernelProvider.Kernel.ContextVariablesInput.Trim();

        if (string.IsNullOrWhiteSpace(planXml))
        {
            throw new SKException(
                "Unable to create plan. No response from Function Flow function. " +
                $"\nGoal:{goal}\nFunctions:\n{planXml}");
        }

        plan.Content = planXml;

        try
        {
            SequentialPlanParser.ToPlanFromXml(planXml, plan);
        }
        catch (SKException e)
        {
            throw new SKException($"Unable to create plan for goal with available functions.\nGoal:{goal}\nFunctions:\n{planXml}", e);
        }

        //    //if (plan.Steps.Count == 0)
        //    //{
        //    //    throw new SKException($"Not possible to create plan for goal with available functions.\nGoal:{goal}\nFunctions:\n{planText}");
        //    //}

        //    return plan;

        //var requestSetting = CompleteRequestSettings.FromCompletionConfig(new PromptTemplateConfig().Completion);
        //return function.InvokeAsync(requestSetting, cancellationToken);
    }
}
