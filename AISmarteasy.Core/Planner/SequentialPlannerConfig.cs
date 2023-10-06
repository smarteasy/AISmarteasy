namespace AISmarteasy.Core.Planner;

public sealed class SequentialPlannerConfig : PlannerConfigBase
{
    public int? MaxTokens { get; set; }

    public bool AllowMissingFunctions { get; set; } = false;
}
