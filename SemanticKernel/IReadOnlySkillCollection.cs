﻿using System.Diagnostics.CodeAnalysis;
using SemanticKernel.Function;

namespace SemanticKernel;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public interface IReadOnlySkillCollection
{
    ISKFunction GetFunction(string functionName);

    ISKFunction GetFunction(string skillName, string functionName);

    bool TryGetFunction(string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction);

    bool TryGetFunction(string skillName, string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction);

    FunctionsView GetFunctionsView(bool includeSemantic = true, bool includeNative = true);
}
