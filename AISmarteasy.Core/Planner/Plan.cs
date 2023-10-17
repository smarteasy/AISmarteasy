using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Prompt;

namespace AISmarteasy.Core.Planner;

public sealed class Plan : SemanticFunction, IPlan
{
    public string Content { get; set; }
    public string Answer { get; set; }

    [JsonPropertyName("state")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables State { get; } = new();

    [JsonPropertyName("steps")] 
    public IList<Plan> Steps { get; } = new List<Plan>();

    [JsonPropertyName("outputs")] 
    public IList<string> Outputs { get; set; } = new List<string>();

    private static readonly Regex VariablesRegex = new(@"\$(?<var>\w+)");

    private const string DEFAULT_RESULT_KEY = "PLAN.RESULT";

    internal Plan(IPromptTemplate promptTemplate, string pluginName, string functionName, string description)
    :base(promptTemplate, pluginName, functionName, description)
    {
        Content = string.Empty;
        Answer = string.Empty;
    }
    public void AddSteps(Plan value)
    {
        Steps.Add(value);
    }

    [JsonIgnore] 
    public bool HasNextStep => NextStepIndex < Steps.Count;

    [JsonPropertyName("next_step_index")] public int NextStepIndex { get; private set; }

    public async Task RunAsync(AIRequestSettings requestSettings, CancellationToken cancellationToken = default)
    {
        if (HasNextStep)
        {
            var step = Steps[NextStepIndex];
            var functionVariables = GetNextStepVariables(KernelProvider.Kernel.Context.Variables, step);
            var context = new SKContext(functionVariables);
            KernelProvider.Kernel.Context = context;

            await step.InvokeAsync(requestSettings, cancellationToken).ConfigureAwait(false);

            if (Outputs.Intersect(step.Outputs).Any())
            {
                Answer = State.TryGetValue(DEFAULT_RESULT_KEY, out string? currentPlanResult)
                    ? $"{currentPlanResult}\n{context.Variables.Input}" : context.Variables.Input;
                State.Set(DEFAULT_RESULT_KEY, Answer);                
            }

            foreach (var item in step.Outputs)
            {
                State.Set(item, context.Variables.TryGetValue(item, out string? val) ? val : context.Variables.Input);
            }

            NextStepIndex++;
        }
    }

    private ContextVariables GetNextStepVariables(ContextVariables variables, Plan step)
    {
        var stepVariables = new ContextVariables();

        foreach (var variable in step.Parameters)
        {
            stepVariables.Set(variable.Name, variable.DefaultValue);
        }
        
        var input = stepVariables.Input;
        if (!string.IsNullOrEmpty(input))
        {
            input = ExpandFromVariables(variables, input);
        }
        else if (!string.IsNullOrEmpty(variables.Input))
        {
            input = variables.Input;
        }
        else if (!string.IsNullOrEmpty(State.Input))
        {
            input = State.Input;
        }
        else if (step.Steps.Count > 0)
        {
            input = string.Empty;
        }
        else if (!string.IsNullOrEmpty(Description))
        {
            input = Description;
        }

        stepVariables = new ContextVariables(input);
        KernelProvider.Kernel.Context = new SKContext(stepVariables);

        var functionParameters = step.Describe();
        foreach (var param in functionParameters.Parameters)
        {
            if (param.Name.Equals(ContextVariables.MainKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (variables.TryGetValue(param.Name, out string? value))
            {
                stepVariables.Set(param.Name, value);
            }
            else if (State.TryGetValue(param.Name, out value) && !string.IsNullOrEmpty(value))
            {
                stepVariables.Set(param.Name, value);
            }
        }

        foreach (var item in step.Parameters)
        {
            if (stepVariables.ContainsKey(item.Name))
            {
                continue;
            }

            var expandedValue = ExpandFromVariables(variables, item.DefaultValue!);
            if (!expandedValue.Equals(item.DefaultValue, StringComparison.OrdinalIgnoreCase))
            {
                stepVariables.Set(item.Name, expandedValue);
            }
            else if (variables.TryGetValue(item.Name, out string? value))
            {
                stepVariables.Set(item.Name, value);
            }
            else if (State.TryGetValue(item.Name, out value))
            {
                stepVariables.Set(item.Name, value);
            }
            else
            {
                stepVariables.Set(item.Name, expandedValue);
            }
        }

        foreach (var item in variables)
        {
            if (!stepVariables.ContainsKey(item.Key))
            {
                stepVariables.Set(item.Key, item.Value);
            }
        }

        return stepVariables;
    }

    private string ExpandFromVariables(ContextVariables variables, string input)
    {
        var result = input;
        var matches = VariablesRegex.Matches(input);
        var orderedMatches = matches.Select(m => m.Groups["var"].Value).Distinct()
            .OrderByDescending(m => m.Length);

        foreach (var varName in orderedMatches)
        {
            if (variables.TryGetValue(varName, out string? value) || this.State.TryGetValue(varName, out value))
            {
                result = result.Replace($"${varName}", value);
            }
        }

        return result;
    }

    public string ToJson(bool indented = false)
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = indented });
    }
}