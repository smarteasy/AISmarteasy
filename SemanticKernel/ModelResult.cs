﻿// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;

#pragma warning disable CA1024

namespace SemanticKernel;

public sealed class ModelResult
{
    private readonly object result;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelResult"/> class with the specified result object.
    /// </summary>
    /// <param name="result">The result object to be stored in the ModelResult instance.</param>
    public ModelResult(object result)
    {
        Verify.NotNull(result);

        this.result = result;
    }

    /// <summary>
    /// Gets the raw result object stored in the <see cref="ModelResult"/>instance.
    /// </summary>
    /// <returns>The raw result object.</returns>
    public object GetRawResult() => this.result;

    /// <summary>
    /// Gets the result object stored in the <see cref="ModelResult"/> instance, cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast the result object to.</typeparam>
    /// <returns>The result object cast to the specified type.</returns>
    /// <exception cref="InvalidCastException">Thrown when the result object cannot be cast to the specified type.</exception>
    public T GetResult<T>()
    {
        if (this.result is T typedResult)
        {
            return typedResult;
        }

        throw new InvalidCastException($"Cannot cast {this.result.GetType()} to {typeof(T)}");
    }

    /// <summary>
    /// Gets the result object stored in the ModelResult instance as a JSON element.
    /// </summary>
    /// <returns>The result object as a JSON element.</returns>
    public JsonElement GetJsonResult()
    {
        return Json.Deserialize<JsonElement>(this.result.ToJson());
    }
}
