using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AISmarteasy.Core.Context;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Service;

namespace AISmarteasy.Core.Planner;

public sealed class Plan : IPlan
{
    [JsonPropertyName("state")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables State { get; } = new();

    [JsonPropertyName("steps")]
    public IReadOnlyList<Plan> Steps => _steps.AsReadOnly();

    [JsonPropertyName("parameters")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables Parameters { get; set; } = new();

    [JsonPropertyName("outputs")]
    public IList<string> Outputs { get; set; } = new List<string>();

    [JsonIgnore]
    public bool HasNextStep => NextStepIndex < Steps.Count;

    [JsonPropertyName("next_step_index")]
    public int NextStepIndex { get; private set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("plugin_name")]
    public string PluginName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public AIRequestSettings? RequestSettings { get; private set; }

    public Plan(string goal)
    {
        Name = GetRandomPlanName();
        Description = goal;
        PluginName = nameof(Plan);
    }

    public Plan(string goal, params ISKFunction[] steps) : this(goal)
    {
        AddSteps(steps);
    }

    public Plan(string goal, params Plan[] steps) : this(goal)
    {
        AddSteps(steps);
    }

    public Plan(ISKFunction function)
    {
        SetFunction(function);
    }

    [JsonConstructor]
    public Plan(
        string name,
        string pluginName,
        string description,
        int nextStepIndex,
        ContextVariables state,
        ContextVariables parameters,
        IList<string> outputs,
        IReadOnlyList<Plan> steps)
    {
        Name = name;
        PluginName = pluginName;
        Description = description;
        NextStepIndex = nextStepIndex;
        State = state;
        Parameters = parameters;
        Outputs = outputs;
        _steps.Clear();
        AddSteps(steps.ToArray());
    }

    public Task InvokeAsync(AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
    {
        var context = KernelProvider.Kernel.Context;
        AddVariablesToContext(State, context);
        return Function.InvokeAsync(requestSettings, cancellationToken);
    }






    public static Plan FromJson(string json, IPlugin? functions = null, bool requireFunctions = true)
    {
        var plan = JsonSerializer.Deserialize<Plan>(json, new JsonSerializerOptions { IncludeFields = true }) ?? new Plan(string.Empty);

        if (functions != null)
        {
            plan = SetAvailableFunctions(plan, functions, requireFunctions);
        }

        return plan;
    }

    public string ToJson(bool indented = false)
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = indented });
    }

    public void AddSteps(params Plan[] steps)
    {
        this._steps.AddRange(steps);
    }

    public void AddSteps(params ISKFunction[] steps)
    {
        this._steps.AddRange(steps.Select(step => step is Plan plan ? plan : new Plan(step)));
    }


    //public Task<Plan> RunNextStepAsync(IKernel kernel, ContextVariables variables, CancellationToken cancellationToken = default)
    //{
    //    var context = new SKContext(
    //        variables,
    //        null);

    //    //var context = new SKContext(
    //    //    variables,
    //    //    kernel.Functions);

    //    return InvokeNextStepAsync(context, cancellationToken);
    //}

    public async Task<Plan> RunNextStepAsync(CancellationToken cancellationToken = default)
    {
        if (!HasNextStep) return this;

        var step = Steps[NextStepIndex];
        var functionVariables = GetNextStepVariables(KernelProvider.Kernel.Context.Variables, step);
        var context = new SKContext(functionVariables);
        KernelProvider.Kernel.Context = context;

        await step.InvokeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (Outputs.Intersect(step.Outputs).Any())
        {
            if (State.TryGetValue(DEFAULT_RESULT_KEY, out string? currentPlanResult))
            {
                State.Set(DEFAULT_RESULT_KEY, $"{currentPlanResult}\n{context.Variables.Input}");
            }
            else
            {
                State.Set(DEFAULT_RESULT_KEY, context.Variables.Input);
            }
        }

        foreach (var item in step.Outputs)
        {
            State.Set(item, context.Variables.TryGetValue(item, out string? val) ? val : context.Variables.Input);
        }

        NextStepIndex++;

        return this;
    }

    public FunctionView Describe()
    {
        if (this.Function is not null)
        {
            return this.Function.Describe();
        }

        var stepParameters = this.Steps.SelectMany(s => s.Parameters);

        var stepDescriptions = this.Steps.SelectMany(s => s.Describe().Parameters);

        var parameters = this.Parameters.Select(p =>
        {
            var matchingParameter = stepParameters.FirstOrDefault(sp => sp.Value.Equals($"${p.Key}", StringComparison.OrdinalIgnoreCase));
            var stepDescription = stepDescriptions.FirstOrDefault(sd => sd.Name.Equals(matchingParameter.Key, StringComparison.OrdinalIgnoreCase));

            return new ParameterView(p.Key, stepDescription?.Description, stepDescription?.DefaultValue, stepDescription?.Type);
        }
        ).ToList();

        return new(Name, PluginName, Description, parameters);
    }

    public ISKFunction SetDefaultPluginCollection(IPlugin plugins)
    {
        return this;
    }







    public ISKFunction SetDefaultFunctionCollection(IPlugin functions)
    {
        return this;
        //return this.Function is not null ? Function.SetDefaultPluginCollection(functions) : this;
    }

    [Obsolete("Methods, properties and classes which include Skill in the name have been renamed. Use ISKFunction.SetDefaultFunctionCollection instead. This will be removed in a future release.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS1591
    public ISKFunction SetDefaultSkillCollection(IPlugin skills) =>
        this.SetDefaultFunctionCollection(skills);

    public ISKFunction SetAIService(Func<IAIService> serviceFactory)
    {
        return this; //Function is not null ? this.Function.SetAIService(serviceFactory) : this;
    }

    public ISKFunction SetAIConfiguration(AIRequestSettings? requestSettings)
    {
        return Function is not null ? this.Function.SetAIConfiguration(requestSettings) : this;
    }

    internal string ExpandFromVariables(ContextVariables variables, string input)
    {
        var result = input;
        var matches = VariablesRegex.Matches(input);
        var orderedMatches = matches.Cast<Match>().Select(m => m.Groups["var"].Value).Distinct().OrderByDescending(m => m.Length);

        foreach (var varName in orderedMatches)
        {
            if (variables.TryGetValue(varName, out string? value) || this.State.TryGetValue(varName, out value))
            {
                result = result.Replace($"${varName}", value);
            }
        }

        return result;
    }

    private static Plan SetAvailableFunctions(Plan plan, IPlugin plugin, bool requireFunctions = true)
    {
        if (requireFunctions && plan.Steps.Count == 0)
        {
            throw new SKException($"Function '{plan.PluginName}.{plan.Name}' not found in function collection");
        }

        foreach (var step in plan.Steps)
        {
            SetAvailableFunctions(step, plugin, requireFunctions);
        }

        return plan;
    }

    private static void AddVariablesToContext(ContextVariables vars, SKContext context)
    {
        foreach (var item in vars)
        {
            if (!context.Variables.TryGetValue(item.Key, out string? value) || string.IsNullOrEmpty(value))
            {
                context.Variables.Set(item.Key, item.Value);
            }
        }
    }

    private SKContext UpdateContextWithOutputs(SKContext context)
    {
        var resultString = this.State.TryGetValue(DEFAULT_RESULT_KEY, out string? result) ? result : this.State.ToString();
        context.Variables.Update(resultString);

        // copy previous step's variables to the next step
        foreach (var item in this._steps[this.NextStepIndex - 1].Outputs)
        {
            if (this.State.TryGetValue(item, out string? val))
            {
                context.Variables.Set(item, val);
            }
            else
            {
                context.Variables.Set(item, resultString);
            }
        }

        return context;
    }

    private ContextVariables GetNextStepVariables(ContextVariables variables, Plan step)
    {
        var input = string.Empty;
        if (!string.IsNullOrEmpty(step.Parameters.Input))
        {
            input = ExpandFromVariables(variables, step.Parameters.Input!);
        }
        else if (!string.IsNullOrEmpty(variables.Input))
        {
            input = variables.Input;
        }
        else if (!string.IsNullOrEmpty(this.State.Input))
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

        var stepVariables = new ContextVariables(input);
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
            if (stepVariables.ContainsKey(item.Key))
            {
                continue;
            }

            var expandedValue = this.ExpandFromVariables(variables, item.Value);
            if (!expandedValue.Equals(item.Value, StringComparison.OrdinalIgnoreCase))
            {
                stepVariables.Set(item.Key, expandedValue);
            }
            else if (variables.TryGetValue(item.Key, out string? value))
            {
                stepVariables.Set(item.Key, value);
            }
            else if (this.State.TryGetValue(item.Key, out value))
            {
                stepVariables.Set(item.Key, value);
            }
            else
            {
                stepVariables.Set(item.Key, expandedValue);
            }
        }

        foreach (KeyValuePair<string, string> item in variables)
        {
            if (!stepVariables.ContainsKey(item.Key))
            {
                stepVariables.Set(item.Key, item.Value);
            }
        }

        return stepVariables;
    }

    private void SetFunction(ISKFunction function)
    {
        Function = function;
        Name = function.Name;
        PluginName = function.PluginName;
        Description = function.Description;
        RequestSettings = function.RequestSettings;
    }

    private static string GetRandomPlanName() => "plan" + Guid.NewGuid().ToString("N");

    private ISKFunction Function { get; set; }

    private readonly List<Plan> _steps = new();

    private static readonly Regex VariablesRegex = new(@"\$(?<var>\w+)");

    private const string DEFAULT_RESULT_KEY = "PLAN.RESULT";

    private string DebuggerDisplay
    {
        get
        {
            string display = this.Description;

            if (!string.IsNullOrWhiteSpace(this.Name))
            {
                display = $"{this.Name} ({display})";
            }

            if (this._steps.Count > 0)
            {
                display += $", Steps = {this._steps.Count}, NextStep = {this.NextStepIndex}";
            }

            return display;
        }
    }
}
