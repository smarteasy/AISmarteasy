using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SemanticKernel;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class SKContext
{
    private CultureInfo _culture;

    public string Result => this.Variables.ToString();

    //    /// <summary>
    //    /// When a prompt is processed, aka the current data after any model results processing occurred.
    //    /// (One prompt can have multiple results).
    //    /// </summary>
    //    public IReadOnlyCollection<ModelResult> ModelResults { get; set; } = Array.Empty<ModelResult>();

    public CultureInfo Culture
    {
        get => this._culture;
        set => this._culture = value ?? CultureInfo.CurrentCulture;
    }

    public ContextVariables Variables { get; }

    public IReadOnlySkillCollection Skills { get; }

    public ILoggerFactory LoggerFactory { get; }

    public SKContext(
        ContextVariables? variables = null,
        IReadOnlySkillCollection? skills = null,
        ILoggerFactory? loggerFactory = null)
    {
        this.Variables = variables ?? new();
        this.Skills = skills ?? NullReadOnlySkillCollection.Instance;
        this.LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        this._culture = CultureInfo.CurrentCulture;
    }

    public override string ToString()
    {
        return this.Result;
    }

    public SKContext Clone()
    {
        return new SKContext(
            variables: this.Variables.Clone(),
            skills: this.Skills,
            loggerFactory: this.LoggerFactory)
        {
            Culture = this.Culture,
        };
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string display = this.Variables.DebuggerDisplay;

            if (this.Skills is IReadOnlySkillCollection skills)
            {
                var view = skills.GetFunctionsView();
                display += $", Skills = {view.NativeFunctions.Count + view.SemanticFunctions.Count}";
            }

            display += $", Culture = {this.Culture.EnglishName}";

            return display;
        }
    }
}
