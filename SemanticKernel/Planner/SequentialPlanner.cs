using SemanticKernel.Context;
using SemanticKernel.Function;
using SemanticKernel.Prompt;

namespace SemanticKernel.Planner;

public sealed class SequentialPlanner : ISequentialPlanner
{
    private const string StopSequence = "<!-- END -->";
    private const string AvailableFunctionsKey = "available_functions";

    public SequentialPlanner()
    {
        var kernel = KernelProvider.Kernel;

        //Config = config ?? new();
        //Config.ExcludedPlugins.Add(RestrictedPluginName);

        //var config = PromptTemplateConfig.FromJson(File.ReadAllText(configPath));
        var config = new SequentialPlannerConfig() { MaxTokens = 1024 };
        //var template = new PromptTemplate(File.ReadAllText(promptPath), config);

        string promptTemplate = EmbeddedResource.Read("skprompt.txt");
        //var template = new PromptTemplate(promptTemplate, config);

        //_functionFlowFunction = kernel.CreateSemanticFunction(
        //    promptTemplate: promptTemplate,
        //    pluginName: RestrictedPluginName,
        //    description: "Given a request or command or goal generate a step by step plan to " +
        //                 "fulfill the request using functions. This ability is also known as decision making and function flow",
        //    requestSettings: new AIRequestSettings()
        //    {
        //        ExtensionData = new Dictionary<string, object>()
        //        {
        //            { "Temperature", 0.0 },
        //            { "StopSequences", new[] { StopSequence } },
        //            { "MaxTokens", this.Config.MaxTokens ?? 1024 },
        //        }
        //    });

     //   _kernel = kernel;
    }
    
    private readonly ISKFunction _functionFlowFunction;

    private const string RestrictedPluginName = "SequentialPlanner_Excluded";
}
