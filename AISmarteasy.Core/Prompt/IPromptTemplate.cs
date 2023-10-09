using AISmarteasy.Core.Function;

namespace AISmarteasy.Core.Prompt;

public interface IPromptTemplate
{
    Task<string> RenderAsync(CancellationToken cancellationToken = default);

    IList<ParameterView> Parameters { get; }
}
