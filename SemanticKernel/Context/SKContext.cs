using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Function;

namespace SemanticKernel.Context;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class SKContext
{
    private CultureInfo _culture;

    public string Result => Variables.ToString();

    public IReadOnlyCollection<ModelResult> ModelResults { get; set; } = Array.Empty<ModelResult>();

    public CultureInfo Culture
    {
        get => _culture;
        set => _culture = value ?? CultureInfo.CurrentCulture;
    }

    public ContextVariables Variables { get; }

    public IReadOnlyPluginCollection Plugins { get; }

    public ILoggerFactory LoggerFactory { get; }

    public SKContext()
    : this(new ContextVariables(), NullReadOnlyPluginCollection.Instance, NullLoggerFactory.Instance)   
    {
    }

    public SKContext(ContextVariables variables, IReadOnlyPluginCollection plugins)
        : this(variables, plugins, NullLoggerFactory.Instance)
    {
    }
    public SKContext(IReadOnlyPluginCollection plugins, ILoggerFactory loggerFactory)
        : this(new ContextVariables(), plugins, loggerFactory)
    {
    }
    public SKContext(ContextVariables variables,
        IReadOnlyPluginCollection plugins,
        ILoggerFactory loggerFactory)
    {
        Variables = variables;
        Plugins = plugins;
        LoggerFactory = loggerFactory;
        _culture = CultureInfo.CurrentCulture;
    }

    public override string ToString()
    {
        return Result;
    }

    public SKContext Clone()
    {
        return new SKContext(
            variables: Variables.Clone(),
            plugins: Plugins,
            loggerFactory: LoggerFactory)
        {
            Culture = Culture,
        };
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string display = Variables.DebuggerDisplay;

            if (Plugins is IReadOnlyPluginCollection skills)
            {
                var view = skills.GetFunctionsView();
                display += $", Skills = {view.NativeFunctions.Count + view.SemanticFunctions.Count}";
            }

            display += $", Culture = {Culture.EnglishName}";

            return display;
        }
    }
}
