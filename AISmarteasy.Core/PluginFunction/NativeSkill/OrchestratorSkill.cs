using System.Text.Json.Nodes;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;


public class OrchestratorSkill
{
    [SKFunction]
    public async Task<string> RouteRequestAsync()
    {
        var kernel = KernelProvider.Kernel;

        var input = kernel.Context.Variables.Input;
        var parameters = new Dictionary<string, string>
        {
            ["options"] = "Sqrt, Multiply"
        };

        var getIntent = kernel.FindFunction("OrchestratorSkill", "GetIntent");
        await kernel.RunFunctionAsync(getIntent, parameters);
        var intent = KernelProvider.Kernel.ContextVariablesInput.Trim();

        parameters = new Dictionary<string, string>
        {
            ["input"] = input
        };
        var getNumbers = kernel.FindFunction("OrchestratorSkill", "GetNumbers");
        await kernel.RunFunctionAsync(getNumbers, parameters);

        var numbersJson = KernelProvider.Kernel.ContextVariablesInput;
        JsonObject numbers = JsonNode.Parse(numbersJson)!.AsObject();

        switch (intent)
        {
            case "Sqrt":
                var sqrt = kernel.FindFunction("MathSkill", "Sqrt");
                parameters = new Dictionary<string, string>
                {
                    ["number"] = numbers["number"]!.ToString()
                };
                await kernel.RunFunctionAsync(sqrt, parameters);
                return KernelProvider.Kernel.ContextVariablesInput;
            case "Multiply":
                var multiply = kernel.FindFunction("MathPlugin", "Multiply");

                parameters = new Dictionary<string, string>
                {
                    ["first"] = numbers["first"]!.ToString(),
                    ["second"] = numbers["second"]!.ToString()
                };

                await kernel.RunFunctionAsync(multiply, parameters);
                return KernelProvider.Kernel.ContextVariablesInput;
            default:
                return "I'm sorry, I don't understand.";
        }
    }
}