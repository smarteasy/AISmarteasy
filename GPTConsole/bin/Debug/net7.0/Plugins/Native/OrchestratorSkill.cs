using System.Text.Json.Nodes;
using AISmarteasy.Core;
using AISmarteasy.Core.Function;

namespace Plugins.Native.Skills;

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
        var answer = await kernel.RunFunction(getIntent, parameters);
        var intent = answer.Text.Trim();

        parameters = new Dictionary<string, string>
        {
            ["input"] = input
        };
        var getNumbers = kernel.FindFunction("OrchestratorSkill", "GetNumbers");
        answer = await kernel.RunFunction(getNumbers, parameters);

        var numbersJson = answer.Text;
        JsonObject numbers = JsonNode.Parse(numbersJson)!.AsObject();

        switch (intent)
        {
            case "Sqrt":
                var sqrt = kernel.FindFunction("MathSkill", "Sqrt");
                parameters = new Dictionary<string, string>
                {
                    ["number"] = numbers["number"]!.ToString()
                };
                return (await kernel.RunFunction(sqrt, parameters)).Text;
            case "Multiply":
                var multiply = kernel.FindFunction("MathPlugin", "Multiply");

                parameters = new Dictionary<string, string>
                {
                    ["first"] = numbers["first"]!.ToString(),
                    ["second"] = numbers["second"]!.ToString()
                };

                return (await kernel.RunFunction(multiply, parameters)).Text;
            default:
                return "I'm sorry, I don't understand.";
        }
    }
}