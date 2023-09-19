﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SemanticKernel.Function;

[DebuggerDisplay("Count = 0")]
internal sealed class NullReadOnlySkillCollection : IReadOnlyPluginCollection
{
    public static readonly NullReadOnlySkillCollection Instance = new();

    /// <inheritdoc/>
    public ISKFunction GetFunction(string functionName)
    {
        return ThrowFunctionNotAvailable(functionName);
    }

    public ISKFunction GetFunction(string skillName, string functionName)
    {
        return ThrowFunctionNotAvailable(skillName, functionName);
    }

    public bool TryGetFunction(string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction)
    {
        availableFunction = null;
        return false;
    }

    public bool TryGetFunction(string skillName, string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction)
    {
        availableFunction = null;
        return false;
    }

    public FunctionsView GetFunctionsView(bool includeSemantic = true, bool includeNative = true)
    {
        return new();
    }

    private NullReadOnlySkillCollection()
    {
    }

    [DoesNotReturn]
    private static ISKFunction ThrowFunctionNotAvailable(string skillName, string functionName)
    {
        throw new SKException($"Function not available: {skillName}.{functionName}");
    }

    [DoesNotReturn]
    private static ISKFunction ThrowFunctionNotAvailable(string functionName)
    {
        throw new SKException($"Function not available: {functionName}");
    }
}
