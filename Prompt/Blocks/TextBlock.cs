using Microsoft.Extensions.Logging;
using SemanticKernel.Prompt.Blocks;

namespace SemanticKernel.Prompt;

public sealed class TextBlock : Block, ITextRendering
{
    public override BlockTypeKind Type => BlockTypeKind.Text;

    public TextBlock(string? text, ILoggerFactory? loggerFactory = null)
        : base(text, loggerFactory)
    {
    }

    public TextBlock(string text, int startIndex, int stopIndex, ILoggerFactory? loggerFactory)
        : base(text.Substring(startIndex, stopIndex - startIndex), loggerFactory)
    {
    }

    public override bool IsValid(out string errorMsg)
    {
        errorMsg = "";
        return true;
    }

    public string Render(ContextVariables? variables)
    {
        return Content;
    }
}
