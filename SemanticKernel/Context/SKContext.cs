using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Connector.OpenAI.TextCompletion;

namespace SemanticKernel.Context;

public sealed class SKContext
{
    public string Result => Variables.ToString();

    public IReadOnlyCollection<ModelResult> ModelResults { get; set; } = Array.Empty<ModelResult>();

    public CultureInfo Culture { get; set; }

    public ContextVariables Variables { get; }

    public ILoggerFactory LoggerFactory { get; }

    public SKContext(ContextVariables variables)
        : this(variables, NullLoggerFactory.Instance)
    {
    }
    public SKContext(ILoggerFactory loggerFactory)
        : this(new ContextVariables(), loggerFactory)
    {
    }
    public SKContext(ContextVariables variables, ILoggerFactory loggerFactory)
    {
        Variables = variables;
        LoggerFactory = loggerFactory;
        Culture = CultureInfo.CurrentCulture;
    }

    public override string ToString()
    {
        return Result;
    }
    public SKContext Clone()
    {
        return new SKContext(
            variables: Variables.Clone())
        {
            Culture = Culture,
        };
    }
}
