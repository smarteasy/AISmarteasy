namespace AISmarteasy.Core.Planning;

public sealed class StepwisePlannerConfig : PlannerConfigBase
{
    public StepwisePlannerConfig()
    {
        MaxTokens = 4000;
    }

    public double MaxTokensRatio { get; set; } = 0.1;

    internal int MaxCompletionTokens => (int)(MaxTokens * MaxTokensRatio);

    internal int MaxPromptTokens => (int)(MaxTokens * (1 - MaxTokensRatio));

    public int MaxIterations { get; set; } = 10;

    public int MinIterationTimeMs { get; set; }

    public PromptTemplateConfig? PromptUserConfig { get; set; }

    public string Suffix { get; set; } = @"Let's break down the problem step by step and think about the best approach. Label steps as they are taken.

Continue the thought process!";
}
