using AISmarteasy.Core.Context;

namespace AISmarteasy.Core.Prompt;

public interface ITextRendering
{
    public string Render(ContextVariables? variables);
}
