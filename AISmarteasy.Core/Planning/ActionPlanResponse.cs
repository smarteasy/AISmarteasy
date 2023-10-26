namespace AISmarteasy.Core.Planning;

public sealed class ActionPlanResponse
{
    public sealed class PlanData
    {
        public string Rationale { get; set; } = string.Empty;

        public string Function { get; set; } = string.Empty;

        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public PlanData Plan { get; set; } = new();
}
