using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SemanticKernel.Function;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
[DebuggerTypeProxy(typeof(ReadOnlySkillCollectionTypeProxy))]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class SkillCollection : ISkillCollection
{
    internal const string GlobalSkill = "_GLOBAL_FUNCTIONS_";

    public SkillCollection(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(SkillCollection)) : NullLogger.Instance;
        _skillCollection = new(StringComparer.OrdinalIgnoreCase);
    }

    public ISkillCollection AddFunction(ISKFunction functionInstance)
    {
        Verify.NotNull(functionInstance);

        ConcurrentDictionary<string, ISKFunction> skill = this._skillCollection.GetOrAdd(functionInstance.SkillName, static _ => new(StringComparer.OrdinalIgnoreCase));
        skill[functionInstance.Name] = functionInstance;

        return this;
    }

    public ISKFunction GetFunction(string functionName) =>
        this.GetFunction(GlobalSkill, functionName);

    public ISKFunction GetFunction(string skillName, string functionName)
    {
        if (!this.TryGetFunction(skillName, functionName, out ISKFunction? functionInstance))
        {
            this.ThrowFunctionNotAvailable(skillName, functionName);
        }

        return functionInstance;
    }

    public bool TryGetFunction(string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction) =>
        this.TryGetFunction(GlobalSkill, functionName, out availableFunction);

    public bool TryGetFunction(string skillName, string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction)
    {
        Verify.NotNull(skillName);
        Verify.NotNull(functionName);

        if (this._skillCollection.TryGetValue(skillName, out ConcurrentDictionary<string, ISKFunction>? skill))
        {
            return skill.TryGetValue(functionName, out availableFunction);
        }

        availableFunction = null;
        return false;
    }

    public FunctionsView GetFunctionsView(bool includeSemantic = true, bool includeNative = true)
    {
        var result = new FunctionsView();

        if (includeSemantic || includeNative)
        {
            foreach (var skill in this._skillCollection)
            {
                foreach (KeyValuePair<string, ISKFunction> f in skill.Value)
                {
                    if (f.Value.IsSemantic ? includeSemantic : includeNative)
                    {
                        result.AddFunction(f.Value.Describe());
                    }
                }
            }
        }

        return result;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string DebuggerDisplay => $"Count = {this._skillCollection.Count}";

    [DoesNotReturn]
    private void ThrowFunctionNotAvailable(string skillName, string functionName)
    {
        this._logger.LogError("Function not available: skill:{0} function:{1}", skillName, functionName);
        throw new SKException($"Function not available {skillName}.{functionName}");
    }

    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ISKFunction>> _skillCollection;
}
