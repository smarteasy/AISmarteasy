using AISmarteasy.Core.Context;

namespace AISmarteasy.Core.Prompt;

public interface ICodeRendering
{
    public Task<string> RenderCodeAsync(SKContext context, CancellationToken cancellationToken = default);
}
