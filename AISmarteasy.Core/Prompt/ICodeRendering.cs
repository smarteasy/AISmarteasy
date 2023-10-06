using SemanticKernel.Context;

namespace SemanticKernel.Prompt;

public interface ICodeRendering
{
    public Task<string> RenderCodeAsync(SKContext context, CancellationToken cancellationToken = default);
}
