namespace SemanticKernel.Prompt;

public interface IPromptTemplateEngine
{
    Task<string> RenderAsync(
        string templateText,
        SKContext context,
        CancellationToken cancellationToken = default);
}
