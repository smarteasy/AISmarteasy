using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Function;
using SemanticKernel.Service;

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

    public IReadOnlySkillCollection Skills { get; }

    public ILoggerFactory LoggerFactory { get; }

    public SKContext(
        ContextVariables? variables = null,
        IReadOnlySkillCollection? skills = null,
        ILoggerFactory? loggerFactory = null)
    {
        Variables = variables ?? new();
        Skills = skills ?? NullReadOnlySkillCollection.Instance;
        LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
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
            skills: Skills,
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

            if (Skills is IReadOnlySkillCollection skills)
            {
                var view = skills.GetFunctionsView();
                display += $", Skills = {view.NativeFunctions.Count + view.SemanticFunctions.Count}";
            }

            display += $", Culture = {Culture.EnglishName}";

            return display;
        }
    }
}
