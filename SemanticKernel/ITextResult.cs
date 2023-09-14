// Copyright (c) Microsoft. All rights reserved.

namespace SemanticKernel;

public interface ITextResult
{
    ModelResult ModelResult { get; }

    /// <summary>
    /// Asynchronously retrieves the text completion result.
    /// </summary>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, with the result being the completed text.</returns>
    Task<string> GetCompletionAsync(CancellationToken cancellationToken = default);
}
