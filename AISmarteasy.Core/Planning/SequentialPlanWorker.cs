using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.PluginFunction;

namespace AISmarteasy.Core.Planning;

public class SequentialPlanWorker : Worker
{
    public SequentialPlanWorker(string goal)
        : base(WorkerTypeKind.Sequential, goal)
    {
    }

    public override async Task BuildPlanAsync()
    {
        Verify.NotNull(KernelProvider.Kernel);

        var kernel = KernelProvider.Kernel;

        var pluginName = "OrchestratorSkill";
        var functionName = "SequentialPlanner";
        kernel.Plugins.TryGetValue(pluginName, out var plugin);
        var function = (SemanticFunction)plugin!.GetFunction(functionName);

        var plan = new Plan(function.PromptTemplate, function.PluginName, function.Name, function.Description);
        await BuildPlanContentAsync(Type, plan, Goal, function).ConfigureAwait(false);

        Plan = plan;
    }

    public override async Task<Plan?> RunPlanAsync(CancellationToken cancellationToken = default)
    {
        Verify.NotNull(KernelProvider.Kernel);

        var requestSetting = AIRequestSettings.FromCompletionConfig(PromptTemplateConfig.Completion);

        while (Plan!.HasNextStep)
        {
            await Plan.RunAsync(requestSetting, cancellationToken).ConfigureAwait(false);
        }

        return Plan;
    }
}
