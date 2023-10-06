using SemanticKernel.Context;

namespace SemanticKernel.Prompt;

public interface ITextRendering
{
    public string Render(ContextVariables? variables);
}
