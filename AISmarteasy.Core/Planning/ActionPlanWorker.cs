using AISmarteasy.Core.PluginFunction;

namespace AISmarteasy.Core.Planning;

public class ActionPlanWorker : Worker
{
    public ActionPlanWorker(string goal)
    : base(WorkerTypeKind.Action, goal)
    {
    }

    public override async Task BuildPlanAsync()
    {
        Verify.NotNull(KernelProvider.Kernel);

        var kernel = KernelProvider.Kernel;

        var pluginName = "OrchestratorSkill";
        var functionName = "ActionPlanner";

        kernel.Plugins.TryGetValue(pluginName, out var plugin);
        var function = (SemanticFunction)plugin!.GetFunction(functionName);

        var plan = new Plan(function.PromptTemplate, function.PluginName, function.Name, function.Description);
        await BuildPlanContentAsync(Type, plan, Goal, function).ConfigureAwait(false);

        Plan = plan;
    }

    public override async Task<Plan?> RunPlanAsync(CancellationToken cancellationToken = default)
    {
        Verify.NotNull(KernelProvider.Kernel);

        try
        {
            var kernel = KernelProvider.Kernel;
            var parsePlanResult = Worker.ParsePlannerResult(Plan!.Content);
            Worker.SplitPluginFunctionName(parsePlanResult?.Plan.Function ?? string.Empty, out string pluginName, out string functionName);
            var function = kernel.FindFunction(pluginName, functionName);
            await kernel.RunFunctionAsync(function ?? throw new InvalidOperationException(), new Dictionary<string, string>());
            Plan.Answer = kernel.ContextVariablesInput;
        }
        catch (SKException e)
        {
            Console.WriteLine(e);
            throw;
        }

        return Plan;
    }
}
