﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SemanticKernel.Prompt;

public abstract class Block
{
    public virtual BlockTypeKind Type => BlockTypeKind.Undefined;

    internal virtual bool? SynchronousRendering => null;

    public string Content { get; }

    private protected ILogger Logger { get; }

    private protected Block(string? content, ILoggerFactory? loggerFactory)
    {
        this.Content = content ?? string.Empty;
        this.Logger = loggerFactory is not null ? loggerFactory.CreateLogger(this.GetType()) : NullLogger.Instance;
    }

    public abstract bool IsValid(out string errorMsg);
}
