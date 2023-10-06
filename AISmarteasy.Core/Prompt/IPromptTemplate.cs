using AISmarteasy.Core.Context;
using AISmarteasy.Core.Function;

namespace AISmarteasy.Core.Prompt;

public interface IPromptTemplate
{
    Task<string> RenderAsync(
        SKContext context,
        CancellationToken cancellationToken = default);

    IList<ParameterView> Parameters { get; }
}
