﻿using System.Diagnostics.CodeAnalysis;

namespace SemanticKernel;


[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public interface IReadOnlySkillCollection
{
    ISKFunction GetFunction(string functionName);

    /// <summary>
    /// Gets the function stored in the collection.
    /// </summary>
    /// <param name="skillName">The name of the skill with which the function is associated.</param>
    /// <param name="functionName">The name of the function to retrieve.</param>
    /// <returns>The function retrieved from the collection.</returns>
    /// <exception cref="SKException">The specified function could not be found in the collection.</exception>
    ISKFunction GetFunction(string skillName, string functionName);

    /// <summary>
    /// Check if a function is available in the current context, and return it.
    /// </summary>
    /// <param name="functionName">The name of the function to retrieve.</param>
    /// <param name="availableFunction">When this method returns, the function that was retrieved if one with the specified name was found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the function was found; otherwise, <see langword="false"/>.</returns>
    bool TryGetFunction(string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction);

    /// <summary>
    /// Check if a function is available in the current context, and return it.
    /// </summary>
    /// <param name="skillName">The name of the skill with which the function is associated.</param>
    /// <param name="functionName">The name of the function to retrieve.</param>
    /// <param name="availableFunction">When this method returns, the function that was retrieved if one with the specified name was found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the function was found; otherwise, <see langword="false"/>.</returns>
    bool TryGetFunction(string skillName, string functionName, [NotNullWhen(true)] out ISKFunction? availableFunction);

    /// <summary>
    /// Get all registered functions details, minus the delegates
    /// </summary>
    /// <param name="includeSemantic">Whether to include semantic functions in the list</param>
    /// <param name="includeNative">Whether to include native functions in the list</param>
    /// <returns>An object containing all the functions details</returns>
    FunctionsView GetFunctionsView(bool includeSemantic = true, bool includeNative = true);
}
